using UnityEngine;

namespace Reversi.Utils
{
    /// <summary>
    /// 스타일 헬퍼 유틸리티
    /// 프로시저럴 스프라이트 및 텍스처 생성 담당
    /// </summary>
    public static class StyleHelper
    {
        private static Texture2D _whiteTexture;

        public static Texture2D WhiteTexture
        {
            get
            {
                if (_whiteTexture == null)
                {
                    _whiteTexture = new Texture2D(1, 1);
                    _whiteTexture.SetPixel(0, 0, Color.white);
                    _whiteTexture.Apply();
                }
                return _whiteTexture;
            }
        }

        public static Texture2D CreateFeltTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];

            Color baseColor = new Color32(40, 110, 60, 255); // 딥 그린
            Color noiseColor = new Color32(50, 130, 70, 255);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.5f +
                                  Mathf.PerlinNoise(x * 0.5f, y * 0.5f) * 0.3f +
                                  Random.Range(-0.05f, 0.05f);

                    pixels[y * width + x] = Color.Lerp(baseColor, noiseColor, noise);
                }
            }

            DrawGridLines(pixels, width, height);

            tex.SetPixels(pixels);
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply();
            return tex;
        }

        private static void DrawGridLines(Color[] pixels, int width, int height)
        {
            int cols = 8;
            int rows = 8;
            int cellW = width / cols;
            int cellH = height / rows;
            Color gridColor = new Color(0, 0, 0, 0.4f);

            for (int i = 1; i < cols; i++)
            {
                int x = i * cellW;
                for (int y = 0; y < height; y++)
                {
                    SetPixelSafe(pixels, width, x, y, gridColor);
                    SetPixelSafe(pixels, width, x + 1, y, gridColor);
                }
            }

            for (int i = 1; i < rows; i++)
            {
                int y = i * cellH;
                for (int x = 0; x < width; x++)
                {
                    SetPixelSafe(pixels, width, x, y, gridColor);
                    SetPixelSafe(pixels, width, x, y + 1, gridColor);
                }
            }

            DrawStarPoint(pixels, width, 2, 2, cellW, cellH);
            DrawStarPoint(pixels, width, 6, 2, cellW, cellH);
            DrawStarPoint(pixels, width, 2, 6, cellW, cellH);
            DrawStarPoint(pixels, width, 6, 6, cellW, cellH);
        }

        private static void DrawStarPoint(Color[] pixels, int width, int col, int row, int cw, int ch)
        {
            int px = col * cw;
            int py = row * ch;
            Color pointColor = new Color(0, 0, 0, 0.8f);
            int radius = 3;
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y <= radius * radius)
                        SetPixelSafe(pixels, width, px + x, py + y, pointColor);
                }
            }
        }

        private static void SetPixelSafe(Color[] pixels, int width, int x, int y, Color c)
        {
            if (x >= 0 && x < width && y >= 0 && y < pixels.Length / width)
            {
                int idx = y * width + x;
                pixels[idx] = Color.Lerp(pixels[idx], c, c.a);
            }
        }

        public static Texture2D CreateWoodTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];
            Color darkWood = new Color32(60, 40, 20, 255);
            Color lightWood = new Color32(100, 70, 40, 255);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float grain = Mathf.PerlinNoise(x * 0.05f, y * 0.005f) +
                                  Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.3f;
                    pixels[y * width + x] = Color.Lerp(darkWood, lightWood, grain);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        public static Sprite CreateRoundedSprite(int width, int height, float radius, Color color)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
            Color[] pixels = new Color[width * height];
            float rSquared = radius * radius;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = 0, dy = 0;
                    bool checkCorner = false;
                    if (x < radius) { dx = radius - x; checkCorner = true; }
                    else if (x > width - radius) { dx = x - (width - radius); checkCorner = true; }
                    if (y < radius) { dy = radius - y; checkCorner = true; }
                    else if (y > height - radius) { dy = y - (height - radius); checkCorner = true; }

                    if (checkCorner && (dx * dx + dy * dy > rSquared + 0.5f))
                        pixels[y * width + x] = Color.clear;
                    else
                        pixels[y * width + x] = color;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        public static Sprite CreateCircleSprite(int size, Color color)
        {
            return CreateRoundedSprite(size, size, size * 0.5f, color);
        }
    }
}
