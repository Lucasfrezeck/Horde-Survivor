using System.Collections.Generic;
using UnityEngine;

public class KillCounterHUD : MonoBehaviour
{
    private static readonly Color FrameColor = new Color(0.66f, 0.12f, 0.14f, 1f);
    private static readonly Color PanelColor = new Color(0.035f, 0.03f, 0.03f, 0.97f);

    private int killCount;
    private Texture2D skullTexture;
    private GUIStyle countStyle;

    private void OnEnable()
    {
        EnemyHealth.EnemyKilled += RegisterKill;

        if (skullTexture == null)
        {
            skullTexture = PixelHudIconFactory.Create(
                new[]
                {
                    ".....DDDDDDDD.....",
                    "...DDHHHHHHHHDD...",
                    "..DHBBBBBBBBBBHD..",
                    ".DHBBBBBBBBBBBBHD.",
                    "DHBBBBSSBBBBSSBBHD",
                    "DHBBBBBBBBBBBBBBHD",
                    "DHBBBDDDDBBBBDDBBHD",
                    "DHBDDDDDDBBDDDDDDBHD",
                    "DHBDDDDDDBBDDDDDDBHD",
                    "DHBDDDDDDBBDDDDDDBHD",
                    "DHBBBBBBBDDBBBBBBHD",
                    "DHBBBBBBDDDDBBBBBHD",
                    "DHBBBBBBBDDBBBBBBHD",
                    "DHBBBBBBBBBBBBBBHD",
                    ".DHBBBBBBBBBBBBHD.",
                    "..DHBBBBBBBBBBHD..",
                    "...DHBBBBBBBBHD...",
                    "....DHBBBBBBHD....",
                    ".....DHBBBBHD.....",
                    "......DBDDBD......",
                    "......DD..DD......",
                    ".......DDDD.......",
                    "..................",
                    ".................."
                },
                new Dictionary<char, Color32>
                {
                    ['D'] = new Color32(35, 28, 27, 255),
                    ['B'] = new Color32(220, 211, 188, 255),
                    ['H'] = new Color32(255, 248, 229, 255),
                    ['S'] = new Color32(168, 156, 136, 255)
                });
        }
    }

    private void OnDisable()
    {
        EnemyHealth.EnemyKilled -= RegisterKill;
    }

    private void OnGUI()
    {
        EnsureStyles();

        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;
        float scale = Mathf.Clamp(Screen.height / 1080f, 0.7f, 1.4f);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);

        const float panelWidth = 178f;
        const float panelHeight = 60f;
        Rect panelRect = new Rect(Screen.width / scale - panelWidth, 0f, panelWidth, panelHeight);

        GUI.color = FrameColor;
        GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
        GUI.color = PanelColor;
        GUI.DrawTexture(new Rect(panelRect.x + 3f, panelRect.y + 3f, panelWidth - 6f, panelHeight - 6f),
            Texture2D.whiteTexture);

        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(panelRect.x + 12f, panelRect.y + 12f, 30f, 36f), skullTexture);
        GUI.Label(new Rect(panelRect.x + 52f, panelRect.y + 8f, 114f, 44f), killCount.ToString("000"), countStyle);

        GUI.color = previousColor;
        GUI.matrix = previousMatrix;
    }

    private void RegisterKill()
    {
        killCount++;
    }

    private void EnsureStyles()
    {
        if (countStyle != null)
        {
            return;
        }

        countStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 30,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
    }

    private void OnDestroy()
    {
        if (skullTexture != null)
        {
            Destroy(skullTexture);
        }
    }
}
