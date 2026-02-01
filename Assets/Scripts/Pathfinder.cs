using System;
using System.Collections.Generic;
using UnityEngine;

public static class Pathfinder
{
    public static readonly Vector2Int[] Directions =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    // Átalános A* Pathfinding
    // start: Kezdő mező
    // end: Cél mező
    // getCost: Függvény ami visszaadja egy mező mozgási költségét (pl. erdő 2f, mező 1f, út 0.5f)
    // isTraversable: Függvény ami megmondja, hogy egy mező járható-e (pl. hegy nem járható)
    public static List<Vector2Int> FindPath(
        Vector2Int start,
        Vector2Int end,
        Func<Vector2Int, float> getCost,
        Func<Vector2Int, bool> isTraversable)
    {
        var openList = new PriorityQueue<Vector2Int, float>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var costSoFar = new Dictionary<Vector2Int, float>();

        openList.Enqueue(start, 0);
        costSoFar[start] = 0;

        while (openList.Count > 0)
        {
            var current = openList.Dequeue();

            if (current == end)
                return ReconstructPath(cameFrom, current);

            foreach (var dir in Directions)
            {
                var next = current + dir;

                // Megnézi, hogy járható-e
                if (!isTraversable(next)) continue;

                // Kiszámolja az új költséget
                float newCost = costSoFar[current] + getCost(next);

                // Megnézi, hogy jobb-e az új út
                if (!costSoFar.TryGetValue(next, out float oldCost) || newCost < oldCost)
                {
                    costSoFar[next] = newCost;
                    cameFrom[next] = current;

                    // Heurisztika: Manhattan távolság
                    float h = Mathf.Abs(next.x - end.x) + Mathf.Abs(next.y - end.y);
                    openList.Enqueue(next, newCost + h);
                }
            }
        }
        return null;
    }

    private static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        var path = new List<Vector2Int> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }
        path.Reverse();
        return path;
    }
}

public class PriorityQueue<TElement, TPriority> where TPriority : IComparable<TPriority>
{
    private List<(TElement Element, TPriority Priority)> elements = new List<(TElement, TPriority)>();

    public int Count => elements.Count;

    public void Enqueue(TElement element, TPriority priority)
    {
        elements.Add((element, priority));
    }

    public TElement Dequeue()
    {
        int bestIndex = 0;
        for (int i = 1; i < elements.Count; i++)
        {
            if (elements[i].Priority.CompareTo(elements[bestIndex].Priority) < 0)
                bestIndex = i;
        }

        TElement bestItem = elements[bestIndex].Element;
        elements.RemoveAt(bestIndex);
        return bestItem;
    }
}