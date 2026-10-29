using UnityEngine;

public class EnemyMagicProjectile : MonoBehaviour
{
    private static readonly Color ProjectileColor = new Color(0.54f, 0.16f, 0.95f, 1f);
    private static Material projectileMaterial;

    private Vector3 direction;
    private int damage;
    private float speed;
    private float maxDistance;
    private float traveledDistance;
    private float collisionRadius;

    public static EnemyMagicProjectile Spawn(
        Vector3 position,
        Vector3 direction,
        int damage,
        float speed,
        float maxDistance)
    {
        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.name = "Imp Magic Projectile";
        projectile.transform.position = position;
        projectile.transform.rotation = direction.sqrMagnitude > 0f
            ? Quaternion.LookRotation(direction, Vector3.up)
            : Quaternion.identity;
        projectile.transform.localScale = Vector3.one * 0.35f;

        SphereCollider sphereCollider = projectile.GetComponent<SphereCollider>();
        sphereCollider.enabled = false;

        Renderer projectileRenderer = projectile.GetComponent<Renderer>();
        projectileRenderer.sharedMaterial = GetProjectileMaterial();
        projectileRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        projectileRenderer.receiveShadows = false;

        EnemyMagicProjectile behaviour = projectile.AddComponent<EnemyMagicProjectile>();
        behaviour.Initialize(direction, damage, speed, maxDistance, sphereCollider.radius);
        return behaviour;
    }

    private static Material GetProjectileMaterial()
    {
        if (projectileMaterial != null)
        {
            return projectileMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }
        if (shader == null)
        {
            return null;
        }

        projectileMaterial = new Material(shader) { name = "ImpMagicProjectileRuntime" };
        if (projectileMaterial.HasProperty("_BaseColor"))
        {
            projectileMaterial.SetColor("_BaseColor", ProjectileColor);
        }
        if (projectileMaterial.HasProperty("_Color"))
        {
            projectileMaterial.SetColor("_Color", ProjectileColor);
        }
        if (projectileMaterial.HasProperty("_EmissionColor"))
        {
            projectileMaterial.EnableKeyword("_EMISSION");
            projectileMaterial.SetColor("_EmissionColor", ProjectileColor * 1.5f);
        }

        return projectileMaterial;
    }

    private void Initialize(
        Vector3 projectileDirection,
        int projectileDamage,
        float projectileSpeed,
        float projectileMaxDistance,
        float colliderRadius)
    {
        direction = projectileDirection.sqrMagnitude > 0f
            ? projectileDirection.normalized
            : transform.forward;
        damage = projectileDamage;
        speed = Mathf.Max(0f, projectileSpeed);
        maxDistance = Mathf.Max(0f, projectileMaxDistance);
        collisionRadius = colliderRadius * transform.lossyScale.x;

    }

    private void FixedUpdate()
    {
        float stepDistance = speed * Time.fixedDeltaTime;
        if (stepDistance <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (Physics.SphereCast(
                transform.position,
                collisionRadius,
                direction,
                out RaycastHit hit,
                stepDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            PlayerHealth playerHealth = hit.collider.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }

            Destroy(gameObject);
            return;
        }

        transform.position += direction * stepDistance;
        traveledDistance += stepDistance;
        if (traveledDistance >= maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (projectileMaterial != null && !Application.isPlaying)
        {
            Destroy(projectileMaterial);
            projectileMaterial = null;
        }
    }
}
