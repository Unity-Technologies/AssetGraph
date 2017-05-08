using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Assertions;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;

/**
 * AssetBundles-Browser integration
 * 
 * set ASSETBUNDLE_BROWSER_INSTALLED to 1 if you install AssetBundles-Browser
 * and want to use the integration.
 * 
 * AssetBundles-Browser Available at:
 * https://github.com/hiroki-o/AssetBundles-Browser
 */

#if ASSETBUNDLE_BROWSER_INSTALLED

using UnityEngine.AssetBundles.AssetBundleOperation;
using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {
	public class GraphToolABOperation : ABOperation
    {
		public string Name {
			get {
				return "AssetBundles";
			}
		}

		public string ProviderName {
			get {
				return "GraphTool";
			}
		}

		public string[] GetAssetPathsFromAssetBundle (string assetBundleName) {
			return AssetBundleBuildMap.GetBuildMap ().GetAssetPathsFromAssetBundle (assetBundleName);
		}

		public string GetAssetBundleName(string assetPath) {
			return AssetBundleBuildMap.GetBuildMap ().GetAssetBundleName (assetPath);
		}

		public string GetImplicitAssetBundleName(string assetPath) {
			return AssetBundleBuildMap.GetBuildMap ().GetImplicitAssetBundleName (assetPath);
		}

		public string[] GetAllAssetBundleNames() {
			return AssetBundleBuildMap.GetBuildMap ().GetAllAssetBundleNames ();
		}

		public bool IsReadOnly() {
			return true;
		}

		public void SetAssetBundleNameAndVariant (string assetPath, string bundleName, string variantName) {
			// readonly. do nothing
		}

		public void RemoveUnusedAssetBundleNames() {
			// readonly. do nothing
		}

		public bool CanSpecifyBuildTarget { 
			get { return true; } 
		}
		public bool CanSpecifyBuildOutputDirectory { 
			get { return false; } 
		}

		public bool CanSpecifyBuildOptions { 
			get { return false; } 
		}

		public bool BuildAssetBundles (ABBuildInfo info) {
			
            AssetBundleBuildMap.GetBuildMap ().Clear ();

			var guids = AssetDatabase.FindAssets(Model.Settings.GRAPH_SEARCH_CONDITION);

			foreach(var guid in guids) {
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var graph = AssetDatabase.LoadAssetAtPath<Model.ConfigGraph>(path);
				if (!graph.UseAsAssetPostprocessor) {
					var result = AssetBundleGraphUtility.ExecuteGraph (info.buildTarget, graph);
					if (result.IsAnyIssueFound) {
						return false;
					}
				}
			}

			return true;
		}
    }

	[CustomABOperationProvider("AssetBundle Graph Tool")]
	public class AssetDatabaseABOperationProvider : ABOperationProvider
	{
		public int GetABOperationCount () {
			return 1;
		}
		public ABOperation CreateOperation(int index) {
			return new GraphToolABOperation ();
		}
	}
}

#endif
