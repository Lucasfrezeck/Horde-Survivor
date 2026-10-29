using UnityEngine;
using UnityEngine.AI;

// Routes around static scenery while CharacterController still handles movement and crowd collisions.
public sealed class EnemyNavigation
{
    private NavMeshPath path;
    private Vector3[] corners;
    private Vector3 lastTarget;
    private int corner;
    private float nextRefresh;

    public Vector3 Direction(Vector3 position, Vector3 target)
    {
        if (path == null) path = new NavMeshPath();
        if (Time.time >= nextRefresh || (target - lastTarget).sqrMagnitude > 4f)
        {
            nextRefresh = Time.time + Random.Range(0.3f, 0.5f);
            lastTarget = target;
            corners = null;
            if (NavMesh.SamplePosition(position, out NavMeshHit start, 3f, NavMesh.AllAreas)
                && NavMesh.SamplePosition(target, out NavMeshHit end, 3f, NavMesh.AllAreas)
                && NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path))
            {
                corners = path.corners;
                corner = corners.Length > 1 ? 1 : 0;
            }
        }
        Vector3 destination = target;
        if (corners != null && corners.Length > 0)
        {
            while (corner < corners.Length - 1 && Flat(corners[corner] - position).sqrMagnitude < 0.36f) corner++;
            destination = corners[corner];
        }
        return Flat(destination - position).normalized;
    }

    public static bool TrySpawn(Vector3 candidate, Vector3 player, out Vector3 position)
    {
        position = candidate;
        if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas)) return false;
        position = hit.position + Vector3.up;
        if (Physics.CheckCapsule(position + Vector3.down * 0.4f, position + Vector3.up * 0.4f,
            0.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return false;
        NavMeshPath path = new NavMeshPath();
        return NavMesh.SamplePosition(player, out NavMeshHit target, 3f, NavMesh.AllAreas)
            && NavMesh.CalculatePath(hit.position, target.position, NavMesh.AllAreas, path)
            && path.status == NavMeshPathStatus.PathComplete;
    }

    public static bool CanSee(Transform actor, PlayerHealth player)
    {
        Vector3 origin = actor.position + Vector3.up * 0.3f;
        Vector3 offset = player.transform.position + Vector3.up * 0.3f - origin;
        return !Physics.Raycast(origin, offset.normalized, out RaycastHit hit, offset.magnitude,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            || hit.collider.GetComponentInParent<PlayerHealth>() == player;
    }

    private static Vector3 Flat(Vector3 value) { value.y = 0f; return value; }
}
