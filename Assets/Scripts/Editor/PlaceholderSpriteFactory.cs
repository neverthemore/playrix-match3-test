#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Match3.EditorTools
{
    public static class PlaceholderSpriteFactory
    {
        private const int TextureSize = 128;
        public const string SpriteFolder = "Assets/Sprites";

        public static Sprite GetOrCreate(string name, System.Func<Texture2D> painter)
        {
            string path = $"{SpriteFolder}/{name}.png";

            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
            {
                return existing;
            }

            Directory.CreateDirectory(SpriteFolder);

            var texture = painter();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ConfigureImporter(path);

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void ConfigureImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = TextureSize;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }


        public static Texture2D PaintChip(Color color)
        {
            var texture = NewTexture();
            float radius = TextureSize * 0.44f;
            var center = new Vector2(TextureSize * 0.5f, TextureSize * 0.5f);

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);

                    float alpha = Mathf.Clamp01(radius - distance);
                    if (alpha <= 0f)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float shade = Mathf.InverseLerp(TextureSize, 0f, x * 0.5f + (TextureSize - y) * 0.5f);
                    var pixel = Color.Lerp(color, Color.white, shade * 0.35f);

                    float rim = Mathf.InverseLerp(radius * 0.72f, radius, distance);
                    pixel = Color.Lerp(pixel, color * 0.55f, rim * 0.8f);

                    pixel.a = alpha;
                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply();
            return texture;
        }

        public static Texture2D PaintRoundedSquare(Color fill, Color border, float inset, float cornerRadius, float borderWidth)
        {
            var texture = NewTexture();
            float min = TextureSize * inset;
            float max = TextureSize * (1f - inset);
            float radius = TextureSize * cornerRadius;

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float distance = RoundedBoxDistance(x + 0.5f, y + 0.5f, min, max, radius);

                    float alpha = Mathf.Clamp01(-distance);
                    if (alpha <= 0f)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float borderBlend = Mathf.Clamp01((distance + TextureSize * borderWidth) / (TextureSize * borderWidth));
                    var pixel = Color.Lerp(fill, border, borderBlend);
                    pixel.a = alpha * fill.a;
                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply();
            return texture;
        }

        private static float RoundedBoxDistance(float x, float y, float min, float max, float radius)
        {
            float innerMin = min + radius;
            float innerMax = max - radius;

            float dx = Mathf.Max(innerMin - x, 0f, x - innerMax);
            float dy = Mathf.Max(innerMin - y, 0f, y - innerMax);

            return Mathf.Sqrt(dx * dx + dy * dy) - radius;
        }

        public static Texture2D PaintLineArrow(bool horizontal)
        {
            var texture = NewTexture();
            float half = TextureSize * 0.5f;
            float thickness = TextureSize * 0.10f;

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float across = horizontal ? Mathf.Abs(y + 0.5f - half) : Mathf.Abs(x + 0.5f - half);
                    float along = horizontal ? x + 0.5f : y + 0.5f;

                    float offset = TextureSize * 0.16f;
                    float nearBar = Mathf.Min(Mathf.Abs(across - offset), Mathf.Abs(across + offset));
                    float alpha = Mathf.Clamp01(thickness * 0.5f - nearBar);

                    float edgeFade = Mathf.Clamp01(Mathf.Min(along, TextureSize - along) / (TextureSize * 0.18f));
                    alpha *= edgeFade;

                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }

        public static Texture2D PaintBurst(int points, Color color)
        {
            var texture = NewTexture();
            var center = new Vector2(TextureSize * 0.5f, TextureSize * 0.5f);
            float outer = TextureSize * 0.40f;
            float inner = TextureSize * 0.17f;

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    var offset = new Vector2(x + 0.5f, y + 0.5f) - center;
                    float distance = offset.magnitude;
                    float angle = Mathf.Atan2(offset.y, offset.x);

                    float wave = (Mathf.Cos(angle * points) + 1f) * 0.5f;
                    float radius = Mathf.Lerp(inner, outer, wave);

                    float alpha = Mathf.Clamp01(radius - distance);
                    var pixel = color;
                    pixel.a = alpha;
                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply();
            return texture;
        }

        public static Texture2D PaintTriangle(Color color)
        {
            var texture = NewTexture();

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float u = (x + 0.5f) / TextureSize;
                    float v = (y + 0.5f) / TextureSize;

                    float halfHeight = Mathf.Lerp(0.30f, 0f, Mathf.InverseLerp(0.18f, 0.86f, u));
                    bool inside = u >= 0.18f && u <= 0.86f && Mathf.Abs(v - 0.5f) <= halfHeight;

                    var pixel = color;
                    pixel.a = inside ? 1f : 0f;
                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply();
            return texture;
        }

        public static Texture2D PaintRainbowBall()
        {
            var texture = NewTexture();
            var center = new Vector2(TextureSize * 0.5f, TextureSize * 0.5f);
            float radius = TextureSize * 0.44f;

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    var offset = new Vector2(x + 0.5f, y + 0.5f) - center;
                    float distance = offset.magnitude;

                    float alpha = Mathf.Clamp01(radius - distance);
                    if (alpha <= 0f)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float hue = (Mathf.Atan2(offset.y, offset.x) / (Mathf.PI * 2f) + 1f) % 1f;
                    float saturation = Mathf.Lerp(0.25f, 0.95f, Mathf.Clamp01(distance / radius));
                    var pixel = Color.HSVToRGB(hue, saturation, 1f);
                    pixel.a = alpha;
                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply();
            return texture;
        }

        public static Texture2D PaintCracks()
        {
            var texture = NewTexture();

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float u = (x + 0.5f) / TextureSize;
                    float v = (y + 0.5f) / TextureSize;

                    float first = Mathf.Abs((u - 0.2f) - (v - 0.15f) * 0.75f);
                    float second = Mathf.Abs((1f - u - 0.25f) - (v - 0.3f) * 0.55f);
                    float nearest = Mathf.Min(first, second);

                    float alpha = Mathf.Clamp01((0.035f - nearest) / 0.035f);

                    bool insideBox = u > 0.12f && u < 0.88f && v > 0.12f && v < 0.88f;
                    if (!insideBox) alpha = 0f;

                    texture.SetPixel(x, y, new Color(0.08f, 0.06f, 0.05f, alpha * 0.85f));
                }
            }

            texture.Apply();
            return texture;
        }

        public static Texture2D PaintGlow()
        {
            var texture = NewTexture();
            var center = new Vector2(TextureSize * 0.5f, TextureSize * 0.5f);
            float radius = TextureSize * 0.5f;

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float normalized = Mathf.Clamp01(1f - distance / radius);

                    float alpha = normalized * normalized;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }

        public static Texture2D PaintCheckmark(Color color)
        {
            var texture = NewTexture();

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    var p = new Vector2((x + 0.5f) / TextureSize, (y + 0.5f) / TextureSize);

                    float shortStroke = DistanceToSegment(p, new Vector2(0.22f, 0.52f), new Vector2(0.43f, 0.30f));
                    float longStroke = DistanceToSegment(p, new Vector2(0.43f, 0.30f), new Vector2(0.80f, 0.72f));
                    float distance = Mathf.Min(shortStroke, longStroke);

                    float alpha = Mathf.Clamp01((0.075f - distance) / 0.02f);
                    var pixel = color;
                    pixel.a = alpha;
                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply();
            return texture;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(point, a + ab * t);
        }

        public static Texture2D PaintUiPanel()
        {
            return PaintRoundedSquare(Color.white, Color.white, 0.02f, 0.14f, 0.001f);
        }

        private static Texture2D NewTexture()
        {
            return new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        }
    }
}
#endif
