using UnityEngine;
using UnityEditor;
using System.IO;

public static class SpriteCropper
{
        [MenuItem("Tools/Measure Board Grid")]
    public static void MeasureBoardGrid()
    {
        string path = "Assets/board.png";
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) { Debug.LogError("board.png not found"); return; }

        bool wasReadable = importer.isReadable;
        if (!wasReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        Color32[] pixels = tex.GetPixels32();
        int w = tex.width;
        int h = tex.height;

        // Find the inner green area by scanning for the wooden frame boundary
        // The frame is brown (~RGB 140,100,50), green squares are either light or dark green
        // Scan from left to find where green starts (inner edge of frame)
        int leftEdge = 0, rightEdge = w - 1, bottomEdge = 0, topEdge = h - 1;

        // Scan middle row from left
        int midY = h / 2;
        for (int x = 0; x < w; x++)
        {
            Color32 c = pixels[midY * w + x];
            if (c.g > 80 && c.g > c.r * 1.2f && c.a > 200)
            {
                leftEdge = x;
                break;
            }
        }
        // Scan from right
        for (int x = w - 1; x >= 0; x--)
        {
            Color32 c = pixels[midY * w + x];
            if (c.g > 80 && c.g > c.r * 1.2f && c.a > 200)
            {
                rightEdge = x;
                break;
            }
        }
        // Scan middle col from bottom
        int midX = w / 2;
        for (int y = 0; y < h; y++)
        {
            Color32 c = pixels[y * w + midX];
            if (c.g > 80 && c.g > c.r * 1.2f && c.a > 200)
            {
                bottomEdge = y;
                break;
            }
        }
        // Scan from top
        for (int y = h - 1; y >= 0; y--)
        {
            Color32 c = pixels[y * w + midX];
            if (c.g > 80 && c.g > c.r * 1.2f && c.a > 200)
            {
                topEdge = y;
                break;
            }
        }

        float greenW = rightEdge - leftEdge;
        float greenH = topEdge - bottomEdge;
        float centerX = (leftEdge + rightEdge) / 2f;
        float centerY = (bottomEdge + topEdge) / 2f;
        float imgCenterX = w / 2f;
        float imgCenterY = h / 2f;
        float offsetX = (centerX - imgCenterX) / 100f; // PPU=100
        float offsetY = (centerY - imgCenterY) / 100f;

        Debug.Log($"Board image: {w}x{h}");
        Debug.Log($"Green area: left={leftEdge}, right={rightEdge}, bottom={bottomEdge}, top={topEdge}");
        Debug.Log($"Green size: {greenW}x{greenH} px");
        Debug.Log($"Green size in local units (PPU=100): {greenW/100f}x{greenH/100f}");
        Debug.Log($"Grid offset from center: ({offsetX}, {offsetY})");
        Debug.Log($"Recommended BOARD_SIZE = {greenW/100f}f (width) or {greenH/100f}f (height)");
        Debug.Log($"Cell size = {greenW/100f/8f}f");

        if (!wasReadable)
        {
            importer.isReadable = false;
            importer.SaveAndReimport();
        }
    }

[MenuItem("Tools/Crop Board Sprite")]
    public static void CropBoardSprite()
    {
        string path = "Assets/board.png";
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) { Debug.LogError("board.png not found"); return; }

        // 읽기 가능하도록 설정
        bool wasReadable = importer.isReadable;
        if (!wasReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null) { Debug.LogError("Failed to load texture"); return; }

        int w = tex.width;
        int h = tex.height;
        Color32[] pixels = tex.GetPixels32();

        // BoundingBox 계산
        int minX = w, minY = h, maxX = 0, maxY = 0;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (pixels[y * w + x].a > 10)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        if (maxX <= minX || maxY <= minY) { Debug.LogError("No opaque pixels"); return; }

        int cropW = maxX - minX + 1;
        int cropH = maxY - minY + 1;

        Texture2D cropped = new Texture2D(cropW, cropH, TextureFormat.RGBA32, false);
        Color[] croppedPixels = tex.GetPixels(minX, minY, cropW, cropH);
        cropped.SetPixels(croppedPixels);
        cropped.Apply();

        byte[] pngData = cropped.EncodeToPNG();
        string fullPath = Path.GetFullPath(path);
        File.WriteAllBytes(fullPath, pngData);

        // 원복
        if (!wasReadable)
        {
            importer.isReadable = false;
        }
        importer.SaveAndReimport();

        Object.DestroyImmediate(cropped);
        Debug.Log($"Board cropped! Original: {w}x{h}, Cropped: {cropW}x{cropH} (from [{minX},{minY}])");
        AssetDatabase.Refresh();
    }
}
