using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Builds the same authored layout on every run without modifying the original terrain asset.
[DefaultExecutionOrder(-1000)]
[RequireComponent(typeof(Terrain), typeof(TerrainCollider))]
public class DesertArena : MonoBehaviour
{
    [SerializeField, Min(1f)] private float areaMultiplier = 6f;
    [SerializeField] private int seed = 5371;
    [SerializeField] private Material sandstoneMaterial;
    private Terrain terrain;
    private TerrainData originalData;
    private TerrainData runtimeData;
    private GameObject scenery;
    private readonly List<Mesh> meshes = new List<Mesh>();
    private readonly List<Material> materials = new List<Material>();
    private readonly List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();
    private NavMeshData navigationData;
    private NavMeshDataInstance navigation;
    private System.Random random;

    private void Awake()
    {
        terrain = GetComponent<Terrain>();
        originalData = terrain.terrainData;
        if (originalData == null) return;
        random = new System.Random(seed);
        runtimeData = Instantiate(originalData);
        runtimeData.name = "Expanded Desert";
        Vector3 size = originalData.size;
        Vector3 center = transform.position + size * 0.5f;
        float scale = Mathf.Sqrt(areaMultiplier);
        size = new Vector3(size.x * scale, Mathf.Max(size.y, 8f), size.z * scale);
        runtimeData.size = size;
        transform.position = new Vector3(center.x - size.x * 0.5f, transform.position.y, center.z - size.z * 0.5f);
        int resolution = runtimeData.heightmapResolution;
        float[,] heights = new float[resolution, resolution];
        for (int z = 0; z < resolution; z++)
        for (int x = 0; x < resolution; x++)
        {
            float wx = transform.position.x + x / (resolution - 1f) * size.x;
            float wz = transform.position.z + z / (resolution - 1f) * size.z;
            // Leave a flat clearing around the original player start and soften all slopes.
            float clearing = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(9f, 20f, new Vector2(wx, wz).magnitude));
            float dunes = Mathf.PerlinNoise(wx * 0.045f + 41f, wz * 0.045f + 17f) * 2.4f;
            heights[z, x] = dunes * clearing / size.y;
        }
        runtimeData.SetHeights(0, 0, heights);
        // TerrainLayer.tileSize is already in world units: reuse it so the sand is not stretched.
        terrain.terrainData = runtimeData;
        GetComponent<TerrainCollider>().terrainData = runtimeData;
        terrain.Flush();
        scenery = new GameObject("Desert Rock Formations");
        scenery.transform.SetParent(transform, false);
        CreateMaterials();
        sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Terrain,
            sourceObject = runtimeData, transform = transform.localToWorldMatrix, area = 0 });

        Vector2[] formations = { new Vector2(-0.25f, -0.22f), new Vector2(0.23f, -0.18f),
            new Vector2(-0.27f, 0.20f), new Vector2(0.25f, 0.24f),
            new Vector2(-0.04f, 0.33f), new Vector2(0.34f, 0.02f) };
        foreach (Vector2 formation in formations)
        {
            Vector3 point = new Vector3(center.x + formation.x * size.x, 0f, center.z + formation.y * size.z);
            Quaternion orientation = Quaternion.Euler(0f, Range(0f, 180f), 0f);
            for (int i = 0; i < 4; i++)
                Rock(point + orientation * new Vector3(i * 1.8f - 2.7f, 0f, Range(-1f, 1f)),
                    new Vector3(Range(1.4f, 2.5f), Range(2.2f, 4.3f), Range(1.5f, 2.5f)), true);
        }
        // A continuous collider boundary is concealed by the surrounding rock rim.
        for (int side = 0; side < 4; side++)
        {
            bool alongX = side < 2;
            float length = alongX ? size.x : size.z;
            int count = Mathf.CeilToInt(length / 4f);
            for (int i = 0; i <= count; i++)
            {
                float t = i / (float)count;
                Vector3 point = alongX
                    ? new Vector3(transform.position.x + t * size.x, 0f, transform.position.z + (side == 0 ? 1.5f : size.z - 1.5f))
                    : new Vector3(transform.position.x + (side == 2 ? 1.5f : size.x - 1.5f), 0f, transform.position.z + t * size.z);
                Rock(point, new Vector3(Range(2.6f, 3.6f), Range(4f, 6.5f), Range(2.6f, 3.6f)), true);
            }
            GameObject boundary = new GameObject("Arena Boundary");
            boundary.transform.SetParent(scenery.transform, false);
            Vector3 position = alongX
                ? new Vector3(center.x, transform.position.y + 5f, transform.position.z + (side == 0 ? 1f : size.z - 1f))
                : new Vector3(transform.position.x + (side == 2 ? 1f : size.x - 1f), transform.position.y + 5f, center.z);
            boundary.transform.position = position;
            BoxCollider wall = boundary.AddComponent<BoxCollider>();
            wall.size = alongX ? new Vector3(size.x, 12f, 1f) : new Vector3(1f, 12f, size.z);
            sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, size = wall.size,
                transform = boundary.transform.localToWorldMatrix, area = 1 });
        }
        for (int i = 0; i < 120; i++)
        {
            Vector3 point = new Vector3(Range(transform.position.x + 5f, transform.position.x + size.x - 5f), 0f,
                Range(transform.position.z + 5f, transform.position.z + size.z - 5f));
            if (new Vector2(point.x, point.z).magnitude < 8f) continue;
            float radius = Range(0.15f, 0.55f);
            Rock(point, new Vector3(radius, radius * Range(0.5f, 0.9f), radius), false);
        }
        Physics.SyncTransforms();
        NavMeshBuildSettings settings = NavMesh.GetSettingsByIndex(0);
        settings.agentRadius = 0.65f;
        settings.agentHeight = 2f;
        settings.agentSlope = 35f;
        settings.agentClimb = 0.3f;
        settings.overrideVoxelSize = true;
        settings.voxelSize = 0.15f;
        navigationData = NavMeshBuilder.BuildNavMeshData(settings, sources,
            new Bounds(new Vector3(center.x, transform.position.y + 5f, center.z), new Vector3(size.x + 4f, 30f, size.z + 4f)),
            Vector3.zero, Quaternion.identity);
        if (navigationData != null) navigation = NavMesh.AddNavMeshData(navigationData);
        else Debug.LogError("Could not build navigation for the desert arena.", this);
    }

    private float Range(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());

    private void CreateMaterials()
    {
        Color[] colors = { new Color(0.44f, 0.31f, 0.20f), new Color(0.57f, 0.42f, 0.28f), new Color(0.65f, 0.51f, 0.35f) };
        foreach (Color color in colors)
        {
            Material material = sandstoneMaterial != null ? new Material(sandstoneMaterial)
                : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name = "Desert Sandstone";
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.05f);
            material.enableInstancing = true;
            materials.Add(material);
        }
    }

    private void Rock(Vector3 point, Vector3 size, bool solid)
    {
        point.y = terrain.SampleHeight(point) + transform.position.y - (solid ? 0.12f : 0.025f);
        GameObject rock = new GameObject(solid ? "Sandstone Rock" : "Decorative Stone");
        rock.transform.SetParent(scenery.transform, false);
        rock.transform.SetPositionAndRotation(point, Quaternion.Euler(0f, Range(0f, 360f), 0f));
        rock.transform.localScale = size;
        Mesh mesh = RockMesh();
        meshes.Add(mesh);
        rock.AddComponent<MeshFilter>().sharedMesh = mesh;
        rock.AddComponent<MeshRenderer>().sharedMaterial = materials[random.Next(materials.Count)];
        if (!solid) return;
        rock.AddComponent<MeshCollider>().sharedMesh = mesh;
        sources.Add(new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Mesh, sourceObject = mesh,
            transform = rock.transform.localToWorldMatrix, area = 1 });
    }

    private Mesh RockMesh()
    {
        const int sides = 7;
        Vector3[] ring = new Vector3[sides * 3];
        for (int level = 0; level < 3; level++)
        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2f / sides;
            float radius = (level == 2 ? 0.5f : 1f) * Range(0.8f, 1.1f);
            ring[level * sides + i] = new Vector3(Mathf.Cos(angle) * radius,
                level == 0 ? 0f : (level == 1 ? Range(0.45f, 0.65f) : Range(0.9f, 1.05f)), Mathf.Sin(angle) * radius);
        }
        List<Vector3> vertices = new List<Vector3>();
        void Triangle(Vector3 a, Vector3 b, Vector3 c) { vertices.Add(a); vertices.Add(b); vertices.Add(c); }
        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides;
            for (int level = 0; level < 2; level++)
            {
                int start = level * sides;
                Triangle(ring[start + i], ring[start + sides + i], ring[start + next]);
                Triangle(ring[start + next], ring[start + sides + i], ring[start + sides + next]);
            }
            Triangle(Vector3.zero, ring[i], ring[next]);
            Triangle(Vector3.up, ring[2 * sides + next], ring[2 * sides + i]);
        }
        int[] indices = new int[vertices.Count];
        for (int i = 0; i < indices.Length; i++) indices[i] = i;
        Mesh mesh = new Mesh { name = "Low Poly Sandstone" };
        mesh.SetVertices(vertices);
        mesh.triangles = indices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void OnDestroy()
    {
        if (navigation.valid) navigation.Remove();
        if (terrain != null && originalData != null)
        {
            terrain.terrainData = originalData;
            TerrainCollider terrainCollider = GetComponent<TerrainCollider>();
            if (terrainCollider != null) terrainCollider.terrainData = originalData;
        }
        Release(navigationData);
        Release(runtimeData);
        foreach (Mesh mesh in meshes) Release(mesh);
        foreach (Material material in materials) Release(material);
    }

    private static void Release(Object value)
    {
        if (value == null) return;
        if (Application.isPlaying) Destroy(value);
        else DestroyImmediate(value);
    }
}
