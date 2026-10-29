using UnityEngine;

public class PlayerAutoAttack : MonoBehaviour
{
    private static readonly int AttackParameter = Animator.StringToHash("Attack");
    private static readonly int AttackVariantParameter = Animator.StringToHash("AttackVariant");
    private static readonly int PunchCrossState = Animator.StringToHash("Base Layer.Armature|Punch_Cross");
    private static readonly int PunchJabState = Animator.StringToHash("Base Layer.Armature|Punch_Jab");

    [SerializeField] private Transform attackOrigin;
    [SerializeField, Range(0.05f, 0.9f)] private float punchCrossImpactTime = 0.4f;
    [SerializeField, Range(0.05f, 0.9f)] private float punchJabImpactTime = 0.3f;

    private PlayerStats playerStats;
    private Animator animator;
    private float nextAttackTime;
    private float attackStartedTime;
    private bool isAttackInProgress;
    private bool attackHitApplied;
    private bool attackAnimationStarted;
    private int nextAttackVariant;
    private int currentAttackVariant;

    private void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        animator = GetComponentInChildren<Animator>();
        nextAttackVariant = Random.Range(0, 2);

        if (playerStats == null)
        {
            Debug.LogError("PlayerAutoAttack precisa de um componente PlayerStats no mesmo objeto.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (playerStats == null || playerStats.attackSpeed <= 0f)
        {
            return;
        }

        if (isAttackInProgress)
        {
            UpdateAttack();
            return;
        }

        if (Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + 1f / playerStats.attackSpeed;
        if (!HasEnemyInCone(transform.forward))
        {
            return;
        }

        BeginAttack();
    }

    private void BeginAttack()
    {
        attackStartedTime = Time.time;
        attackHitApplied = false;
        attackAnimationStarted = false;
        isAttackInProgress = true;
        currentAttackVariant = nextAttackVariant;

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetInteger(AttackVariantParameter, currentAttackVariant);
            nextAttackVariant = 1 - nextAttackVariant;
            animator.SetTrigger(AttackParameter);
        }
        else
        {
            ApplyConeDamage(transform.forward);
            attackHitApplied = true;
            isAttackInProgress = false;
        }
    }

    private void UpdateAttack()
    {
        if (TryGetAttackAnimationProgress(out float attackProgress))
        {
            attackAnimationStarted = true;

            // Aplica o dano no ponto de impacto da animação, e não no início do movimento.
            float impactTime = currentAttackVariant == 0
                ? punchCrossImpactTime
                : punchJabImpactTime;
            if (!attackHitApplied && attackProgress >= impactTime)
            {
                ApplyConeDamage(transform.forward);
                attackHitApplied = true;
            }

            if (attackHitApplied && attackProgress >= 0.95f)
            {
                isAttackInProgress = false;
            }

            return;
        }

        if (attackAnimationStarted)
        {
            isAttackInProgress = false;
            return;
        }

        if (animator == null
            || animator.runtimeAnimatorController == null
            || Time.time >= attackStartedTime + 0.5f)
        {
            ApplyConeDamage(transform.forward);
            attackHitApplied = true;
            isAttackInProgress = false;
        }
    }

    private bool TryGetAttackAnimationProgress(out float normalizedTime)
    {
        normalizedTime = 0f;
        if (animator == null)
        {
            return false;
        }

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        if (IsPunchState(currentState.fullPathHash))
        {
            normalizedTime = currentState.normalizedTime;
            return true;
        }

        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
            if (IsPunchState(nextState.fullPathHash))
            {
                normalizedTime = nextState.normalizedTime;
                return true;
            }
        }

        return false;
    }

    private static bool IsPunchState(int stateHash)
    {
        return stateHash == PunchCrossState || stateHash == PunchJabState;
    }

    private bool HasEnemyInCone(Vector3 forward)
    {
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        Vector3 flatForward = GetFlatDirection(forward);
        float minimumFacingDot = Mathf.Cos(playerStats.attackConeAngle * 0.5f * Mathf.Deg2Rad);
        float attackRangeSquared = playerStats.attackRange * playerStats.attackRange;
        Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position;

        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy == null || enemy.CurrentHealth <= 0)
            {
                continue;
            }

            Vector3 offset = enemy.transform.position - origin;
            offset.y = 0f;
            if (offset.sqrMagnitude <= attackRangeSquared
                && offset.sqrMagnitude > 0f
                && Vector3.Dot(flatForward, offset.normalized) >= minimumFacingDot)
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyConeDamage(Vector3 forward)
    {
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        Vector3 flatForward = GetFlatDirection(forward);
        float minimumFacingDot = Mathf.Cos(playerStats.attackConeAngle * 0.5f * Mathf.Deg2Rad);
        float attackRangeSquared = playerStats.attackRange * playerStats.attackRange;
        Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position;

        // O golpe atinge todos os inimigos que estiverem dentro do alcance e do cone frontal.
        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy == null || enemy.CurrentHealth <= 0)
            {
                continue;
            }

            Vector3 offset = enemy.transform.position - origin;
            offset.y = 0f;
            if (offset.sqrMagnitude <= attackRangeSquared
                && offset.sqrMagnitude > 0f
                && Vector3.Dot(flatForward, offset.normalized) >= minimumFacingDot)
            {
                enemy.TakeDamage(playerStats.attackDmg);
            }
        }
    }

    private static Vector3 GetFlatDirection(Vector3 direction)
    {
        direction.y = 0f;
        return direction.sqrMagnitude > 0f ? direction.normalized : Vector3.zero;
    }
}
