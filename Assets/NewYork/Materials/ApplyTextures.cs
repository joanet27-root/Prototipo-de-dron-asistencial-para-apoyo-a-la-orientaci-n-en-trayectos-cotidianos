using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class ApplyMtlTextures
{
    // Ajusta estos paths a tu proyecto:
    private const string MtlPath = "Assets/NewYork/drone-city.mtl";
    private const string MaterialsFolder = "Assets/NewYork/Materials";
    private const string TexturesFolder = "Assets/NewYork"; // donde están tus png/jpg (puede ser subcarpetas)

    [MenuItem("Tools/MTL/Aplicar map_Kd a materiales (mat0..)", priority = 0)]
    public static void Apply()
    {
        if (!File.Exists(MtlPath))
        {
            Debug.LogError($"No existe el MTL en: {MtlPath}");
            return;
        }

        var mtlText = File.ReadAllText(MtlPath);

        // Captura bloques: newmtl <name> ... map_Kd <file>
        // Soporta espacios en rutas simples (si no hay comillas raras).
        var re = new Regex(@"newmtl\s+(?<mat>\S+)(?<body>[\s\S]*?)(?:\r?\n\s*map_Kd\s+(?<tex>[^\r\n]+))",
            RegexOptions.Multiline);

        int applied = 0, missingMat = 0, missingTex = 0;

        foreach (Match m in re.Matches(mtlText))
        {
            string matName = m.Groups["mat"].Value.Trim();
            string texFile = m.Groups["tex"].Value.Trim();

            // Buscar material por nombre dentro de MaterialsFolder
            string[] matGuids = AssetDatabase.FindAssets($"{matName} t:Material", new[] { MaterialsFolder });
            if (matGuids.Length == 0)
            {
                missingMat++;
                Debug.LogWarning($"Material no encontrado: {matName} (buscado en {MaterialsFolder})");
                continue;
            }

            string matPath = AssetDatabase.GUIDToAssetPath(matGuids[0]);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                missingMat++;
                Debug.LogWarning($"No pude cargar el material: {matPath}");
                continue;
            }

            // Buscar textura por nombre de archivo (sin extensión) dentro de TexturesFolder
            string texNameNoExt = Path.GetFileNameWithoutExtension(texFile);
            string[] texGuids = AssetDatabase.FindAssets($"{texNameNoExt} t:Texture2D", new[] { TexturesFolder });
            if (texGuids.Length == 0)
            {
                missingTex++;
                Debug.LogWarning($"Textura no encontrada: {texFile} (buscada en {TexturesFolder})");
                continue;
            }

            string texPath = AssetDatabase.GUIDToAssetPath(texGuids[0]);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

            // Propiedades típicas según pipeline
            bool set = false;
            if (mat.HasProperty("_BaseMap")) { mat.SetTexture("_BaseMap", tex); set = true; }          // URP Lit
            if (!set && mat.HasProperty("_MainTex")) { mat.SetTexture("_MainTex", tex); set = true; } // Standard / muchos shaders
            if (!set && mat.HasProperty("_BaseColorMap")) { mat.SetTexture("_BaseColorMap", tex); set = true; } // HDRP Lit

            if (!set)
            {
                Debug.LogWarning($"El material {matName} no tiene _BaseMap/_MainTex/_BaseColorMap. Shader: {mat.shader.name}");
                continue;
            }

            EditorUtility.SetDirty(mat);
            applied++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Listo. Texturas aplicadas: {applied}. Materiales faltantes: {missingMat}. Texturas faltantes: {missingTex}.");
    }
}