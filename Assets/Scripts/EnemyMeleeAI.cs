using System.Collections.Generic;
using UnityEngine;

public class EnemyMeleeAI : MonoBehaviour
{
    private static readonly List<EnemyMeleeAI> ActiveEnemies = new List<EnemyMeleeAI>();
    private static readonly int SpeedParameter = Animator.StringToHash("Speed");
    private static readonly int AttackParameter = Animator.StringToHash("Attack");
    private static readonly int AttackState = Animator.StringToHash("Base Layer.Armature|Sword_Attack");
    private static readonly int IdleState = Animator.StringToHash("Base Layer.Armature|Idle_Loop");
    private static readonly int JogState = Animator.StringToHash("Base Layer.Armature|Jog_Fwd_Loop");

    [Header("Target and movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float turnSpeed = 540f;
    [SerializeField] private RuntimeAnimatorController enemyAnimatorController;
    [SerializeField] private float referenceMoveSpeed = 7.5f;
    [SerializeField] private float runAnimationSpeed = 1.2f;

    [Header("Melee attack")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField, Range(0f, 180f)] private float attackConeAngle = 120f;
    [SerializeField] private float attackWindup = 0.15f;
    [SerializeField, Range(0.05f, 0.9f)] private float attackImpactNormalizedTime = 0.35f;
    [SerializeField] private int attackDamage = 12;
    [SerializeField] private float attackCooldown = 1f;

    [Header("Enemy spacing")]
    [SerializeField] private float separationRadius = 1.25f;
    [SerializeField] private float separationStrength = 1f;

    private PlayerHealth playerHealth;
    private Animator animator;
    private CharacterController characterController;
    private float nextAttackTime;
    private float attackStartedTime;
    private float windupEndTime;
    private bool isWindingUp;
    private bool attackHitApplied;
    private bool attackAnimationStarted;
    private readonly EnemyPatrol patrol = new EnemyPatrol();

    private void OnEnable()
    {
        if (!ActiveEnemies.Contains(this))
        {
            ActiveEnemies.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveEnemies.Remove(this);
    }

    private void Start()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        animator = GetComponentInChildren<Animator>();
        characterController = GetComponent<CharacterController>();
        if (animator != null && enemyAnimatorController != null)
        {
            animator.runtimeAnimatorController = enemyAnimatorController;
            animator.applyRootMotion = false;
        }

        if (playerHealth == null)
        {
            Debug.LogError("EnemyMeleeAI precisa encontrar um PlayerHealth na cena.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (playerHealth != null && playerHealth.CurrentHealth <= 0)
        {
            isWindingUp = false;
            patrol.Tick(transform, characterController, animator, moveSpeed, turnSpeed,
                referenceMoveSpeed, runAnimationSpeed, GetSeparationDirection() * separationStrength);
            return;
        }

        if (playerHealth == null)
        {
            isWindingUp = false;
            SetMovingAnimation(false);
            return;
        }

        Vector3 toPlayer = GetFlatDirectionToPlayer();
        float distanceToPlayer = GetFlatDistanceToPlayer();
        RotateToward(toPlayer);

        if (isWindingUp)
        {
            SetMovingAnimation(false);

            // Cancel the prepared hit if the player escapes or gets behind the enemy.
            if (!attackHitApplied && !CanAttack(distanceToPlayer, toPlayer))
            {
                isWindingUp = false;
                if (animator != null)
                {
                    animator.ResetTrigger(AttackParameter);
                    animator.CrossFade(
                        distanceToPlayer > attackRange ? JogState : IdleState,
                        0.08f);
                }
            }
            else if (TryGetAttackAnimationProgress(out float attackProgress))
            {
                attackAnimationStarted = true;

                if (!attackHitApplied
                    && Time.time >= windupEndTime
                    && attackProgress >= attackImpactNormalizedTime)
                {
                    playerHealth.TakeDamage(attackDamage);
                    nextAttackTime = Time.time + attackCooldown;
                    attackHitApplied = true;
                }

                if (attackHitApplied && attackProgress >= 0.95f)
                {
                    isWindingUp = false;
                }
            }
            else if (attackAnimationStarted)
            {
                isWindingUp = false;
            }
            else if (animator == null
                || animator.runtimeAnimatorController == null
                || Time.time >= attackStartedTime + 0.5f)
            {
                if (Time.time >= windupEndTime)
                {
                    playerHealth.TakeDamage(attackDamage);
                    nextAttackTime = Time.time + attackCooldown;
                    attackHitApplied = true;
                    isWindingUp = false;
                }
            }

            return;
        }

        if (Time.time >= nextAttackTime && CanAttack(distanceToPlayer, toPlayer))
        {
            SetMovingAnimation(false);
            if (animator != null)
            {
                animator.SetTrigger(AttackParameter);
            }

            isWindingUp = true;
            attackHitApplied = false;
            attackAnimationStarted = false;
            attackStartedTime = Time.time;
            windupEndTime = Time.time + attackWindup;
            return;
        }

        SetMovingAnimation(distanceToPlayer > attackRange);
        MoveTowardPlayer(toPlayer, distanceToPlayer);
    }

    private void SetMovingAnimation(bool isMoving)
    {
        if (animator != null)
        {
            animator.SetFloat(SpeedParameter, isMoving ? 1f : 0f);
            animator.speed = isMoving
                ? runAnimationSpeed * moveSpeed / Mathf.Max(referenceMoveSpeed, 0.01f)
                : 1f;
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
        if (currentState.fullPathHash == AttackState)
        {
            normalizedTime = currentState.normalizedTime;
            return true;
        }

        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
            if (nextState.fullPathHash == AttackState)
            {
                normalizedTime = nextState.normalizedTime;
                return true;
            }
        }

        return false;
    }

    private Vector3 GetFlatDirectionToPlayer()
    {
        Vector3 direction = playerHealth.transform.position - transform.position;
        direction.y = 0f;
        return direction.normalized;
    }

    private float GetFlatDistanceToPlayer()
    {
        Vector3 offset = playerHealth.transform.position - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    private bool CanAttack(float distanceToPlayer, Vector3 directionToPlayer)
    {
        if (distanceToPlayer > attackRange || directionToPlayer.sqrMagnitude <= 0f)
        {
            return false;
        }

        float minimumFacingDot = Mathf.Cos(attackConeAngle * 0.5f * Mathf.Deg2Rad);
        return Vector3.Dot(transform.forward, directionToPlayer) >= minimumFacingDot;
    }

    private void RotateToward(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime);
    }

    private void MoveTowardPlayer(Vector3 directionToPlayer, float distanceToPlayer)
    {
        if (distanceToPlayer <= attackRange || directionToPlayer.sqrMagnitude <= 0f)
        {
            return;
        }

        Vector3 separation = GetSeparationDirection();
        Vector3 movementDirection = (directionToPlayer + separation * separationStrength).normalized;
        Vector3 movement = movementDirection * moveSpeed * Time.deltaTime;
        if (characterController != null)
        {
            characterController.Move(movement);
        }
        else
        {
            transform.position += movement;
        }
    }

    private Vector3 GetSeparationDirection()
    {
        Vector3 separation = Vector3.zero;
        int neighbors = 0;

        foreach (EnemyMeleeAI enemy in ActiveEnemies)
        {
            if (enemy == this)
            {
                continue;
            }

            Vector3 away = transform.position - enemy.transform.position;
            away.y = 0f;
            float distance = away.magnitude;
            if (distance > 0f && distance < separationRadius)
            {
                separation += away.normalized * (1f - distance / separationRadius);
                neighbors++;
            }
        }

        return neighbors > 0 ? separation / neighbors : Vector3.zero;
    }
}
