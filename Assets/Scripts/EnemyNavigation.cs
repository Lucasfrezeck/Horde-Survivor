using UnityEngine;
using UnityEngine.AI;

// Routes around static scenery while CharacterController still handles movement and crowd collisions.
public sealed class EnemyNavigation
{
    private NavMeshPath path;
    private readonly Vector3[] corners = new Vector3[64];
    private int cornerCount;
    private static int budgetFrame = -1;
    private static int queriesThisFrame;
    private static NavMeshPath spawnPath;
    private Vector3 lastTarget;
    private int corner;
    private float nextRefresh;
    private float forceRefresh;
    private int sightFrame = -1;
    private bool canSee;

    public Vector3 Direction(Vector3 position, Vector3 target)
    {
        if (path == null) path = new NavMeshPath();
        bool needsRefresh = cornerCount == 0 || (target - lastTarget).sqrMagnitude > 0.16f || Time.time >= forceRefresh;
        if (Time.time >= nextRefresh && needsRefresh && TakeQueryBudget())
        {
            nextRefresh = Time.time + Random.Range(0.3f, 0.5f);
            lastTarget = target;
            forceRefresh = Time.time + 2f;
            cornerCount = 0;
            if (NavMesh.SamplePosition(position, out NavMeshHit start, 3f, NavMesh.AllAreas)
                && NavMesh.SamplePosition(target, out NavMeshHit end, 3f, NavMesh.AllAreas))
            {
                if (!NavMesh.Raycast(start.position, end.position, out _, NavMesh.AllAreas))
                {
                    corners[0] = end.position;
                    cornerCount = 1;
                }
                else if (NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path))
                    cornerCount = path.GetCornersNonAlloc(corners);
                corner = cornerCount > 1 ? 1 : 0;
            }
        }
        Vector3 destination = target;
        if (cornerCount > 0)
        {
            while (corner < cornerCount - 1 && Flat(corners[corner] - position).sqrMagnitude < 0.36f) corner++;
            destination = corners[corner];
        }
        return Flat(destination - position).normalized;
    }

    private static bool TakeQueryBudget()
    {
        if (budgetFrame != Time.frameCount)
        {
            budgetFrame = Time.frameCount;
            queriesThisFrame = 0;
        }
        if (queriesThisFrame >= 12) return false;
        queriesThisFrame++;
        return true;
    }

    public static bool TrySpawn(Vector3 candidate, Vector3 player, out Vector3 position)
    {
        position = candidate;
        if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas)) return false;
        position = hit.position + Vector3.up;
        if (Physics.CheckCapsule(position + Vector3.down * 0.4f, position + Vector3.up * 0.4f,
            0.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return false;
        if (!NavMesh.SamplePosition(player, out NavMeshHit target, 3f, NavMesh.AllAreas)) return false;
        if (!NavMesh.Raycast(hit.position, target.position, out _, NavMesh.AllAreas)) return true;
        if (spawnPath == null) spawnPath = new NavMeshPath();
        return NavMesh.CalculatePath(hit.position, target.position, NavMesh.AllAreas, spawnPath)
            && spawnPath.status == NavMeshPathStatus.PathComplete;
    }

    public bool CanSee(Transform actor, PlayerHealth player)
    {
        if (sightFrame == Time.frameCount) return canSee;
        sightFrame = Time.frameCount;
        Vector3 origin = actor.position + Vector3.up * 0.3f;
        Vector3 offset = player.transform.position + Vector3.up * 0.3f - origin;
        canSee = !Physics.Raycast(origin, offset.normalized, out RaycastHit hit, offset.magnitude,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            || hit.collider.GetComponentInParent<PlayerHealth>() == player;
        return canSee;
    }

    private static Vector3 Flat(Vector3 value) { value.y = 0f; return value; }
}
