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
    private Floor floor;
    
    public List<NativeFieldData> FieldDatas { get; private set; }
    public NativeFieldClearData FieldClearData { get; private set; }
    private List<UnitsInField> Fields { get; set; } = new List<UnitsInField>();
    private Stack<int> emptyFields = new Stack<int>();
    private NativeArray<int2> offsets;

    public bool Initialized { get; private set; } = false;

    private int costToNode = 2;

    public void AddUnitInField(Unit unit, int index) => Fields[index].Units.Add(unit);

    public void RemoveUnitInField(Unit unit, int index)
    {
        Fields[index].Units.Remove(unit);

        if(Fields[index].Units.Count <= 0)
            emptyFields.Push(index);
    }

    public int FieldsCount() => Fields.Count;

    public async Task<int> CreateFieldToTarget(Unit unit, Vector2 targetPoint)
    {
        return await Task.Run(() =>
        {
            // Debug.Log("Start flow field navigation");
            // var sw0 = System.Diagnostics.Stopwatch.StartNew();
            // var sw1 = System.Diagnostics.Stopwatch.StartNew();

            NativeBitArray visitedTags = new NativeBitArray(floor.Grid.Length, Allocator.Persistent);

            int width = (int)Mathf.Sqrt(visitedTags.Length);

            Node targetNode = null;
            int fieldIndex;

            targetNode = NavigationHelper.SetTargetNode(floor, unit, targetPoint);

            // sw1.Stop();
            // Debug.Log($"Target node complete: task time({sw1.ElapsedMilliseconds}ms), all time({sw0.ElapsedMilliseconds}ms), start create field");
            // sw1.Restart();

            // Debug.Log(floor.Grid.Length);

            if (emptyFields.TryPop(out int index))
            {
                CreateField(index);
                fieldIndex = index;
            }
            else
            {
                CreateField();
                fieldIndex = Fields.Count - 1;
            }

            // Debug.Log($"fieldIndex = {fieldIndex}");

            // sw1.Stop();
            // Debug.Log($"Create field complete: task time({sw1.ElapsedMilliseconds}ms), all time({sw0.ElapsedMilliseconds}ms), start fill field");
            // sw1.Restart();

            FillField(visitedTags, width, Array.IndexOf(floor.Grid, targetNode), fieldIndex); // FillField(visitedTags, ref targetNode, fieldIndex);

            // sw1.Stop();
            // Debug.Log($"Field complete: task time({sw1.ElapsedMilliseconds}ms), all time({sw0.ElapsedMilliseconds}ms), start create vectors");
            // sw1.Restart();

            try
            {
                CreateVectors(visitedTags, width, fieldIndex);

                // sw1.Stop();
                // Debug.Log($"Vectors complete: task time({sw1.ElapsedMilliseconds}ms), all time({sw0.ElapsedMilliseconds}ms), start smooth");
                //sw1.Restart();
                
                //SmoothVectors(fieldIndex);

                // sw0.Stop();
                //sw1.Stop();

                // Debug.Log($"Smooth vectors complete: task time({sw1.ElapsedMilliseconds}ms), all time({sw0.ElapsedMilliseconds}ms)");
                // Debug.Log($"All complete: all time {sw0.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }

            visitedTags.Dispose();

            // Debug.Log($"Return field index: oll time {sw0.ElapsedMilliseconds}ms");

            return fieldIndex;
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

    private void CreateField(int index = -1)
    {
        if (index < 0)
        {
            Fields.Add(new UnitsInField());
            CreateNodesField();
        }
        else
            CreateNodesField(index);
    }

    private void CreateNodesField()
    {
        int gridLength = floor.Grid.Length;

        var costs = new NativeArray<int>(gridLength, Allocator.Persistent);
        var vectors = new NativeArray<float2>(gridLength, Allocator.Persistent);

        FieldDatas.Add(new NativeFieldData(costs, vectors));

        CreateNodesField(costs, vectors);

        /*Parallel.For(0, floor.Grid.Length, i =>
        {
            Node node = floor.Grid[i];
            node.FieldDatas.Add(new FieldData(int.MaxValue, Vector2.zero));
        });*/
    }

    private void CreateNodesField(int index)
    {
        var costs = FieldDatas[index].Cost;
        var vectors = FieldDatas[index].Vector;

        CreateNodesField(costs, vectors);
        
        /*Parallel.For(0, floor.Grid.Length, i =>
        {
            Node node = floor.Grid[i];
            node.FieldDatas[index] = new FieldData(int.MaxValue, Vector2.zero);
        });*/
    }

    private void CreateNodesField(NativeArray<int> costs, NativeArray<float2> vectors)
    {
        var job = new CreateFieldJob
        {
            Costs = costs,
            Vectors = vectors
        };

        JobHandle handle = job.Schedule(floor.Grid.Length, 64);
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

        private void UpdateNodeAndEnqueue(NativeArray<int> nodeIndex, NativeQueue<int> openSet, int tentativeFF)
        {
            FieldData.Cost[nodeIndex[0]] = tentativeFF;

            openSet.Enqueue(nodeIndex[0]);
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

    private void FillField(NativeBitArray visitedTags, int width, int targetIndex, int fieldIndex)
    {
        var fieldData = FieldDatas[fieldIndex];
        fieldData.Cost[targetIndex] = 0;
        FieldDatas[fieldIndex] = fieldData;

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

    /*private void FillField(NativeBitArray visitedTags, ref Node targetNode, int fieldIndex)
    {
        FastPriorityQueue openSet = new FastPriorityQueue();
        
        UpdateNodeAndEnqueue(targetNode, openSet, 0, fieldIndex);

        while (openSet.Count > 0)
        {
            Node currentNode = GetCurrentNode(openSet, visitedTags);

            if (currentNode == null) break;

            for (int i = 0; i < currentNode.NeighborsNode.Length; i++)
            {
                Node neighbor = currentNode.NeighborsNode[i];

                if (visitedTags.IsSet(neighbor.ID) || neighbor == null || neighbor.IsClear == false) continue;

                if (neighbor.IsSizeClear[0] == false)
                {
                    visitedTags.Set(neighbor.ID, true);
                    continue;
                }

                int tentativeFF = currentNode.FieldDatas[fieldIndex].Cost + costToNode + (int)(i / 4);

                if (tentativeFF < neighbor.FieldDatas[fieldIndex].Cost)
                    UpdateNodeAndEnqueue(neighbor, openSet, tentativeFF, fieldIndex);
            }
        }
    }

    private Node GetCurrentNode(FastPriorityQueue openSet, NativeBitArray visitedTags)
    {
        while (openSet.Count > 0)
        {
            var cn = openSet.Dequeue();

            if (visitedTags.IsSet(cn.node.ID))
                continue;

            visitedTags.Set(cn.node.ID, true);
            return cn.node;
        }

        return null;
    }

    private void UpdateNodeAndEnqueue(Node node, FastPriorityQueue openSet, int tentativeFF, int fieldIndex)
    {
        node.FieldDatas[fieldIndex] = new FieldData(tentativeFF, node.FieldDatas[fieldIndex].Vector);
        openSet.Enqueue(node, tentativeFF);
    }*/

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

    private void CreateVectors(NativeBitArray visitedTags, int width, int fieldIndex)
    {
        var job = new CreateVectorsJob
        {
            VisitedTags = visitedTags,
            IsClear = FieldClearData.IsClear,
            IsSizeClear = FieldClearData.IsSizeClear,
            Offsets = offsets,
            Width = width,
            InvWidth = 1f / width,
            Vectors = FieldDatas[fieldIndex].Vector,
            Costs = FieldDatas[fieldIndex].Cost
        };

        JobHandle handle = job.Schedule(floor.Grid.Length, 64);
        handle.Complete(); 

        /*Parallel.For(0, floor.Grid.Length, i =>
        {
            Node node = floor.Grid[i];

            if (visitedTags.IsSet(node.ID) == false) return;

            //int lovestFFNeighbors = int.MaxValue;
            Vector2 newVector = Vector2.zero;

            for (int j = 0; j < node.NeighborsNode.Length; j++)
            {
                var neighbor = node.NeighborsNode[j];

                if (neighbor == null || neighbor.IsSizeClear[0] == false) continue;

                float weight = node.FieldDatas[fieldIndex].Cost - neighbor.FieldDatas[fieldIndex].Cost;

                if (weight > 0)
                    newVector += node.VectorsToNeighbors[j] * weight;

                // if (neighbor.FieldDatas[fieldIndex].Cost < lovestFFNeighbors)
                // {
                //     newVector = neighbor.Position - node.Position;
                //     lovestFFNeighbors = neighbor.FieldDatas[fieldIndex].Cost;
                // }
            }

            node.FieldDatas[fieldIndex] = new FieldData(node.FieldDatas[fieldIndex].Cost, newVector);
        });*/
    }

    /*private void SmoothVectors(int fieldIndex)
    {
        Parallel.For(0, floor.Grid.Length, i =>
        {
            if (visitedTags[i] != globalQueryId) return;

            Node node = floor.Grid[i];
            Vector2 sum = Vector2.zero;
            int count = 0;

            for (int j = 0; j < node.NeighborsNode.Length; j++)
            {
                var neighbor = node.NeighborsNode[j];

                if (neighbor == null || neighbor.IsSizeClear[0] == false) continue;

                //sum += neighbor.FieldDatas[fieldIndex].Vector;
            }

            if (count > 0)
                node.FieldDatas[fieldIndex] = new FieldData(node.FieldDatas[fieldIndex].Cost, node.FieldDatas[fieldIndex].Vector + sum / count);
        });
    }*/

    public FlowField_Navigation(Floor floor)
    {
        this.floor = floor;

        SetOffsets();

        FieldDatas = new List<NativeFieldData>();
        FieldClearData = new NativeFieldClearData(new NativeBitArray(floor.Grid.Length, Allocator.Persistent),
                                                    new NativeBitArray(floor.Grid.Length, Allocator.Persistent));

        for(int i = 0; i < floor.Grid.Length; i++)
        {
            FieldClearData.IsClear.Set(i, floor.Grid[i].IsClear);
            FieldClearData.IsSizeClear.Set(i, floor.Grid[i].IsSizeClear[0]);
        }

        Initialized = true;
    }

    public void Dispose()
    {
        Parallel.ForEach(FieldDatas, fd => { fd.Dispose(); });
        FieldClearData.Dispose();

        if (offsets.IsCreated)
            offsets.Dispose();
    }

    /*private class FastPriorityQueue
    {
        private List<(int cost, Node node)> elements = new List<(int cost, Node node)>();

        public int Count => elements.Count;

        public void Enqueue(Node node, int cost)
        {
            elements.Add((cost, node));
            int i = elements.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (elements[i].cost >= elements[parent].cost) break;
                
                var temp = elements[i];
                elements[i] = elements[parent];
                elements[parent] = temp;
                i = parent;
            }
        }

        public (int cost, Node node) Dequeue()
        {
            var result = elements[0];
            int lastIndex = elements.Count - 1;
            elements[0] = elements[lastIndex];
            elements.RemoveAt(lastIndex);

            lastIndex--;
            int i = 0;
            while (true)
            {
                int left = i * 2 + 1;
                int right = i * 2 + 2;
                int smallest = i;

                if (left <= lastIndex && elements[left].cost < elements[smallest].cost) smallest = left;
                if (right <= lastIndex && elements[right].cost < elements[smallest].cost) smallest = right;

                if (smallest == i) break;

                var temp = elements[i];
                elements[i] = elements[smallest];
                elements[smallest] = temp;
                i = smallest;
            }
            return result;
        }

        public void Clear() => elements.Clear();
    }*/
}