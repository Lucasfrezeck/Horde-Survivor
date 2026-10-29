using UnityEngine;

public class GameTimerHUD : MonoBehaviour
{
    private static readonly Color PanelColor = new Color(0.10f, 0.08f, 0.08f, 0.96f);
    private static readonly Color GoldColor = new Color(0.72f, 0.52f, 0.25f, 1f);

    private float startTime;
    public int ElapsedSeconds => Mathf.Max(0, Mathf.FloorToInt(Time.timeSinceLevelLoad - startTime));
    private GUIStyle timerStyle;

    private void Awake()
    {
        startTime = Time.timeSinceLevelLoad;
    }

    private void OnGUI()
    {
        EnsureStyle();

        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;
        float scale = Mathf.Clamp(Screen.height / 1080f, 0.7f, 1.4f);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);

        const float panelWidth = 150f;
        const float panelHeight = 58f;
        float centerX = Screen.width / scale * 0.5f;
        Rect panelRect = new Rect(centerX - panelWidth * 0.5f, 0f, panelWidth, panelHeight);

        GUI.color = GoldColor;
        GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
        GUI.color = PanelColor;
        GUI.DrawTexture(new Rect(panelRect.x + 3f, panelRect.y + 3f, panelWidth - 6f, panelHeight - 6f),
            Texture2D.whiteTexture);

        int elapsedSeconds = ElapsedSeconds;
        string timeText = string.Format("{0:00}:{1:00}", elapsedSeconds / 60, elapsedSeconds % 60);

        GUI.color = Color.white;
        GUI.Label(panelRect, timeText, timerStyle);

        GUI.color = previousColor;
        GUI.matrix = previousMatrix;
    }

    private void EnsureStyle()
    {
        if (timerStyle != null)
        {
            return;
        }

        timerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 26,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
    }
}
