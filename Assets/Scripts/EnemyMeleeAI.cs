using System.Collections.Generic;
using UnityEngine;

public class EnemyMeleeAI : MonoBehaviour
{
    private static readonly List<EnemyMeleeAI> ActiveEnemies = new List<EnemyMeleeAI>();

    [Header("Target and movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float turnSpeed = 540f;

    [Header("Melee attack")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField, Range(0f, 180f)] private float attackConeAngle = 120f;
    [SerializeField] private float attackWindup = 0.15f;
    [SerializeField] private int attackDamage = 12;
    [SerializeField] private float attackCooldown = 1f;

    [Header("Enemy spacing")]
    [SerializeField] private float separationRadius = 1.25f;
    [SerializeField] private float separationStrength = 1f;

    private PlayerHealth playerHealth;
    private float nextAttackTime;
    private float windupEndTime;
    private bool isWindingUp;

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
        if (playerHealth == null)
        {
            Debug.LogError("EnemyMeleeAI precisa encontrar um PlayerHealth na cena.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (playerHealth == null || playerHealth.CurrentHealth <= 0)
        {
            isWindingUp = false;
            return;
        }

        Vector3 toPlayer = GetFlatDirectionToPlayer();
        float distanceToPlayer = GetFlatDistanceToPlayer();
        RotateToward(toPlayer);

        if (isWindingUp)
        {
            // Cancel the prepared hit if the player escapes or gets behind the enemy.
            if (!CanAttack(distanceToPlayer, toPlayer))
            {
                isWindingUp = false;
            }
            else if (Time.time >= windupEndTime)
            {
                playerHealth.TakeDamage(attackDamage);
                nextAttackTime = Time.time + attackCooldown;
                isWindingUp = false;
            }

            return;
        }

        if (Time.time >= nextAttackTime && CanAttack(distanceToPlayer, toPlayer))
        {
            isWindingUp = true;
            windupEndTime = Time.time + attackWindup;
            return;
        }

        MoveTowardPlayer(toPlayer, distanceToPlayer);
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
        transform.position += movementDirection * moveSpeed * Time.deltaTime;
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
