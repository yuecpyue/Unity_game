using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PokemonArena
{
    public static class ArenaMaterialManager
    {
        private const string MaterialsFolder = "Assets/PokemonArena/Materials";

        public static Shader GetUniversalLitShader()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Diffuse");
            return s;
        }

        public static Material GetOrCreateMaterial(string matName, System.Action<Material> configure)
        {
            string path = $"{MaterialsFolder}/{matName}.mat";
#if UNITY_EDITOR
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                configure?.Invoke(existing);
                EditorUtility.SetDirty(existing);
                return existing;
            }
#endif
            Shader shader = GetUniversalLitShader();
            Material mat = new Material(shader) { name = matName };
            configure?.Invoke(mat);

#if UNITY_EDITOR
            if (!System.IO.Directory.Exists(MaterialsFolder))
            {
                System.IO.Directory.CreateDirectory(MaterialsFolder);
            }
            AssetDatabase.CreateAsset(mat, path);
#endif
            return mat;
        }

        public static Material GetCourtMaterial()
        {
            return GetOrCreateMaterial("Mat_Battlefield_Court", mat =>
            {
                Texture2D tex = ArenaTextureGenerator.GetOrCreateCourtTexture();
                SetTextureAndColor(mat, tex, Color.white);
                SetProperty(mat, "_Smoothness", 0.18f);
                SetProperty(mat, "_Metallic", 0.0f);
            });
        }

        public static Material GetPavementMaterial()
        {
            return GetOrCreateMaterial("Mat_Arena_Pavement", mat =>
            {
                Texture2D tex = ArenaTextureGenerator.GetOrCreatePavementTexture();
                SetTextureAndColor(mat, tex, Color.white);
                SetProperty(mat, "_Smoothness", 0.28f);
                SetProperty(mat, "_Metallic", 0.05f);
                if (mat.HasProperty("_BaseMap")) mat.SetTextureScale("_BaseMap", new Vector2(6f, 10f));
                if (mat.HasProperty("_MainTex")) mat.SetTextureScale("_MainTex", new Vector2(6f, 10f));
            });
        }

        public static Material GetFenceWoodMaterial()
        {
            return GetOrCreateMaterial("Mat_Fence_Wood", mat =>
            {
                Texture2D tex = ArenaTextureGenerator.GetOrCreateWoodTexture();
                SetTextureAndColor(mat, tex, new Color(0.96f, 0.96f, 0.94f));
                SetProperty(mat, "_Smoothness", 0.35f);
                SetProperty(mat, "_Metallic", 0.0f);
            });
        }

        public static Material GetFenceTrimMaterial()
        {
            return GetOrCreateMaterial("Mat_Fence_Trim", mat =>
            {
                Texture2D tex = ArenaTextureGenerator.GetOrCreateWoodTexture();
                SetTextureAndColor(mat, tex, new Color(0.85f, 0.22f, 0.22f)); // Pokéball red accent on fence caps
                SetProperty(mat, "_Smoothness", 0.45f);
                SetProperty(mat, "_Metallic", 0.1f);
            });
        }

        public static Material GetDarkMetalMaterial()
        {
            return GetOrCreateMaterial("Mat_Dark_Metal", mat =>
            {
                Texture2D tex = ArenaTextureGenerator.GetOrCreateMetalTexture();
                SetTextureAndColor(mat, tex, new Color(0.18f, 0.20f, 0.24f));
                SetProperty(mat, "_Smoothness", 0.65f);
                SetProperty(mat, "_Metallic", 0.85f);
            });
        }

        public static Material GetGoldTrimMaterial()
        {
            return GetOrCreateMaterial("Mat_Gold_Trim", mat =>
            {
                SetTextureAndColor(mat, null, new Color(0.95f, 0.75f, 0.22f));
                SetProperty(mat, "_Smoothness", 0.82f);
                SetProperty(mat, "_Metallic", 0.90f);
            });
        }

        public static Material GetTrainerRedMaterial()
        {
            return GetOrCreateMaterial("Mat_Trainer_Red", mat =>
            {
                SetTextureAndColor(mat, null, new Color(0.88f, 0.18f, 0.18f));
                SetProperty(mat, "_Smoothness", 0.45f);
                SetProperty(mat, "_Metallic", 0.1f);
            });
        }

        public static Material GetTrainerBlueMaterial()
        {
            return GetOrCreateMaterial("Mat_Trainer_Blue", mat =>
            {
                SetTextureAndColor(mat, null, new Color(0.16f, 0.48f, 0.88f));
                SetProperty(mat, "_Smoothness", 0.45f);
                SetProperty(mat, "_Metallic", 0.1f);
            });
        }

        public static Material GetEmissiveLightMaterial()
        {
            return GetOrCreateMaterial("Mat_Floodlight_Lens", mat =>
            {
                Color emissionCol = new Color(1.0f, 0.96f, 0.88f) * 3.5f;
                SetTextureAndColor(mat, null, Color.white);
                SetProperty(mat, "_Smoothness", 0.9f);
                SetProperty(mat, "_Metallic", 0.1f);

                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", emissionCol);
                }
            });
        }

        public static Material GetCornerLanternMaterial()
        {
            return GetOrCreateMaterial("Mat_Corner_Lantern", mat =>
            {
                Color cyanGlow = new Color(0.2f, 0.85f, 1.0f) * 2.8f;
                SetTextureAndColor(mat, null, new Color(0.85f, 0.95f, 1f));
                SetProperty(mat, "_Smoothness", 0.95f);
                SetProperty(mat, "_Metallic", 0.05f);

                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", cyanGlow);
                }
            });
        }

        public static Material GetOuterGroundMaterial()
        {
            return GetOrCreateMaterial("Mat_Outer_Ground", mat =>
            {
                SetTextureAndColor(mat, null, new Color(0.20f, 0.42f, 0.18f));
                SetProperty(mat, "_Smoothness", 0.15f);
                SetProperty(mat, "_Metallic", 0.0f);
            });
        }

        private static void SetTextureAndColor(Material mat, Texture2D tex, Color col)
        {
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);
        }

        private static void SetProperty(Material mat, string prop, float val)
        {
            if (mat.HasProperty(prop)) mat.SetFloat(prop, val);
        }
    }
}
