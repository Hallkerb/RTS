using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System;
using Unity.Jobs;
using Unity.Burst;
using System.ComponentModel;
using Unity.Collections;
using Unity.Mathematics;

public class UnitsInField
{
    public HashSet<Unit> Units { get; private set; } = new HashSet<Unit>(); 
}

public struct NativeFieldData
{
    public NativeArray<int> Cost;
    public NativeArray<float2> Vector;

    public void Dispose()
    {
        if (Cost.IsCreated)
            Cost.Dispose();

        if (Vector.IsCreated)
            Vector.Dispose();
    }

    public NativeFieldData(NativeArray<int> cost, NativeArray<float2> vector)
    {
        Cost = cost;
        Vector = vector;
    }
}

public struct NativeFieldClearData
{
    public NativeBitArray IsClear;
    public NativeBitArray IsSizeClear;

    public void Dispose()
    {
        if (IsClear.IsCreated)
            IsClear.Dispose();

        if (IsSizeClear.IsCreated)
            IsSizeClear.Dispose();
    }

    public NativeFieldClearData(NativeBitArray isClear, NativeBitArray isSizeClear)
    {
        IsClear = isClear;
        IsSizeClear = isSizeClear;
    }
}

public class FlowField_Navigation
{
    private Grid grid;
    
    public List<NativeFieldData> FieldDatas { get; private set; }
    public NativeFieldClearData FieldClearData { get; private set; }
    private List<UnitsInField> Fields { get; set; } = new List<UnitsInField>();
    private Stack<int> emptyFields = new Stack<int>();
    private NativeArray<int2> offsets;

    private readonly object fieldsLock = new object();
    private readonly object clearLock = new object();

    public bool Initialized { get; private set; } = false;

    private int costToNode = 2;

    public void AddUnitInField(Unit unit, int index)
    {
        lock (fieldsLock)
        {
            Fields[index].Units.Add(unit);
        }
    }

    public void RemoveUnitInField(Unit unit, int index)
    {
        lock (fieldsLock)
        {
            if (Fields[index].Units.Remove(unit) && Fields[index].Units.Count == 0)
                emptyFields.Push(index);
        }
    }

    public int FieldsCount() => Fields.Count;

    public async Task<int> CreateFieldToTarget(Unit unit, Vector2 targetPoint)
    {
        return await Task.Run(() =>
        {
            // Debug.Log("Start flow field navigation");
            // var sw0 = System.Diagnostics.Stopwatch.StartNew();
            // var sw1 = System.Diagnostics.Stopwatch.StartNew();

            NativeBitArray visitedTags = new NativeBitArray(grid.Nodes.Length, Allocator.Persistent);

            try
            {
                int width = (int)Mathf.Sqrt(visitedTags.Length);

                int fieldIndex;
                NativeFieldData fieldData;

                Node targetNode = NavigationHelper.SetTargetNode(grid, targetPoint);

                // sw1.Stop();
                // Debug.Log($"Target node complete: task time({sw1.ElapsedMilliseconds}ms), all time({sw0.ElapsedMilliseconds}ms), start create field");
                // sw1.Restart();

                // Debug.Log(floor.Grid.Length);

                lock (fieldsLock)
                {
                    if (!emptyFields.TryPop(out fieldIndex))
                    {
                        fieldIndex = Fields.Count;
                        Fields.Add(new UnitsInField());

                        int len = grid.Nodes.Length;
                        FieldDatas.Add(new NativeFieldData(
                            new NativeArray<int>(len, Allocator.Persistent),
                            new NativeArray<float2>(len, Allocator.Persistent)));
                    }

                    fieldData = FieldDatas[fieldIndex];
                }

                CreateNodesField(fieldData.Cost, fieldData.Vector);

                // Debug.Log($"fieldIndex = {fieldIndex}");

                // sw1.Stop();
                // Debug.Log($"Create field complete: task time({sw1.ElapsedMilliseconds}ms), all time({sw0.ElapsedMilliseconds}ms), start fill field");
                // sw1.Restart();

                FillField(visitedTags, width, targetNode.ID, fieldData); // FillField(visitedTags, ref targetNode, fieldIndex);

                // sw1.Stop();
                // Debug.Log($"Field complete: task time({sw1.ElapsedMilliseconds}ms), all time({sw0.ElapsedMilliseconds}ms), start create vectors");
                // sw1.Restart();

                CreateVectors(visitedTags, width, fieldData);

                // sw1.Stop();
                // Debug.Log($"Vectors complete: task time({sw1.ElapsedMilliseconds}ms), all time({sw0.ElapsedMilliseconds}ms), start smooth");
                //sw1.Restart();
                
                //SmoothVectors(fieldIndex);

                // sw0.Stop();
                //sw1.Stop();

                // Debug.Log($"Smooth vectors complete: task time({sw1.ElapsedMilliseconds}ms), all time({sw0.ElapsedMilliseconds}ms)");
                // Debug.Log($"All complete: all time {sw0.ElapsedMilliseconds}ms");

                // Debug.Log($"Return field index: oll time {sw0.ElapsedMilliseconds}ms");

                return fieldIndex;
            }
            finally
            {
                visitedTags.Dispose();
            }
        });
    }

    [BurstCompile]
    private struct CreateFieldJob : IJobParallelFor
    {
        public NativeArray<int> Costs;
        public NativeArray<float2> Vectors;

        public void Execute(int index)
        {
            Costs[index] = int.MaxValue;
            Vectors[index] = float2.zero;
        }
    }

    private void CreateNodesField(NativeArray<int> costs, NativeArray<float2> vectors)
    {
        var job = new CreateFieldJob
        {
            Costs = costs,
            Vectors = vectors
        };

        JobHandle handle = job.Schedule(grid.Nodes.Length, 64);
        handle.Complete();
    }

    [BurstCompile]
    private struct FillFieldJob : IJob
    {
        public NativeBitArray VisitedTags;
        public NativeFieldData FieldData;

        [Unity.Collections.ReadOnly]
        public int CostToNode;

        [Unity.Collections.ReadOnly]
        public NativeBitArray IsClear;

        [Unity.Collections.ReadOnly]
        public NativeBitArray IsSizeClear;

        [Unity.Collections.ReadOnly]
        public NativeArray<int2> Offsets;

        [Unity.Collections.ReadOnly]
        public int Width;

        [Unity.Collections.ReadOnly]
        public float InvWidth;

        public NativeQueue<int> OpenSet;
        
        public void Execute()
        {
            while (OpenSet.Count > 0)
            {
                int currentIndex = GetCurrentIndex(OpenSet, VisitedTags);

                if (currentIndex < 0) break;

                int y = (int)(currentIndex * InvWidth + 0.0001f);
                int x = currentIndex - (y * Width);
                
                int2 currentCoord = new int2(x, y);

                for (int i = 0; i < Offsets.Length; i++)
                {
                    int2 neighborCoord = currentCoord + Offsets[i];
                
                    if (neighborCoord.x < 0 || neighborCoord.x >= Width || 
                        neighborCoord.y < 0 || neighborCoord.y >= Width) continue;

                    int neighborIndex = neighborCoord.y * Width + neighborCoord.x;

                    if (VisitedTags.IsSet(neighborIndex) || IsClear.IsSet(neighborIndex) == false) continue;

                    if (IsSizeClear.IsSet(neighborIndex) == false)
                    {
                        VisitedTags.Set(neighborIndex, true);
                        continue;
                    }

                    int tentativeFF = FieldData.Cost[currentIndex] + CostToNode + (int)(i / 4);

                    if (tentativeFF < FieldData.Cost[neighborIndex])
                        UpdateNodeAndEnqueue(neighborIndex, OpenSet, tentativeFF);
                }
            }
        }

        private void UpdateNodeAndEnqueue(int nodeIndex, NativeQueue<int> openSet, int tentativeFF)
        {
            FieldData.Cost[nodeIndex] = tentativeFF;

            openSet.Enqueue(nodeIndex);
        }

        private int GetCurrentIndex(NativeQueue<int> openSet, NativeBitArray visitedTags)
        {
            while (openSet.Count > 0)
            {
                int index = openSet.Dequeue();

                if (visitedTags.IsSet(index))
                    continue;

                visitedTags.Set(index, true);
                return index;
            }

            return -1;
        }
    }

    private void FillField(NativeBitArray visitedTags, int width, int targetIndex, NativeFieldData fieldData)
    {
        fieldData.Cost[targetIndex] = 0;

        NativeQueue<int> queue = new NativeQueue<int>(Allocator.Persistent);
        queue.Enqueue(targetIndex);

        var job = new FillFieldJob
        {
            VisitedTags = visitedTags,
            FieldData = fieldData,
            CostToNode = costToNode,
            IsClear = FieldClearData.IsClear,
            IsSizeClear = FieldClearData.IsSizeClear,
            OpenSet = queue,
            Offsets = offsets,
            Width = width,
            InvWidth = 1f / width
        };

        JobHandle handle = job.Schedule();
        handle.Complete(); 

        queue.Dispose();
    }

    private void SetOffsets()
    {
        offsets = new NativeArray<int2>(8, Allocator.Persistent);
        offsets[0] = new int2(0, 1);
        offsets[1] = new int2(0, -1);
        offsets[2] = new int2(1, 0);
        offsets[3] = new int2(-1, 0);
        offsets[4] = new int2(1, 1);
        offsets[5] = new int2(-1, 1);
        offsets[6] = new int2(1, -1);
        offsets[7] = new int2(-1, -1);
    }

    [BurstCompile]
    private struct CreateVectorsJob : IJobParallelFor
    {
        [Unity.Collections.ReadOnly]
        public NativeBitArray VisitedTags;

        [Unity.Collections.ReadOnly]
        public NativeBitArray IsClear;

        [Unity.Collections.ReadOnly]
        public NativeBitArray IsSizeClear;

        [Unity.Collections.ReadOnly]
        public NativeArray<int2> Offsets;

        [Unity.Collections.ReadOnly]
        public int Width;

        [Unity.Collections.ReadOnly]
        public float InvWidth;

        [Unity.Collections.ReadOnly]
        public NativeArray<int> Costs;

        [WriteOnly]
        public NativeArray<float2> Vectors;

        public void Execute(int index)
        {
            if (VisitedTags.IsSet(index) == false) return;

            int y = (int)(index * InvWidth + 0.0001f);
            int x = index - (y * Width);
            
            int2 currentCoord = new int2(x, y);
            float2 newVector = float2.zero;

            int currentCost = Costs[index];

            for (int i = 0; i < Offsets.Length; i++)
            {
                var offset = Offsets[i];
                int2 neighborCoord = currentCoord + offset;
                
                if (neighborCoord.x < 0 || neighborCoord.x >= Width || 
                    neighborCoord.y < 0 || neighborCoord.y >= Width) continue;

                int neighborIndex = neighborCoord.y * Width + neighborCoord.x;

                if (IsSizeClear.IsSet(neighborIndex) == false) continue;

                float weight = currentCost - Costs[neighborIndex];

                if (weight > 0)
                    newVector += (float2)offset * weight;
            }

            Vectors[index] = newVector;
        }
    }

    private void CreateVectors(NativeBitArray visitedTags, int width, NativeFieldData fieldData)
    {
        var job = new CreateVectorsJob
        {
            VisitedTags = visitedTags,
            IsClear = FieldClearData.IsClear,
            IsSizeClear = FieldClearData.IsSizeClear,
            Offsets = offsets,
            Width = width,
            InvWidth = 1f / width,
            Vectors = fieldData.Vector,
            Costs = fieldData.Cost
        };

        JobHandle handle = job.Schedule(grid.Nodes.Length, 64);
        handle.Complete(); 
    }

    public void SyncNode(Node node)
    {
        lock (clearLock)
        {
            FieldClearData.IsClear.Set(node.ID, node.IsClear);
            FieldClearData.IsSizeClear.Set(node.ID, node.IsSizeClear[0]);
        }
    }

    public FlowField_Navigation(Grid grid)
    {
        this.grid = grid;

        SetOffsets();

        FieldDatas = new List<NativeFieldData>();
        FieldClearData = new NativeFieldClearData(new NativeBitArray(grid.Nodes.Length, Allocator.Persistent),
                                                    new NativeBitArray(grid.Nodes.Length, Allocator.Persistent));

        for(int i = 0; i < grid.Nodes.Length; i++)
        {
            FieldClearData.IsClear.Set(i, grid.Nodes[i].IsClear);
            FieldClearData.IsSizeClear.Set(i, grid.Nodes[i].IsSizeClear[0]);
        }

        Initialized = true;
    }

    public void Dispose()
    {
        foreach (var fd in FieldDatas)
            fd.Dispose();

        FieldClearData.Dispose();

        if (offsets.IsCreated)
            offsets.Dispose();
    }
}