// SpriteSheetToTMPFont.cs
// Place in: Assets/Editor/
// Menu: Tools → Sprite Sheet → Generate TMP Sprite Asset
//       Tools → Sprite Sheet → [DEBUG] Dump SpriteAsset Properties   ← run this FIRST

using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using TMPro;

public class SpriteSheetToTMPFont : EditorWindow
{
    private Texture2D spriteSheet;
    private int    columns    = 13;
    private int    rows       = 1;
    private int    cellWidth  = 0;
    private int    cellHeight = 0;
    private int    paddingX   = 0;
    private int    paddingY   = 0;

    private string characterOrder = "1234567890.,+";
    private string outputFolder   = "Assets/Fonts/TMP";
    private string assetName      = "MyBitmapFont";

    private Vector2 scroll;
    private string  status   = "";
    private bool    showInfo = true;

    private static readonly string P_GOLD_SILVER  = "1234567890.,+";
    private static readonly string P_MORE_NUMBERS = "0123456789";
    private static readonly string P_NUM_TEXT     = ",.0123456789";

    // ── DEBUG: dumps every serialized property name on a fresh TMP_SpriteAsset ──
    [MenuItem("Tools/Sprite Sheet/[DEBUG] Dump SpriteAsset Properties")]
    public static void DumpSpriteAssetProperties()
    {
        var sa = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
        var so = new SerializedObject(sa);
        var sb = new StringBuilder();
        sb.AppendLine("=== TMP_SpriteAsset serialized properties ===");

        var prop = so.GetIterator();
        bool enterChildren = true;
        int depth = 0;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = prop.propertyType == SerializedPropertyType.Generic
                         || prop.propertyType == SerializedPropertyType.ManagedReference;
            depth = prop.depth;
            sb.AppendLine($"{new string(' ', depth * 2)}[{prop.depth}] {prop.propertyPath}  ({prop.propertyType})  arraySize={( prop.isArray ? prop.arraySize.ToString() : "-")}");
            if (depth > 3) enterChildren = false; // don't recurse too deep
        }

        string result = sb.ToString();
        Debug.Log(result);

        // Also write to a file so you can read it easily
        string outPath = "Assets/TMP_SpriteAsset_Properties.txt";
        File.WriteAllText(outPath, result);
        AssetDatabase.ImportAsset(outPath);
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(outPath));

        ScriptableObject.DestroyImmediate(sa);
        Debug.Log($"Property dump written to {outPath}");
    }

    [MenuItem("Tools/Sprite Sheet/Generate TMP Sprite Asset")]
    public static void ShowWindow()
    {
        var w = GetWindow<SpriteSheetToTMPFont>("TMP Sprite Font Generator");
        w.minSize = new Vector2(460, 660);
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        var h1 = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Sprite Sheet  →  TMP Sprite Asset", h1);
        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("1 · Source Texture", EditorStyles.boldLabel);
        spriteSheet = (Texture2D)EditorGUILayout.ObjectField(
            "Sprite Sheet", spriteSheet, typeof(Texture2D), false);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("2 · Grid Layout", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        columns    = EditorGUILayout.IntField("Columns",              columns);
        rows       = EditorGUILayout.IntField("Rows",                 rows);
        cellWidth  = EditorGUILayout.IntField("Cell Width  (0=auto)", cellWidth);
        cellHeight = EditorGUILayout.IntField("Cell Height (0=auto)", cellHeight);
        paddingX   = EditorGUILayout.IntField("Padding X",            paddingX);
        paddingY   = EditorGUILayout.IntField("Padding Y",            paddingY);
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("3 · Character Order  (left→right, top→bottom)", EditorStyles.boldLabel);
        characterOrder = EditorGUILayout.TextField("Characters", characterOrder);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Presets");
        if (GUILayout.Button("Gold/Silver")) characterOrder = P_GOLD_SILVER;
        if (GUILayout.Button("0-9 only"))    characterOrder = P_MORE_NUMBERS;
        if (GUILayout.Button(",.0-9"))       characterOrder = P_NUM_TEXT;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("4 · Output", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        outputFolder = EditorGUILayout.TextField("Folder",     outputFolder);
        assetName    = EditorGUILayout.TextField("Asset Name", assetName);
        EditorGUI.indentLevel--;

        if (spriteSheet != null)
        {
            EditorGUILayout.Space(4);
            showInfo = EditorGUILayout.Foldout(showInfo, "Info");
            if (showInfo)
            {
                int cw = cellWidth  > 0 ? cellWidth  : spriteSheet.width  / Mathf.Max(1, columns);
                int ch = cellHeight > 0 ? cellHeight : spriteSheet.height / Mathf.Max(1, rows);
                EditorGUILayout.HelpBox(
                    $"Texture: {spriteSheet.width} × {spriteSheet.height}\n" +
                    $"Cell: {cw} × {ch}   Cells: {columns * rows}   Chars: {characterOrder.Length}",
                    MessageType.Info);
            }
        }

        EditorGUILayout.Space(8);
        GUI.enabled = spriteSheet != null && characterOrder.Length > 0;
        if (GUILayout.Button("Generate TMP Sprite Asset + Helper Script", GUILayout.Height(38)))
            Generate();
        GUI.enabled = true;

        if (!string.IsNullOrEmpty(status))
        {
            EditorGUILayout.Space(4);
            var msgType = status.StartsWith("ERROR") ? MessageType.Error : MessageType.Info;
            EditorGUILayout.HelpBox(status, msgType);
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "STEP 0 (first time only):\n" +
            "Run Tools → Sprite Sheet → [DEBUG] Dump SpriteAsset Properties\n" +
            "then paste the contents of TMP_SpriteAsset_Properties.txt here so\n" +
            "the correct field names can be confirmed for your TMP version.\n\n" +
            "After generating:\n" +
            "1. Assign .asset to TMP component Extra Settings → Sprite Asset.\n" +
            "2. Use BitmapNumberText component to set values.",
            MessageType.Warning);

        EditorGUILayout.EndScrollView();
    }

    private void Generate()
    {
        status = "";

        if (spriteSheet == null)        { status = "ERROR: No sprite sheet assigned.";        return; }
        if (characterOrder.Length == 0) { status = "ERROR: Character order is empty.";        return; }
        if (columns <= 0 || rows <= 0)  { status = "ERROR: Columns and Rows must be > 0.";    return; }

        string srcPath = AssetDatabase.GetAssetPath(spriteSheet);
        EnsureReadable(srcPath);
        spriteSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(srcPath);

        int texW  = spriteSheet.width;
        int texH  = spriteSheet.height;
        int cw    = cellWidth  > 0 ? cellWidth  : texW / columns;
        int ch    = cellHeight > 0 ? cellHeight : texH / rows;
        int count = Mathf.Min(columns * rows, characterOrder.Length);

        if (!Directory.Exists(outputFolder))
            Directory.CreateDirectory(outputFolder);

        string assetPath = $"{outputFolder}/{assetName}.asset";
        string matPath   = $"{outputFolder}/{assetName}_Mat.mat";

        if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null) AssetDatabase.DeleteAsset(assetPath);
        if (AssetDatabase.LoadAssetAtPath<Object>(matPath)   != null) AssetDatabase.DeleteAsset(matPath);
        AssetDatabase.SaveAssets();

        // Configure source as Sprite Multiple
        var srcImporter = AssetImporter.GetAtPath(srcPath) as TextureImporter;
        if (srcImporter != null)
        {
            srcImporter.textureType         = TextureImporterType.Sprite;
            srcImporter.spriteImportMode    = SpriteImportMode.Multiple;
            srcImporter.isReadable          = true;
            srcImporter.mipmapEnabled       = false;
            srcImporter.filterMode          = FilterMode.Bilinear;
            srcImporter.alphaIsTransparency = true;
            srcImporter.npotScale           = TextureImporterNPOTScale.None;

            var metaList = new List<SpriteMetaData>();
            for (int i = 0; i < count; i++)
            {
                int col = i % columns;
                int row = i / columns;
                metaList.Add(new SpriteMetaData
                {
                    name      = characterOrder[i].ToString(),
                    rect      = new Rect(col * cw + paddingX,
                                        texH - (row + 1) * ch + paddingY,
                                        cw - paddingX * 2,
                                        ch - paddingY * 2),
                    pivot     = new Vector2(0.5f, 0.5f),
                    alignment = 0
                });
            }
            srcImporter.spritesheet = metaList.ToArray();
            srcImporter.SaveAndReimport();
        }
        spriteSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(srcPath);

        Shader shader = Shader.Find("TextMeshPro/Sprite") ?? Shader.Find("Sprites/Default");
        if (shader == null) { status = "ERROR: TextMeshPro/Sprite shader not found."; return; }

        var mat = new Material(shader) { name = assetName + "_Mat", mainTexture = spriteSheet };
        AssetDatabase.CreateAsset(mat, matPath);
        AssetDatabase.SaveAssets();
        mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

        var sa = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
        sa.name        = assetName;
        sa.material    = mat;
        sa.spriteSheet = spriteSheet;
        AssetDatabase.CreateAsset(sa, assetPath);

        var so = new SerializedObject(sa);

        // Discover glyph/character table property names dynamically
        string glyphPropName = null;
        string charPropName  = null;
        var probe = so.GetIterator();
        probe.NextVisible(true);
        while (probe.NextVisible(false))
        {
            string low = probe.name.ToLower();
            if (probe.isArray && low.Contains("glyph"))  glyphPropName = probe.name;
            if (probe.isArray && low.Contains("char"))   charPropName  = probe.name;
        }

        // Fallback to known names
        if (glyphPropName == null) glyphPropName = so.FindProperty("m_SpriteGlyphTable")     != null ? "m_SpriteGlyphTable"     :
                                                   so.FindProperty("spriteGlyphTable")        != null ? "spriteGlyphTable"        : null;
        if (charPropName  == null) charPropName  = so.FindProperty("m_SpriteCharacterTable") != null ? "m_SpriteCharacterTable" :
                                                   so.FindProperty("spriteCharacterTable")    != null ? "spriteCharacterTable"    : null;

        if (glyphPropName == null || charPropName == null)
        {
            // Dump all property names into the error message so we can fix it
            var sb = new StringBuilder();
            sb.AppendLine("ERROR: Cannot find glyph/char table in TMP_SpriteAsset.");
            sb.AppendLine("Run [DEBUG] Dump SpriteAsset Properties from the menu.");
            sb.AppendLine("Array properties found:");
            var it = so.GetIterator(); it.NextVisible(true);
            while (it.NextVisible(false))
                if (it.isArray) sb.AppendLine($"  • {it.propertyPath}");
            status = sb.ToString();
            AssetDatabase.DeleteAsset(assetPath);
            return;
        }

        var glyphTable = so.FindProperty(glyphPropName);
        var charTable  = so.FindProperty(charPropName);

        glyphTable.arraySize = count;
        charTable.arraySize  = count;

        for (int i = 0; i < count; i++)
        {
            int col = i % columns;
            int row = i / columns;
            int x   = col * cw + paddingX;
            int y   = texH - (row + 1) * ch + paddingY;
            int w   = cw - paddingX * 2;
            int h   = ch - paddingY * 2;

            var ge = glyphTable.GetArrayElementAtIndex(i);
            SetI(ge, "m_Index", i);

            var rect = ge.FindPropertyRelative("m_GlyphRect");
            if (rect != null) { SetI(rect,"m_X",x); SetI(rect,"m_Y",y); SetI(rect,"m_Width",w); SetI(rect,"m_Height",h); }

            var metrics = ge.FindPropertyRelative("m_Metrics");
            if (metrics != null) { SetF(metrics,"m_Width",w); SetF(metrics,"m_Height",h); SetF(metrics,"m_HorizontalBearingX",0); SetF(metrics,"m_HorizontalBearingY",h); SetF(metrics,"m_HorizontalAdvance",w); }

            SetF(ge, "m_Scale", 1f);
            SetI(ge, "m_AtlasIndex", 0);

            var ce = charTable.GetArrayElementAtIndex(i);
            SetI(ce, "m_Unicode",    (int)characterOrder[i]);
            SetI(ce, "m_GlyphIndex", i);
            SetF(ce, "m_Scale",      1f);
            var np = ce.FindPropertyRelative("m_Name");
            if (np != null) np.stringValue = characterOrder[i].ToString();
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(sa);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        GenerateHelperScript(count);
        EditorGUIUtility.PingObject(sa);

        status = $"Done!  →  {assetPath}\n" +
                 $"Glyphs: {count}   Characters: \"{characterOrder.Substring(0, count)}\"\n\n" +
                 "Assign .asset to TMP component Extra Settings → Sprite Asset.\n" +
                 "Use BitmapNumberText component to set values.\n" +
                 $"Rich text: <sprite=\"{assetName}\" name=\"1\">";
    }

    private void GenerateHelperScript(int glyphCount)
    {
        string scriptPath = $"{outputFolder}/BitmapNumberText.cs";
        string charList   = characterOrder.Substring(0, Mathf.Min(glyphCount, characterOrder.Length));
        string code = $@"// BitmapNumberText.cs  (auto-generated by SpriteSheetToTMPFont)
// Attach to any GameObject that has a TextMeshProUGUI component.
// Call SetValue(123) to display colored number sprites.

using System.Text;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class BitmapNumberText : MonoBehaviour
{{
    [Tooltip(""Name of the TMP Sprite Asset (no .asset extension)"")]
    public string spriteAssetName = ""{assetName}"";

    private TextMeshProUGUI _tmp;
    private readonly StringBuilder _sb = new StringBuilder(64);

    private void Awake() => _tmp = GetComponent<TextMeshProUGUI>();

    public void SetValue(long value)   => SetText(value.ToString());
    public void SetValue(float value, string fmt = ""0.##"") => SetText(value.ToString(fmt));

    public void SetText(string text)
    {{
        _sb.Clear();
        foreach (char c in text)
            _sb.Append($""<sprite=\""{{spriteAssetName}}\"" name=\""{{c}}\"" tint>"");
        if (_tmp == null) _tmp = GetComponent<TextMeshProUGUI>();
        _tmp.text = _sb.ToString();
    }}

    [ContextMenu(""Test: Display 12345"")]
    private void TestDisplay() => SetText(""12345"");
}}
";
        string fullPath = Path.Combine(Application.dataPath, "..", scriptPath);
        File.WriteAllText(fullPath, code);
        AssetDatabase.ImportAsset(scriptPath);
    }

    private static void SetI(SerializedProperty p, string n, int v)
    { var c = p.FindPropertyRelative(n); if (c != null) c.intValue = v; }

    private static void SetF(SerializedProperty p, string n, float v)
    { var c = p.FindPropertyRelative(n); if (c != null) c.floatValue = v; }

    private static void EnsureReadable(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;
        if (!imp.isReadable) { imp.isReadable = true; imp.SaveAndReimport(); }
    }
}