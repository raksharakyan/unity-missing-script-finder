// MissingScriptFinder.cs
// Drop into any Editor/ folder in your Unity project.
// Open via: Tools → Missing Script Finder
//
// Features:
//   - Scans the currently open scene for missing scripts
//   - Scans ALL scenes in the project (without opening them)
//   - Scans all Prefabs in the project
//   - Click any result to ping the GameObject in the Hierarchy
//   - Checkbox per result — remove selected items only
//   - Remove selected OR remove all — works on both scenes AND prefabs
//   - Prefab changes saved back to disk automatically via PrefabUtility
//   - Select All / None shortcuts
//   - Export results to a .txt log file

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MissingScriptTools.Editor
{
    public class MissingScriptFinder : EditorWindow
    {
        // ── Data ───────────────────────────────────────────────────────────────

        private enum ScanMode { CurrentScene, AllScenes, Prefabs }

        private class Result
        {
            public string     SceneOrAsset;    // scene name or prefab path
            public string     GameObjectPath;  // full hierarchy path
            public int        ComponentIndex;  // index of the missing component
            public GameObject Go;              // live ref (scene) or prefab root
            public string     PrefabAssetPath; // set only for prefab results
            public bool       Selected = true;
        }

        private ScanMode     _mode         = ScanMode.CurrentScene;
        private List<Result> _results      = new List<Result>();
        private Vector2      _scroll;
        private bool         _scanning     = false;
        private string       _statusMsg    = "";
        private bool         _statusErr    = false;
        private string       _searchFilter = "";

        // ── Menu ──────────────────────────────────────────────────────────────

        [MenuItem("Tools/Missing Script Finder")]
        public static void Open()
        {
            var w = GetWindow<MissingScriptFinder>("Missing Script Finder");
            w.minSize = new Vector2(480, 400);
        }

        // ── GUI ───────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            DrawHeader();
            DrawModeSelector();
            DrawActionBar();
            DrawSearchBar();
            DrawResults();
            DrawStatus();
        }

        void DrawHeader()
        {
            GUILayout.Space(8);
            var title = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField("  Missing Script Finder", title, GUILayout.Height(26));
            EditorGUILayout.LabelField("Scan scenes & prefabs for broken component references",
                new GUIStyle(EditorStyles.centeredGreyMiniLabel), GUILayout.Height(16));
            GUILayout.Space(6);
            DrawLine();
        }

        void DrawModeSelector()
        {
            GUILayout.Space(6);
            EditorGUILayout.LabelField("Scan target", EditorStyles.boldLabel);
            GUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            DrawModeButton("Current scene", ScanMode.CurrentScene, "ti-home");
            DrawModeButton("All scenes",    ScanMode.AllScenes,    "ti-stack");
            DrawModeButton("All prefabs",   ScanMode.Prefabs,      "ti-box");
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);
            DrawLine();
        }

        void DrawModeButton(string label, ScanMode mode, string icon)
        {
            bool active = _mode == mode;
            var style = new GUIStyle(GUI.skin.button)
            {
                fontStyle = active ? FontStyle.Bold : FontStyle.Normal
            };
            if (active)
            {
                GUI.backgroundColor = new Color(0.4f, 0.5f, 1f, 0.4f);
            }
            if (GUILayout.Button(label, style, GUILayout.Height(28)))
            {
                _mode = mode;
                _results.Clear();
                _statusMsg = "";
            }
            GUI.backgroundColor = Color.white;
        }

        void DrawActionBar()
        {
            GUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = !_scanning;
            if (GUILayout.Button("🔍  Scan", GUILayout.Height(32), GUILayout.Width(90)))
                RunScan();

            GUI.enabled = _results.Count > 0;

            // Select all / none
            if (GUILayout.Button("All", GUILayout.Height(32), GUILayout.Width(38)))
                _results.ForEach(r => r.Selected = true);
            if (GUILayout.Button("None", GUILayout.Height(32), GUILayout.Width(44)))
                _results.ForEach(r => r.Selected = false);

            int selectedCount = _results.FindAll(r => r.Selected).Count;
            GUI.enabled = selectedCount > 0;
            if (GUILayout.Button($"🗑  Remove selected ({selectedCount})", GUILayout.Height(32)))
                RemoveSelected();

            GUI.enabled = _results.Count > 0;
            if (GUILayout.Button("🗑  Remove all", GUILayout.Height(32)))
                RemoveAllMissing();

            if (GUILayout.Button("💾  Export", GUILayout.Height(32), GUILayout.Width(72)))
                ExportLog();

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(6);
            DrawLine();
        }

        void DrawSearchBar()
        {
            GUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Filter:", GUILayout.Width(42));
            _searchFilter = EditorGUILayout.TextField(_searchFilter);
            if (GUILayout.Button("✕", GUILayout.Width(24)))
                _searchFilter = "";
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4);
        }

        void DrawResults()
        {
            var filtered = string.IsNullOrEmpty(_searchFilter)
                ? _results
                : _results.FindAll(r =>
                    r.GameObjectPath.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    r.SceneOrAsset.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0);

            // Summary bar
            var summaryStyle = new GUIStyle(EditorStyles.helpBox) { fontSize = 12 };
            if (_results.Count == 0)
            {
                EditorGUILayout.LabelField(_scanning ? "Scanning..." : "No results yet — hit Scan to start.",
                    summaryStyle, GUILayout.Height(24));
            }
            else
            {
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = filtered.Count > 0
                    ? new Color(1f, 0.4f, 0.3f, 0.25f)
                    : new Color(0.3f, 0.9f, 0.5f, 0.25f);
                EditorGUILayout.LabelField(
                    filtered.Count > 0
                        ? $"  ⚠  {filtered.Count} missing script(s) found"
                        : "  ✓  No missing scripts — all clear!",
                    summaryStyle, GUILayout.Height(24));
                GUI.backgroundColor = prev;
            }

            GUILayout.Space(4);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            string lastScene = null;
            foreach (var r in filtered)
            {
                // Scene/asset group header
                if (r.SceneOrAsset != lastScene)
                {
                    GUILayout.Space(6);
                    EditorGUILayout.LabelField(r.SceneOrAsset,
                        new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 });
                    lastScene = r.SceneOrAsset;
                }

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                // Checkbox for selective removal
                r.Selected = EditorGUILayout.Toggle(r.Selected, GUILayout.Width(18));

                // Warning icon
                EditorGUILayout.LabelField(
                    EditorGUIUtility.IconContent("console.warnicon.sml"),
                    GUILayout.Width(20), GUILayout.Height(20));

                // Path + component index
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(r.GameObjectPath,
                    new GUIStyle(EditorStyles.label) { wordWrap = true });
                EditorGUILayout.LabelField($"Component index: {r.ComponentIndex}",
                    EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                // Ping button (only works for current scene results)
                GUI.enabled = r.Go != null;
                if (GUILayout.Button("Select", GUILayout.Width(54), GUILayout.Height(36)))
                {
                    Selection.activeGameObject = r.Go;
                    EditorGUIUtility.PingObject(r.Go);
                }
                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(2);
            }

            EditorGUILayout.EndScrollView();
        }

        void DrawStatus()
        {
            if (string.IsNullOrEmpty(_statusMsg)) return;
            GUILayout.Space(6);
            var style = new GUIStyle(EditorStyles.helpBox) { wordWrap = true };
            style.normal.textColor = _statusErr ? Color.red : new Color(0.2f, 0.75f, 0.3f);
            EditorGUILayout.LabelField(_statusMsg, style);
        }

        // ── Scan logic ────────────────────────────────────────────────────────

        void RunScan()
        {
            _results.Clear();
            _statusMsg = "";
            _scanning  = true;
            Repaint();

            try
            {
                switch (_mode)
                {
                    case ScanMode.CurrentScene: ScanCurrentScene(); break;
                    case ScanMode.AllScenes:    ScanAllScenes();    break;
                    case ScanMode.Prefabs:      ScanPrefabs();      break;
                }

                _statusMsg = _results.Count == 0
                    ? "✓ Scan complete — no missing scripts found."
                    : $"⚠ Scan complete — {_results.Count} missing script(s) found.";
                _statusErr = _results.Count > 0;
            }
            catch (Exception e)
            {
                _statusMsg = $"Error during scan: {e.Message}";
                _statusErr = true;
                Debug.LogException(e);
            }
            finally
            {
                _scanning = false;
                Repaint();
            }
        }

        // ── Current scene ─────────────────────────────────────────────────────

        void ScanCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                SetStatus("No active scene open.", error: true);
                return;
            }

            foreach (var root in scene.GetRootGameObjects())
                ScanGameObject(root, scene.name, keepRef: true);
        }

        // ── All scenes ────────────────────────────────────────────────────────

        void ScanAllScenes()
        {
            // Save dirty scenes first
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            string originalScenePath = SceneManager.GetActiveScene().path;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                EditorUtility.DisplayProgressBar(
                    "Scanning scenes", path, (float)Array.IndexOf(guids, guid) / guids.Length);

                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (var root in scene.GetRootGameObjects())
                    ScanGameObject(root, Path.GetFileNameWithoutExtension(path), keepRef: false);
            }

            EditorUtility.ClearProgressBar();

            // Restore original scene
            if (!string.IsNullOrEmpty(originalScenePath))
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
        }

        // ── Prefabs ───────────────────────────────────────────────────────────

        void ScanPrefabs()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            int total = guids.Length;

            for (int i = 0; i < total; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                EditorUtility.DisplayProgressBar("Scanning prefabs", path, (float)i / total);

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                // keepRef: true so we hold the prefab root for editing later
                // PrefabAssetPath is passed so RemoveSelected/All can save it
                ScanGameObject(prefab, path, keepRef: true, prefabAssetPath: path);
            }

            EditorUtility.ClearProgressBar();
        }

        // ── Shared scan ───────────────────────────────────────────────────────

        void ScanGameObject(GameObject go, string sceneOrAsset, bool keepRef,
                            string parentPath = "", string prefabAssetPath = null)
        {
            string path = string.IsNullOrEmpty(parentPath) ? go.name : $"{parentPath}/{go.name}";

            var components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    _results.Add(new Result
                    {
                        SceneOrAsset    = sceneOrAsset,
                        GameObjectPath  = path,
                        ComponentIndex  = i,
                        Go              = keepRef ? go : null,
                        PrefabAssetPath = prefabAssetPath
                    });
                }
            }

            foreach (Transform child in go.transform)
                ScanGameObject(child.gameObject, sceneOrAsset, keepRef, path, prefabAssetPath);
        }

        // ── Remove all missing ────────────────────────────────────────────────

        void RemoveSelected()
        {
            var toRemove = _results.FindAll(r => r.Selected && r.Go != null);
            if (toRemove.Count == 0)
            {
                SetStatus("No items selected — tick the checkboxes next to the ones you want to remove.", error: true);
                return;
            }

            bool confirm = EditorUtility.DisplayDialog(
                "Remove selected missing scripts",
                $"Remove {toRemove.Count} selected missing script reference(s)?\n\nThis cannot be undone.",
                "Remove", "Cancel");

            if (!confirm) return;

            DoRemove(toRemove);
            _results.RemoveAll(r => r.Selected && r.Go != null);
            SetStatus($"✓ Done. {_results.Count} item(s) remaining.", error: false);
            Repaint();
        }

        void RemoveAllMissing()
        {
            var removable = _results.FindAll(r => r.Go != null);
            if (removable.Count == 0)
            {
                SetStatus("Nothing to remove — run a Current Scene or Prefabs scan first.", error: true);
                return;
            }

            bool confirm = EditorUtility.DisplayDialog(
                "Remove ALL missing scripts",
                $"Remove ALL {removable.Count} missing script reference(s)?\n\nThis cannot be undone.",
                "Remove all", "Cancel");

            if (!confirm) return;

            DoRemove(removable);
            _results.Clear();
            SetStatus("✓ All missing scripts removed. Save your scene/prefabs to keep changes.", error: false);
            Repaint();
        }

        /// Shared removal logic — handles both scene GameObjects and prefab assets.
        void DoRemove(List<Result> items)
        {
            // Group by prefab asset path (null = scene object)
            var sceneObjects  = new HashSet<GameObject>();
            var prefabAssets  = new Dictionary<string, GameObject>(); // path → prefab root

            foreach (var r in items)
            {
                if (r.Go == null) continue;

                if (string.IsNullOrEmpty(r.PrefabAssetPath))
                {
                    // Scene GameObject
                    if (!sceneObjects.Contains(r.Go))
                    {
                        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(r.Go);
                        sceneObjects.Add(r.Go);
                    }
                }
                else
                {
                    // Prefab asset — collect roots, process once per prefab
                    if (!prefabAssets.ContainsKey(r.PrefabAssetPath))
                        prefabAssets[r.PrefabAssetPath] = r.Go;
                }
            }

            // Remove from prefabs and save back to disk
            foreach (var kvp in prefabAssets)
            {
                // We need to traverse the whole prefab root, not just one GO
                RemoveMissingFromHierarchy(kvp.Value);
                PrefabUtility.SavePrefabAsset(GetPrefabRoot(kvp.Value));
            }

            // Mark scene dirty if we touched scene objects
            if (sceneObjects.Count > 0)
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            AssetDatabase.SaveAssets();
        }

        void RemoveMissingFromHierarchy(GameObject go)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            foreach (Transform child in go.transform)
                RemoveMissingFromHierarchy(child.gameObject);
        }

        GameObject GetPrefabRoot(GameObject go)
        {
            // Walk up to find the actual prefab root asset
            var root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
            return root != null ? root : go;
        }

        // ── Export log ────────────────────────────────────────────────────────

        void ExportLog()
        {
            if (_results.Count == 0)
            {
                SetStatus("Nothing to export — run a scan first.", error: true);
                return;
            }

            string path = EditorUtility.SaveFilePanel(
                "Save missing script log", Application.dataPath,
                $"MissingScripts_{DateTime.Now:yyyyMMdd_HHmmss}", "txt");

            if (string.IsNullOrEmpty(path)) return;

            var sb = new StringBuilder();
            sb.AppendLine($"Missing Script Report — {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Scan mode: {_mode}");
            sb.AppendLine($"Total found: {_results.Count}");
            sb.AppendLine(new string('-', 60));

            string lastScene = null;
            foreach (var r in _results)
            {
                if (r.SceneOrAsset != lastScene)
                {
                    sb.AppendLine();
                    sb.AppendLine($"[{r.SceneOrAsset}]");
                    lastScene = r.SceneOrAsset;
                }
                sb.AppendLine($"  {r.GameObjectPath}  (component index: {r.ComponentIndex})");
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            SetStatus($"✓ Log saved to: {path}", error: false);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        void SetStatus(string msg, bool error)
        {
            _statusMsg = msg;
            _statusErr = error;
            if (error) Debug.LogWarning("[MissingScriptFinder] " + msg);
            else       Debug.Log("[MissingScriptFinder] " + msg);
        }

        void DrawLine()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.2f));
        }
    }
}
