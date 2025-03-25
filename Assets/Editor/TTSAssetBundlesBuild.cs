using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetBundles {
    public class TTSAssetBundlesBuild {
        private const string fileExt = ".unity3d";
        private static readonly string rootFolder = Path.GetDirectoryName(Application.dataPath);

        public static string[] GetAssetBundleNames() {
            return AssetDatabase.GetAllAssetBundleNames();
        }

        public static void BuildList(List<string> items, string outputPath) {
            List<AssetBundleBuild> buildCandidates = new List<AssetBundleBuild>();
            string fullPath = ConstructFullPath(outputPath);
            PrepareDirs(fullPath);
            PrepareBuildCandidates(items, buildCandidates);
            Debug.Log("Building Asset Bundles...");
            BuildPipeline.BuildAssetBundles(fullPath, buildCandidates.ToArray(), BuildAssetBundleOptions.None, EditorUserBuildSettings.selectedStandaloneTarget);
            Cleanup(fullPath);
            Debug.Log($"Finished building {buildCandidates.Count} Asset Bundles");
            Debug.Log($"Build complete! Check {fullPath}");
        }

        public static string ConstructFullPath(string relativePath) {
            return Path.GetFullPath(Path.Combine(rootFolder, relativePath));
        }

        public static bool ValidatePath(string fullPath) {
            return fullPath != rootFolder && fullPath.StartsWith(rootFolder, StringComparison.OrdinalIgnoreCase);
        }

        private static void PrepareBuildCandidates(List<string> items, List<AssetBundleBuild> buildCandidates) {
            Debug.Log($"Preparing {items.Count} build candidates...");
            foreach (string item in items) {
                AssetBundleBuild build = new AssetBundleBuild();
                string[] paths = AssetDatabase.GetAssetPathsFromAssetBundle(item);

                build.assetBundleName = item;
                build.assetNames = paths;
                buildCandidates.Add(build);
            }
        }

        public static void DeleteList(List<string> items) {
            foreach (string item in items) {
                AssetDatabase.RemoveAssetBundleName(item, true);
            }
        }

        private static void Cleanup(string destination) {
            Debug.Log("Cleanup destination...");
            string[] files = Directory.GetFiles(destination, "*", SearchOption.AllDirectories);
            foreach (string file in files) {
                CleanupFile(file);
            }
        }

        private static void CleanupFile(string file) {
            if (Path.GetFileName(file) == "AssetBundles" || file.EndsWith(".manifest")) {
                Debug.Log($"Removed {file}");
                File.Delete(file);
            } else if (!file.EndsWith(fileExt)) {
                string fileWithExt = Path.ChangeExtension(file, fileExt);
                if (File.Exists(fileWithExt)) {
                    File.Delete(fileWithExt);
                }
                Debug.Log($"Saved to {fileWithExt}");
                File.Move(file, fileWithExt);
            }
        }

        private static void PrepareDirs(string fullPath) {
            if (!ValidatePath(fullPath)) {
                Debug.LogError($"Expected root: {rootFolder}");
                throw new UnauthorizedAccessException($"Attempted path traversal outside of the project directory: {fullPath}");
            }

            if (!Directory.Exists(fullPath)) {
                Directory.CreateDirectory(fullPath);
            }
        }
    }
}
