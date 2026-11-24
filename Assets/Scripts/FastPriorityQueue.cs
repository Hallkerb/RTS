using System.Collections.Generic;

public class FastPriorityQueue<T>
{
    private List<(float cost, T element)> elements = new List<(float cost, T element)>();

    public int Count => elements.Count;

    public void Enqueue(float cost, T element)
    {
        elements.Add((cost, element));
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

    public (float cost, T element) Dequeue()
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
}
