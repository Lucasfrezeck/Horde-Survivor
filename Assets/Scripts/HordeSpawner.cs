using UnityEngine;

public class HordeSpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyTemplate;
    [SerializeField] private float hordeDuration = 60f;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int enemiesPerSpawn = 2;
    [SerializeField] private int maxAliveEnemies = 24;
    [SerializeField] private float minSpawnRadius = 8f;
    [SerializeField] private float maxSpawnRadius = 12f;
    [SerializeField] private float terrainEdgeMargin = 2f;

    private float hordeStartTime;
    private float nextSpawnTime;

    private void Awake()
    {
        if (enemyTemplate == null)
        {
            Debug.LogError("HordeSpawner precisa de um modelo de inimigo.", this);
            enabled = false;
            return;
        }

        enemyTemplate.SetActive(false);
    }

    private void Start()
    {
        hordeStartTime = Time.timeSinceLevelLoad;
        nextSpawnTime = hordeStartTime;
    }

    private void Update()
    {
        float elapsedTime = Time.timeSinceLevelLoad - hordeStartTime;
        if (elapsedTime >= hordeDuration || Time.timeSinceLevelLoad < nextSpawnTime)
        {
            return;
        }

        nextSpawnTime = Time.timeSinceLevelLoad + spawnInterval;

        int aliveEnemies = FindObjectsByType<EnemyMeleeAI>(FindObjectsSortMode.None).Length;
        int availableSlots = maxAliveEnemies - aliveEnemies;
        int spawnCount = Mathf.Min(enemiesPerSpawn, availableSlots);
        if (spawnCount <= 0)
        {
            return;
        }

        float groupAngle = Random.Range(0f, 360f);
        for (int i = 0; i < spawnCount; i++)
        {
            float angle = groupAngle + Random.Range(-25f, 25f);
            float radius = Random.Range(minSpawnRadius, maxSpawnRadius);
            Vector3 spawnPosition = GetSpawnPosition(angle, radius);

            GameObject enemy = Instantiate(enemyTemplate, spawnPosition, Quaternion.identity);
            enemy.name = "Puglin";
            enemy.SetActive(true);
        }
    }

    private Vector3 GetSpawnPosition(float angleDegrees, float radius)
    {
        float angleRadians = angleDegrees * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Sin(angleRadians), 0f, Mathf.Cos(angleRadians)) * radius;
        Vector3 spawnPosition = transform.position + offset;

        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null || terrain.terrainData == null)
        {
            spawnPosition.y = transform.position.y;
            return spawnPosition;
        }

        Vector3 terrainOrigin = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;
        spawnPosition.x = Mathf.Clamp(
            spawnPosition.x,
            terrainOrigin.x + terrainEdgeMargin,
            terrainOrigin.x + terrainSize.x - terrainEdgeMargin);
        spawnPosition.z = Mathf.Clamp(
            spawnPosition.z,
            terrainOrigin.z + terrainEdgeMargin,
            terrainOrigin.z + terrainSize.z - terrainEdgeMargin);
        spawnPosition.y = terrain.SampleHeight(spawnPosition) + terrainOrigin.y + 1f;
        return spawnPosition;
    }
}
