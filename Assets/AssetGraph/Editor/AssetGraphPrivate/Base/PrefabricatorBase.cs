using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace AssetGraph {
	public class PrefabricatorBase : INodeBase {
		public void Setup (string nodeId, string labelToNext, string package, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {			
			var validation = true;
			foreach (var sources in groupedSources.Values) {
				foreach (var source in sources) {
					if (string.IsNullOrEmpty(source.importedPath)) {
						Debug.LogError("resource:" + source.pathUnderSourceBase + " is not imported yet, should import before prefabricate.");
						validation = false;
					}
				}
			}

			if (!validation) return;

			/*
				through all.
			*/
			var outputDict = new Dictionary<string, List<InternalAssetData>>();

			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];
				outputDict[groupKey] = inputSources;
			};

			Output(nodeId, labelToNext, outputDict, new List<string>());
		}

		public void Run (string nodeId, string labelToNext, string package, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			var usedCache = new List<string>();

			var validation = true;
			foreach (var sources in groupedSources.Values) {
				foreach (var source in sources) {
					if (string.IsNullOrEmpty(source.importedPath)) {
						Debug.LogError("resource:" + source.pathUnderSourceBase + " is not imported yet, should import before prefabricate.");
						validation = false;
					}
				}
			}

			if (!validation) return;

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

				var outputSources = new List<InternalAssetData>();

				Func<GameObject, string, string> Prefabricate = (GameObject baseObject, string prefabName) => {
					var newPrefabOutputPath = Path.Combine(recommendedPrefabPath, prefabName);
					
					if (!GraphStackController.IsCachedForEachSource(inputSources, alreadyCached, newPrefabOutputPath)) {
						
						// not cached, create new.
						UnityEngine.Object prefabFile = PrefabUtility.CreateEmptyPrefab(newPrefabOutputPath);
					
						// export prefab data.
						PrefabUtility.ReplacePrefab(baseObject, prefabFile);

						// save prefab.
						AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
						AssetDatabase.SaveAssets();
						generated.Add(newPrefabOutputPath);
					} else {
						// cached.
						usedCache.Add(newPrefabOutputPath);
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
					In(groupKey, assets, recommendedPrefabPath, Prefabricate);
				} catch (Exception e) {
					Debug.LogError("Prefabricator:" + this + " error:" + e);
				}

				if (!isUsed) {
					Debug.LogWarning("should use 'Prefabricate' method for create prefab in Prefabricator for cache.");
				}
				

				// generate next output.

				/*
					add assets in this node to next output.
					it contains "cached" or "generated as prefab" or "else" assets.
					output all assets.
				*/
				var currentAssetsInThisNode = FileController.FilePathsInFolder(recommendedPrefabPath);
				foreach (var newAssetPath in currentAssetsInThisNode) {
					if (generated.Contains(newAssetPath)) {
						var newAsset = InternalAssetData.InternalAssetDataGeneratedByImporterOrPrefabricator(
							newAssetPath,
							AssetDatabase.AssetPathToGUID(newAssetPath),
							AssetGraphInternalFunctions.GetAssetType(newAssetPath),
							true
						);
						outputSources.Add(newAsset);
					} else {
						var newAsset = InternalAssetData.InternalAssetDataGeneratedByImporterOrPrefabricator(
							newAssetPath,
							AssetDatabase.AssetPathToGUID(newAssetPath),
							AssetGraphInternalFunctions.GetAssetType(newAssetPath),
							false
						);
						outputSources.Add(newAsset);
					}
				}


				/*
					add current resources to next node's resources.
				*/
				foreach (var assetData in inputSources) {
					var inheritedInternalAssetData = InternalAssetData.InternalAssetDataByImporter(
						assetData.traceId,
						assetData.absoluteSourcePath,
						assetData.sourceBasePath,
						assetData.fileNameAndExtension,
						assetData.pathUnderSourceBase,
						assetData.importedPath,
						assetData.assetId,
						assetData.assetType
					);
					outputSources.Add(inheritedInternalAssetData);
				}


				outputDict[groupKey] = outputSources;
			}

			Output(nodeId, labelToNext, outputDict, usedCache);
		}

		private bool isUsed = false;
		private void PrefabricateIsUsed () {
			isUsed = true;
		}

		public virtual void In (string groupKey, List<AssetInfo> source, string recommendedPrefabOutputDir, Func<GameObject, string, string> Prefabricate) {
			Debug.LogError("should implement \"public override void In (List<AssetGraph.AssetInfo> source, string recommendedPrefabOutputDir)\" in class:" + this);
		}
	}
}