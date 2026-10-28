using UnityEngine;

public class PlayerHealthHUD : MonoBehaviour
{
    private static readonly Color PanelColor = new Color(0.10f, 0.08f, 0.08f, 0.96f);
    private static readonly Color GoldColor = new Color(0.72f, 0.52f, 0.25f, 1f);
    private static readonly Color HealthColor = new Color(0.72f, 0.12f, 0.14f, 1f);
    private static readonly Color LowHealthColor = new Color(0.96f, 0.30f, 0.12f, 1f);

    private PlayerHealth playerHealth;
    private GUIStyle labelStyle;
    private GUIStyle valueStyle;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("PlayerHealthHUD precisa de um componente PlayerHealth no mesmo objeto.", this);
            enabled = false;
        }
    }

    private void OnGUI()
    {
        if (playerHealth == null)
        {
            return;
        }

        EnsureStyles();

        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;
        float scale = Mathf.Clamp(Screen.height / 1080f, 0.7f, 1.4f);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);

        Rect panelRect = new Rect(28f, 28f, 350f, 92f);
        GUI.color = GoldColor;
        GUI.DrawTexture(panelRect, Texture2D.whiteTexture);

        GUI.color = PanelColor;
        GUI.DrawTexture(new Rect(31f, 31f, 344f, 86f), Texture2D.whiteTexture);

        GUI.color = Color.white;
        GUI.Label(new Rect(45f, 38f, 170f, 25f), "Health", labelStyle);
        GUI.Label(new Rect(202f, 38f, 158f, 25f),
            playerHealth.CurrentHealth + " / " + playerHealth.MaxHealth, valueStyle);

        Rect barFrame = new Rect(42f, 77f, 322f, 28f);
        GUI.color = GoldColor;
        GUI.DrawTexture(barFrame, Texture2D.whiteTexture);
        GUI.color = new Color(0.06f, 0.05f, 0.05f, 1f);
        GUI.DrawTexture(new Rect(44f, 79f, 318f, 24f), Texture2D.whiteTexture);

        float healthRatio = playerHealth.MaxHealth > 0
            ? Mathf.Clamp01((float)playerHealth.CurrentHealth / playerHealth.MaxHealth)
            : 0f;
        GUI.color = healthRatio <= 0.25f ? LowHealthColor : HealthColor;
        GUI.DrawTexture(new Rect(44f, 79f, 318f * healthRatio, 24f), Texture2D.whiteTexture);

        GUI.color = previousColor;
        GUI.matrix = previousMatrix;
    }

    private void EnsureStyles()
    {
        if (labelStyle != null)
        {
            return;
        }

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = GoldColor }
        };

        valueStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight,
            normal = { textColor = Color.white }
        };
    }
}
