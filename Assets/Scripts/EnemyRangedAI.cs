using System.Collections.Generic;
using UnityEngine;

public class EnemyRangedAI : MonoBehaviour
{
    private static readonly List<EnemyRangedAI> ActiveEnemies = new List<EnemyRangedAI>();
    private static readonly int SpeedParameter = Animator.StringToHash("Speed");
    private static readonly int AttackParameter = Animator.StringToHash("Attack");
    private static readonly int SpellState = Animator.StringToHash("Base Layer.Armature|Spell_Simple_Shoot");

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float turnSpeed = 540f;
    [SerializeField] private float referenceMoveSpeed = 7.5f;
    [SerializeField] private float runAnimationSpeed = 1.2f;

    [Header("Spell attack")]
    [SerializeField] private float attackRange = 12f;
    [SerializeField] private int attackDamage = 15;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField, Range(0.05f, 0.9f)] private float projectileImpactNormalizedTime = 0.4f;
    [SerializeField] private float projectileSpawnHeight = 1.5f;
    [SerializeField] private float projectileSpawnForwardOffset = 0.75f;
    [SerializeField] private float castAnimationFallbackDelay = 0.5f;

    [Header("Enemy spacing")]
    [SerializeField] private float separationRadius = 1.25f;
    [SerializeField] private float separationStrength = 1f;

    private PlayerHealth playerHealth;
    private Animator animator;
    private CharacterController characterController;
    private RuntimeAnimatorController animatorController;
    private float nextAttackTime;
    private float castStartedTime;
    private bool isCasting;
    private bool projectileSpawned;
    private bool spellAnimationStarted;

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

    public void Initialize(RuntimeAnimatorController controller)
    {
        animatorController = controller;
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator != null && animatorController != null)
        {
            animator.runtimeAnimatorController = animatorController;
            animator.applyRootMotion = false;
        }
    }

    private void Start()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        animator = GetComponentInChildren<Animator>();
        characterController = GetComponent<CharacterController>();

        if (animator != null && animatorController != null)
        {
            animator.runtimeAnimatorController = animatorController;
            animator.applyRootMotion = false;
        }

        if (playerHealth == null)
        {
            Debug.LogError("EnemyRangedAI precisa encontrar um PlayerHealth na cena.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (playerHealth == null || playerHealth.CurrentHealth <= 0)
        {
            isCasting = false;
            SetMovingAnimation(false);
            return;
        }

        Vector3 directionToPlayer = GetFlatDirectionToPlayer();
        float distanceToPlayer = GetFlatDistanceToPlayer();
        RotateToward(directionToPlayer);

        if (isCasting)
        {
            UpdateCast();
            return;
        }

        if (distanceToPlayer > attackRange)
        {
            SetMovingAnimation(true);
            MoveTowardPlayer(directionToPlayer);
            return;
        }

        SetMovingAnimation(false);
        if (Time.time >= nextAttackTime)
        {
            BeginCast();
        }
    }

    private void BeginCast()
    {
        isCasting = true;
        projectileSpawned = false;
        spellAnimationStarted = false;
        castStartedTime = Time.time;

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.ResetTrigger(AttackParameter);
            animator.SetTrigger(AttackParameter);
        }
    }

    private void UpdateCast()
    {
        if (TryGetSpellAnimationProgress(out float normalizedTime))
        {
            spellAnimationStarted = true;

            if (!projectileSpawned && normalizedTime >= projectileImpactNormalizedTime)
            {
                FireProjectile();
            }

            if (projectileSpawned && normalizedTime >= 0.95f)
            {
                isCasting = false;
            }

            return;
        }

        if (spellAnimationStarted)
        {
            isCasting = false;
            return;
        }

        if (animator == null
            || animator.runtimeAnimatorController == null
            || Time.time >= castStartedTime + castAnimationFallbackDelay)
        {
            FireProjectile();
            isCasting = false;
        }
    }

    private bool TryGetSpellAnimationProgress(out float normalizedTime)
    {
        normalizedTime = 0f;
        if (animator == null)
        {
            return false;
        }

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        if (currentState.fullPathHash == SpellState)
        {
            normalizedTime = currentState.normalizedTime;
            return true;
        }

        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
            if (nextState.fullPathHash == SpellState)
            {
                normalizedTime = nextState.normalizedTime;
                return true;
            }
        }

        return false;
    }

    private void FireProjectile()
    {
        if (projectileSpawned || playerHealth == null)
        {
            return;
        }

        projectileSpawned = true;
        nextAttackTime = Time.time + attackCooldown;

        Vector3 spawnPosition = transform.position
            + Vector3.up * projectileSpawnHeight
            + transform.forward * projectileSpawnForwardOffset;
        CharacterController playerController = playerHealth.GetComponent<CharacterController>();
        Vector3 targetPosition = playerController != null
            ? playerController.bounds.center + Vector3.up * 0.65f
            : playerHealth.transform.position + Vector3.up * 0.65f;
        Vector3 direction = (targetPosition - spawnPosition).normalized;

        EnemyMagicProjectile.Spawn(
            spawnPosition,
            direction,
            attackDamage,
            projectileSpeed,
            attackRange);
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

    private Vector3 GetFlatDirectionToPlayer()
    {
        Vector3 direction = playerHealth.transform.position - transform.position;
        direction.y = 0f;
        return direction.sqrMagnitude > 0f ? direction.normalized : Vector3.zero;
    }

    private float GetFlatDistanceToPlayer()
    {
        Vector3 offset = playerHealth.transform.position - transform.position;
        offset.y = 0f;
        return offset.magnitude;
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

    private void MoveTowardPlayer(Vector3 directionToPlayer)
    {
        Vector3 movementDirection = (directionToPlayer + GetSeparationDirection() * separationStrength).normalized;
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

        foreach (EnemyRangedAI enemy in ActiveEnemies)
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
