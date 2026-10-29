using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private float visibleDuration = 0.5f;
    [SerializeField, Min(0f)] private float fillTransitionDuration = 0.2f;
    [SerializeField] private float heightOffset = 1.45f;

    private static readonly Color GoldColor = new Color(0.58f, 0.40f, 0.19f, 1f);
    private static readonly Color HealthColor = new Color(0.68f, 0.14f, 0.16f, 1f);
    private static Sprite whiteSprite;

    private EnemyHealth enemyHealth;
    private Renderer[] enemyRenderers;
    private Transform barCanvas;
    private GameObject barRoot;
    private Image healthFill;
    private Coroutine hideRoutine;
    private Coroutine fillRoutine;
    private Camera mainCamera;

    public void Initialize(EnemyHealth health)
    {
        if (enemyHealth != null)
        {
            enemyHealth.HealthChanged -= UpdateHealth;
        }

        enemyHealth = health;
        enemyHealth.HealthChanged += UpdateHealth;
        enemyRenderers = GetComponentsInChildren<Renderer>();
        mainCamera = Camera.main;
        CreateBar();
        UpdateHealth(enemyHealth.CurrentHealth, enemyHealth.MaxHealth);
    }

    private void LateUpdate()
    {
        if (barCanvas == null || barRoot == null || !barRoot.activeSelf)
        {
            return;
        }

        UpdateBarPosition();
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null)
        {
            barCanvas.LookAt(mainCamera.transform.position, Vector3.up);
        }
    }

    private void UpdateBarPosition()
    {
        float topY = float.NegativeInfinity;
        if (enemyRenderers != null)
        {
            foreach (Renderer enemyRenderer in enemyRenderers)
            {
                if (enemyRenderer != null && enemyRenderer.enabled && enemyRenderer.gameObject.activeInHierarchy)
                {
                    topY = Mathf.Max(topY, enemyRenderer.bounds.max.y + 0.15f);
                }
            }
        }

        if (float.IsNegativeInfinity(topY))
        {
            topY = transform.position.y + heightOffset;
        }

        barCanvas.position = new Vector3(transform.position.x, topY, transform.position.z);
    }

    private void OnDestroy()
    {
        if (enemyHealth != null)
        {
            enemyHealth.HealthChanged -= UpdateHealth;
        }
    }

    private void UpdateHealth(int currentHealth, int maxHealth)
    {
        if (healthFill == null)
        {
            return;
        }

        float targetFill = maxHealth > 0
            ? Mathf.Clamp01((float)currentHealth / maxHealth)
            : 0f;

        if (currentHealth < maxHealth)
        {
            barRoot.SetActive(true);

            if (fillRoutine != null)
            {
                StopCoroutine(fillRoutine);
            }

            fillRoutine = StartCoroutine(AnimateFill(targetFill));

            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
            }

            hideRoutine = StartCoroutine(HideAfterDelay());
        }
    }

    private IEnumerator AnimateFill(float targetFill)
    {
        float startFill = healthFill.fillAmount;
        if (fillTransitionDuration <= 0f)
        {
            healthFill.fillAmount = targetFill;
            fillRoutine = null;
            yield break;
        }

        healthFill.fillAmount = startFill;
        yield return null;

        float elapsed = 0f;
        while (elapsed < fillTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / fillTransitionDuration);
            healthFill.fillAmount = Mathf.Lerp(startFill, targetFill, progress);
            yield return null;
        }

        healthFill.fillAmount = targetFill;
        fillRoutine = null;
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(visibleDuration);
        barRoot.SetActive(false);
        hideRoutine = null;
    }

    private void CreateBar()
    {
        GameObject canvasObject = new GameObject("Enemy Health Bar", typeof(RectTransform), typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);
        // This halves the current bar area while preserving its proportions.
        canvasObject.transform.localScale = Vector3.one * 0.005f;

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 5;
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(180f, 26f);
        barCanvas = canvasObject.transform;
        UpdateBarPosition();

        RectTransform frame = CreateRect("Frame", barCanvas);
        frame.anchorMin = Vector2.zero;
        frame.anchorMax = Vector2.one;
        frame.offsetMin = Vector2.zero;
        frame.offsetMax = Vector2.zero;
        CreateImage(frame.gameObject, GoldColor);

        RectTransform background = CreateRect("Background", frame);
        background.anchorMin = Vector2.zero;
        background.anchorMax = Vector2.one;
        background.offsetMin = new Vector2(2f, 2f);
        background.offsetMax = new Vector2(-2f, -2f);
        CreateImage(background.gameObject, new Color(0.08f, 0.06f, 0.06f, 1f));

        RectTransform fillRect = CreateRect("Fill", background);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        healthFill = fillRect.gameObject.AddComponent<Image>();
        healthFill.sprite = GetWhiteSprite();
        healthFill.color = HealthColor;
        healthFill.fillAmount = 1f;
        healthFill.type = Image.Type.Filled;
        healthFill.fillMethod = Image.FillMethod.Horizontal;
        healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;

        barRoot = canvasObject;
        barRoot.SetActive(false);
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child.GetComponent<RectTransform>();
    }

    private static Image CreateImage(GameObject target, Color color)
    {
        Image image = target.AddComponent<Image>();
        image.sprite = GetWhiteSprite();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Sprite GetWhiteSprite()
    {
        if (whiteSprite == null)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        }

        return whiteSprite;
    }
}
