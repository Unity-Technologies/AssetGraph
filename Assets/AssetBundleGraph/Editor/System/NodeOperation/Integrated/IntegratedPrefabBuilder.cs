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
			ConnectionData connectionFromInput,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<AssetReference>> inputGroupAssets, 
			PerformGraph.Output Output) 
		{
			Profiler.BeginSample("AssetBundleGraph.GUIPrefabBuilder.Setup");
			ValidatePrefabBuilder(node, target, inputGroupAssets,
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

			var builder = PrefabBuilderUtility.CreatePrefabBuilder(node, target);
			UnityEngine.Assertions.Assert.IsNotNull(builder);

			var prefabOutputDir = FileUtility.EnsurePrefabBuilderCacheDirExists(target, node);
			Dictionary<string, List<AssetReference>> output = new Dictionary<string, List<AssetReference>>();

			foreach(var key in inputGroupAssets.Keys) {
				List<UnityEngine.Object> allAssets = LoadAllAssets(inputGroupAssets[key]);
				var prefabFileName = builder.CanCreatePrefab(key, allAssets);
				if(prefabFileName != null) {
					output[key] = new List<AssetReference> () {
						AssetReferenceDatabase.GetPrefabReference(FileUtility.PathCombine(prefabOutputDir, prefabFileName + ".prefab"))
					};
				}
				allAssets.ForEach(o => Resources.UnloadAsset(o));
			}

			Output(output);
			Profiler.EndSample();
		}

		private static List<UnityEngine.Object> LoadAllAssets(List<AssetReference> assets) {
			List<UnityEngine.Object> objects = new List<UnityEngine.Object>();
			assets.ForEach(a => objects.AddRange( AssetDatabase.LoadAllAssetsAtPath(a.importFrom).AsEnumerable() ));
			return objects;
		}

		public void Run (BuildTarget target, 
			NodeData node, 
			ConnectionData connectionFromInput,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<AssetReference>> inputGroupAssets, 
			PerformGraph.Output Output) 
		{
			Profiler.BeginSample("AssetBundleGraph.GUIPrefabBuilder.Run");

			var builder = PrefabBuilderUtility.CreatePrefabBuilder(node, target);
			UnityEngine.Assertions.Assert.IsNotNull(builder);

			var prefabOutputDir = FileUtility.EnsurePrefabBuilderCacheDirExists(target, node);
			Dictionary<string, List<AssetReference>> output = new Dictionary<string, List<AssetReference>>();

			foreach(var key in inputGroupAssets.Keys) {
				var allAssets = LoadAllAssets(inputGroupAssets[key]);
				var prefabFileName = builder.CanCreatePrefab(key, allAssets);
				UnityEngine.GameObject obj = builder.CreatePrefab(key, allAssets);
				if(obj == null) {
					throw new AssetBundleGraphException(string.Format("{0} :PrefabBuilder {1} returned null in CreatePrefab() [groupKey:{2}]", 
						node.Name, builder.GetType().FullName, key));
				}
					
				var prefabSavePath = FileUtility.PathCombine(prefabOutputDir, prefabFileName + ".prefab");
				PrefabUtility.CreatePrefab(prefabSavePath, obj, ReplacePrefabOptions.Default);

				output[key] = new List<AssetReference> () {
					AssetReferenceDatabase.GetPrefabReference(prefabSavePath)
				};
				GameObject.DestroyImmediate(obj);
				allAssets.ForEach(o => Resources.UnloadAsset(o));
			}

			Output(output);
			Profiler.EndSample();
		}

		public static void ValidatePrefabBuilder (
			NodeData node,
			BuildTarget target,
			Dictionary<string, List<AssetReference>> inputGroupAssets,
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

			if(null != builder) {
				foreach(var key in inputGroupAssets.Keys) {
					var assets = inputGroupAssets[key];
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
							List<UnityEngine.Object> allAssets = LoadAllAssets(inputGroupAssets[key]);
							if(string.IsNullOrEmpty(builder.CanCreatePrefab(key, allAssets))) {
								canNotCreatePrefab(key);
							}
							allAssets.ForEach(o => Resources.UnloadAsset(o));
						}
					}
				}
			}
		}			
	}
}