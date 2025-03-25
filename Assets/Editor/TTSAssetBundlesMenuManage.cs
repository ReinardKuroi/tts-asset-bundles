using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace AssetBundles {
    public class TTSAssetBundlesMenuManage : EditorWindow {
        private const string TogglePrefsKey = "com.company.TTSAssetBundles.MenuManageAsset.BundleToggles";
        private const string DestPrefsKeys = "com.company.TTSAssetBundles.MenuManageAsset.BundleDestination";
        private const string AllTogglePrefsKey = "com.company.TTSAssetBundles.MenuManageAsset.BundleToggleAll";
        private const char PrefsSeparator = '§';
        private const int MaxErrDisplayRows = 7;

        private readonly Dictionary<string, bool> assetBundleToggles = new Dictionary<string, bool>();

        private Vector2 scroll;
        private bool toggleAll;
        private string outputPath;
        private bool showErr;

        [MenuItem("Assets/Manage Asset Bundles")]
        public static void OpenWindow() {
            GetWindow<TTSAssetBundlesMenuManage>("Manage Asset Bundles");
        }

        private void OnGUI() {
            GUILayout.Space(12);
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Settings", EditorStyles.boldLabel);
            RenderOutputFolderSetting();
            EditorGUILayout.EndVertical();
            GUILayout.Space(12);
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Select Asset Bundles to Build or Delete", EditorStyles.boldLabel);
            RenderToggleAll(assetBundleToggles);
            RenderRefresh(assetBundleToggles);
            RenderToggles(assetBundleToggles);
            EditorGUILayout.EndVertical();
            RenderActionButtons();
            GUILayout.Space(12);
        }

        private void OnEnable() {
            InitToggleData(assetBundleToggles);
            LoadMenuData(assetBundleToggles);
        }

        private void OnDestroy() {
            SaveMenuData(assetBundleToggles);
        }

        private void RenderOutputFolderSetting() {
            EditorGUI.BeginChangeCheck();
            outputPath = EditorGUILayout.TextField("Save to:", outputPath);
            if (EditorGUI.EndChangeCheck()) {
                showErr =  !TTSAssetBundlesBuild.ValidatePath(TTSAssetBundlesBuild.ConstructFullPath(outputPath));
            }
            if (showErr) {
                GUIStyle warningStyle = new GUIStyle(EditorStyles.label) {
                    normal = { textColor = Color.red },
                    fontSize = 10
                };
                GUILayout.Label("Invalid path. Ensure it's within the project directory.", warningStyle);
            }
        }

        private void RenderRefresh(Dictionary<string, bool> toggles) {
            if (toggles.Count == 0) {
                GUILayout.Space(24);
                if (GUILayout.Button("Refresh")) {
                    OnEnable();
                }
                GUILayout.Space(24);
            }
        }

        private void RenderActionButtons() {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(12);
            EditorGUI.BeginDisabledGroup(assetBundleToggles.All(x => !x.Value));
            if (GUILayout.Button("Delete Selected") && ConfirmDeletionPopup(assetBundleToggles)) {
                DeleteSelected(assetBundleToggles);
            }
            GUILayout.Space(24);
            if (GUILayout.Button("Build Selected")) {
                BuildSelected(assetBundleToggles);
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.Space(12);
            EditorGUILayout.EndHorizontal();
        }

        private bool ConfirmDeletionPopup(Dictionary<string, bool> toggles) {
            string query;
            List<string> selected = Selected(toggles);
            if (selected.Count > MaxErrDisplayRows) {
                string deletionCandidated = string.Join("\n * ", selected.Take(MaxErrDisplayRows).ToList());
                int extraRows = selected.Count - MaxErrDisplayRows;
                query = $"Delete selected items?\n\n * {deletionCandidated}\n * ...\nand {extraRows} more";
            } else {
                string deletionCandidated = string.Join("\n * ", selected);
                query = $"Delete selected items?\n\n * {deletionCandidated}";
            }
           
            return EditorUtility.DisplayDialog("Delete selected", query, "Confirm", "Cancel");
        }

        private List<string> Selected(Dictionary<string, bool> toggles) {
            return toggles.Where(x => x.Value).Select(x => x.Key).ToList();
        }

        private void RenderToggles(Dictionary<string, bool> toggleList) {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUI.BeginChangeCheck();
            foreach (string name in toggleList.Keys.ToList()) {
                toggleList[name] = EditorGUILayout.ToggleLeft(name, toggleList[name]);
            }
            if (EditorGUI.EndChangeCheck()) {
                SyncToggle(assetBundleToggles);
            }
            EditorGUILayout.EndScrollView();
        }

        private void InitToggleData(Dictionary<string, bool> toggles) {
            toggles.Clear();
            foreach (string name in TTSAssetBundlesBuild.GetAssetBundleNames()) {
                toggles[name] = false;
            }
        }

        private void LoadMenuData(Dictionary<string, bool> toggles) {
            HashSet<string> togglesOn;
            string data = EditorPrefs.GetString(TogglePrefsKey, "");

            outputPath = EditorPrefs.GetString(DestPrefsKeys, "AssetBundles");
            toggleAll = EditorPrefs.GetBool(AllTogglePrefsKey, false);
            togglesOn = new HashSet<string>(data.Split(PrefsSeparator));
            foreach (string name in toggles.Keys.ToList()) {
                if (toggleAll || togglesOn.Contains(name)) {
                    toggles[name] = true;
                }
            }
        }

        private void SaveMenuData(Dictionary<string, bool> toggles) {
            string data = string.Join(PrefsSeparator.ToString(), Selected(toggles));
            EditorPrefs.SetString(TogglePrefsKey, data);
            EditorPrefs.SetString(DestPrefsKeys, outputPath);
            EditorPrefs.SetBool(AllTogglePrefsKey, toggleAll);
        }

        private void RenderToggleAll(Dictionary<string, bool> toggles) {
            EditorGUI.BeginChangeCheck();
            toggleAll = EditorGUILayout.ToggleLeft("All", toggleAll, EditorStyles.boldLabel);
            if (EditorGUI.EndChangeCheck()) {
                foreach (string toggleName in toggles.Keys.ToList()) {
                    toggles[toggleName] = toggleAll;
                }
            }
        }

        private void SyncToggle(Dictionary<string, bool> toggles) {
            toggleAll = toggles.All(x => x.Value);
        }

        private void BuildSelected(Dictionary<string, bool> toggles) {
            SaveMenuData(toggles);
            TTSAssetBundlesBuild.BuildList(Selected(toggles), outputPath);
            OnEnable();
        }

        private void DeleteSelected(Dictionary<string, bool> toggles) {
            SaveMenuData(toggles);
            TTSAssetBundlesBuild.DeleteList(Selected(toggles));
            OnEnable();
        }
    }
}
