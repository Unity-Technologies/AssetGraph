using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {
    [System.Serializable]
    [CustomAssetGenerator("Duplicate Asset", "v1.0", 1)]
    public class Duplicator : IAssetGenerator {

        public string GetAssetExtension (AssetReference asset) {
            return asset.extension;
        }

        public Type GetAssetType(AssetReference asset) {
            return asset.filterType;
        }

        public bool CanGenerateAsset (AssetReference asset, out string message) {
            message = "";
            return true;
        }

        public bool GenerateAsset (AssetReference asset, string generateAssetPath) {

            bool generated = false;

            try {
                string fullPath = FileUtility.PathCombine (Directory.GetParent(Application.dataPath).ToString(), generateAssetPath);

                File.Copy(asset.absolutePath, fullPath, true);
                generated = true;
            } catch(Exception e) {
                LogUtility.Logger.LogError ("Duplicator", e.Message);
            }

            return generated;
        }

        public void OnInspectorGUI (Action onValueChanged) {
            // do nothing
        }
    }
}