using UnityEngine;

public class HordeSpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyTemplate;
    [SerializeField] private RuntimeAnimatorController impAnimatorController;
    [SerializeField] private float hordeDuration = 60f;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int puglinsPerSpawn = 2;
    [SerializeField] private int impsPerSpawn = 1;
    [SerializeField] private int maxAlivePuglins = 24;
    [SerializeField] private int maxAliveImps = 12;
    [SerializeField] private int maxAliveEnemies = 36;
    [SerializeField] private float minSpawnRadius = 8f;
    [SerializeField] private float maxSpawnRadius = 12f;
    [SerializeField] private float terrainEdgeMargin = 2f;

    private float hordeStartTime;
    private float nextSpawnTime;
    private GameObject impModel;

    private void Awake()
    {
        impModel = Resources.Load<GameObject>("ImpModel");

        if (enemyTemplate == null)
        {
            Debug.LogError("HordeSpawner precisa de um modelo de inimigo.", this);
            enabled = false;
            return;
        }

        if (impModel == null || impAnimatorController == null)
        {
            Debug.LogError("HordeSpawner não encontrou Assets/Resources/ImpModel.prefab ou o Animator Controller do Imp.", this);
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

        GetAliveEnemyCounts(out int alivePuglins, out int aliveImps);
        int availableTotalSlots = Mathf.Max(0, maxAliveEnemies - alivePuglins - aliveImps);
        if (availableTotalSlots <= 0)
        {
            return;
        }

        float groupAngle = Random.Range(0f, 360f);
        int puglinSpawnCount = Mathf.Min(
            Mathf.Max(0, puglinsPerSpawn),
            Mathf.Min(Mathf.Max(0, maxAlivePuglins - alivePuglins), availableTotalSlots));
        availableTotalSlots -= puglinSpawnCount;

        int impSpawnCount = Mathf.Min(
            Mathf.Max(0, impsPerSpawn),
            Mathf.Min(Mathf.Max(0, maxAliveImps - aliveImps), availableTotalSlots));

        for (int i = 0; i < puglinSpawnCount; i++)
        {
            InstantiatePuglin(groupAngle);
        }

        for (int i = 0; i < impSpawnCount; i++)
        {
            InstantiateImp(groupAngle);
        }
    }

    private static void GetAliveEnemyCounts(out int puglinCount, out int impCount)
    {
        puglinCount = 0;
        impCount = 0;

        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy == null || enemy.CurrentHealth <= 0)
            {
                continue;
            }

            if (enemy.GetComponent<EnemyMeleeAI>() != null)
            {
                puglinCount++;
            }
            else if (enemy.GetComponent<EnemyRangedAI>() != null)
            {
                impCount++;
            }
        }
    }

    private void InstantiatePuglin(float groupAngle)
    {
        Vector3 spawnPosition = GetRandomGroupSpawnPosition(groupAngle);
        GameObject enemy = Instantiate(enemyTemplate, spawnPosition, Quaternion.identity);
        enemy.name = "Puglin";
        enemy.SetActive(true);
    }

    private void InstantiateImp(float groupAngle)
    {
        Vector3 spawnPosition = GetRandomGroupSpawnPosition(groupAngle);
        GameObject imp = new GameObject("Imp");
        imp.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);

        GameObject model = Instantiate(impModel, imp.transform, false);
        model.name = "ImpModel";
        model.transform.localPosition = Vector3.down;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one * 2f;

        CharacterController characterController = imp.AddComponent<CharacterController>();
        characterController.height = 2f;
        characterController.radius = 0.5f;
        characterController.center = Vector3.zero;
        characterController.stepOffset = 0.3f;

        EnemyHealth health = imp.AddComponent<EnemyHealth>();
        health.maxHealth = 60;

        EnemyRangedAI rangedAI = imp.AddComponent<EnemyRangedAI>();
        rangedAI.Initialize(impAnimatorController);
    }

    private Vector3 GetRandomGroupSpawnPosition(float groupAngle)
    {
        float angle = groupAngle + Random.Range(-25f, 25f);
        float radius = Random.Range(minSpawnRadius, maxSpawnRadius);
        return GetSpawnPosition(angle, radius);
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
