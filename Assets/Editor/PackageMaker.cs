// Unity 2021.3+
// Tools → Packaging → Build Asset Package
//
// Modes
//  - Copy (staging): copy assets into Packages/<id>/..., keep originals under Assets/.
//                    Remaps GUIDs inside the COPIED files so copies point to copies.
//                    By default, scripts are NOT copied (toggleable).
//  - Publish (move): move assets into Packages/<id>/..., including scripts.
//                    Moving preserves GUIDs, so existing scenes keep working.
//
// New:
//  - Auto-create a TMP Font Asset from a TTF/OTF and assign to all TMP components in the prefab
//    before packaging, so you don't depend on LiberationSans.
//
// Also:
//  - Writes a minimal package.json, waits for Package Manager to index, then pins
//    the EXACT versions of TMP / Input System / XRI installed in THIS project.
//  - Handles TMP bundled via uGUI (pins com.unity.ugui when TMP package isn't present).
//  - Avoids triggering Awake() during detection (uses AssetDatabase.LoadAssetAtPath).
//  - Generates asmdefs when scripts are included.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro; // requires TMP (package or bundled via uGUI) present in the project
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using Object = UnityEngine.Object;
using UPMInfo = UnityEditor.PackageManager.PackageInfo;

public class AssetPackageBuilderWindow : EditorWindow
{
    // ─────────────────────────── UI State ───────────────────────────
    private Object prefabAsset;
    private string packageId = "com.devweber.your-asset"; // e.g., com.deven.count
    private string displayName = "Your Asset";
    private string version = "1.0.0";
    private string description = "Asset package.";

    private BuildMode buildMode = BuildMode.CopyStaging; // default
    private bool includeScripts = false; // default for Copy mode
    private bool dryRun = true; // preview first

    // NEW: auto-create & assign TMP Font Asset
    private bool autoCreateAndAssignTMPFont = true;
    private Font sourceTTF; // drop a .ttf/.otf here
    private string generatedFontName = "PackageFont SDF";

    private Vector2 scroll;

    private enum BuildMode
    {
        CopyStaging,
        PublishMove,
    }

    // ─────────────────────── Categorization Tables ───────────────────────
    private static readonly string[] ModelExt =
    {
        ".fbx",
        ".obj",
        ".dae",
        ".3ds",
        ".dxf",
        ".glb",
        ".gltf",
        ".blend",
    };
    private static readonly string[] TextureExt =
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".tga",
        ".psd",
        ".tif",
        ".tiff",
        ".exr",
        ".hdr",
        ".bmp",
        ".ktx",
        ".ktx2",
    };
    private static readonly string[] MaterialExt = { ".mat" };
    private static readonly string[] ShaderExt =
    {
        ".shader",
        ".cginc",
        ".hlsl",
        ".shadergraph",
        ".shadersubgraph",
    };
    private static readonly string[] AnimExt = { ".anim" };
    private static readonly string[] ControllerExt =
    {
        ".controller",
        ".overrideController",
        ".mask",
    };
    private static readonly string[] AudioExt =
    {
        ".wav",
        ".mp3",
        ".ogg",
        ".aiff",
        ".aif",
        ".flac",
    };
    private static readonly string[] ScriptExt = { ".cs", ".asmdef", ".asmref" };
    private static readonly string[] FontExt = { ".ttf", ".otf", ".ttc" }; // TMP Font Asset detected by type

    private static readonly HashSet<string> PatchableTextExt = new HashSet<string>(
        StringComparer.OrdinalIgnoreCase
    )
    {
        ".prefab",
        ".mat",
        ".anim",
        ".controller",
        ".overrideController",
        ".mask",
        ".asset",
        ".shadergraph",
        ".shadersubgraph",
        ".rendergraph",
        ".playable",
    };

    [MenuItem("Tools/Packaging/Build Asset Package")]
    public static void Open() => GetWindow<AssetPackageBuilderWindow>("Build Asset Package");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Create UPM package from Prefab", EditorStyles.boldLabel);

        prefabAsset = EditorGUILayout.ObjectField("Prefab", prefabAsset, typeof(GameObject), false);

        EditorGUILayout.Space(6);

        // Mode selector
        EditorGUILayout.LabelField("Mode", EditorStyles.boldLabel);
        buildMode = (BuildMode)
            GUILayout.Toolbar((int)buildMode, new[] { "Copy (staging)", "Publish (move)" });
        EditorGUILayout.Space(4);

        // Package metadata
        packageId = EditorGUILayout.TextField("Package ID", packageId);
        if (string.IsNullOrWhiteSpace(displayName) || displayName == "Your Asset")
            displayName = AutoDisplayNameFromId(packageId);
        displayName = EditorGUILayout.TextField("Display Name", displayName);
        version = EditorGUILayout.TextField(
            "Version",
            string.IsNullOrWhiteSpace(version) ? "1.0.0" : version
        );
        description = EditorGUILayout.TextField(
            "Description",
            string.IsNullOrWhiteSpace(description) ? "Asset package." : description
        );

        // Include scripts toggle (forced ON in Publish mode)
        using (new EditorGUI.DisabledScope(buildMode == BuildMode.PublishMove))
        {
            includeScripts = EditorGUILayout.ToggleLeft(
                "Include scripts (.cs) if referenced",
                buildMode == BuildMode.PublishMove ? true : includeScripts
            );
        }

        // NEW: TMP font creation UI
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("TextMeshPro Font", EditorStyles.boldLabel);
        autoCreateAndAssignTMPFont = EditorGUILayout.ToggleLeft(
            "Create TMP Font Asset from TTF & assign to prefab",
            autoCreateAndAssignTMPFont
        );
        using (new EditorGUI.DisabledScope(!autoCreateAndAssignTMPFont))
        {
            sourceTTF = (Font)
                EditorGUILayout.ObjectField("Source TTF/OTF", sourceTTF, typeof(Font), false);
            generatedFontName = EditorGUILayout.TextField(
                "Generated font asset name",
                string.IsNullOrWhiteSpace(generatedFontName) ? "PackageFont SDF" : generatedFontName
            );
        }

        dryRun = EditorGUILayout.ToggleLeft("Dry Run (preview only)", dryRun);

        // Hints
        if (buildMode == BuildMode.CopyStaging)
        {
            EditorGUILayout.HelpBox(
                "Copy mode: copies into the package and leaves originals under Assets/. "
                    + "To avoid duplicate type conflicts, scripts are skipped by default. "
                    + "Enable scripts only if you plan to remove/disable the originals later.",
                MessageType.Info
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Publish mode: moves assets (including scripts) into the package. "
                    + "GUIDs are preserved, so scene references keep working. This makes the package authoritative.",
                MessageType.Info
            );
        }

        EditorGUILayout.Space(8);
        if (GUILayout.Button("Build Package", GUILayout.Height(32)))
        {
            try
            {
                BuildPackage();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PackageBuilder] Exception: {ex}");
            }
        }
    }

    private static void EnsureUnityFolder(string fullPath)
    {
        if (AssetDatabase.IsValidFolder(fullPath))
            return;

        var parts = fullPath.Split('/');
        string current = parts[0]; // "Packages"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    // ───────────────────────── Build ─────────────────────────
    private void BuildPackage()
    {
        // Validate
        if (prefabAsset == null)
        {
            EditorUtility.DisplayDialog("Missing Prefab", "Drop a prefab asset first.", "OK");
            return;
        }
        var prefabPath = AssetDatabase.GetAssetPath(prefabAsset);
        if (
            string.IsNullOrEmpty(prefabPath)
            || !prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)
        )
        {
            EditorUtility.DisplayDialog("Not a Prefab", "Please select a prefab asset file.", "OK");
            return;
        }
        if (!IsValidPackageId(packageId))
        {
            EditorUtility.DisplayDialog(
                "Invalid Package ID",
                "Use an id like 'com.yourname.asset' (lowercase letters, digits, dots, hyphens).",
                "OK"
            );
            return;
        }

        bool copyInsteadOfMove = (buildMode == BuildMode.CopyStaging);

        string packageRoot = $"Packages/{packageId}";
        string runtimeRoot = $"{packageRoot}/Runtime";

        var folders = new Dictionary<string, string>
        {
            ["Prefabs"] = $"{runtimeRoot}/Prefabs",
            ["Models"] = $"{runtimeRoot}/Models",
            ["Materials"] = $"{runtimeRoot}/Materials",
            ["Textures"] = $"{runtimeRoot}/Textures",
            ["Shaders"] = $"{runtimeRoot}/Shaders",
            ["Animations"] = $"{runtimeRoot}/Animations",
            ["Audio"] = $"{runtimeRoot}/Audio",
            ["Meshes"] = $"{runtimeRoot}/Meshes",
            ["Scripts"] = $"{runtimeRoot}/Scripts",
            ["Fonts"] = $"{runtimeRoot}/Fonts",
            ["Editor"] = $"{packageRoot}/Editor",
            ["Other"] = $"{runtimeRoot}/Other",
        };

        // Create dirs up-front so Dry Run shows real paths
        EnsureUnityFolder(packageRoot);
        foreach (var d in folders.Values)
            EnsureUnityFolder(d);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        // ── (NEW) Optional: create TMP Font Asset from TTF and assign to prefab BEFORE collecting deps ──
        if (autoCreateAndAssignTMPFont)
        {
            if (sourceTTF == null)
            {
                Debug.LogWarning(
                    "[PackageBuilder] TMP auto-create is enabled but no TTF/OTF was provided. Skipping font generation."
                );
            }
            else
            {
                // Create under Assets/Fonts so it becomes part of the dependency graph and is copied/moved
                string fontsRoot = "Assets/Fonts";
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), fontsRoot));
                string fontAssetPath = $"{fontsRoot}/{SanitizeFileName(generatedFontName)}.asset";

                TMP_FontAsset fontAsset = CreateTMPFontAsset(sourceTTF, fontAssetPath);
                if (fontAsset != null)
                {
                    AssignTMPFontToPrefab(prefabPath, fontAsset);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                }
            }
        }

        // Collect dependencies (prefab included)
        var deps = AssetDatabase
            .GetDependencies(prefabPath, true)
            .Where(p => !string.IsNullOrEmpty(p))
            .Distinct()
            .ToList();

        // Plan
        var plan = new List<(string src, string dst, string cat)>();
        foreach (var path in deps)
        {
            // Skip assets already under other packages
            if (
                path.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith(packageRoot, StringComparison.OrdinalIgnoreCase)
            )
                continue;

            var ext = Path.GetExtension(path).ToLowerInvariant();

            // In Copy mode, default is to skip scripts to avoid duplicate types (unless user enabled)
            if (
                copyInsteadOfMove
                && !includeScripts
                && (ext == ".cs" || ext == ".asmdef" || ext == ".asmref")
            )
                continue;

            string category = Categorize(path);
            if (ext == ".cs" || ext == ".asmdef" || ext == ".asmref")
                category = IsEditorScript(path) ? "Editor" : "Scripts";

            string targetDir = folders.TryGetValue(category, out var dir) ? dir : folders["Other"];

            // Prefab itself → Prefabs
            if (string.Equals(path, prefabPath, StringComparison.OrdinalIgnoreCase))
                targetDir = folders["Prefabs"];

            var fileName = Path.GetFileName(path);
            var dstCandidate = $"{targetDir}/{fileName}";
            var dst = dryRun ? dstCandidate : AssetDatabase.GenerateUniqueAssetPath(dstCandidate);

            if (!PathsEqual(path, dst))
                plan.Add((path, dst, category));
        }

        // Minimal package.json first; force import; wait for embedded
        WritePackageJsonMinimal(packageRoot, packageId, displayName, version, description);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        WaitForEmbeddedPackage(
            packageRoot,
            onReady: () =>
            {
                // Detect needed Unity packages for THIS prefab (TMP/Input/XRI)
                var neededPkgIds = DetectNeededPackages(prefabPath, deps);

                // Resolve exact versions installed in THIS project (async)
                UpmInstalledResolver.Resolve(
                    neededPkgIds,
                    installed =>
                    {
                        // TMP bundled via uGUI fallback
                        if (
                            neededPkgIds.Contains("com.unity.textmeshpro")
                            && !installed.ContainsKey("com.unity.textmeshpro")
                            && installed.ContainsKey("com.unity.ugui")
                        )
                        {
                            installed["com.unity.ugui"] = installed["com.unity.ugui"];
                        }

                        // Overwrite package.json with pinned deps and refresh
                        WritePackageJson(
                            packageRoot,
                            packageId,
                            displayName,
                            version,
                            description,
                            installed
                        );
                        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                        // Auto asmdefs if scripts are included (Publish mode always includes; Copy mode optional)
                        if (includeScripts)
                        {
                            bool hasRuntimeScripts = plan.Any(p =>
                                Path.GetExtension(p.src)
                                    .Equals(".cs", StringComparison.OrdinalIgnoreCase)
                                && !IsEditorScript(p.src)
                            );
                            bool hasEditorScripts = plan.Any(p =>
                                Path.GetExtension(p.src)
                                    .Equals(".cs", StringComparison.OrdinalIgnoreCase)
                                && IsEditorScript(p.src)
                            );

                            if (hasRuntimeScripts)
                                WriteAsmDef(
                                    Path.Combine(folders["Scripts"]),
                                    PackageToAsmName(packageId, "Runtime"),
                                    editor: false,
                                    references: null
                                );

                            if (hasEditorScripts)
                                WriteAsmDef(
                                    Path.Combine(folders["Editor"]),
                                    PackageToAsmName(packageId, "Editor"),
                                    editor: true,
                                    references: hasRuntimeScripts
                                        ? new[] { PackageToAsmName(packageId, "Runtime") }
                                        : null
                                );
                        }

                        if (dryRun)
                        {
                            Debug.Log(
                                $"[PackageBuilder] Dry Run — would {(copyInsteadOfMove ? "copy" : "move")} {plan.Count} assets into {packageRoot}"
                            );
                            if (installed != null && installed.Count > 0)
                                Debug.Log(
                                    $"[PackageBuilder] Would pin dependencies: {string.Join(", ", installed.Select(kv => $"{kv.Key}@{kv.Value}"))}"
                                );
                            foreach (var (src, dst, cat) in plan)
                                Debug.Log($"  [{cat}] {src}  →  {dst}");
                            AssetDatabase.Refresh();
                            return;
                        }

                        // Execute COPY or MOVE
                        int done = 0,
                            failed = 0;
                        var guidMap = new Dictionary<string, string>(); // srcGUID -> dstGUID (for copy remap)
                        var copiedPaths = new List<string>(); // for remap pass

                        AssetDatabase.StartAssetEditing();
                        try
                        {
                            foreach (var (src, dst, cat) in plan)
                            {
                                if (copyInsteadOfMove)
                                {
                                    bool ok = AssetDatabase.CopyAsset(src, dst);
                                    if (ok)
                                    {
                                        done++;
                                        var srcGuid = AssetDatabase.AssetPathToGUID(src);
                                        var dstGuid = AssetDatabase.AssetPathToGUID(dst);
                                        if (
                                            !string.IsNullOrEmpty(srcGuid)
                                            && !string.IsNullOrEmpty(dstGuid)
                                        )
                                            guidMap[srcGuid] = dstGuid;
                                        copiedPaths.Add(dst);
                                    }
                                    else
                                    {
                                        failed++;
                                        Debug.LogError(
                                            $"[PackageBuilder] Copy failed: {src} → {dst}"
                                        );
                                    }
                                }
                                else
                                {
                                    var err = AssetDatabase.MoveAsset(src, dst);
                                    if (string.IsNullOrEmpty(err))
                                    {
                                        done++;
                                    }
                                    else
                                    {
                                        // Rare fallback: filesystem move with .meta
                                        try
                                        {
                                            var absSrc = Path.Combine(
                                                Directory.GetCurrentDirectory(),
                                                src
                                            );
                                            var absDst = Path.Combine(
                                                Directory.GetCurrentDirectory(),
                                                dst
                                            );
                                            Directory.CreateDirectory(
                                                Path.GetDirectoryName(absDst)!
                                            );
                                            FileUtil.MoveFileOrDirectory(absSrc, absDst);
                                            var metaSrc = absSrc + ".meta";
                                            var metaDst = absDst + ".meta";
                                            if (File.Exists(metaSrc))
                                                FileUtil.MoveFileOrDirectory(metaSrc, metaDst);
                                            done++;
                                        }
                                        catch (Exception e)
                                        {
                                            failed++;
                                            Debug.LogError(
                                                $"[PackageBuilder] Move failed: {src} → {dst}\nUPM error: {err}\nFS error: {e}"
                                            );
                                        }
                                    }
                                }
                            }
                        }
                        finally
                        {
                            AssetDatabase.StopAssetEditing();
                            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                            AssetDatabase.SaveAssets();
                        }

                        // If we COPIED: remap GUIDs inside copied text assets so copies reference copies
                        if (copyInsteadOfMove && copiedPaths.Count > 0 && guidMap.Count > 0)
                        {
                            RemapGuidsInCopiedAssets(copiedPaths, guidMap);
                            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                        }

                        EditorUtility.DisplayDialog(
                            "Package Built",
                            $"Mode: {(copyInsteadOfMove ? "Copy (staging)" : "Publish (move)")}\n"
                                + $"Package: {packageId}\n"
                                + $"{(copyInsteadOfMove ? "Copied" : "Moved")}: {done}\nFailed: {failed}\n"
                                + $"Path: {packageRoot}",
                            "OK"
                        );
                    }
                );
            }
        );
    }

    // ───────────────────────── Helpers ─────────────────────────

    private static string AutoDisplayNameFromId(string id)
    {
        if (string.IsNullOrEmpty(id))
            return "Your Asset";
        var last = id.Split('.').Last();
        var parts = last.Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
            parts[i] =
                char.ToUpper(parts[i][0]) + (parts[i].Length > 1 ? parts[i].Substring(1) : "");
        return string.Join(" ", parts);
    }

    private static bool IsValidPackageId(string id) =>
        !string.IsNullOrWhiteSpace(id) && Regex.IsMatch(id, @"^[a-z0-9\-\.]+$") && id.Contains(".");

    private static bool PathsEqual(string a, string b) =>
        string.Equals(
            a.Replace('\\', '/'),
            b.Replace('\\', '/'),
            StringComparison.OrdinalIgnoreCase
        );

    private static string Categorize(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();

        if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            return "Prefabs";
        if (ModelExt.Contains(ext))
            return "Models";
        if (MaterialExt.Contains(ext))
            return "Materials";
        if (TextureExt.Contains(ext))
            return "Textures";
        if (ShaderExt.Contains(ext))
            return "Shaders";
        if (AnimExt.Contains(ext) || ControllerExt.Contains(ext))
            return "Animations";
        if (AudioExt.Contains(ext))
            return "Audio";
        if (FontExt.Contains(ext))
            return "Fonts";

        var t = AssetDatabase.GetMainAssetTypeAtPath(path);
        if (t == typeof(Mesh))
            return "Meshes";
        if (t != null && t.Name == "TMP_FontAsset")
            return "Fonts"; // no hard ref to TMPro type here

        return "Other";
    }

    private static bool IsEditorScript(string path)
    {
        try
        {
            if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                return false;
            var text = File.ReadAllText(path);
            if (
                text.Contains("using UnityEditor")
                || text.Contains("[CustomEditor(")
                || text.Contains(": Editor")
            )
                return true;
        }
        catch
        { /* ignore */
        }
        return false;
    }

    private static string PackageToAsmName(string packageId, string suffix)
    {
        // com.deven.count -> Deven.Count.Runtime
        var parts = packageId.Split('.');
        var core =
            parts.Length >= 2
                ? string.Join(".", parts.Skip(1).Select(Cap))
                : packageId.Replace('.', '_');
        return $"{core}.{suffix}";
        static string Cap(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + (s.Length > 1 ? s[1..] : "");
    }

    private static void WriteAsmDef(string dir, string asmName, bool editor, string[] references)
    {
        Directory.CreateDirectory(dir);
        var refs =
            (references != null && references.Length > 0)
                ? "\"references\": ["
                    + string.Join(",", references.Select(r => $"\"{r}\""))
                    + "],\n"
                : "";
        var includePlatforms = editor ? "\"includePlatforms\": [\"Editor\"],\n" : "";
        var json =
            "{\n"
            + $"  \"name\": \"{asmName}\",\n"
            + $"  {refs}"
            + $"  {includePlatforms}"
            + "  \"autoReferenced\": true,\n"
            + "  \"allowUnsafeCode\": false,\n"
            + "  \"overrideReferences\": false,\n"
            + "  \"precompiledReferences\": [],\n"
            + "  \"defineConstraints\": [],\n"
            + "  \"noEngineReferences\": false\n"
            + "}";
        File.WriteAllText(Path.Combine(dir, asmName + ".asmdef"), json);
    }

    private static void WritePackageJson(
        string packageRoot,
        string id,
        string name,
        string ver,
        string desc,
        Dictionary<string, string> deps
    )
    {
        string unityField =
            Application.unityVersion.Split('.')[0] + "." + Application.unityVersion.Split('.')[1];

        string depsBlock = "";
        if (deps != null && deps.Count > 0)
        {
            var lines = deps.Select(kv => $"    \"{kv.Key}\": \"{kv.Value}\"");
            depsBlock = ",\n  \"dependencies\": {\n" + string.Join(",\n", lines) + "\n  }";
        }

        var json =
            $@"{{
  ""name"": ""{id}"",
  ""displayName"": ""{name.Replace("\"", "\\\"")}"",
  ""version"": ""{ver}"",
  ""unity"": ""{unityField}"",
  ""description"": ""{desc.Replace("\"", "\\\"")}""
  {depsBlock}
}}";
        Directory.CreateDirectory(packageRoot);
        File.WriteAllText(Path.Combine(packageRoot, "package.json"), json);
    }

    private static void WritePackageJsonMinimal(
        string packageRoot,
        string id,
        string name,
        string ver,
        string desc
    )
    {
        string unityField =
            Application.unityVersion.Split('.')[0] + "." + Application.unityVersion.Split('.')[1];
        var json =
            $@"{{
  ""name"": ""{id}"",
  ""displayName"": ""{name.Replace("\"", "\\\"")}"",
  ""version"": ""{ver}"",
  ""unity"": ""{unityField}"",
  ""description"": ""{desc.Replace("\"", "\\\"")}""
}}";
        Directory.CreateDirectory(packageRoot);
        File.WriteAllText(Path.Combine(packageRoot, "package.json"), json);
    }

    private static void WaitForEmbeddedPackage(
        string packageRoot,
        Action onReady,
        float timeoutSeconds = 5f
    )
    {
        double start = EditorApplication.timeSinceStartup;
        void Tick()
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(packageRoot);
            if (info != null)
            {
                EditorApplication.update -= Tick;
                onReady?.Invoke();
                return;
            }
            if (EditorApplication.timeSinceStartup - start > timeoutSeconds)
            {
                EditorApplication.update -= Tick;
                Debug.LogWarning(
                    $"[PackageBuilder] Package import not detected within {timeoutSeconds:0.0}s. Continuing anyway."
                );
                onReady?.Invoke();
            }
        }
        EditorApplication.update += Tick;
    }

    // Detect TMP / Input System / XRI usage without invoking Awake
    private static string[] DetectNeededPackages(string prefabPath, IReadOnlyList<string> deps)
    {
        var need = new List<string>();

        if (
            PrefabUsesNamespace(prefabPath, "TMPro")
            || DepsContainPathFragment(deps, "TextMesh Pro/")
        )
            need.Add("com.unity.textmeshpro");

        if (
            PrefabUsesNamespace(prefabPath, "UnityEngine.InputSystem")
            || deps.Any(p => p.EndsWith(".inputactions", StringComparison.OrdinalIgnoreCase))
        )
            need.Add("com.unity.inputsystem");

        if (PrefabUsesNamespace(prefabPath, "UnityEngine.XR.Interaction.Toolkit"))
            need.Add("com.unity.xr.interaction.toolkit");

        return need.Distinct().ToArray();
    }

    private static bool PrefabUsesNamespace(string prefabPath, string nsPrefix)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (!prefab)
            return false;

        var comps = prefab.GetComponentsInChildren<Component>(true);
        foreach (var comp in comps)
        {
            if (!comp)
                continue;
            var ns = comp.GetType().Namespace ?? "";
            if (ns.StartsWith(nsPrefix, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private static bool DepsContainPathFragment(IEnumerable<string> paths, string fragment) =>
        paths.Any(p => p.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0);

    // Copy-mode GUID remap: ensure copied files reference the COPIES, not originals
    private static void RemapGuidsInCopiedAssets(
        IEnumerable<string> dstPaths,
        Dictionary<string, string> guidMap
    )
    {
        foreach (var dst in dstPaths)
        {
            var ext = Path.GetExtension(dst);
            if (!PatchableTextExt.Contains(ext))
                continue;

            var abs = Path.Combine(Directory.GetCurrentDirectory(), dst);
            if (!File.Exists(abs))
                continue;

            string text = File.ReadAllText(abs);
            bool changed = false;

            foreach (var kv in guidMap)
            {
                if (text.Contains(kv.Key, StringComparison.Ordinal))
                {
                    text = text.Replace(kv.Key, kv.Value, StringComparison.Ordinal);
                    changed = true;
                }
            }

            if (changed)
                File.WriteAllText(abs, text);
        }
    }

    // ───────────────────── NEW: TMP Font helpers ─────────────────────

    private static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Trim();
    }

    private static TMP_FontAsset CreateTMPFontAsset(Font source, string assetPath)
    {
        try
        {
            // Create the TMP font asset in-memory
            TMP_FontAsset tmp = TMP_FontAsset.CreateFontAsset(source);
            if (tmp == null)
            {
                Debug.LogError(
                    "[PackageBuilder] Failed to create TMP_FontAsset from the provided TTF/OTF."
                );
                return null;
            }

            // Ensure it has a material (TMP usually creates one, but be explicit)
            if (tmp.material == null)
            {
                var mat = new Material(Shader.Find("TextMeshPro/Distance Field"));
                tmp.material = mat;
                AssetDatabase.CreateAsset(tmp, assetPath);
                AssetDatabase.AddObjectToAsset(mat, tmp);
                AssetDatabase.SaveAssets();
            }
            else
            {
                // Save as a .asset on disk
                AssetDatabase.CreateAsset(tmp, assetPath);
                AssetDatabase.SaveAssets();
            }

            Debug.Log($"[PackageBuilder] Created TMP Font Asset at {assetPath}");
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        }
        catch (Exception e)
        {
            Debug.LogError($"[PackageBuilder] Exception while creating TMP_FontAsset: {e}");
            return null;
        }
    }

    private static void AssignTMPFontToPrefab(string prefabPath, TMP_FontAsset fontAsset)
    {
        // We need to open the prefab to serialize the assignment
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            // 3D TextMeshPro
            foreach (var tmp in root.GetComponentsInChildren<TextMeshPro>(true))
            {
                if (tmp != null)
                    tmp.font = fontAsset;
            }
            // UI TextMeshProUGUI
            foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (tmp != null)
                    tmp.font = fontAsset;
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log("[PackageBuilder] Assigned TMP Font Asset to all TMP components in prefab.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}

// ────────────────── Installed-version resolver (UPM) ──────────────────
static class UpmInstalledResolver
{
    public static void Resolve(string[] packageIds, Action<Dictionary<string, string>> onDone)
    {
        var want = new HashSet<string>(packageIds ?? Array.Empty<string>());
        var map = new Dictionary<string, string>();

        if (want.Count == 0)
        {
            onDone?.Invoke(map);
            return;
        }

        var req = Client.List(true); // include indirect
        void Poll()
        {
            if (!req.IsCompleted)
                return;
            EditorApplication.update -= Poll;

            if (req.Status == StatusCode.Success)
            {
                foreach (var p in req.Result)
                    if (want.Contains(p.name))
                        map[p.name] = p.version;

                // TMP via uGUI fallback
                if (
                    want.Contains("com.unity.textmeshpro")
                    && !map.ContainsKey("com.unity.textmeshpro")
                )
                {
                    var ugui = req.Result.FirstOrDefault(pi => pi.name == "com.unity.ugui");
                    if (ugui != null)
                        map["com.unity.ugui"] = ugui.version;
                }
            }
            else
            {
                Debug.LogError(
                    "[UPM] Failed to list installed packages; dependencies will be empty."
                );
            }

            foreach (var id in want)
                if (!map.ContainsKey(id) && id != "com.unity.textmeshpro")
                    Debug.LogWarning(
                        $"[UPM] {id} not installed in this project; not adding to dependencies."
                    );

            onDone?.Invoke(map);
        }
        EditorApplication.update += Poll;
    }
}
