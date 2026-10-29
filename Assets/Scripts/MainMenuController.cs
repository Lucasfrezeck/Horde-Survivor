using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Transform puglin;
    [SerializeField] private RuntimeAnimatorController puglinController;
    [SerializeField] private RuntimeAnimatorController impController;
    private static readonly Color Gold = new Color(0.72f, 0.52f, 0.25f);
    private Camera menuCamera;
    private Image fade;
    private CanvasGroup controls;
    private bool leaving;
    private float orbitTime;

    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        menuCamera = Camera.main;
        transform.rotation = Quaternion.Euler(0f, 155f, 0f);
        Idle(GetComponentInChildren<Animator>(), null);
        if (puglin != null)
        {
            puglin.gameObject.SetActive(true);
            puglin.position = GroundPosition(transform.position + new Vector3(3f, 0f, 4f));
            puglin.rotation = Quaternion.Euler(0f, 165f, 0f);
            Idle(puglin.GetComponentInChildren<Animator>(), puglinController);
        }
        GameObject impPrefab = Resources.Load<GameObject>("ImpModel");
        if (impPrefab != null)
        {
            GameObject imp = Instantiate(impPrefab);
            imp.name = "Menu Imp";
            Vector3 position = GroundPosition(transform.position + new Vector3(-2f, 0f, 5f));
            imp.transform.SetPositionAndRotation(position + Vector3.down, Quaternion.Euler(0f, 165f, 0f));
            imp.transform.localScale = Vector3.one * 2f;
            Idle(imp.GetComponentInChildren<Animator>(), impController);
        }
        BuildMenu();
        UpdateCamera();
    }

    private static Vector3 GroundPosition(Vector3 position)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null) position.y = terrain.SampleHeight(position) + terrain.transform.position.y + 1f;
        return position;
    }

    private static void Idle(Animator animator, RuntimeAnimatorController controller)
    {
        if (animator == null) return;
        if (controller != null) animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.SetFloat("Speed", 0f);
        animator.Play("Base Layer.Armature|Idle_Loop", 0, 0f);
        for (int i = 1; i < animator.layerCount; i++) animator.SetLayerWeight(i, 0f);
    }

    private void LateUpdate()
    {
        orbitTime += Time.unscaledDeltaTime;
        UpdateCamera();
    }

    private void UpdateCamera()
    {
        if (menuCamera == null) return;
        float angle = Mathf.Sin(orbitTime * 0.12f) * 0.12f;
        Vector3 offset = new Vector3(Mathf.Sin(angle) * 8f, 2.5f, -Mathf.Cos(angle) * 8f);
        menuCamera.transform.position = transform.position + offset;
        Vector3 forward = -new Vector3(offset.x, 0f, offset.z).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        menuCamera.transform.LookAt(transform.position + Vector3.up * 0.3f - right * 2.6f);
        menuCamera.fieldOfView = 43f;
    }

    private void BuildMenu()
    {
        if (EventSystem.current == null)
        {
            GameObject events = new GameObject("Menu Event System", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
            events.AddComponent<StandaloneInputModule>();
#endif
        }
        GameObject canvasObject = new GameObject("Main Menu UI", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform shade = Rect("Shade", canvasObject.transform, Vector2.zero, Vector2.zero);
        Stretch(shade);
        shade.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.22f);
        RectTransform panel = Rect("Menu Panel", canvasObject.transform, new Vector2(0f, 0f), new Vector2(700f, 0f));
        panel.anchorMin = new Vector2(0f, 0f);
        panel.anchorMax = new Vector2(0f, 1f);
        panel.pivot = new Vector2(0f, 0.5f);
        panel.gameObject.AddComponent<Image>().color = new Color(0.025f, 0.018f, 0.018f, 0.94f);
        controls = panel.gameObject.AddComponent<CanvasGroup>();
        RectTransform border = Rect("Gold Edge", panel, Vector2.zero, new Vector2(2f, 0f));
        border.anchorMin = new Vector2(1f, 0f);
        border.anchorMax = new Vector2(1f, 1f);
        border.gameObject.AddComponent<Image>().color = Gold;
        Label("HORDE", panel, new Vector2(0f, 245f), 78, Gold);
        Label("SURVIVOR", panel, new Vector2(0f, 160f), 68, new Color(0.75f, 0.04f, 0.05f));
        Button play = MenuButton("PLAY", panel, 10f, Play);
        MenuButton("SETTINGS", panel, -90f, null).interactable = false;
        MenuButton("QUIT", panel, -190f, Quit);
        EventSystem.current.SetSelectedGameObject(play.gameObject);

        RectTransform fadeRect = Rect("Transition", canvasObject.transform, Vector2.zero, Vector2.zero);
        Stretch(fadeRect);
        fade = fadeRect.gameObject.AddComponent<Image>();
        fade.color = Color.clear;
        fade.raycastTarget = false;
    }

    private void Play()
    {
        if (leaving) return;
        leaving = true;
        controls.interactable = false;
        StartCoroutine(StartGame());
    }

    private IEnumerator StartGame()
    {
        fade.raycastTarget = true;
        float elapsed = 0f;
        while (elapsed < 0.6f)
        {
            elapsed += Time.unscaledDeltaTime;
            fade.color = new Color(0f, 0f, 0f, Mathf.Clamp01(elapsed / 0.6f));
            yield return null;
        }
        Time.timeScale = 1f;
        yield return SceneManager.LoadSceneAsync("Arena");
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static Button MenuButton(string text, Transform parent, float y, UnityEngine.Events.UnityAction action)
    {
        RectTransform frame = Rect(text, parent, new Vector2(0f, y), new Vector2(440f, 72f));
        frame.gameObject.AddComponent<Image>().color = Gold;
        RectTransform face = Rect("Face", frame, Vector2.zero, Vector2.zero);
        Stretch(face, 2f);
        Image image = face.gameObject.AddComponent<Image>();
        image.color = Color.white;
        Button button = face.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.035f, 0.025f, 0.02f);
        colors.highlightedColor = new Color(0.15f, 0.105f, 0.055f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.25f, 0.16f, 0.07f);
        colors.disabledColor = new Color(0.08f, 0.07f, 0.065f);
        button.colors = colors;
        if (action != null) button.onClick.AddListener(action);
        Label(text, face, Vector2.zero, 30, action != null ? Gold : new Color(0.4f, 0.36f, 0.29f));
        return button;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * inset;
        rect.offsetMax = -Vector2.one * inset;
    }

    private static void Label(string text, Transform parent, Vector2 position, int size, Color color)
    {
        Text label = Rect(text, parent, position, new Vector2(600f, 100f)).gameObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = text;
        label.fontSize = size;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color;
        label.raycastTarget = false;
    }
}
