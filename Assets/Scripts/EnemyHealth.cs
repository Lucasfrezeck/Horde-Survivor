
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class EnemyHealth : MonoBehaviour
{
    private static readonly int AttackParameter = Animator.StringToHash("Attack");
    private static readonly int DeathParameter = Animator.StringToHash("Death");
    private static readonly int DeathState = Animator.StringToHash("Base Layer.Armature|Death01");

    [SerializeField, Min(0f)] private float corpseHoldDuration = 0.75f;
    [SerializeField, Min(0.1f)] private float corpseSinkDuration = 1f;
    [SerializeField, Min(0.1f)] private float corpseSinkDepth = 1.25f;
    [SerializeField, Min(0f)] private float deathAnimationFallbackDuration = 1.2f;
    [SerializeField, Min(0f)] private float corpseGroundClearance = 0.02f;

    public int maxHealth = 30;
    private int currentHealth;
    private Renderer[] corpseRenderers;
    private readonly List<Vector3> bakedCorpseVertices = new List<Vector3>();
    private Mesh corpsePoseMesh;
    private bool keepCorpseAboveGround;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => Mathf.Max(0, maxHealth);
    public event Action<int, int> HealthChanged;
    public static event Action EnemyKilled;

    private void LateUpdate()
    {
        if (keepCorpseAboveGround)
        {
            KeepCorpseAboveGround();
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created.
    private void Start()
    {
        currentHealth = MaxHealth;
        EnemyHealthBar healthBar = GetComponent<EnemyHealthBar>();
        if (healthBar == null)
        {
            healthBar = gameObject.AddComponent<EnemyHealthBar>();
        }

        healthBar.Initialize(this);
        HealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || currentHealth <= 0)
        {
            return;
        }

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        HealthChanged?.Invoke(currentHealth, MaxHealth);
        Debug.Log(gameObject.name + "Recebeu " + damage + "de dano. Vida atual: " + currentHealth);

        if (currentHealth == 0)
        {
            EnemyKilled?.Invoke();
            StartCoroutine(DeathSequence());
        }
    }

    private IEnumerator DeathSequence()
    {
        corpseRenderers = GetComponentsInChildren<Renderer>();
        keepCorpseAboveGround = true;

        EnemyMeleeAI enemyAI = GetComponent<EnemyMeleeAI>();
        if (enemyAI != null)
        {
            enemyAI.enabled = false;
        }

        CharacterController characterController = GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        Animator enemyAnimator = GetComponentInChildren<Animator>();
        bool canPlayDeathAnimation = enemyAnimator != null
            && enemyAnimator.runtimeAnimatorController != null
            && enemyAnimator.layerCount > 0
            && enemyAnimator.HasState(0, DeathState);

        if (canPlayDeathAnimation)
        {
            enemyAnimator.speed = 1f;
            // Death01 uses vertical root motion to bring the body down to the ground.
            enemyAnimator.applyRootMotion = true;
            enemyAnimator.ResetTrigger(AttackParameter);
            enemyAnimator.ResetTrigger(DeathParameter);
            enemyAnimator.SetTrigger(DeathParameter);

            bool enteredDeathState = false;
            float elapsed = 0f;
            const float animationTimeout = 3f;
            while (elapsed < animationTimeout)
            {
                AnimatorStateInfo currentState = enemyAnimator.GetCurrentAnimatorStateInfo(0);
                if (currentState.fullPathHash == DeathState)
                {
                    enteredDeathState = true;
                    if (currentState.normalizedTime >= 1f)
                    {
                        break;
                    }
                }
                else if (enemyAnimator.IsInTransition(0)
                    && enemyAnimator.GetNextAnimatorStateInfo(0).fullPathHash == DeathState)
                {
                    enteredDeathState = true;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!enteredDeathState)
            {
                yield return new WaitForSeconds(deathAnimationFallbackDuration);
            }
        }
        else
        {
            yield return new WaitForSeconds(deathAnimationFallbackDuration);
        }

        yield return new WaitForEndOfFrame();
        SettleCorpseOnGround();
        yield return new WaitForSeconds(corpseHoldDuration);
        keepCorpseAboveGround = false;
        yield return SinkCorpseAndDestroy();
    }

    private void KeepCorpseAboveGround()
    {
        if (!TryGetLowestCorpsePoint(out float lowestPoint))
        {
            return;
        }

        float penetration = GetGroundHeight(transform.position) + corpseGroundClearance - lowestPoint;
        if (penetration > 0f)
        {
            transform.position += Vector3.up * penetration;
        }
    }

    private void SettleCorpseOnGround()
    {
        if (!TryGetLowestCorpsePoint(out float lowestPoint))
        {
            return;
        }

        float groundHeight = GetGroundHeight(transform.position);
        transform.position += Vector3.up * (groundHeight + corpseGroundClearance - lowestPoint);
    }

    private bool TryGetLowestCorpsePoint(out float lowestPoint)
    {
        lowestPoint = float.PositiveInfinity;
        if (corpseRenderers == null)
        {
            return false;
        }

        foreach (Renderer corpseRenderer in corpseRenderers)
        {
            if (corpseRenderer == null || !corpseRenderer.enabled)
            {
                continue;
            }

            if (corpseRenderer is SkinnedMeshRenderer skinnedRenderer)
            {
                if (corpsePoseMesh == null)
                {
                    corpsePoseMesh = new Mesh { name = "CorpseGroundCheck" };
                }

                // Keep the baked vertices in renderer-local scale; TransformPoint then
                // applies the complete hierarchy scale exactly once in world space.
                skinnedRenderer.BakeMesh(corpsePoseMesh, true);
                bakedCorpseVertices.Clear();
                corpsePoseMesh.GetVertices(bakedCorpseVertices);
                foreach (Vector3 vertex in bakedCorpseVertices)
                {
                    float worldY = skinnedRenderer.transform.TransformPoint(vertex).y;
                    lowestPoint = Mathf.Min(lowestPoint, worldY);
                }
            }
            else
            {
                lowestPoint = Mathf.Min(lowestPoint, corpseRenderer.bounds.min.y);
            }
        }

        return !float.IsPositiveInfinity(lowestPoint);
    }

    private float GetGroundHeight(Vector3 position)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            return terrain.SampleHeight(position) + terrain.transform.position.y;
        }

        Vector3 rayOrigin = position + Vector3.up * 10f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 50f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return hit.point.y;
        }

        CharacterController characterController = GetComponent<CharacterController>();
        float halfHeight = characterController != null ? characterController.height * 0.5f : 1f;
        return transform.position.y - halfHeight;
    }

    private IEnumerator SinkCorpseAndDestroy()
    {
        Vector3 startPosition = transform.position;
        float elapsed = 0f;

        while (elapsed < corpseSinkDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / corpseSinkDuration);
            float sinkAmount = Mathf.SmoothStep(0f, corpseSinkDepth, progress);
            transform.position = startPosition + Vector3.down * sinkAmount;
            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (corpsePoseMesh != null)
        {
            Destroy(corpsePoseMesh);
        }
    }
}
