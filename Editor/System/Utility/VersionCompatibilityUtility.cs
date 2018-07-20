using UnityEngine;
using UnityEditor;
using System;
using System.Collections;

namespace Unity.AssetGraph {

    public static class VersionCompatibilityUtility {

        public static string UpdateClassName(string className) {
            if (string.IsNullOrEmpty(className))
            {
                return className;
            }
            
            // v1.3 -> v1.4
            return className.Replace("UnityEngine.AssetBundles.GraphTool.", "Unity.AssetGraph.");
        }
    }
}
