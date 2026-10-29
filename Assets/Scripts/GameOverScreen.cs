using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

[RequireComponent(typeof(PlayerHealth))]
public class GameOverScreen : MonoBehaviour
{
    private static readonly Color Gold = new Color(0.72f, 0.52f, 0.25f);
    private PlayerHealth health;
    private CanvasGroup group;
    private GameObject canvasObject;
    private Texture2D vignetteTexture;
    private bool shown;
    private bool restarting;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        health.Died += Show;
    }

    private void Show()
    {
        if (shown) return;
        shown = true;
        GameTimerHUD timer = GetComponent<GameTimerHUD>();
        KillCounterHUD counter = GetComponent<KillCounterHUD>();
        int seconds = timer != null ? timer.ElapsedSeconds : Mathf.FloorToInt(Time.timeSinceLevelLoad);
        int kills = counter != null ? counter.KillCount : 0;
        if (timer != null) timer.enabled = false;
        if (counter != null) counter.enabled = false;
        PlayerHealthHUD healthHUD = GetComponent<PlayerHealthHUD>();
        if (healthHUD != null) healthHUD.enabled = false;
        foreach (HordeSpawner spawner in FindObjectsByType<HordeSpawner>(FindObjectsSortMode.None))
            spawner.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        BuildScreen(seconds, kills);
        StartCoroutine(Reveal());
    }

    private IEnumerator Reveal()
    {
        float elapsed = 0f;
        while (elapsed < 0.65f)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.65f));
            group.interactable = elapsed >= 0.65f;
            yield return null;
        }
        group.alpha = 1f;
        group.interactable = true;
    }

    private void BuildScreen(int seconds, int kills)
    {
        if (EventSystem.current == null)
        {
            GameObject events = new GameObject("Game Over Event System", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
            events.AddComponent<StandaloneInputModule>();
#endif
        }
        canvasObject = new GameObject("Game Over Screen", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        group = canvasObject.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;

        RectTransform backdrop = CreateRect("Vignette", canvasObject.transform, Vector2.zero, Vector2.zero);
        Stretch(backdrop);
        RawImage vignette = backdrop.gameObject.AddComponent<RawImage>();
        vignette.texture = CreateVignette();

        RectTransform band = CreateRect("Death Banner", canvasObject.transform, Vector2.zero, new Vector2(0f, 180f));
        band.anchorMin = new Vector2(0f, 0.5f);
        band.anchorMax = new Vector2(1f, 0.5f);
        Image panel = band.gameObject.AddComponent<Image>();
        panel.color = new Color(0.015f, 0.012f, 0.012f, 0.96f);
        panel.raycastTarget = false;
        Border(band, true);
        Border(band, false);
        Label("YOU DIED", band, Vector2.zero, new Vector2(900f, 140f), 82, new Color(0.76f, 0.025f, 0.035f));

        string timeText = string.Format("{0:00}:{1:00}", seconds / 60, seconds % 60);
        Label("TIME SURVIVED   " + timeText, canvasObject.transform,
            new Vector2(0f, -130f), new Vector2(700f, 38f), 27, Gold, true);
        Label("ENEMIES KILLED   " + kills, canvasObject.transform,
            new Vector2(0f, -170f), new Vector2(700f, 38f), 27, Gold, true);

        RectTransform buttonFrame = CreateRect("Restart", canvasObject.transform,
            new Vector2(0f, -250f), new Vector2(290f, 66f));
        buttonFrame.gameObject.AddComponent<Image>().color = Gold;
        RectTransform buttonFace = CreateRect("Button", buttonFrame, Vector2.zero, Vector2.zero);
        Stretch(buttonFace, 2f);
        Image face = buttonFace.gameObject.AddComponent<Image>();
        face.color = new Color(0.035f, 0.025f, 0.02f);
        Button button = buttonFace.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(2.5f, 2.5f, 2.5f);
        colors.pressedColor = new Color(0.65f, 0.65f, 0.65f);
        button.colors = colors;
        button.onClick.AddListener(Restart);
        Label("RESTART", buttonFace, Vector2.zero, new Vector2(260f, 56f), 28, Gold);
        RectTransform menuFrame = CreateRect("Main Menu", canvasObject.transform,
            new Vector2(0f, -335f), new Vector2(290f, 66f));
        menuFrame.gameObject.AddComponent<Image>().color = Gold;
        RectTransform menuFace = CreateRect("Button", menuFrame, Vector2.zero, Vector2.zero);
        Stretch(menuFace, 2f);
        Image menuImage = menuFace.gameObject.AddComponent<Image>();
        menuImage.color = face.color;
        Button menuButton = menuFace.gameObject.AddComponent<Button>();
        menuButton.targetGraphic = menuImage;
        menuButton.colors = colors;
        menuButton.onClick.AddListener(() => LoadScene("MainMenu"));
        Label("MAIN MENU", menuFace, Vector2.zero, new Vector2(260f, 56f), 28, Gold);
        EventSystem.current.SetSelectedGameObject(buttonFace.gameObject);
    }

    private void Restart()
    {
        LoadScene("Arena");
    }

    private void LoadScene(string sceneName)
    {
        if (restarting) return;
        restarting = true;
        StopAllCoroutines();
        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync(sceneName);
    }

    private Texture2D CreateVignette()
    {
        const int size = 256;
        vignetteTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        vignetteTexture.name = "Death Vignette";
        vignetteTexture.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float nx = x / (size - 1f) * 2f - 1f;
            float ny = y / (size - 1f) * 2f - 1f;
            float distance = Mathf.Sqrt(nx * nx + ny * ny);
            float alpha = Mathf.SmoothStep(0f, 0.96f, Mathf.InverseLerp(0.35f, 1.15f, distance));
            pixels[y * size + x] = new Color(0f, 0f, 0f, alpha);
        }
        vignetteTexture.SetPixels(pixels);
        vignetteTexture.Apply();
        return vignetteTexture;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * inset;
        rect.offsetMax = -Vector2.one * inset;
    }

    private static void Border(RectTransform parent, bool top)
    {
        RectTransform line = CreateRect(top ? "Top Gold Border" : "Bottom Gold Border", parent,
            Vector2.zero, new Vector2(0f, 2f));
        line.anchorMin = new Vector2(0f, top ? 1f : 0f);
        line.anchorMax = new Vector2(1f, top ? 1f : 0f);
        line.gameObject.AddComponent<Image>().color = Gold;
    }

    private static void Label(string text, Transform parent, Vector2 position, Vector2 size, int fontSize, Color color,
        bool outlined = false)
    {
        Text label = CreateRect(text, parent, position, size).gameObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color;
        label.raycastTarget = false;
        if (outlined)
        {
            Outline outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = true;
        }
    }

    private void OnDestroy()
    {
        if (health != null) health.Died -= Show;
        if (canvasObject != null) Destroy(canvasObject);
        if (vignetteTexture != null) Destroy(vignetteTexture);
    }
}
