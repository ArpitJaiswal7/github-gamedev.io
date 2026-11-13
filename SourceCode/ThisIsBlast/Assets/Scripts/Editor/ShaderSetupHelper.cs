#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ThisIsBlast.Editor
{
    [InitializeOnLoad]
    public static class ShaderSetupHelper
    {
        static ShaderSetupHelper()
        {
            EnsureMaterialsAndShaders();
        }

        [MenuItem("This is Blast/Ensure Required Shaders & Materials")]
        public static void EnsureMaterialsAndShaders()
        {
            // 1. Create Resources/Materials folder if missing
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Materials"))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Materials");
            }

            // 2. Create or verify GameStandardMaterial.mat in Resources
            string matPath = "Assets/Resources/Materials/GameStandardMaterial.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Shader stdShader = Shader.Find("Standard") ?? Shader.Find("Mobile/Diffuse");
            if (mat == null && stdShader != null)
            {
                mat = new Material(stdShader);
                AssetDatabase.CreateAsset(mat, matPath);
                Debug.Log("<color=green>[ShaderSetup] Created GameStandardMaterial.mat in Resources/Materials</color>");
            }

            // 3. Ensure AlwaysIncludedShaders contains Standard, Mobile/Diffuse, Unlit/Color, etc.
            var graphicsSettingsObj = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (graphicsSettingsObj != null && graphicsSettingsObj.Length > 0)
            {
                SerializedObject serializedGraphics = new SerializedObject(graphicsSettingsObj[0]);
                SerializedProperty arrayProp = serializedGraphics.FindProperty("m_AlwaysIncludedShaders");

                string[] shadersToInclude = { "Standard", "Mobile/Diffuse", "Unlit/Color", "Sprites/Default", "Particles/Standard Unlit" };
                bool changed = false;

                foreach (var shaderName in shadersToInclude)
                {
                    Shader s = Shader.Find(shaderName);
                    if (s == null) continue;

                    bool alreadyIncluded = false;
                    for (int i = 0; i < arrayProp.arraySize; i++)
                    {
                        var elem = arrayProp.GetArrayElementAtIndex(i);
                        if (elem.objectReferenceValue == s)
                        {
                            alreadyIncluded = true;
                            break;
                        }
                    }

                    if (!alreadyIncluded)
                    {
                        int index = arrayProp.arraySize;
                        arrayProp.InsertArrayElementAtIndex(index);
                        arrayProp.GetArrayElementAtIndex(index).objectReferenceValue = s;
                        changed = true;
                        Debug.Log($"<color=cyan>[ShaderSetup] Added '{shaderName}' to GraphicsSettings.AlwaysIncludedShaders</color>");
                    }
                }

                if (changed)
                {
                    serializedGraphics.ApplyModifiedProperties();
                    AssetDatabase.SaveAssets();
                }
            }
        }
    }
}
#endif
