using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

namespace AssetBundleGraph {

	public class IntegratedPrefabBuilder : INodeOperation {

		public void Setup (BuildTarget target, 
			NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			ValidatePrefabBuilder(node, target, incoming,
				() => {
					throw new NodeException(node.Name + " :PrefabBuilder is not configured. Please configure from Inspector.", node.Id);
				},
				() => {
					throw new NodeException(node.Name + " :Failed to create PrefabBuilder from settings. Please fix settings from Inspector.", node.Id);
				},
				(string groupKey) => {
					throw new NodeException(string.Format("{0} :Can not create prefab with incoming assets for group {1}.", node.Name, groupKey), node.Id);
				},
				(AssetReference badAsset) => {
					throw new NodeException(string.Format("{0} :Can not import incoming asset {1}.", node.Name, badAsset.fileNameAndExtension), node.Id);
				}
			);

			if(incoming == null) {
				return;
			}

			var builder = PrefabBuilderUtility.CreatePrefabBuilder(node, target);
			UnityEngine.Assertions.Assert.IsNotNull(builder);


			var prefabOutputDir = FileUtility.EnsurePrefabBuilderCacheDirExists(target, node);
			Dictionary<string, List<AssetReference>> output = null;
			if(Output != null) {
				output = new Dictionary<string, List<AssetReference>>();
			}

			var aggregatedGroups = new Dictionary<string, List<AssetReference>>();
			foreach(var ag in incoming) {
				foreach(var key in ag.assetGroups.Keys) {
					if(!aggregatedGroups.ContainsKey(key)){
						aggregatedGroups[key] = new List<AssetReference>();
					}
					aggregatedGroups[key].AddRange(ag.assetGroups[key].AsEnumerable());
				}
			}

			foreach(var key in aggregatedGroups.Keys) {

				var assets = aggregatedGroups[key];
				var thresold = PrefabBuilderUtility.GetPrefabBuilderAssetThreshold(node.ScriptClassName);
				if( thresold < assets.Count ) {
					var guiName = PrefabBuilderUtility.GetPrefabBuilderGUIName(node.ScriptClassName);
					throw new NodeException(string.Format("{0} :Too many assets passed to {1} for group:{2}. {3}'s threshold is set to {4}", 
						node.Name, guiName, key, guiName,thresold), node.Id);
				}

				List<UnityEngine.Object> allAssets = LoadAllAssets(assets);
				var prefabFileName = builder.CanCreatePrefab(key, allAssets);
				if(output != null && prefabFileName != null) {
					output[key] = new List<AssetReference> () {
						AssetReferenceDatabase.GetPrefabReference(FileUtility.PathCombine(prefabOutputDir, prefabFileName + ".prefab"))
					};
				}
				UnloadAllAssets(assets);
			}

			if(Output != null) {
				var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
					null : connectionsToOutput.First();
				Output(dst, output);
			}
		}

		private static List<UnityEngine.Object> LoadAllAssets(List<AssetReference> assets) {
			List<UnityEngine.Object> objects = new List<UnityEngine.Object>();

			foreach(var a in assets) {
				objects.AddRange(a.allData.AsEnumerable());
			}
			return objects;
		}

		private static void UnloadAllAssets(List<AssetReference> assets) {
			assets.ForEach(a => a.ReleaseData());
		}

		public void Run (BuildTarget target, 
			NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output,
			Action<NodeData, string, float> progressFunc) 
		{
			if(incoming == null) {
				return;
			}

			var builder = PrefabBuilderUtility.CreatePrefabBuilder(node, target);
			UnityEngine.Assertions.Assert.IsNotNull(builder);

			var prefabOutputDir = FileUtility.EnsurePrefabBuilderCacheDirExists(target, node);
			Dictionary<string, List<AssetReference>> output = null;
			if(Output != null) {
				output = new Dictionary<string, List<AssetReference>>();
			}

			var aggregatedGroups = new Dictionary<string, List<AssetReference>>();
			foreach(var ag in incoming) {
				foreach(var key in ag.assetGroups.Keys) {
					if(!aggregatedGroups.ContainsKey(key)){
						aggregatedGroups[key] = new List<AssetReference>();
					}
					aggregatedGroups[key].AddRange(ag.assetGroups[key].AsEnumerable());
				}
			}

			foreach(var key in aggregatedGroups.Keys) {

				var assets = aggregatedGroups[key];

				var allAssets = LoadAllAssets(assets);

				var prefabFileName = builder.CanCreatePrefab(key, allAssets);
				var prefabSavePath = FileUtility.PathCombine(prefabOutputDir, prefabFileName + ".prefab");

				if (!Directory.Exists(Path.GetDirectoryName(prefabSavePath))) {
					Directory.CreateDirectory(Path.GetDirectoryName(prefabSavePath));
				}

				if(PrefabBuildInfo.DoesPrefabNeedRebuilding(node, target, key, assets)) {
					UnityEngine.GameObject obj = builder.CreatePrefab(key, allAssets);
					if(obj == null) {
						throw new AssetBundleGraphException(string.Format("{0} :PrefabBuilder {1} returned null in CreatePrefab() [groupKey:{2}]", 
							node.Name, builder.GetType().FullName, key));
					}

					LogUtility.Logger.LogFormat(LogType.Log, "{0} is (re)creating Prefab:{1} with {2}({3})", node.Name, prefabFileName,
						PrefabBuilderUtility.GetPrefabBuilderGUIName(node.ScriptClassName),
						PrefabBuilderUtility.GetPrefabBuilderVersion(node.ScriptClassName));

					if(progressFunc != null) progressFunc(node, string.Format("Creating {0}", prefabFileName), 0.5f);

					PrefabUtility.CreatePrefab(prefabSavePath, obj, node.ReplacePrefabOptions);
					PrefabBuildInfo.SavePrefabBuildInfo(node, target, key, assets);
					GameObject.DestroyImmediate(obj);
				}
				UnloadAllAssets(assets);

				if(output != null) {
					output[key] = new List<AssetReference> () {
						AssetReferenceDatabase.GetPrefabReference(prefabSavePath)
					};
				}
			}

			if(Output != null) {
				var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
					null : connectionsToOutput.First();
				Output(dst, output);
			}
		}

		public static void ValidatePrefabBuilder (
			NodeData node,
			BuildTarget target,
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			Action noBuilderData,
			Action failedToCreateBuilder,
			Action<string> canNotCreatePrefab,
			Action<AssetReference> canNotImportAsset
		) {
			if(string.IsNullOrEmpty(node.InstanceData[target])) {
				noBuilderData();
			}

			var builder = PrefabBuilderUtility.CreatePrefabBuilder(node, target);

			if(null == builder ) {
				failedToCreateBuilder();
			}

			if(null != builder && null != incoming) {
				foreach(var ag in incoming) {
					foreach(var key in ag.assetGroups.Keys) {
						var assets = ag.assetGroups[key];
						if(assets.Any()) {
							bool isAllGoodAssets = true;
							foreach(var a in assets) {
								if(string.IsNullOrEmpty(a.importFrom)) {
									canNotImportAsset(a);
									isAllGoodAssets = false;
								}
							}
							if(isAllGoodAssets) {
								// do not call LoadAllAssets() unless all assets have importFrom
								var al = ag.assetGroups[key];
								List<UnityEngine.Object> allAssets = LoadAllAssets(al);
								if(string.IsNullOrEmpty(builder.CanCreatePrefab(key, allAssets))) {
									canNotCreatePrefab(key);
								}
								UnloadAllAssets(al);
							}
						}
					}
				}
			}
		}			
	}
}