using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace AssetBundleGraph {
	public class PrefabricatorBase : INodeBase {
		public void Setup (string nodeName, string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {			
			var invalids = new List<string>();
			foreach (var sources in groupedSources.Values) {
				foreach (var source in sources) {
					if (string.IsNullOrEmpty(source.importedPath)) {
						invalids.Add(source.pathUnderSourceBase);
					}
				}
			}

			if (invalids.Any()) {
				throw new NodeException(string.Join(", ", invalids.ToArray()) + " are not imported yet. These assets need to be imported before prefabricated.", nodeId);
			}
				
			var recommendedPrefabOutputDirectoryPath = FileController.PathCombine(AssetBundleGraphSettings.PREFABRICATOR_CACHE_PLACE, nodeId, GraphStackController.GetCurrentPlatformPackageFolder());				
			var outputDict = new Dictionary<string, List<InternalAssetData>>();
			
			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];

				var recommendedPrefabPath = FileController.PathCombine(recommendedPrefabOutputDirectoryPath, groupKey);
				if (!recommendedPrefabPath.EndsWith(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR.ToString())) recommendedPrefabPath = recommendedPrefabPath + AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR.ToString();
				
				/*
					ready input resource info for execute. not contains cache in this node.
				*/
				var assets = new List<AssetInfo>();
				foreach (var assetData in inputSources) {
					var assetName = assetData.fileNameAndExtension;
					var assetType = assetData.assetType;
					var assetPath = assetData.importedPath;
					var assetId = assetData.assetId;
					assets.Add(new AssetInfo(assetName, assetType, assetPath, assetId));
				}

				// collect generated prefab path.
				var generated = new List<string>();
				
				/*
					Prefabricate(string prefabName) method.
				*/
				Func<string, string> Prefabricate = (string prefabName) => {
					var newPrefabOutputPath = Path.Combine(recommendedPrefabPath, prefabName);
					generated.Add(newPrefabOutputPath);
					isPrefabricateFunctionCalled = true;

					return newPrefabOutputPath;
				};

				EstimatePrefab(nodeName, nodeId, groupKey, assets, recommendedPrefabPath, Prefabricate);

				if (!isPrefabricateFunctionCalled) {
					Debug.LogWarning(nodeName +": Prefabricate delegate was not called. Prefab might not be created properly.");
				}

				foreach (var generatedPrefabPath in generated) {
					var newAsset = InternalAssetData.InternalAssetDataGeneratedByImporterOrModifierOrPrefabricator(
						generatedPrefabPath,
						string.Empty,// dummy data
						typeof(string),// dummy data
						true,// absolutely new in setup.
						false
					);

					if (!outputDict.ContainsKey(groupKey)) outputDict[groupKey] = new List<InternalAssetData>();
					outputDict[groupKey].Add(newAsset);
				}
				outputDict[groupKey].AddRange(inputSources);
			
			} 				

			Output(nodeId, labelToNext, outputDict, new List<string>());

		}

		public void Run (string nodeName, string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			var usedCache = new List<string>();
			
			var invalids = new List<string>();
			foreach (var sources in groupedSources.Values) {
				foreach (var source in sources) {
					if (string.IsNullOrEmpty(source.importedPath)) {
						invalids.Add(source.pathUnderSourceBase);
					}
				}
			}
			
			if (invalids.Any()) {
				throw new NodeException(string.Join(", ", invalids.ToArray()) + " are not imported yet. These assets need to be imported before prefabricated.", nodeId);
			}
			
			var recommendedPrefabOutputDirectoryPath = FileController.PathCombine(AssetBundleGraphSettings.PREFABRICATOR_CACHE_PLACE, nodeId, GraphStackController.GetCurrentPlatformPackageFolder());
			
			var outputDict = new Dictionary<string, List<InternalAssetData>>();
			var cachedOrGenerated = new List<string>();

			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];

				var recommendedPrefabPath = FileController.PathCombine(recommendedPrefabOutputDirectoryPath, groupKey);
				if (!recommendedPrefabPath.EndsWith(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR.ToString())) recommendedPrefabPath = recommendedPrefabPath + AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR.ToString();
				
				/*
					ready input resource info for execute. not contains cache in this node.
				*/
				var assets = new List<AssetInfo>();
				foreach (var assetData in inputSources) {
					var assetName = assetData.fileNameAndExtension;
					var assetType = assetData.assetType;
					var assetPath = assetData.importedPath;
					var assetId = assetData.assetId;
					assets.Add(new AssetInfo(assetName, assetType, assetPath, assetId));
				}

				// collect generated prefab path.
				var generated = new List<string>();
				var outputSources = new List<InternalAssetData>();
				
				
				/*
					Prefabricate(GameObject baseObject, string prefabName, bool forceGenerate) method.
				*/
				Func<GameObject, string, bool, string> Prefabricate = (GameObject baseObject, string prefabName, bool forceGenerate) => {
					var newPrefabOutputPath = Path.Combine(recommendedPrefabPath, prefabName);
					
					if (forceGenerate || !GraphStackController.IsCachedForEachSource(inputSources, alreadyCached, newPrefabOutputPath)) {
						// not cached, create new.
						UnityEngine.Object prefabFile = PrefabUtility.CreateEmptyPrefab(newPrefabOutputPath);
					
						// export prefab data.
						PrefabUtility.ReplacePrefab(baseObject, prefabFile);

						// save prefab.
						AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
						AssetDatabase.SaveAssets();
						generated.Add(newPrefabOutputPath);
						cachedOrGenerated.Add(newPrefabOutputPath);
						Debug.Log(nodeName + " created new prefab: " + newPrefabOutputPath );
					} else {
						// cached.
						usedCache.Add(newPrefabOutputPath);
						cachedOrGenerated.Add(newPrefabOutputPath);
						Debug.Log(nodeName + " used cached prefab: " + newPrefabOutputPath );
					}

					isPrefabricateFunctionCalled = true;

					return newPrefabOutputPath;
				};

				if (!Directory.Exists(recommendedPrefabPath)) {
					// create recommended directory.
					Directory.CreateDirectory(recommendedPrefabPath);
				}

				/*
					execute inheritee's input method.
				*/
				try {
					CreatePrefab(nodeName, nodeId, groupKey, assets, recommendedPrefabPath, Prefabricate);
				} catch (Exception e) {
					Debug.LogError(nodeName + " Error:" + e);
					throw new NodeException(nodeName + " Error:" + e, nodeId);
				}

				if (!isPrefabricateFunctionCalled) {
					Debug.LogWarning(nodeName +": Prefabricate delegate was not called. Prefab might not be created properly.");
				}

				/*
					ready assets-output-data from this node to next output.
					it contains "cached" or "generated as prefab" or "else" assets.
					output all assets.
				*/
				var currentAssetsInThisNode = FileController.FilePathsInFolder(recommendedPrefabPath);
				foreach (var generatedCandidateAssetPath in currentAssetsInThisNode) {
					
					/*
						candidate is new, regenerated prefab.
					*/
					if (generated.Contains(generatedCandidateAssetPath)) {
						var newAsset = InternalAssetData.InternalAssetDataGeneratedByImporterOrModifierOrPrefabricator(
							generatedCandidateAssetPath,
							AssetDatabase.AssetPathToGUID(generatedCandidateAssetPath),
							AssetBundleGraphInternalFunctions.GetAssetType(generatedCandidateAssetPath),
							true,
							false
						);
						outputSources.Add(newAsset);
						continue;
					}
					
					/*
						candidate is not new prefab.
					*/
					var cachedPrefabAsset = InternalAssetData.InternalAssetDataGeneratedByImporterOrModifierOrPrefabricator(
						generatedCandidateAssetPath,
						AssetDatabase.AssetPathToGUID(generatedCandidateAssetPath),
						AssetBundleGraphInternalFunctions.GetAssetType(generatedCandidateAssetPath),
						false,
						false
					);
					outputSources.Add(cachedPrefabAsset);
				}


				/*
					add current resources to next node's resources.
				*/
				outputSources.AddRange(inputSources);

				outputDict[groupKey] = outputSources;
			}

			// delete unused cached prefabs.
			var unusedCachePaths = alreadyCached.Except(cachedOrGenerated).Where(path => !GraphStackController.IsMetaFile(path)).ToList();
			foreach (var unusedCachePath in unusedCachePaths) {
				// unbundlize unused prefabricated cached asset.
				var assetImporter = AssetImporter.GetAtPath(unusedCachePath);
  				assetImporter.assetBundleName = string.Empty;

				FileController.DeleteFileThenDeleteFolderIfEmpty(unusedCachePath);
			}


			Output(nodeId, labelToNext, outputDict, usedCache);
		}

		private bool isPrefabricateFunctionCalled = false;

		public virtual void EstimatePrefab (string nodeName, string nodeId, string groupKey, List<AssetInfo> sources, string recommendedPrefabOutputDir, Func<string, string> Prefabricate) {
			Debug.LogError(nodeName + ":Subclass did not implement \"EstimatePrefab ()\" method:" + this);
			throw new NodeException(nodeName + ":Subclass did not implement \"EstimatePrefab ()\" method:" + this, nodeId);
		}

		public virtual void CreatePrefab (string nodeName, string nodeId, string groupKey, List<AssetInfo> sources, string recommendedPrefabOutputDir, Func<GameObject, string, bool, string> Prefabricate) {
			Debug.LogError(nodeName + ":Subclass did not implement \"CreatePrefab ()\" method:" + this);
			throw new NodeException(nodeName + ":Subclass did not implement \"EstimatePrefab ()\" method:" + this, nodeId);
		}


		public static void ValidatePrefabScriptType (string prefabScriptType, Action NullOrEmpty, Action PrefabTypeIsNull) {
			if (string.IsNullOrEmpty(prefabScriptType)) NullOrEmpty();
			var loadedType = System.Reflection.Assembly.GetExecutingAssembly().CreateInstance(prefabScriptType);
			if (loadedType == null) PrefabTypeIsNull();
		}
	}
}