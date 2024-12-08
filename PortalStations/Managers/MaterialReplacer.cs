using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Managers
{
    [PublicAPI]
    public static class MaterialReplacer
    {
        private static readonly Dictionary<GameObject, bool> ObjectToSwap;
        private static readonly Dictionary<string, Material> OriginalMaterials;
        private static readonly Dictionary<GameObject, ShaderType> ObjectsForShaderReplace;
        private static readonly HashSet<Shader> CachedShaders = new();
        private static readonly List<MaterialData> m_materials = new();
        private static readonly Dictionary<Material, string> MaterialsToReplace = new();
        private static bool hasRun = false;

        public static void SwapMaterial(AssetBundle assetBundle, string materialName, string original)
        {
            if (assetBundle.LoadAsset<Material>(materialName) is { } material)
            {
                MaterialsToReplace[material] = original;
            }
            else
            {
                Debug.LogWarning(materialName + " is null");
            }
        }

        static MaterialReplacer()
        {
            OriginalMaterials = new Dictionary<string, Material>();
            ObjectToSwap = new Dictionary<GameObject, bool>();
            ObjectsForShaderReplace = new Dictionary<GameObject, ShaderType>();
            Harmony harmony = new Harmony("org.bepinex.helpers.PieceManager");
            harmony.Patch(AccessTools.DeclaredMethod(typeof(ZoneSystem), nameof(ZoneSystem.Start)), postfix: new HarmonyMethod(typeof(MaterialReplacer), nameof(ReplaceAllMaterialsWithOriginal)));
        }

        public enum ShaderType
        {
            PieceShader,
            VegetationShader,
            RockShader,
            RugShader,
            GrassShader,
            CustomCreature,
            UseUnityShader
        }

        public static void RegisterGameObjectForShaderSwap(GameObject go, ShaderType type)
        {
            if (!ObjectsForShaderReplace.ContainsKey(go))
            {
                ObjectsForShaderReplace.Add(go, type);
            }
        }

        public static void RegisterGameObjectForMatSwap(GameObject go, bool isJotunnMock = false)
        {
            if (!ObjectToSwap.ContainsKey(go))
            {
                ObjectToSwap.Add(go, isJotunnMock);
            }
        }

        private static void GetAllMaterials()
        {
            foreach (var material in Resources.FindObjectsOfTypeAll<Material>())
            {
                OriginalMaterials[material.name] = material;
            }
        }

        [HarmonyPriority(Priority.VeryHigh)]
        private static void ReplaceAllMaterialsWithOriginal()
        {
            if (UnityEngine.SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null || hasRun) return;

            if (OriginalMaterials.Count == 0) GetAllMaterials();

            foreach (var kvp in ObjectToSwap)
            {
                var go = kvp.Key;
                var isJotunnMock = kvp.Value;
                ProcessGameObjectMaterials(go, isJotunnMock);
            }

            // Get all assetbundles and find the shaders in them
            var assetBundles = Resources.FindObjectsOfTypeAll<AssetBundle>();
            foreach (var bundle in assetBundles)
            {
                IEnumerable<Shader>? bundleShaders;
                try
                {
                    bundleShaders = bundle.isStreamedSceneAssetBundle && bundle
                        ? bundle.GetAllAssetNames().Select(bundle.LoadAsset<Shader>).Where(shader => shader != null)
                        : bundle.LoadAllAssets<Shader>();
                }
                catch (Exception)
                {
                    continue;
                }

                if (bundleShaders == null) continue;
                foreach (var shader in bundleShaders)
                {
                    CachedShaders.Add(shader);
                }
            }

            foreach (var kvp in ObjectsForShaderReplace)
            {
                var go = kvp.Key;
                var shaderType = kvp.Value;
                ProcessGameObjectShaders(go, shaderType);
            }

            ProcessMaterials();
            

            hasRun = true;
        }

        public static void ProcessGameObjectMaterials(GameObject go, bool isJotunnMock)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                var newMaterials = renderer.sharedMaterials.Select(material => ReplaceMaterial(material, isJotunnMock)).ToArray();
                renderer.sharedMaterials = newMaterials;
            }
        }

        private static Material ReplaceMaterial(Material originalMaterial, bool isJotunnMock)
        {
            string replacementPrefix = isJotunnMock ? "JVLmock_" : "_REPLACE_";
            if (!originalMaterial.name.StartsWith(replacementPrefix, StringComparison.Ordinal))
            {
                return originalMaterial;
            }

            string cleanName = originalMaterial.name.Replace(" (Instance)", "").Replace(replacementPrefix, "");
            if (OriginalMaterials.TryGetValue(cleanName, out var replacementMaterial))
            {
                return replacementMaterial;
            }

            Debug.LogWarning($"No suitable material found to replace: {cleanName}");
            return originalMaterial;
        }

        private static void ProcessMaterials()
        {
            foreach (MaterialData data in m_materials) data.Process();

            foreach (var kvp in MaterialsToReplace)
            {
                if (!OriginalMaterials.TryGetValue(kvp.Value, out Material original)) continue;
                kvp.Key.shader = original.shader;
                if (kvp.Key.shader.name == "Custom/Water")
                {
                    if (kvp.Key.HasProperty("_ColorTop")) kvp.Key.SetColor("_ColorTop", original.GetColor("_ColorTop"));
                }
            }
        }

        private static void ProcessGameObjectShaders(GameObject go, ShaderType shaderType)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material != null)
                    {
                        material.shader = GetShaderForType(material.shader, shaderType, material.shader.name);
                        if (material.HasProperty("_ValueNoise")) material.SetFloat("_ValueNoise", 0f);
                    }
                }
            }
        }

        private static Shader GetShaderForType(Shader orig, ShaderType shaderType, string originalShaderName)
        {
            switch (shaderType)
            {
                case ShaderType.PieceShader: return FindShaderWithName(orig, "Custom/Piece");
                case ShaderType.VegetationShader: return FindShaderWithName(orig, "Custom/Vegetation");
                case ShaderType.RockShader: return FindShaderWithName(orig, "Custom/StaticRock");
                case ShaderType.RugShader: return FindShaderWithName(orig, "Custom/Rug");
                case ShaderType.GrassShader: return FindShaderWithName(orig, "Custom/Grass");
                case ShaderType.CustomCreature: return FindShaderWithName(orig, "Custom/Creature");
                case ShaderType.UseUnityShader: return FindShaderWithName(orig, FindShaderWithName(orig, originalShaderName) != null ? originalShaderName : "ToonDeferredShading2017");
                default: return FindShaderWithName(orig, "Standard");
            }
        }

        public static Shader FindShaderWithName(Shader origShader, string name)
        {
            foreach (var shader in CachedShaders)
            {
                if (shader.name == name)
                {
                    return shader;
                }
            }


            return origShader;
        }
        
        

        public class MaterialData
        {
            public GameObject? PrefabToModify;
            private readonly Material m_material = null!;
            private readonly ShaderType m_type;
            public readonly Dictionary<string, float> m_floatProperties = new();
            public readonly Dictionary<string, Texture> m_texProperties = new();
            public readonly Dictionary<string, Color> m_colorProperties = new();
            public MaterialData(AssetBundle assetBundle, string materialName, ShaderType type)
            {
                if (assetBundle.LoadAsset<Material>(materialName) is { } material)
                {
                    m_material = material;
                    m_type = type;
                    m_materials.Add(this);
                }
                else
                {
                    Debug.LogWarning(materialName + " is null");
                }
            }

            public void Process()
            {
                m_material.shader = GetShaderForType(m_material.shader, m_type, m_material.shader.name);
                foreach (KeyValuePair<string, float> kvp in m_floatProperties)
                {
                    if (m_material.HasProperty(kvp.Key)) m_material.SetFloat(kvp.Key, kvp.Value);
                }

                foreach (var kvp in m_texProperties)
                {
                    if (m_material.HasProperty(kvp.Key)) m_material.SetTexture(kvp.Key, kvp.Value);
                }

                foreach (var kvp in m_colorProperties)
                {
                    if (m_material.HasProperty(kvp.Key)) m_material.SetColor(kvp.Key, kvp.Value);
                }

                if (PrefabToModify is { } prefab)
                {
                    foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
                    {
                        List<Material> materials = new(){m_material};
                        renderer.sharedMaterials = materials.ToArray();
                        renderer.materials = materials.ToArray();
                    }
                }
            }
        }
    }
}