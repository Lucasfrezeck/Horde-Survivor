using UnityEngine;

// Shared non-combat movement for enemies after the player dies.
public sealed class EnemyPatrol
{
    private static readonly int SpeedParameter = Animator.StringToHash("Speed");
    private static readonly int AttackParameter = Animator.StringToHash("Attack");
    private static readonly int IdleState = Animator.StringToHash("Base Layer.Armature|Idle_Loop");
    private Vector3 origin;
    private Vector3 destination;
    private float deadline;
    private float waitUntil;
    private bool started;
    private bool waiting;

    public void Tick(Transform actor, CharacterController controller, Animator animator,
        float moveSpeed, float turnSpeed, float referenceSpeed, float animationSpeed, Vector3 separation)
    {
        if (!started)
        {
            started = true;
            origin = actor.position;
            PickDestination();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.ResetTrigger(AttackParameter);
                if (animator.HasState(0, IdleState)) animator.CrossFadeInFixedTime(IdleState, 0.15f);
            }
        }

        Vector3 offset = destination - actor.position;
        offset.y = 0f;
        if (!waiting && (offset.sqrMagnitude < 0.36f || Time.time >= deadline))
        {
            waiting = true;
            waitUntil = Time.time + Random.Range(0.75f, 2f);
        }
        if (waiting && Time.time >= waitUntil)
        {
            PickDestination();
            offset = destination - actor.position;
            offset.y = 0f;
        }

        float patrolSpeed = Mathf.Max(0f, moveSpeed) * 0.55f;
        Vector3 direction = waiting ? Vector3.zero : (offset.normalized + separation).normalized;
        if (direction.sqrMagnitude > 0.001f)
        {
            actor.rotation = Quaternion.RotateTowards(actor.rotation,
                Quaternion.LookRotation(direction), turnSpeed * Time.deltaTime);
        }
        if (controller != null && controller.enabled)
            controller.SimpleMove(direction * patrolSpeed);
        else
            actor.position += direction * patrolSpeed * Time.deltaTime;

        if (animator != null)
        {
            bool moving = direction.sqrMagnitude > 0.001f && patrolSpeed > 0f;
            animator.SetFloat(SpeedParameter, moving ? 1f : 0f);
            animator.speed = moving ? animationSpeed * patrolSpeed / Mathf.Max(referenceSpeed, 0.01f) : 1f;
        }
    }

    private void PickDestination()
    {
        Vector2 offset = Random.insideUnitCircle * 6f;
        destination = origin + new Vector3(offset.x, 0f, offset.y);
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null && terrain.terrainData != null)
        {
            Vector3 min = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            destination.x = Mathf.Clamp(destination.x, min.x + 2f, min.x + size.x - 2f);
            destination.z = Mathf.Clamp(destination.z, min.z + 2f, min.z + size.z - 2f);
        }
        // Reselect when blocked by other enemies or scenery instead of walking in place forever.
        deadline = Time.time + Random.Range(4f, 7f);
        waiting = false;
    }
}
