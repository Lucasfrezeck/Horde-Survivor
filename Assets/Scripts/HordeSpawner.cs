using System.Collections;
using UnityEngine;

public class HordeSpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyTemplate;
    [SerializeField] private RuntimeAnimatorController impAnimatorController;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int puglinsPerSpawn = 12;
    [SerializeField] private int impsPerSpawn = 3;
    [SerializeField] private int maxAlivePuglins = 144;
    [SerializeField] private int maxAliveImps = 36;
    [SerializeField] private int maxAliveEnemies = 180;
    [Header("Horde pressure and breathing room")]
    [SerializeField, Min(0.1f)] private float reinforcementDuration = 12f;
    [SerializeField, Min(0f)] private float minimumRestDuration = 8f;
    [SerializeField, Range(0f, 1f)] private float minimumPopulationFraction = 0.55f;
    [SerializeField, Range(0f, 1f)] private float maximumPopulationFraction = 0.85f;
    [SerializeField, Range(0f, 0.95f)] private float resumePopulationFraction = 0.5f;
    [SerializeField] private float minSpawnRadius = 8f;
    [SerializeField] private float maxSpawnRadius = 12f;
    [SerializeField] private float terrainEdgeMargin = 2f;

    private float nextSpawnTime;
    private GameObject impModel;
    private Coroutine spawnRoutine;
    private HordePressureCycle pressureCycle;

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
        nextSpawnTime = Time.timeSinceLevelLoad;
        pressureCycle = new HordePressureCycle(reinforcementDuration, minimumRestDuration,
            resumePopulationFraction, ChoosePopulationTarget, Time.timeSinceLevelLoad);
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;
        if (spawnRoutine != null || Time.timeSinceLevelLoad < nextSpawnTime)
        {
            return;
        }

        nextSpawnTime = Time.timeSinceLevelLoad + spawnInterval;

        GetAliveEnemyCounts(out int alivePuglins, out int aliveImps);
        // Restore the director safely after an Editor script reload.
        pressureCycle ??= new HordePressureCycle(reinforcementDuration, minimumRestDuration,
            resumePopulationFraction, ChoosePopulationTarget, Time.timeSinceLevelLoad);
        int aliveTotal = alivePuglins + aliveImps;
        int availableTotalSlots = Mathf.Min(Mathf.Max(0, maxAliveEnemies - aliveTotal),
            pressureCycle.AvailableSlots(Time.timeSinceLevelLoad, aliveTotal));
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

        if (puglinSpawnCount + impSpawnCount > 0)
            spawnRoutine = StartCoroutine(SpawnGroup(groupAngle, puglinSpawnCount, impSpawnCount));
    }

    private IEnumerator SpawnGroup(float groupAngle, int puglinSpawnCount, int impSpawnCount)
    {
        // Spread model, Animator and health-bar creation across frames instead of cloning 18 at once.
        for (int i = 0; i < puglinSpawnCount; i++)
        {
            while (Time.timeScale <= 0f) yield return null;
            InstantiatePuglin(groupAngle);
            yield return null;
        }

        for (int i = 0; i < impSpawnCount; i++)
        {
            while (Time.timeScale <= 0f) yield return null;
            InstantiateImp(groupAngle);
            yield return null;
        }
        spawnRoutine = null;
    }

    private void OnDisable()
    {
        if (spawnRoutine != null) StopCoroutine(spawnRoutine);
        spawnRoutine = null;
    }

    private int ChoosePopulationTarget()
    {
        int limit = Mathf.Max(1, Mathf.Min(maxAliveEnemies, maxAlivePuglins + maxAliveImps));
        int lower = Mathf.Clamp(Mathf.RoundToInt(limit * minimumPopulationFraction), 1, limit);
        int upper = Mathf.Clamp(Mathf.RoundToInt(limit * maximumPopulationFraction), lower, limit);
        return Random.Range(lower, upper + 1);
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
        if (!TryGetRandomGroupSpawnPosition(groupAngle, out Vector3 spawnPosition)) return;
        GameObject enemy = Instantiate(enemyTemplate, spawnPosition, Quaternion.identity);
        enemy.name = "Puglin";
        enemy.SetActive(true);
    }

    private void InstantiateImp(float groupAngle)
    {
        if (!TryGetRandomGroupSpawnPosition(groupAngle, out Vector3 spawnPosition)) return;
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
        health.experienceReward = 2;
        health.healthDropChance = 0.03f;

        EnemyRangedAI rangedAI = imp.AddComponent<EnemyRangedAI>();
        rangedAI.Initialize(impAnimatorController);
    }

    private bool TryGetRandomGroupSpawnPosition(float groupAngle, out Vector3 spawnPosition)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            float angle = attempt < 10 ? groupAngle + Random.Range(-25f, 25f) : Random.Range(0f, 360f);
            Vector3 candidate = GetSpawnPosition(angle, Random.Range(minSpawnRadius, maxSpawnRadius));
            if (EnemyNavigation.TrySpawn(candidate, transform.position, out spawnPosition))
            {
                Vector3 offset = spawnPosition - transform.position;
                offset.y = 0f;
                if (offset.magnitude >= minSpawnRadius - 0.5f && offset.magnitude <= maxSpawnRadius + 0.5f)
                    return true;
            }
        }
        spawnPosition = default;
        return false;
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
