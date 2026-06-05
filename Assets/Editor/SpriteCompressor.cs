using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class SpriteResolutionTool : EditorWindow
{
    private enum ResolutionMode
    {
        Compress,
        Increase
    }

    private enum CompressionRatio
    {
        Half = 2,
        Third = 3,
        Quarter = 4
    }

    private enum IncreaseRatio
    {
        Same = 1,
        Double = 2,
        Triple = 3
    }

    private ResolutionMode currentMode = ResolutionMode.Compress;
    private CompressionRatio selectedCompressionRatio = CompressionRatio.Half;
    private IncreaseRatio selectedIncreaseRatio = IncreaseRatio.Double;
    private bool compressOnlyUISprites = true;
    private Vector2 scrollPosition;
    private List<string> spritePaths = new List<string>();
    private bool showPreview = false;
    private string selectedFolderPath = "Assets";
    private bool useManualPath = false;
    private bool includeSubfolders = true;

    [MenuItem("Tools/Sprite Resolution Tool")]
    public static void ShowWindow()
    {
        GetWindow<SpriteResolutionTool>("Sprite Resolution Tool");
    }

    private void OnEnable()
    {
        RefreshSpriteList();
    }

    private void OnGUI()
    {
        GUILayout.Label("Sprite Resolution Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Mode Selection
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Operation Mode", EditorStyles.boldLabel);
        currentMode = (ResolutionMode)EditorGUILayout.EnumPopup("Mode:", currentMode);
        
        string modeInfo = currentMode == ResolutionMode.Compress 
            ? "Compress: Reduce sprite resolution to save memory and file size" 
            : "Increase: Upscale sprite resolution for higher quality";
        EditorGUILayout.HelpBox(modeInfo, MessageType.Info);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Folder selection mode
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Folder Selection", EditorStyles.boldLabel);
        
        useManualPath = EditorGUILayout.Toggle("Use Manual Path Selection", useManualPath);
        
        if (useManualPath)
        {
            EditorGUILayout.BeginHorizontal();
            selectedFolderPath = EditorGUILayout.TextField("Folder Path:", selectedFolderPath);
            
            if (GUILayout.Button("Browse...", GUILayout.Width(80)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Sprite Folder", selectedFolderPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    // Convert absolute path to relative Unity path
                    if (path.StartsWith(Application.dataPath))
                    {
                        selectedFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Invalid Path", 
                            "Please select a folder within your Unity project's Assets folder.", 
                            "OK");
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // Quick folder selection buttons
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Quick Select:", GUILayout.Width(80));
            
            if (GUILayout.Button("Assets Root"))
            {
                selectedFolderPath = "Assets";
            }
            
            if (GUILayout.Button("Current Selection"))
            {
                Object[] selectedObjects = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
                if (selectedObjects.Length > 0)
                {
                    string assetPath = AssetDatabase.GetAssetPath(selectedObjects[0]);
                    if (AssetDatabase.IsValidFolder(assetPath))
                    {
                        selectedFolderPath = assetPath;
                    }
                    else
                    {
                        selectedFolderPath = Path.GetDirectoryName(assetPath);
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("No Selection", "Please select a folder or file in the Project window first.", "OK");
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // Include subfolders option
            includeSubfolders = EditorGUILayout.Toggle("Include Subfolders", includeSubfolders);
            
            // Validate path and show info
            if (!string.IsNullOrEmpty(selectedFolderPath))
            {
                if (AssetDatabase.IsValidFolder(selectedFolderPath))
                {
                    EditorGUILayout.HelpBox($"✓ Valid path: {selectedFolderPath}", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("✗ Invalid folder path! Please select a valid folder within Assets.", MessageType.Warning);
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Select folder(s) in the Project window, then click 'Refresh Sprite List'", MessageType.Info);
            includeSubfolders = EditorGUILayout.Toggle("Include Subfolders", includeSubfolders);
        }
        
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Resolution settings based on mode
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Resolution Settings", EditorStyles.boldLabel);
        
        string ratioInfo = "";
        
        if (currentMode == ResolutionMode.Compress)
        {
            selectedCompressionRatio = (CompressionRatio)EditorGUILayout.EnumPopup("Compression Ratio:", selectedCompressionRatio);
            
            switch (selectedCompressionRatio)
            {
                case CompressionRatio.Half:
                    ratioInfo = "Compress to 1/2 (50%) of original resolution";
                    break;
                case CompressionRatio.Third:
                    ratioInfo = "Compress to 1/3 (33.3%) of original resolution";
                    break;
                case CompressionRatio.Quarter:
                    ratioInfo = "Compress to 1/4 (25%) of original resolution";
                    break;
            }
        }
        else // Increase mode
        {
            selectedIncreaseRatio = (IncreaseRatio)EditorGUILayout.EnumPopup("Increase Ratio:", selectedIncreaseRatio);
            
            switch (selectedIncreaseRatio)
            {
                case IncreaseRatio.Same:
                    ratioInfo = "Keep same resolution (1x) - useful for refreshing import settings";
                    break;
                case IncreaseRatio.Double:
                    ratioInfo = "Increase to 2x (200%) of original resolution";
                    break;
                case IncreaseRatio.Triple:
                    ratioInfo = "Increase to 3x (300%) of original resolution";
                    break;
            }
        }
        
        EditorGUILayout.HelpBox(ratioInfo, MessageType.Info);
        
        compressOnlyUISprites = EditorGUILayout.Toggle("Only UI Sprites", compressOnlyUISprites);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Selected folders info
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Selected Sprites", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Refresh Sprite List"))
        {
            RefreshSpriteList();
        }

        EditorGUILayout.LabelField("Sprites Found:", spritePaths.Count.ToString());
        
        if (spritePaths.Count > 0)
        {
            // Calculate total size info
            long totalSize = CalculateTotalSize();
            string sizeText = FormatBytes(totalSize);
            EditorGUILayout.LabelField("Total Size:", sizeText);
        }
        
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Preview section
        showPreview = EditorGUILayout.Foldout(showPreview, "Preview Sprites to Process");
        if (showPreview)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            foreach (string path in spritePaths)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(path);
                
                // Show current resolution
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex != null)
                {
                    EditorGUILayout.LabelField($"{tex.width}x{tex.height}", GUILayout.Width(100));
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.Space();

        // Process button
        EditorGUI.BeginDisabledGroup(spritePaths.Count == 0);
        string buttonText = currentMode == ResolutionMode.Compress ? "Compress Selected Sprites" : "Increase Resolution of Selected Sprites";
        
        if (GUILayout.Button(buttonText, GUILayout.Height(40)))
        {
            string confirmMessage = currentMode == ResolutionMode.Compress
                ? $"This will compress {spritePaths.Count} sprite(s) to 1/{(int)selectedCompressionRatio} of their original resolution.\n\nThis action cannot be undone automatically. Continue?"
                : $"This will increase {spritePaths.Count} sprite(s) to {(int)selectedIncreaseRatio}x their original resolution.\n\nThis action cannot be undone automatically. Continue?";
            
            if (EditorUtility.DisplayDialog("Confirm Resolution Change", confirmMessage, "Process", "Cancel"))
            {
                ProcessSprites();
            }
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();

        // Instructions
        EditorGUILayout.HelpBox(
            "Instructions:\n" +
            "• Choose operation mode (Compress or Increase)\n" +
            "• Enable 'Use Manual Path Selection' to browse for a folder\n" +
            "  - Use 'Browse...' button to select any folder\n" +
            "  - Use 'Quick Select' buttons for common locations\n" +
            "• OR select folder(s) in the Project window\n" +
            "• Toggle 'Include Subfolders' to search recursively\n" +
            "• Choose compression/increase ratio\n" +
            "• Click 'Refresh Sprite List' to see found sprites\n" +
            "• Click the process button to apply changes",
            MessageType.Info);
    }

    private void RefreshSpriteList()
    {
        spritePaths.Clear();

        if (useManualPath)
        {
            // Use manually selected folder path
            if (!string.IsNullOrEmpty(selectedFolderPath) && AssetDatabase.IsValidFolder(selectedFolderPath))
            {
                FindSpritesInFolder(selectedFolderPath);
            }
            else
            {
                Debug.LogWarning("Invalid folder path: " + selectedFolderPath);
            }
        }
        else
        {
            // Get selected objects in the Project window
            Object[] selectedObjects = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);

            if (selectedObjects.Length == 0)
            {
                Debug.LogWarning("No folders or assets selected in Project window. Please select folders or enable 'Use Manual Path Selection'.");
            }

            foreach (Object obj in selectedObjects)
            {
                string assetPath = AssetDatabase.GetAssetPath(obj);

                if (AssetDatabase.IsValidFolder(assetPath))
                {
                    // It's a folder, find all sprites inside
                    FindSpritesInFolder(assetPath);
                }
                else
                {
                    // It's a file, check if it's a sprite
                    if (IsValidSprite(assetPath))
                    {
                        spritePaths.Add(assetPath);
                    }
                }
            }
        }

        spritePaths = spritePaths.Distinct().ToList();
        
        Debug.Log($"Found {spritePaths.Count} sprite(s) in selected folder(s).");
    }

    private void FindSpritesInFolder(string folderPath)
    {
        // Find textures in the specified folder
        string[] searchFolders = includeSubfolders ? new[] { folderPath } : null;
        
        string[] guids;
        if (includeSubfolders)
        {
            // Search in folder and all subfolders
            guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        }
        else
        {
            // Search only in the immediate folder (no subfolders)
            guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
            
            // Filter out assets from subfolders
            List<string> filteredGuids = new List<string>();
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string assetFolder = Path.GetDirectoryName(assetPath).Replace('\\', '/');
                
                if (assetFolder == folderPath)
                {
                    filteredGuids.Add(guid);
                }
            }
            guids = filteredGuids.ToArray();
        }

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (IsValidSprite(assetPath))
            {
                spritePaths.Add(assetPath);
            }
        }
    }

    private bool IsValidSprite(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        
        if (importer == null)
            return false;

        // Check if it's a sprite
        if (importer.textureType != TextureImporterType.Sprite)
            return false;

        // If filtering for UI sprites only
        if (compressOnlyUISprites)
        {
            // Check if the sprite is in a UI folder or has UI-related naming
            return assetPath.ToLower().Contains("ui") || 
                   assetPath.ToLower().Contains("sprite");
        }

        return true;
    }

    private long CalculateTotalSize()
    {
        long totalSize = 0;
        
        foreach (string path in spritePaths)
        {
            string fullPath = Path.Combine(Application.dataPath.Replace("Assets", ""), path);
            if (File.Exists(fullPath))
            {
                FileInfo fileInfo = new FileInfo(fullPath);
                totalSize += fileInfo.Length;
            }
        }
        
        return totalSize;
    }

    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        
        return $"{len:0.##} {sizes[order]}";
    }

    private void ProcessSprites()
    {
        int successCount = 0;
        int failCount = 0;

        AssetDatabase.StartAssetEditing();

        try
        {
            foreach (string assetPath in spritePaths)
            {
                try
                {
                    if (ProcessSprite(assetPath))
                    {
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to process {assetPath}: {e.Message}");
                    failCount++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.Refresh();

        string modeText = currentMode == ResolutionMode.Compress ? "Compression" : "Resolution Increase";
        EditorUtility.DisplayDialog($"{modeText} Complete",
            $"Successfully processed: {successCount}\nFailed: {failCount}",
            "OK");

        Debug.Log($"Sprite {modeText.ToLower()} complete. Success: {successCount}, Failed: {failCount}");
    }

    private bool ProcessSprite(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        
        if (importer == null)
            return false;

        // Get original texture to determine its size
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (texture == null)
            return false;

        int originalMaxSize = Mathf.Max(texture.width, texture.height);
        int newMaxSize;

        if (currentMode == ResolutionMode.Compress)
        {
            // Compression mode
            newMaxSize = originalMaxSize / (int)selectedCompressionRatio;
            
            // Ensure minimum size
            newMaxSize = Mathf.Max(newMaxSize, 32);
        }
        else
        {
            // Increase mode
            newMaxSize = originalMaxSize * (int)selectedIncreaseRatio;
            
            // Ensure we don't exceed Unity's maximum texture size
            newMaxSize = Mathf.Min(newMaxSize, 8192);
        }

        // Round to nearest power of 2 for better performance
        newMaxSize = Mathf.NextPowerOfTwo(newMaxSize);

        // Clamp to valid Unity texture sizes
        newMaxSize = Mathf.Clamp(newMaxSize, 32, 8192);

        // Apply new settings
        importer.maxTextureSize = newMaxSize;
        importer.isReadable = false; // Improve memory usage

        // Set compression settings
        TextureImporterPlatformSettings platformSettings = importer.GetDefaultPlatformTextureSettings();
        platformSettings.maxTextureSize = newMaxSize;
        platformSettings.format = TextureImporterFormat.Automatic;
        
        if (currentMode == ResolutionMode.Compress)
        {
            platformSettings.textureCompression = TextureImporterCompression.Compressed;
        }
        else
        {
            // For increased resolution, use higher quality compression
            platformSettings.textureCompression = TextureImporterCompression.CompressedHQ;
        }
        
        importer.SetPlatformTextureSettings(platformSettings);

        // Save and reimport
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        string modeText = currentMode == ResolutionMode.Compress 
            ? $"Compressed to 1/{(int)selectedCompressionRatio}" 
            : $"Increased to {(int)selectedIncreaseRatio}x";
        
        Debug.Log($"{modeText} - {assetPath}: {originalMaxSize}px -> {newMaxSize}px");

        return true;
    }

    private void OnSelectionChange()
    {
        if (!useManualPath)
        {
            RefreshSpriteList();
            Repaint();
        }
    }
}