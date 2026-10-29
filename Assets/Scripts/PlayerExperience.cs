using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(PlayerHealth), typeof(PlayerStats))]
public class PlayerExperience : MonoBehaviour
{
    [SerializeField, Min(0.5f)] private float baseCollectionRadius = 3f;
    [SerializeField, Min(1)] private int maximumCrystals = 256;
    private const int MaximumUpgradeRank = 5;
    private const int HealUpgrade = 6;
    private const float CrystalBottomExtent = 0.7f;
    private const float CrystalFootprintRadius = 0.7f;
    private const float BobAmplitude = 0.07f;
    private const float GroundClearance = 0.06f;
    private static readonly string[] Titles = { "POWER", "QUICK STRIKES", "LONG REACH", "VITALITY", "SWIFTNESS", "MAGNETISM", "HEAL" };
    private static readonly string[] Icons = { "DMG", "ATK", "RNG", "HP", "MOV", "XP", "+HP" };
    private sealed class Orb
    {
        public Transform transform;
        public Vector3 ground;
        public Vector3 surface;
        public int value;
        public bool attracted;
        public float phase;
    }
    private readonly List<Orb> orbs = new List<Orb>();
    private readonly Stack<Orb> pool = new Stack<Orb>();
    private readonly int[] ranks = new int[7];
    private readonly ExperienceProgress progress = new ExperienceProgress();
    private PlayerHealth health;
    private PlayerStats stats;
    private ExperienceUI ui;
    private Transform orbRoot;
    private Mesh crystalMesh;
    private Material crystalMaterial;
    private bool ownsMaterial;
    private int baseDamage, baseHealth;
    private float baseAttackSpeed, baseRange, baseMovement;
    private float collectionRadius;
    private bool choosing;
    private float previousTimeScale;
    private int[] offered;

    private void Start()
    {
        health = GetComponent<PlayerHealth>();
        stats = GetComponent<PlayerStats>();
        baseDamage = stats.attackDmg;
        baseHealth = stats.maxHealth;
        baseAttackSpeed = stats.attackSpeed;
        baseRange = stats.attackRange;
        baseMovement = stats.moveSpeed;
        collectionRadius = baseCollectionRadius;
        orbRoot = new GameObject("Experience Crystals").transform;
        crystalMesh = CreateCrystalMesh();
        crystalMaterial = Resources.Load<Material>("ExperienceCrystal");
        if (crystalMaterial == null)
        {
            crystalMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            crystalMaterial.SetColor("_BaseColor", new Color(0.14f, 0.85f, 0.9f));
            ownsMaterial = true;
        }
        ui = new ExperienceUI();
        ui.Refresh(progress);
        EnemyHealth.EnemyDefeated += DropExperience;
        health.Died += OnDeath;
    }

    private void DropExperience(EnemyHealth enemy)
    {
        if (health == null || health.CurrentHealth <= 0 || progress.IsMaxLevel || enemy.experienceReward <= 0) return;
        Vector3 point = enemy.transform.position;
        Terrain terrain = Terrain.activeTerrain;
        point.y = terrain != null ? terrain.SampleHeight(point) + terrain.transform.position.y : point.y - 1f;
        Orb closest = null;
        float closestDistance = float.PositiveInfinity;
        foreach (Orb existing in orbs)
        {
            float distance = (existing.surface - point).sqrMagnitude;
            if (distance < closestDistance) { closest = existing; closestDistance = distance; }
        }
        if (closest != null && (closestDistance < 1f || orbs.Count >= maximumCrystals))
        {
            closest.value += enemy.experienceReward;
            ScaleOrb(closest);
            return;
        }
        Orb orb;
        if (pool.Count > 0) orb = pool.Pop();
        else
        {
            GameObject crystal = new GameObject("XP Crystal", typeof(MeshFilter), typeof(MeshRenderer));
            crystal.transform.SetParent(orbRoot, false);
            crystal.GetComponent<MeshFilter>().sharedMesh = crystalMesh;
            MeshRenderer renderer = crystal.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = crystalMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            orb = new Orb { transform = crystal.transform };
        }
        orb.value = enemy.experienceReward;
        orb.surface = point;
        orb.attracted = false;
        orb.phase = Random.Range(0f, Mathf.PI * 2f);
        orb.transform.position = point;
        ScaleOrb(orb);
        orb.transform.gameObject.SetActive(true);
        orbs.Add(orb);
    }

    private static void ScaleOrb(Orb orb)
    {
        orb.transform.localScale = Vector3.one * Mathf.Min(0.5f, 0.24f + Mathf.Log(orb.value + 1f) * 0.035f);
        // Merged crystals grow. Recompute their resting height using the new size,
        // leaving enough clearance for the lowest point of the floating animation.
        orb.ground = orb.surface;
        orb.ground.y = MinimumCrystalHeight(orb, orb.surface) + BobAmplitude;
        if (!orb.attracted) orb.transform.position = orb.ground;
    }

    private static float MinimumCrystalHeight(Orb orb, Vector3 position)
    {
        Terrain terrain = Terrain.activeTerrain;
        float surface = orb.surface.y;
        float scale = orb.transform.localScale.y;
        if (terrain != null)
        {
            surface = float.NegativeInfinity;
            float radius = CrystalFootprintRadius * scale;
            // Sample the footprint, not only the center, so slopes cannot cover an edge.
            for (int z = -1; z <= 1; z++)
            for (int x = -1; x <= 1; x++)
            {
                Vector3 sample = position + new Vector3(x * radius, 0f, z * radius);
                surface = Mathf.Max(surface, terrain.SampleHeight(sample) + terrain.transform.position.y);
            }
        }
        return surface + CrystalBottomExtent * scale + GroundClearance;
    }

    private void Update()
    {
        if (health == null || health.CurrentHealth <= 0 || choosing || Time.timeScale <= 0f) return;
        Vector3 target = transform.position + Vector3.up * 0.15f;
        for (int i = orbs.Count - 1; i >= 0; i--)
        {
            Orb orb = orbs[i];
            Vector3 offset = orb.transform.position - target;
            offset.y = 0f;
            if (offset.sqrMagnitude <= collectionRadius * collectionRadius) orb.attracted = true;
            if (orb.attracted)
            {
                Vector3 position = Vector3.MoveTowards(orb.transform.position, target, 12f * Time.deltaTime);
                position.y = Mathf.Max(position.y, MinimumCrystalHeight(orb, position));
                orb.transform.position = position;
                if ((orb.transform.position - target).sqrMagnitude < 0.5f)
                {
                    int value = orb.value;
                    Recycle(i);
                    progress.Add(value);
                    ui.Refresh(progress);
                    if (progress.IsMaxLevel) ClearOrbs();
                    if (progress.PendingChoices > 0)
                    {
                        choosing = true;
                        previousTimeScale = Time.timeScale;
                        Time.timeScale = 0f;
                        ShowChoices();
                        break;
                    }
                    if (progress.IsMaxLevel) break;
                }
            }
            else orb.transform.position = orb.ground + Vector3.up * Mathf.Sin(Time.time * 2f + orb.phase) * BobAmplitude;
            orb.transform.rotation = Quaternion.Euler(0f, Time.time * 45f + orb.phase * Mathf.Rad2Deg, 0f);
        }
    }

    private void ShowChoices()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        List<int> eligible = new List<int>();
        for (int i = 0; i < ranks.Length; i++)
        {
            if (i == HealUpgrade ? health.CurrentHealth < health.MaxHealth : ranks[i] < MaximumUpgradeRank)
                eligible.Add(i);
        }
        offered = new int[Mathf.Min(3, eligible.Count)];
        string[] titles = new string[offered.Length], descriptions = new string[offered.Length], icons = new string[offered.Length];
        int[] levels = new int[offered.Length];
        for (int i = 0; i < offered.Length; i++)
        {
            int selected = Random.Range(i, eligible.Count);
            int swap = eligible[i]; eligible[i] = eligible[selected]; eligible[selected] = swap;
            int upgrade = offered[i] = eligible[i];
            titles[i] = Titles[upgrade];
            descriptions[i] = Description(upgrade);
            icons[i] = Icons[upgrade];
            levels[i] = upgrade == HealUpgrade ? -1 : ranks[upgrade];
        }
        ui.Choices(progress.Level - progress.PendingChoices + 1, titles, descriptions, icons, levels, Choose);
    }

    private string Description(int upgrade)
    {
        int next = ranks[upgrade] + 1;
        switch (upgrade)
        {
            case 0: return "Punch damage\n" + stats.attackDmg + " → " + Mathf.RoundToInt(baseDamage * (1f + 0.2f * next));
            case 1: return "Attacks per second\n" + stats.attackSpeed.ToString("0.00") + " → " + (baseAttackSpeed * (1f + 0.15f * next)).ToString("0.00");
            case 2: return "Punch range\n" + stats.attackRange.ToString("0.0") + " → " + (baseRange * (1f + 0.1f * next)).ToString("0.0") + " units";
            case 3: int bonus = Mathf.Max(1, Mathf.RoundToInt(baseHealth * 0.2f));
                string currentHealthEffect = health.CurrentHealth == health.MaxHealth
                    ? "Current HP: " + health.CurrentHealth + " → " + (health.CurrentHealth + bonus)
                    : "Current HP unchanged";
                return "Maximum health\n" + stats.maxHealth + " → " + (stats.maxHealth + bonus) + " HP\n" + currentHealthEffect;
            case 4: return "Movement speed\n" + stats.moveSpeed.ToString("0.0") + " → " + (baseMovement * (1f + 0.1f * next)).ToString("0.0") + " units/s";
            case 5: return "XP collection radius\n" + collectionRadius.ToString("0.0") + " → " + (baseCollectionRadius * (1f + 0.25f * next)).ToString("0.0") + " units";
            default: return "Restore 50 HP\n" + health.CurrentHealth + " → " + Mathf.Min(health.MaxHealth, health.CurrentHealth + 50) + " HP";
        }
    }

    private void Choose(int index)
    {
        if (!choosing || health.CurrentHealth <= 0 || index < 0 || index >= offered.Length) return;
        int upgrade = offered[index];
        if ((upgrade != HealUpgrade && ranks[upgrade] >= MaximumUpgradeRank) || !progress.ConsumeChoice()) return;
        int rank = upgrade == HealUpgrade ? 0 : ++ranks[upgrade];
        switch (upgrade)
        {
            case 0: stats.attackDmg = Mathf.RoundToInt(baseDamage * (1f + 0.2f * rank)); break;
            case 1: stats.attackSpeed = baseAttackSpeed * (1f + 0.15f * rank); break;
            case 2: stats.attackRange = baseRange * (1f + 0.1f * rank); break;
            case 3: health.IncreaseMaximumHealth(Mathf.Max(1, Mathf.RoundToInt(baseHealth * 0.2f))); break;
            case 4: stats.moveSpeed = baseMovement * (1f + 0.1f * rank); break;
            case 5: collectionRadius = baseCollectionRadius * (1f + 0.25f * rank); break;
            case HealUpgrade: health.Heal(50); break;
        }
        ui.Refresh(progress);
        if (progress.PendingChoices > 0) ShowChoices();
        else
        {
            ui.CloseChoices();
            choosing = false;
            Time.timeScale = previousTimeScale;
        }
    }

    private void Recycle(int index)
    {
        Orb orb = orbs[index];
        orb.transform.gameObject.SetActive(false);
        pool.Push(orb);
        orbs.RemoveAt(index);
    }
    private void ClearOrbs() { for (int i = orbs.Count - 1; i >= 0; i--) Recycle(i); }
    private void OnDeath()
    {
        if (choosing) Time.timeScale = previousTimeScale;
        choosing = false;
        ui?.Hide();
    }
    private void OnDestroy()
    {
        EnemyHealth.EnemyDefeated -= DropExperience;
        if (health != null) health.Died -= OnDeath;
        if (choosing) Time.timeScale = previousTimeScale;
        ui?.Dispose();
        if (orbRoot != null) Destroy(orbRoot.gameObject);
        if (crystalMesh != null) Destroy(crystalMesh);
        if (ownsMaterial && crystalMaterial != null) Destroy(crystalMaterial);
    }

    private static Mesh CreateCrystalMesh()
    {
        Vector3[] points = { Vector3.up * 1.4f, Vector3.down * 0.7f, Vector3.right * 0.7f,
            Vector3.forward * 0.7f, Vector3.left * 0.7f, Vector3.back * 0.7f };
        List<Vector3> vertices = new List<Vector3>();
        for (int i = 0; i < 4; i++)
        {
            int a = 2 + i, b = 2 + (i + 1) % 4;
            vertices.Add(points[0]); vertices.Add(points[b]); vertices.Add(points[a]);
            vertices.Add(points[1]); vertices.Add(points[a]); vertices.Add(points[b]);
        }
        int[] indices = new int[vertices.Count];
        for (int i = 0; i < indices.Length; i++) indices[i] = i;
        Mesh mesh = new Mesh { name = "Experience Crystal" };
        mesh.SetVertices(vertices);
        mesh.triangles = indices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
