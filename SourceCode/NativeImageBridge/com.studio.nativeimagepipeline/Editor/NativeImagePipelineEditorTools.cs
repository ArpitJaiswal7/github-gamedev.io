using System.IO;
using UnityEditor;
using UnityEngine;

namespace Studio.NativeImagePipeline.Editor
{
    public static class NativeImagePipelineEditorTools
    {
        [MenuItem("Tools/Native Image Pipeline/Reveal Volatile Cache Folder")]
        public static void RevealVolatileCacheFolder()
        {
            string path = Path.Combine(Application.temporaryCachePath, "ImageCache");
            Directory.CreateDirectory(path);
            EditorUtility.RevealInFinder(path);
        }

        [MenuItem("Tools/Native Image Pipeline/Reveal Permanent ROM Folder")]
        public static void RevealPermanentRomFolder()
        {
            string path = Path.Combine(Application.persistentDataPath, "ImageROM");
            Directory.CreateDirectory(path);
            EditorUtility.RevealInFinder(path);
        }
    }
}
