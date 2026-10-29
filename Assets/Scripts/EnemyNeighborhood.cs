using System.Collections.Generic;
using UnityEngine;

// One spatial snapshot per frame, shared by all enemies of the same type.
public sealed class EnemyNeighborhood
{
    private const float CellSize = 2f;
    private struct Entry { public Transform actor; public Vector3 position; }
    private readonly Dictionary<Vector2Int, List<Entry>> cells = new Dictionary<Vector2Int, List<Entry>>();
    private readonly List<List<Entry>> buffers = new List<List<Entry>>();
    private int frame = -1;

    public Vector3 Direction<T>(Transform actor, float radius, List<T> enemies) where T : Component
    {
        if (radius <= 0f) return Vector3.zero;
        if (frame != Time.frameCount)
        {
            frame = Time.frameCount;
            cells.Clear();
            int usedBuffers = 0;
            foreach (T enemy in enemies)
            {
                if (enemy == null) continue;
                Transform member = enemy.transform;
                Vector3 position = member.position;
                Vector2Int cell = Cell(position);
                if (!cells.TryGetValue(cell, out List<Entry> bucket))
                {
                    if (usedBuffers == buffers.Count) buffers.Add(new List<Entry>());
                    bucket = buffers[usedBuffers++];
                    bucket.Clear();
                    cells.Add(cell, bucket);
                }
                bucket.Add(new Entry { actor = member, position = position });
            }
        }
        Vector3 origin = actor.position;
        Vector2Int center = Cell(origin);
        int range = Mathf.CeilToInt(radius / CellSize);
        float radiusSquared = radius * radius;
        Vector3 separation = Vector3.zero;
        int neighbors = 0;
        for (int z = -range; z <= range; z++)
        for (int x = -range; x <= range; x++)
        {
            if (!cells.TryGetValue(center + new Vector2Int(x, z), out List<Entry> bucket)) continue;
            foreach (Entry entry in bucket)
            {
                if (entry.actor == actor) continue;
                Vector3 away = origin - entry.position;
                away.y = 0f;
                float squared = away.sqrMagnitude;
                if (squared <= 0f || squared >= radiusSquared) continue;
                float distance = Mathf.Sqrt(squared);
                separation += away / distance * (1f - distance / radius);
                neighbors++;
            }
        }
        return neighbors > 0 ? separation / neighbors : Vector3.zero;
    }

    private static Vector2Int Cell(Vector3 position) => new Vector2Int(
        Mathf.FloorToInt(position.x / CellSize), Mathf.FloorToInt(position.z / CellSize));
}
