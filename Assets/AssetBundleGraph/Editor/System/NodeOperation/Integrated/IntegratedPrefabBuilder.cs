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
			ConnectionPointData inputPoint,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<Asset>> inputGroupAssets, 
			List<string> alreadyCached, 
			Action<ConnectionData, Dictionary<string, List<Asset>>, List<string>> Output) 
		{
			ValidatePrefabBuilder(node, target, inputGroupAssets,
				() => {
					throw new NodeException(node.Name + " :PrefabBuilder is not configured. Please configure from Inspector.", node.Id);
				},
				() => {
					throw new NodeException(node.Name + " :Failed to create PrefabBuilder from settings. Please fix settings from Inspector.", node.Id);
				},
				(string groupKey) => {
					throw new NodeException(string.Format("{0} :Can not create prefab with incoming assets for group {1}.", node.Name, groupKey), node.Id);
				}
			);

			var builder = PrefabBuilderUtility.CreatePrefabBuilder(node, target);
			UnityEngine.Assertions.Assert.IsNotNull(builder);

			var prefabOutputDir = FileUtility.EnsurePrefabBuilderCacheDirExists(target, node);
			Dictionary<string, List<Asset>> output = new Dictionary<string, List<Asset>>();

			foreach(var key in inputGroupAssets.Keys) {
				output[key] = builder.CreatePrefab(key, inputGroupAssets[key], prefabOutputDir, false);
			}

			Output(connectionToOutput, output, null);

//			var prefabOutputDir = FileUtility.EnsurePrefabBuilderCacheDirExists(target, node);				
//			var outputDict = new Dictionary<string, List<Asset>>();
//			
//			foreach (var groupKey in inputGroupAssets.Keys) {
//				var assets = inputGroupAssets[groupKey];
//
//				// collect generated prefab path.
//				var generated = new List<string>();
//				
//				/*
//					BuildPrefab(string prefabName) method.
//				*/
//				Func<string, string> BuildPrefab = (string prefabName) => {
//					var newPrefabOutputPath = Path.Combine(prefabOutputDir, prefabName);
//					generated.Add(newPrefabOutputPath);
//					isBuildPrefabFunctionCalled = true;
//
//					return newPrefabOutputPath;
//				};
//
//				ValidateCanCreatePrefab(target, node, groupKey, assets, prefabOutputDir, BuildPrefab);
//
//				if (!isBuildPrefabFunctionCalled) {
//					Debug.LogWarning(node.Name +": BuildPrefab delegate was not called. Prefab might not be created properly.");
//				}
//
//				foreach (var generatedPrefabPath in generated) {
//					var newAsset = Asset.CreateNewAssetWithImportPathAndStatus(
//						generatedPrefabPath,
//						true,// absolutely new in setup.
//						false
//					);
//
//					if (!outputDict.ContainsKey(groupKey)) outputDict[groupKey] = new List<Asset>();
//					outputDict[groupKey].Add(newAsset);
//				}
//
//				/*
//					add input sources for next node.
//				*/
//				if (!outputDict.ContainsKey(groupKey)) outputDict[groupKey] = new List<Asset>();
//				outputDict[groupKey].AddRange(assets);
//			} 				
//
//			Output(connectionToOutput, outputDict, null);
		}

		public void Run (BuildTarget target, 
			NodeData node, 
			ConnectionPointData inputPoint,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<Asset>> inputGroupAssets, 
			List<string> alreadyCached, 
			Action<ConnectionData, Dictionary<string, List<Asset>>, List<string>> Output) 
		{

			var builder = PrefabBuilderUtility.CreatePrefabBuilder(node, target);
			UnityEngine.Assertions.Assert.IsNotNull(builder);

			var prefabOutputDir = FileUtility.EnsurePrefabBuilderCacheDirExists(target, node);
			Dictionary<string, List<Asset>> output = new Dictionary<string, List<Asset>>();

			foreach(var key in inputGroupAssets.Keys) {
				output[key] = builder.CreatePrefab(key, inputGroupAssets[key], prefabOutputDir, true);
			}

			Output(connectionToOutput, output, null);

//			var cachedItems = new List<string>();
//			
//			var invalids = new List<string>();
//			foreach (var sources in inputGroupAssets.Values) {
//				foreach (var source in sources) {
//					if (string.IsNullOrEmpty(source.importFrom)) {
//						invalids.Add(source.absoluteAssetPath);
//					}
//				}
//			}
//			
//			if (invalids.Any()) {
//				throw new NodeException(string.Join(", ", invalids.ToArray()) + " are not imported yet. These assets need to be imported before used to build prefab.", node.Id);
//			}
//
//			var prefabOutputDir = FileUtility.EnsurePrefabBuilderCacheDirExists(target, node);
//			var outputDict = new Dictionary<string, List<Asset>>();
//			var cachedOrGenerated = new List<string>();
//
//			foreach (var groupKey in inputGroupAssets.Keys) {
//				var assets = inputGroupAssets[groupKey];
//
//				// collect generated prefab path.
//				var generated = new List<string>();
//				var outputSources = new List<Asset>();
//
//				/*
//					BuildPrefab(GameObject baseObject, string prefabName, bool forceGenerate) method.
//				*/
//				Func<GameObject, string, bool, string> BuildPrefab = (GameObject baseObject, string prefabName, bool forceGenerate) => {
//					var newPrefabOutputPath = Path.Combine(prefabOutputDir, prefabName);
//					
//					if (forceGenerate || !SystemDataUtility.IsAllAssetsCachedAndUpdated(assets, alreadyCached, newPrefabOutputPath)) {
//						// not cached, create new.
//						UnityEngine.Object prefabFile = PrefabUtility.CreateEmptyPrefab(newPrefabOutputPath);
//					
//						// export prefab data.
//						PrefabUtility.ReplacePrefab(baseObject, prefabFile);
//
//						// save prefab.
//						AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
//						AssetDatabase.SaveAssets();
//						generated.Add(newPrefabOutputPath);
//						cachedOrGenerated.Add(newPrefabOutputPath);
//						Debug.Log(node.Name + " created new prefab: " + newPrefabOutputPath );
//					} else {
//						// cached.
//						cachedItems.Add(newPrefabOutputPath);
//						cachedOrGenerated.Add(newPrefabOutputPath);
//						Debug.Log(node.Name + " used cached prefab: " + newPrefabOutputPath );
//					}
//
//					isBuildPrefabFunctionCalled = true;
//
//					return newPrefabOutputPath;
//				};
//
//				/*
//					execute inheritee's input method.
//				*/
//				try {
//					CreatePrefab(target, node, groupKey, assets, prefabOutputDir, BuildPrefab);
//				} catch (Exception e) {
//					Debug.LogError(node.Name + " Error:" + e);
//					throw new NodeException(node.Name + " Error:" + e, node.Id);
//				}
//
//				if (!isBuildPrefabFunctionCalled) {
//					Debug.LogWarning(node.Name +": BuildPrefab delegate was not called. Prefab might not be created properly.");
//				}
//
//				/*
//					ready assets-output-data from this node to next output.
//					it contains "cached" or "generated as prefab" or "else" assets.
//					output all assets.
//				*/
//				var currentAssetsInThisNode = FileUtility.GetFilePathsInFolder(prefabOutputDir);
//				foreach (var generatedCandidateAssetPath in currentAssetsInThisNode) {
//					
//					/*
//						candidate is new, regenerated prefab.
//					*/
//					if (generated.Contains(generatedCandidateAssetPath)) {
//						var newAsset = Asset.CreateNewAssetWithImportPathAndStatus(
//							generatedCandidateAssetPath,
//							true,
//							false
//						);
//						outputSources.Add(newAsset);
//						continue;
//					}
//					
//					/*
//						candidate is not new prefab.
//					*/
//					var cachedPrefabAsset = Asset.CreateNewAssetWithImportPathAndStatus(
//						generatedCandidateAssetPath,
//						false,
//						false
//					);
//					outputSources.Add(cachedPrefabAsset);
//				}
//
//
//				/*
//					add current resources to next node's resources.
//				*/
//				outputSources.AddRange(assets);
//
//				outputDict[groupKey] = outputSources;
//			}
//
//			// delete unused cached prefabs.
//			var unusedCachePaths = alreadyCached.Except(cachedOrGenerated).Where(path => !FileUtility.IsMetaFile(path)).ToList();
//			foreach (var unusedCachePath in unusedCachePaths) {
//				FileUtility.DeleteFileThenDeleteFolderIfEmpty(unusedCachePath);
//			}
//
//			Output(connectionToOutput, outputDict, cachedItems);
		}

		public static void ValidatePrefabBuilder (
			NodeData node,
			BuildTarget target,
			Dictionary<string, List<Asset>> inputGroupAssets,
			Action noBuilderData,
			Action failedToCreateBuilder,
			Action<string> canNotCreatePrefab
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
						if(!builder.CanCreatePrefab(key, assets)) {
							canNotCreatePrefab(key);
						}
					}
				}
			}
		}			
	}
}