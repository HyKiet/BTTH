#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor tool: Tối ưu tất cả sprite textures để giảm bộ nhớ.
/// Chạy: Tools > Mad Doctor > Optimize All Textures
/// 
/// Vấn đề: Mỗi sprite import ở 2048x2048 + không nén = ~16MB/sprite
///          1104 sprites = ~17GB GPU memory → OUT OF MEMORY!
/// 
/// Fix: Giảm maxTextureSize xuống 512 + bật CrunchedCompression
///      1104 sprites × ~0.25MB = ~276MB → vừa đủ
/// </summary>
public class TextureOptimizer : EditorWindow
{
    [MenuItem("Tools/Mad Doctor/Optimize All Textures")]
    static void OptimizeAllTextures()
    {
        string[] searchFolders = new string[]
        {
            "Assets/Resources/Mad Doctor - Main Character",
            "Assets/Mad Doctor Assets/Sprites"
        };

        int count = 0;
        int modified = 0;

        foreach (string folder in searchFolders)
        {
            if (!AssetDatabase.IsValidFolder(folder)) continue;

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                count++;
                bool changed = false;

                // 1. Giảm maxTextureSize
                int targetMaxSize = GetOptimalMaxSize(path);
                if (importer.maxTextureSize > targetMaxSize)
                {
                    importer.maxTextureSize = targetMaxSize;
                    changed = true;
                }

                // 2. Bật Crunched Compression
                if (!importer.crunchedCompression)
                {
                    importer.crunchedCompression = true;
                    importer.compressionQuality = 80;
                    changed = true;
                }

                // 3. Đảm bảo compression enabled
                if (importer.textureCompression == TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.Compressed;
                    changed = true;
                }

                // 4. Platform overrides
                TextureImporterPlatformSettings standalone = importer.GetPlatformTextureSettings("Standalone");
                if (!standalone.overridden || standalone.maxTextureSize > targetMaxSize || !standalone.crunchedCompression)
                {
                    standalone.overridden = true;
                    standalone.maxTextureSize = targetMaxSize;
                    standalone.format = TextureImporterFormat.DXT5Crunched;
                    standalone.crunchedCompression = true;
                    standalone.compressionQuality = 80;
                    importer.SetPlatformTextureSettings(standalone);
                    changed = true;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                    modified++;
                }

                // Progress bar
                if (count % 50 == 0)
                {
                    EditorUtility.DisplayProgressBar(
                        "Optimizing Textures",
                        $"Processing {count}... ({modified} modified)",
                        (float)count / guids.Length
                    );
                }
            }
        }

        EditorUtility.ClearProgressBar();
        Debug.Log($"[TextureOptimizer] Done! Processed {count} textures, modified {modified}");
        EditorUtility.DisplayDialog(
            "Texture Optimization Complete",
            $"Processed: {count} textures\nModified: {modified} textures\n\nMemory savings: ~90%",
            "OK"
        );
    }

    /// <summary>
    /// Tính toán max texture size tối ưu dựa trên loại sprite.
    /// Sprite nhân vật ~300x227px → maxSize 512 là quá đủ.
    /// </summary>
    static int GetOptimalMaxSize(string path)
    {
        string lower = path.ToLower();

        // Background sprites → lớn hơn
        if (lower.Contains("background")) return 1024;

        // UI sprites
        if (lower.Contains("user interface") || lower.Contains("ui")) return 512;

        // Character/Enemy sprites (idle, walk, death, shoot) → nhỏ
        // Sprite gốc chỉ ~300x227px, không cần 2048
        return 512;
    }

    [MenuItem("Tools/Mad Doctor/Check Texture Memory Usage")]
    static void CheckMemoryUsage()
    {
        string folder = "Assets/Resources/Mad Doctor - Main Character";
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });

        long totalEstimatedBytes = 0;
        int textureCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            // Estimate: maxSize × maxSize × 4 bytes (RGBA) 
            long estimatedBytes = (long)importer.maxTextureSize * importer.maxTextureSize * 4;
            totalEstimatedBytes += estimatedBytes;
            textureCount++;
        }

        float totalMB = totalEstimatedBytes / (1024f * 1024f);
        float totalGB = totalMB / 1024f;

        Debug.Log($"[TextureOptimizer] Resources folder: {textureCount} textures");
        Debug.Log($"[TextureOptimizer] Estimated GPU memory (uncompressed): {totalMB:F0} MB ({totalGB:F1} GB)");
        Debug.Log($"[TextureOptimizer] With CrunchedCompression: ~{totalMB * 0.05f:F0} MB");

        EditorUtility.DisplayDialog(
            "Texture Memory Report",
            $"Textures in Resources: {textureCount}\n" +
            $"Estimated GPU memory: {totalMB:F0} MB ({totalGB:F1} GB)\n" +
            $"With Crunched Compression: ~{totalMB * 0.05f:F0} MB\n\n" +
            $"Run 'Optimize All Textures' to fix!",
            "OK"
        );
    }
}
#endif
