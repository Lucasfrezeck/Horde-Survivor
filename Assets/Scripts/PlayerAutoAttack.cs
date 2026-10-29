using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerAutoAttack : MonoBehaviour
{
    private static readonly int AttackParameter = Animator.StringToHash("Attack");
    private static readonly int AttackVariantParameter = Animator.StringToHash("AttackVariant");
    private static readonly int PunchCrossState = Animator.StringToHash("Attack.Armature|Punch_Cross");
    private static readonly int PunchJabState = Animator.StringToHash("Attack.Armature|Punch_Jab");
    private const int AttackLayer = 2;

    [SerializeField] private Transform attackOrigin;
    [SerializeField, Range(0.05f, 0.9f)] private float punchCrossImpactTime = 0.4f;
    [SerializeField, Range(0.05f, 0.9f)] private float punchJabImpactTime = 0.3f;
    [SerializeField] private Color attackAreaColor = new Color(1f, 1f, 1f, 0.32f);
    [SerializeField, Min(0f)] private float attackAreaHoldDuration = 0.08f;
    [SerializeField, Min(0.01f)] private float attackAreaFadeDuration = 0.25f;
    [SerializeField, Range(8, 48)] private int attackAreaSegments = 24;

    private PlayerStats playerStats;
    private Animator animator;
    private float nextAttackTime;
    private float attackStartedTime;
    private bool isAttackInProgress;
    private bool attackHitApplied;
    private bool attackAnimationStarted;
    private int nextAttackVariant;
    private int currentAttackVariant;
    private GameObject attackAreaObject;
    private Material attackAreaMaterial;
    private Coroutine attackAreaFade;

    private void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        animator = GetComponentInChildren<Animator>();
        nextAttackVariant = Random.Range(0, 2);

        if (playerStats == null)
        {
            Debug.LogError("PlayerAutoAttack precisa de um componente PlayerStats no mesmo objeto.", this);
            enabled = false;
            return;
        }

        CreateAttackAreaEffect();
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
            if (animator.layerCount > AttackLayer)
            {
                animator.SetLayerWeight(AttackLayer, 1f);
            }

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
                SetAttackLayerWeight(0f);
            }

            return;
        }

        if (attackAnimationStarted)
        {
            isAttackInProgress = false;
            SetAttackLayerWeight(0f);
            return;
        }

        if (animator == null
            || animator.runtimeAnimatorController == null
            || Time.time >= attackStartedTime + 0.5f)
        {
            ApplyConeDamage(transform.forward);
            attackHitApplied = true;
            isAttackInProgress = false;
            SetAttackLayerWeight(0f);
        }
    }

    private bool TryGetAttackAnimationProgress(out float normalizedTime)
    {
        normalizedTime = 0f;
        if (animator == null)
        {
            return false;
        }

        if (animator.layerCount <= AttackLayer)
        {
            return false;
        }

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(AttackLayer);
        if (IsPunchState(currentState.fullPathHash))
        {
            normalizedTime = currentState.normalizedTime;
            return true;
        }

        if (animator.IsInTransition(AttackLayer))
        {
            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(AttackLayer);
            if (IsPunchState(nextState.fullPathHash))
            {
                normalizedTime = nextState.normalizedTime;
                return true;
            }
        }

        return false;
    }

    private void SetAttackLayerWeight(float weight)
    {
        if (animator != null && animator.layerCount > AttackLayer)
        {
            animator.SetLayerWeight(AttackLayer, weight);
        }
    }

    private static bool IsPunchState(int stateHash)
    {
        return stateHash == PunchCrossState || stateHash == PunchJabState;
    }

    private void ApplyConeDamage(Vector3 forward)
    {
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        Vector3 flatForward = GetFlatDirection(forward);
        float minimumFacingDot = Mathf.Cos(playerStats.attackConeAngle * 0.5f * Mathf.Deg2Rad);
        float attackRangeSquared = playerStats.attackRange * playerStats.attackRange;
        Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position;
        ShowAttackArea(origin, flatForward);

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

    private void CreateAttackAreaEffect()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            Debug.LogWarning("Não foi possível encontrar um shader para o efeito da área de ataque.", this);
            return;
        }

        attackAreaMaterial = new Material(shader)
        {
            name = "PlayerAttackAreaRuntime",
            renderQueue = (int)RenderQueue.Transparent
        };
        attackAreaMaterial.SetOverrideTag("RenderType", "Transparent");
        if (attackAreaMaterial.HasProperty("_Surface"))
        {
            attackAreaMaterial.SetFloat("_Surface", 1f);
            attackAreaMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        if (attackAreaMaterial.HasProperty("_SrcBlend"))
        {
            attackAreaMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        }
        if (attackAreaMaterial.HasProperty("_DstBlend"))
        {
            attackAreaMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        }
        if (attackAreaMaterial.HasProperty("_ZWrite"))
        {
            attackAreaMaterial.SetFloat("_ZWrite", 0f);
        }
        SetAttackAreaMaterialColor(attackAreaColor);

        attackAreaObject = new GameObject("PlayerAttackAreaEffect");
        attackAreaObject.transform.SetParent(transform, false);
        attackAreaObject.SetActive(false);

        MeshFilter meshFilter = attackAreaObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = attackAreaObject.AddComponent<MeshRenderer>();
        meshFilter.sharedMesh = CreateAttackAreaMesh();
        meshRenderer.sharedMaterial = attackAreaMaterial;
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    private Mesh CreateAttackAreaMesh()
    {
        int segmentCount = Mathf.Max(8, attackAreaSegments);
        Vector3[] vertices = new Vector3[segmentCount + 2];
        int[] triangles = new int[segmentCount * 3];
        vertices[0] = Vector3.zero;

        float halfAngle = playerStats.attackConeAngle * 0.5f;
        for (int i = 0; i <= segmentCount; i++)
        {
            float angle = Mathf.Lerp(-halfAngle, halfAngle, (float)i / segmentCount) * Mathf.Deg2Rad;
            vertices[i + 1] = new Vector3(
                Mathf.Sin(angle) * playerStats.attackRange,
                0f,
                Mathf.Cos(angle) * playerStats.attackRange);
        }

        for (int i = 0; i < segmentCount; i++)
        {
            int triangleIndex = i * 3;
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = i + 1;
            triangles[triangleIndex + 2] = i + 2;
        }

        Mesh mesh = new Mesh { name = "PlayerAttackAreaCone" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void ShowAttackArea(Vector3 origin, Vector3 forward)
    {
        if (attackAreaObject == null)
        {
            return;
        }

        if (attackAreaFade != null)
        {
            StopCoroutine(attackAreaFade);
        }

        Vector3 position = origin;
        position.y += 0.03f;
        attackAreaObject.transform.SetPositionAndRotation(
            position,
            Quaternion.LookRotation(forward, Vector3.up));
        SetAttackAreaMaterialColor(attackAreaColor);
        attackAreaObject.SetActive(true);
        attackAreaFade = StartCoroutine(FadeAttackArea());
    }

    private IEnumerator FadeAttackArea()
    {
        if (attackAreaHoldDuration > 0f)
        {
            yield return new WaitForSeconds(attackAreaHoldDuration);
        }

        float elapsed = 0f;
        while (elapsed < attackAreaFadeDuration)
        {
            elapsed += Time.deltaTime;
            Color color = attackAreaColor;
            color.a *= 1f - Mathf.Clamp01(elapsed / attackAreaFadeDuration);
            SetAttackAreaMaterialColor(color);
            yield return null;
        }

        attackAreaObject.SetActive(false);
        attackAreaFade = null;
    }

    private void SetAttackAreaMaterialColor(Color color)
    {
        if (attackAreaMaterial == null)
        {
            return;
        }

        if (attackAreaMaterial.HasProperty("_BaseColor"))
        {
            attackAreaMaterial.SetColor("_BaseColor", color);
        }
        if (attackAreaMaterial.HasProperty("_Color"))
        {
            attackAreaMaterial.SetColor("_Color", color);
        }
    }

    private void OnDestroy()
    {
        if (attackAreaFade != null)
        {
            StopCoroutine(attackAreaFade);
        }

        if (attackAreaObject != null)
        {
            MeshFilter meshFilter = attackAreaObject.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Destroy(meshFilter.sharedMesh);
            }

            Destroy(attackAreaObject);
        }

        if (attackAreaMaterial != null)
        {
            Destroy(attackAreaMaterial);
        }
    }

    private static Vector3 GetFlatDirection(Vector3 direction)
    {
        direction.y = 0f;
        return direction.sqrMagnitude > 0f ? direction.normalized : Vector3.zero;
    }
}
