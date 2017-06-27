/**
 * AssetBundles-Browser integration
 * 
 * This code will setup the output of the graph tool to be viewable in the browser.
 * 
 * AssetBundles-Browser Available at:
 * https://github.com/Unity-Technologies/AssetBundles-Browser
 */
 
using UnityEditor;
using Model = UnityEngine.AssetBundles.GraphTool.DataModel.Version2;
using System;
using System.Collections.Generic;

namespace UnityEngine.AssetBundles.AssetBundleDataSource
{
    public partial struct ABBuildInfo { }
    public partial interface ABDataSource { }
}

namespace UnityEngine.AssetBundles.GraphTool {
	public class GraphToolABDataSource : AssetBundleDataSource.ABDataSource
    {
        public static List<AssetBundleDataSource.ABDataSource> CreateDataSources()
        {
            var op = new GraphToolABDataSource();
            var retList = new List<AssetBundleDataSource.ABDataSource>();
            retList.Add(op);
            return retList;
        }

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

		public bool BuildAssetBundles (AssetBundleDataSource.ABBuildInfo info) {
			
            AssetBundleBuildMap.GetBuildMap ().Clear ();

			var guids = AssetDatabase.FindAssets(Model.Settings.GRAPH_SEARCH_CONDITION);

			foreach(var guid in guids) {
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var graph = AssetDatabase.LoadAssetAtPath<Model.ConfigGraph>(path);
				if (!graph.UseAsAssetPostprocessor) {
                    Type infoType = info.GetType();
                    var targetInfo = infoType.GetProperty("buildTarget");
                    if (targetInfo.GetValue(info, null) is BuildTarget)
                    {
                        BuildTarget target = (BuildTarget)targetInfo.GetValue(info, null);
                        var result = AssetBundleGraphUtility.ExecuteGraph(target, graph);
                        if (result.IsAnyIssueFound)
                        {
                            return false;
                        }
                    }
				}
			}

			return true;
		}
    }
}
