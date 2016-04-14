using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace AssetGraph {
	public class PrefabricatorBase : INodeBase {
		public void Setup (string nodeId, string labelToNext, string package, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {			
			var invalids = new List<string>();
			foreach (var sources in groupedSources.Values) {
				foreach (var source in sources) {
					if (string.IsNullOrEmpty(source.importedPath)) {
						invalids.Add(source.pathUnderSourceBase);
					}
				}
			}

			if (invalids.Any()) {
				throw new Exception("prefabricator:" + string.Join(", ", invalids.ToArray()) + " are not imported yet, should import before prefabricate.");
			}
			
			var recommendedPrefabOutputDirectoryPath = FileController.PathCombine(AssetGraphSettings.PREFABRICATOR_CACHE_PLACE, nodeId, GraphStackController.Current_Platform_Package_Folder(package));
			
			var outputDict = new Dictionary<string, List<InternalAssetData>>();
			
			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];

				var recommendedPrefabPath = FileController.PathCombine(recommendedPrefabOutputDirectoryPath, groupKey);
				if (!recommendedPrefabPath.EndsWith(AssetGraphSettings.UNITY_FOLDER_SEPARATOR.ToString())) recommendedPrefabPath = recommendedPrefabPath + AssetGraphSettings.UNITY_FOLDER_SEPARATOR.ToString();
				
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
					// set used.
					PrefabricateIsUsed();

					return newPrefabOutputPath;
				};
				
				/*
					execute inheritee's input method.
				*/
				try {
					Estimate(groupKey, assets, recommendedPrefabPath, Prefabricate);
				} catch (Exception e) {
					Debug.LogError("Prefabricator:" + this + " error:" + e);
				}

				if (!isUsed) {
					Debug.LogWarning("should use 'Prefabricate' method for create prefab in Prefabricator for cache.");
				}
				
				foreach (var generatedPrefabPath in generated) {
					var newAsset = InternalAssetData.InternalAssetDataGeneratedByImporterOrPrefabricator(
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

		public void Run (string nodeId, string labelToNext, string package, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
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
				throw new Exception("prefabricator:" + string.Join(", ", invalids.ToArray()) + " are not imported yet, should import before prefabricate.");
			}
			
			var recommendedPrefabOutputDirectoryPath = FileController.PathCombine(AssetGraphSettings.PREFABRICATOR_CACHE_PLACE, nodeId, GraphStackController.Current_Platform_Package_Folder(package));
			
			var outputDict = new Dictionary<string, List<InternalAssetData>>();
			var cachedOrGenerated = new List<string>();

			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];

				var recommendedPrefabPath = FileController.PathCombine(recommendedPrefabOutputDirectoryPath, groupKey);
				if (!recommendedPrefabPath.EndsWith(AssetGraphSettings.UNITY_FOLDER_SEPARATOR.ToString())) recommendedPrefabPath = recommendedPrefabPath + AssetGraphSettings.UNITY_FOLDER_SEPARATOR.ToString();
				
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
						Debug.Log("AssetGraph prefab:" + newPrefabOutputPath + " is newly generated.");
					} else {
						// cached.
						usedCache.Add(newPrefabOutputPath);
						cachedOrGenerated.Add(newPrefabOutputPath);
						Debug.Log("AssetGraph prefab:" + newPrefabOutputPath + " is already cached. if want to regenerate forcely, set Prefabricate(baseObject, prefabName, true) <- forcely regenerate prefab.");
					}

					// set used.
					PrefabricateIsUsed();

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
					Run(groupKey, assets, recommendedPrefabPath, Prefabricate);
				} catch (Exception e) {
					Debug.LogError("Prefabricator:" + this + " error:" + e);
				}

				if (!isUsed) {
					Debug.LogWarning("should use 'Prefabricate' method for create prefab in Prefabricator for cache.");
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
						var newAsset = InternalAssetData.InternalAssetDataGeneratedByImporterOrPrefabricator(
							generatedCandidateAssetPath,
							AssetDatabase.AssetPathToGUID(generatedCandidateAssetPath),
							AssetGraphInternalFunctions.GetAssetType(generatedCandidateAssetPath),
							true,
							false
						);
						outputSources.Add(newAsset);
						continue;
					}
					
					/*
						candidate is not new prefab.
					*/
					var cachedPrefabAsset = InternalAssetData.InternalAssetDataGeneratedByImporterOrPrefabricator(
						generatedCandidateAssetPath,
						AssetDatabase.AssetPathToGUID(generatedCandidateAssetPath),
						AssetGraphInternalFunctions.GetAssetType(generatedCandidateAssetPath),
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

		private bool isUsed = false;
		private void PrefabricateIsUsed () {
			isUsed = true;
		}
		
		public virtual void Estimate (string groupKey, List<AssetInfo> sources, string recommendedPrefabOutputDir, Func<string, string> Prefabricate) {
			Debug.LogError("should implement \"public override void Estimate (List<AssetGraph.AssetInfo> source, string recommendedPrefabOutputDir)\" in class:" + this);
		}


		public virtual void Run (string groupKey, List<AssetInfo> sources, string recommendedPrefabOutputDir, Func<GameObject, string, bool, string> Prefabricate) {
			Debug.LogError("should implement \"public override void Run (List<AssetGraph.AssetInfo> source, string recommendedPrefabOutputDir)\" in class:" + this);
		}


		public static void ValidatePrefabScriptType (string prefabScriptType, Action NullOrEmpty, Action PrefabTypeIsNull) {
			if (string.IsNullOrEmpty(prefabScriptType)) NullOrEmpty();
			var loadedType = System.Reflection.Assembly.GetExecutingAssembly().CreateInstance(prefabScriptType);
			if (loadedType == null) PrefabTypeIsNull();
		}
	}
}