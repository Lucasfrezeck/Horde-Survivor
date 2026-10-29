using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public sealed class ExperienceUI
{
    private static readonly Color Gold = new Color(0.72f, 0.52f, 0.25f);
    private static readonly Color Black = new Color(0.025f, 0.018f, 0.018f, 0.98f);
    private readonly GameObject root;
    private readonly RectTransform modal;
    private RectTransform cards;
    private readonly Text levelLabel;
    private readonly Text xpLabel;
    private readonly Text newLevelLabel;
    private readonly Image fill;

    public ExperienceUI()
    {
        if (EventSystem.current == null)
        {
            GameObject events = new GameObject("Experience Event System", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
            events.AddComponent<StandaloneInputModule>();
#endif
        }
        root = new GameObject("Experience HUD", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform hud = Rect("XP Bar", root.transform, new Vector2(0f, 12f), new Vector2(350f, 82f));
        hud.anchorMin = hud.anchorMax = new Vector2(0.5f, 0f);
        hud.pivot = new Vector2(0.5f, 0f);
        Image background = hud.gameObject.AddComponent<Image>();
        background.color = new Color(0.025f, 0.018f, 0.018f, 0.94f);
        background.raycastTarget = false;
        RectTransform bar = Rect("Gold Frame", hud, new Vector2(0f, -14f), new Vector2(-28f, 18f));
        bar.anchorMin = new Vector2(0f, 0.5f);
        bar.anchorMax = new Vector2(1f, 0.5f);
        Paint(bar, Gold);
        RectTransform track = Rect("Track", bar, Vector2.zero, Vector2.zero);
        Stretch(track, 2f);
        Paint(track, Black);
        RectTransform fillRect = Rect("Experience", track, Vector2.zero, Vector2.zero);
        Stretch(fillRect);
        fill = Paint(fillRect, new Color(0.9f, 0.67f, 0.27f));
        // A generated white sprite is unnecessary: anchors represent progress directly.
        levelLabel = Label("LEVEL 1", hud, new Vector2(74f, 19f), new Vector2(120f, 34f), 20, Gold);
        levelLabel.rectTransform.anchorMin = levelLabel.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        levelLabel.alignment = TextAnchor.MiddleLeft;
        xpLabel = Label("0 XP", hud, new Vector2(-106f, 19f), new Vector2(184f, 34f), 20, Gold);
        xpLabel.rectTransform.anchorMin = xpLabel.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        xpLabel.alignment = TextAnchor.MiddleRight;

        modal = Rect("Level Up", root.transform, Vector2.zero, Vector2.zero);
        Stretch(modal);
        Paint(modal, new Color(0f, 0f, 0f, 0.82f), true);
        Label("LEVEL UP", modal, new Vector2(0f, 295f), new Vector2(1000f, 100f), 68, Gold);
        newLevelLabel = Label("LEVEL 2", modal, new Vector2(0f, 215f), new Vector2(900f, 55f), 28, Color.white);
        Label("CHOOSE AN UPGRADE", modal, new Vector2(0f, 157f), new Vector2(1000f, 50f), 25, Gold);
        Label("CLICK A CARD OR USE ARROW KEYS AND ENTER", modal,
            new Vector2(0f, -305f), new Vector2(1200f, 40f), 21, Gold);
        modal.gameObject.SetActive(false);
    }

    public void Refresh(ExperienceProgress progress)
    {
        levelLabel.text = "LEVEL " + progress.Level;
        xpLabel.text = progress.IsMaxLevel ? "MAX LEVEL" : progress.Experience + " / " + progress.RequiredExperience + " XP";
        float fraction = progress.IsMaxLevel ? 1f : (float)progress.Experience / progress.RequiredExperience;
        fill.rectTransform.anchorMax = new Vector2(fraction, 1f);
    }

    public void Choices(int level, string[] titles, string[] descriptions, string[] icons, int[] ranks, Action<int> choose)
    {
        if (cards != null)
        {
            cards.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(cards.gameObject);
        }
        cards = Rect("Cards", modal, new Vector2(0f, -55f), new Vector2(1260f, 370f));
        newLevelLabel.text = "LEVEL " + level;
        modal.gameObject.SetActive(true);
        for (int i = 0; i < titles.Length; i++)
        {
            int choice = i;
            RectTransform frame = Rect(titles[i], cards, new Vector2((i - 1) * 420f, 0f), new Vector2(390f, 370f));
            Paint(frame, Gold);
            RectTransform face = Rect("Select", frame, Vector2.zero, Vector2.zero);
            Stretch(face, 3f);
            Image image = Paint(face, Color.white, true);
            Button button = face.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Black;
            colors.highlightedColor = new Color(0.19f, 0.13f, 0.055f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.3f, 0.2f, 0.08f);
            button.colors = colors;
            button.onClick.AddListener(() => choose(choice));
            RectTransform badge = Rect("Icon Frame", face, new Vector2(0f, 110f), new Vector2(82f, 64f));
            Paint(badge, Gold);
            RectTransform inside = Rect("Icon", badge, Vector2.zero, Vector2.zero);
            Stretch(inside, 2f);
            Paint(inside, Black);
            Label(icons[i], inside, Vector2.zero, new Vector2(80f, 60f), 25, Gold);
            Label(titles[i], face, new Vector2(0f, 38f), new Vector2(360f, 56f), 28, Gold);
            Label(descriptions[i], face, new Vector2(0f, -41f), new Vector2(355f, 100f), 23, Color.white);
            Label(ranks[i] < 0 ? "INSTANT EFFECT" : "UPGRADE LEVEL  " + ranks[i] + " → " + (ranks[i] + 1), face,
                new Vector2(0f, -130f), new Vector2(355f, 38f), 20, Gold);
            if (i == 0) EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }

    public void CloseChoices()
    {
        modal.gameObject.SetActive(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }
    public void Hide() => root.SetActive(false);
    public void Dispose() { if (root != null) UnityEngine.Object.Destroy(root); }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
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
    private static Image Paint(RectTransform rect, Color color, bool interactive = false)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = interactive;
        return image;
    }
    private static Text Label(string value, Transform parent, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        Text text = Rect(value, parent, position, size).gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }
}
