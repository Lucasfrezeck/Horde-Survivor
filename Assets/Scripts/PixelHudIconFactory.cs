using System.Collections.Generic;
using UnityEngine;

internal static class PixelHudIconFactory
{
    public static Texture2D Create(string[] rows, IDictionary<char, Color32> palette)
    {
        int width = 0;
        foreach (string row in rows)
        {
            width = Mathf.Max(width, row.Length);
        }

        int height = rows.Length;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "PixelHUDIcon",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave
        };

        Color32[] pixels = new Color32[width * height];
        for (int rowIndex = 0; rowIndex < height; rowIndex++)
        {
            string row = rows[rowIndex];
            int horizontalOffset = (width - row.Length) / 2;
            int y = height - 1 - rowIndex;

            for (int x = 0; x < row.Length; x++)
            {
                if (palette.TryGetValue(row[x], out Color32 color))
                {
                    pixels[y * width + horizontalOffset + x] = color;
                }
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }
}
