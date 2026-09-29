using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PokemonArena
{
    public static class ArenaTextureGenerator
    {
        private const string TexturesFolder = "Assets/PokemonArena/Textures";

        public static Texture2D GetOrCreateCourtTexture(int width = 2048, int height = 2048)
        {
            string path = $"{TexturesFolder}/Battlefield_Court.png";
#if UNITY_EDITOR
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
#endif
            Texture2D tex = GenerateCourtTexture(width, height);
            SaveTexture(tex, path);
            return tex;
        }

        public static Texture2D GetOrCreatePavementTexture(int size = 1024)
        {
            string path = $"{TexturesFolder}/Arena_Pavement.png";
#if UNITY_EDITOR
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
#endif
            Texture2D tex = GeneratePavementTexture(size);
            SaveTexture(tex, path);
            return tex;
        }

        public static Texture2D GetOrCreateWoodTexture(int size = 512)
        {
            string path = $"{TexturesFolder}/Arena_FenceWood.png";
#if UNITY_EDITOR
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
#endif
            Texture2D tex = GenerateWoodTexture(size);
            SaveTexture(tex, path);
            return tex;
        }

        public static Texture2D GetOrCreateMetalTexture(int size = 512)
        {
            string path = $"{TexturesFolder}/Arena_MetalDark.png";
#if UNITY_EDITOR
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
#endif
            Texture2D tex = GenerateMetalTexture(size);
            SaveTexture(tex, path);
            return tex;
        }

        public static Texture2D GetOrCreatePokeBallEmblem(int size = 512)
        {
            string path = $"{TexturesFolder}/PokeBall_Emblem.png";
#if UNITY_EDITOR
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
#endif
            Texture2D tex = GeneratePokeBallEmblem(size);
            SaveTexture(tex, path);
            return tex;
        }

        public static void GenerateAllTextures(bool forceOverwrite = false)
        {
            EnsureDirectoryExists(TexturesFolder);

            if (forceOverwrite || !File.Exists($"{TexturesFolder}/Battlefield_Court.png"))
                SaveTexture(GenerateCourtTexture(2048, 2048), $"{TexturesFolder}/Battlefield_Court.png");

            if (forceOverwrite || !File.Exists($"{TexturesFolder}/Arena_Pavement.png"))
                SaveTexture(GeneratePavementTexture(1024), $"{TexturesFolder}/Arena_Pavement.png");

            if (forceOverwrite || !File.Exists($"{TexturesFolder}/Arena_FenceWood.png"))
                SaveTexture(GenerateWoodTexture(512), $"{TexturesFolder}/Arena_FenceWood.png");

            if (forceOverwrite || !File.Exists($"{TexturesFolder}/Arena_MetalDark.png"))
                SaveTexture(GenerateMetalTexture(512), $"{TexturesFolder}/Arena_MetalDark.png");

            if (forceOverwrite || !File.Exists($"{TexturesFolder}/PokeBall_Emblem.png"))
                SaveTexture(GeneratePokeBallEmblem(512), $"{TexturesFolder}/PokeBall_Emblem.png");

#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }

        private static void EnsureDirectoryExists(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        private static void SaveTexture(Texture2D tex, string path)
        {
            EnsureDirectoryExists(Path.GetDirectoryName(path));
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);

#if UNITY_EDITOR
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.alphaIsTransparency = path.Contains("Emblem");
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.anisoLevel = 8;
                importer.SaveAndReimport();
            }
#endif
        }

        #region Battlefield Court Texture Generation
        public static Texture2D GenerateCourtTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            Color32[] pixels = new Color32[width * height];

            // Color Palette
            Color turfDark = new Color(0.24f, 0.58f, 0.22f);     // Deep stadium turf green
            Color turfLight = new Color(0.31f, 0.69f, 0.27f);    // Sunlit turf green
            Color turfBorder = new Color(0.18f, 0.45f, 0.16f);   // Subtle apron transition
            Color whiteChalk = new Color(0.96f, 0.97f, 0.98f);   // Clean crisp line white
            Color pokeballRed = new Color(0.88f, 0.16f, 0.16f);  // Pokémon Red
            Color pokeballWhite = new Color(0.95f, 0.95f, 0.95f);// Pokémon White
            Color darkTrim = new Color(0.13f, 0.14f, 0.16f);     // Dark charcoal
            Color ringRed = new Color(0.92f, 0.24f, 0.20f);      // Trainer 1 battle ring
            Color ringBlue = new Color(0.16f, 0.52f, 0.88f);     // Trainer 2 battle ring

            float cx = width * 0.5f;
            float cy = height * 0.5f;

            // Dimensions in pixel coords
            float courtMarginX = width * 0.08f;
            float courtMarginY = height * 0.08f;
            float courtMinX = courtMarginX;
            float courtMaxX = width - courtMarginX;
            float courtMinY = courtMarginY;
            float courtMaxY = height - courtMarginY;
            float lineWidth = width * 0.008f; // ~16 pixels at 2048

            // Pokéball center specs
            float pokeballRadius = width * 0.20f;
            float pokeballBorder = width * 0.015f;
            float buttonOuterRadius = width * 0.060f;
            float buttonMidRadius = width * 0.045f;
            float buttonInnerRadius = width * 0.025f;

            // Combatant rings (Trainer 1 at Y ~0.28, Trainer 2 at Y ~0.72)
            float ring1Y = height * 0.28f;
            float ring2Y = height * 0.72f;
            float combatRingRadius = width * 0.10f;
            float combatRingThickness = width * 0.009f;

            int stripeCount = 14;
            float stripeHeight = (courtMaxY - courtMinY) / stripeCount;

            for (int y = 0; y < height; y++)
            {
                int yIdx = y * width;
                float ny = (float)y / height;

                for (int x = 0; x < width; x++)
                {
                    float nx = (float)x / width;

                    // 1. Base Lawn Mow Stripe Pattern
                    int stripeIdx = Mathf.FloorToInt((y - courtMinY) / stripeHeight);
                    bool isEvenStripe = (stripeIdx % 2 == 0);
                    Color baseCol = isEvenStripe ? turfDark : turfLight;

                    // Subtle fine procedural noise for grass texture
                    float noise = Mathf.PerlinNoise(nx * 120f, ny * 120f) * 0.10f - 0.05f;
                    float checker = Mathf.PerlinNoise(nx * 20f, ny * 20f) * 0.06f - 0.03f;
                    Color pixel = new Color(
                        Mathf.Clamp01(baseCol.r + noise + checker),
                        Mathf.Clamp01(baseCol.g + noise * 1.2f + checker),
                        Mathf.Clamp01(baseCol.b + noise + checker),
                        1f
                    );

                    // 2. Outer Court Border Line
                    bool onBorderX = (Mathf.Abs(x - courtMinX) <= lineWidth * 0.5f || Mathf.Abs(x - courtMaxX) <= lineWidth * 0.5f) && (y >= courtMinY && y <= courtMaxY);
                    bool onBorderY = (Mathf.Abs(y - courtMinY) <= lineWidth * 0.5f || Mathf.Abs(y - courtMaxY) <= lineWidth * 0.5f) && (x >= courtMinX && x <= courtMaxX);
                    if (onBorderX || onBorderY)
                    {
                        pixel = Color.Lerp(pixel, whiteChalk, 0.95f);
                    }

                    // 3. Center Dividing Line
                    if (Mathf.Abs(y - cy) <= lineWidth * 0.5f && (x >= courtMinX && x <= courtMaxX))
                    {
                        pixel = Color.Lerp(pixel, whiteChalk, 0.95f);
                    }

                    // 4. Combatant Stand Ring 1 (Trainer 1 / South Side)
                    float distRing1 = Mathf.Sqrt((x - cx) * (x - cx) + (y - ring1Y) * (y - ring1Y));
                    if (Mathf.Abs(distRing1 - combatRingRadius) <= combatRingThickness * 0.5f)
                    {
                        pixel = Color.Lerp(pixel, ringRed, 0.92f);
                    }
                    else if (distRing1 < combatRingRadius && distRing1 > combatRingRadius * 0.85f)
                    {
                        pixel = Color.Lerp(pixel, ringRed, 0.25f);
                    }
                    // Inner dot
                    if (distRing1 <= lineWidth * 0.8f)
                    {
                        pixel = ringRed;
                    }

                    // 5. Combatant Stand Ring 2 (Trainer 2 / North Side)
                    float distRing2 = Mathf.Sqrt((x - cx) * (x - cx) + (y - ring2Y) * (y - ring2Y));
                    if (Mathf.Abs(distRing2 - combatRingRadius) <= combatRingThickness * 0.5f)
                    {
                        pixel = Color.Lerp(pixel, ringBlue, 0.92f);
                    }
                    else if (distRing2 < combatRingRadius && distRing2 > combatRingRadius * 0.85f)
                    {
                        pixel = Color.Lerp(pixel, ringBlue, 0.25f);
                    }
                    // Inner dot
                    if (distRing2 <= lineWidth * 0.8f)
                    {
                        pixel = ringBlue;
                    }

                    // 6. Central Poké Ball Decal
                    float distCenter = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (distCenter <= pokeballRadius)
                    {
                        // Outer black rim of Pokéball
                        if (distCenter >= pokeballRadius - pokeballBorder)
                        {
                            pixel = darkTrim;
                        }
                        else
                        {
                            // Central horizontal dividing stripe
                            float dy = y - cy;
                            if (Mathf.Abs(dy) <= pokeballBorder * 0.7f)
                            {
                                pixel = darkTrim;
                            }
                            else if (dy > 0)
                            {
                                // Top Half: Red with smooth radial gradient
                                float redShade = 1.0f - (distCenter / pokeballRadius) * 0.12f;
                                pixel = new Color(pokeballRed.r * redShade, pokeballRed.g * redShade, pokeballRed.b * redShade, 1f);
                            }
                            else
                            {
                                // Bottom Half: White
                                float whiteShade = 0.96f + (distCenter / pokeballRadius) * 0.04f;
                                pixel = new Color(pokeballWhite.r * whiteShade, pokeballWhite.g * whiteShade, pokeballWhite.b * whiteShade, 1f);
                            }
                        }

                        // Central Button
                        if (distCenter <= buttonOuterRadius)
                        {
                            if (distCenter >= buttonMidRadius)
                            {
                                pixel = darkTrim;
                            }
                            else if (distCenter >= buttonInnerRadius)
                            {
                                pixel = pokeballWhite;
                            }
                            else
                            {
                                // Inner metallic center button
                                pixel = new Color(0.85f, 0.88f, 0.92f, 1f);
                            }
                        }
                    }

                    // 7. Outer apron vignette / edge fade
                    if (x < courtMinX - 10 || x > courtMaxX + 10 || y < courtMinY - 10 || y > courtMaxY + 10)
                    {
                        pixel = Color.Lerp(pixel, turfBorder, 0.4f);
                    }

                    pixels[yIdx + x] = pixel;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(true);
            return tex;
        }
        #endregion

        #region Pavement Texture Generation
        public static Texture2D GeneratePavementTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color32[] pixels = new Color32[size * size];

            int tileSize = size / 8;
            int mortarWidth = Mathf.Max(2, size / 128);

            Color tileBase = new Color(0.76f, 0.77f, 0.80f);
            Color mortar = new Color(0.42f, 0.44f, 0.47f);

            for (int y = 0; y < size; y++)
            {
                int yIdx = y * size;
                int tileY = y / tileSize;
                int localY = y % tileSize;

                // Offset every other row like running bond brick/stone
                int xOffset = (tileY % 2 == 1) ? tileSize / 2 : 0;

                for (int x = 0; x < size; x++)
                {
                    int adjX = (x + xOffset) % size;
                    int tileX = adjX / tileSize;
                    int localX = adjX % tileSize;

                    // Tile variation
                    float tileHash = Mathf.Sin(tileX * 12.9898f + tileY * 78.233f) * 43758.5453f;
                    float variation = (tileHash - Mathf.Floor(tileHash)) * 0.12f - 0.06f;

                    // Mortar check
                    bool isMortar = (localX < mortarWidth || localX > tileSize - mortarWidth ||
                                     localY < mortarWidth || localY > tileSize - mortarWidth);

                    Color col;
                    if (isMortar)
                    {
                        col = mortar;
                    }
                    else
                    {
                        // Surface grain
                        float grain = Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 0.06f - 0.03f;
                        // Bevel edge highlight
                        float edgeDist = Mathf.Min(Mathf.Min(localX, tileSize - localX), Mathf.Min(localY, tileSize - localY));
                        float bevel = Mathf.Clamp01((edgeDist - mortarWidth) / 8f);

                        col = new Color(
                            Mathf.Clamp01((tileBase.r + variation + grain) * (0.85f + 0.15f * bevel)),
                            Mathf.Clamp01((tileBase.g + variation + grain) * (0.85f + 0.15f * bevel)),
                            Mathf.Clamp01((tileBase.b + variation + grain) * (0.85f + 0.15f * bevel)),
                            1f
                        );
                    }

                    pixels[yIdx + x] = col;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(true);
            return tex;
        }
        #endregion

        #region Fence Wood Texture Generation
        public static Texture2D GenerateWoodTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color32[] pixels = new Color32[size * size];

            Color woodBase = new Color(0.94f, 0.94f, 0.92f); // Clean painted white/cream
            Color woodDark = new Color(0.82f, 0.82f, 0.80f);

            for (int y = 0; y < size; y++)
            {
                int yIdx = y * size;
                for (int x = 0; x < size; x++)
                {
                    // Vertical wood grain lines
                    float grain = Mathf.PerlinNoise(x * 0.08f, y * 0.02f) * 0.08f;
                    float streak = Mathf.Sin(x * 0.4f) * 0.02f;
                    Color col = Color.Lerp(woodBase, woodDark, grain + streak);
                    pixels[yIdx + x] = col;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(true);
            return tex;
        }
        #endregion

        #region Metal Dark Texture Generation
        public static Texture2D GenerateMetalTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color32[] pixels = new Color32[size * size];

            Color metalBase = new Color(0.18f, 0.20f, 0.24f);

            for (int y = 0; y < size; y++)
            {
                int yIdx = y * size;
                for (int x = 0; x < size; x++)
                {
                    float noise = Mathf.PerlinNoise(x * 0.2f, y * 0.2f) * 0.05f - 0.025f;
                    pixels[yIdx + x] = new Color(
                        metalBase.r + noise,
                        metalBase.g + noise,
                        metalBase.b + noise,
                        1f
                    );
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(true);
            return tex;
        }
        #endregion

        #region Standalone PokeBall Emblem (with Alpha)
        public static Texture2D GeneratePokeBallEmblem(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color32[] pixels = new Color32[size * size];

            float cx = size * 0.5f;
            float cy = size * 0.5f;
            float radius = size * 0.46f;
            float border = size * 0.06f;
            float band = size * 0.05f;
            float btnOuter = size * 0.16f;
            float btnMid = size * 0.12f;
            float btnInner = size * 0.06f;

            Color red = new Color(0.90f, 0.14f, 0.14f, 1f);
            Color white = new Color(0.96f, 0.96f, 0.96f, 1f);
            Color black = new Color(0.12f, 0.12f, 0.14f, 1f);
            Color clear = new Color(0f, 0f, 0f, 0f);

            for (int y = 0; y < size; y++)
            {
                int yIdx = y * size;
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    Color col = clear;

                    if (dist <= radius)
                    {
                        // Antialias outer border
                        float alpha = Mathf.Clamp01(radius - dist + 0.5f);

                        if (dist >= radius - border)
                        {
                            col = black;
                        }
                        else
                        {
                            float dy = y - cy;
                            if (Mathf.Abs(dy) <= band * 0.5f)
                            {
                                col = black;
                            }
                            else if (dy > 0)
                            {
                                col = red;
                            }
                            else
                            {
                                col = white;
                            }
                        }

                        // Button
                        if (dist <= btnOuter)
                        {
                            if (dist >= btnMid) col = black;
                            else if (dist >= btnInner) col = white;
                            else col = new Color(0.85f, 0.88f, 0.92f, 1f);
                        }

                        col.a *= alpha;
                    }

                    pixels[yIdx + x] = col;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(true);
            return tex;
        }
        #endregion
    }
}
