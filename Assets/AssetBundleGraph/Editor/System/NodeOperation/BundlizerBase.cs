// using UnityEngine;
// using UnityEditor;

// using System;
// using System.IO;
// using System.Linq;
// using System.Collections.Generic;

// namespace AssetGraph {
// 	public class BundlizerBase : INodeOperationBase {
// 		public void Setup (string nodeName, string nodeId, string labelToNext, string package, Dictionary<string, List<Asset>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output) {
// 			var outputDict = new Dictionary<string, List<Asset>>();

// 			foreach (var groupKey in groupedSources.Keys) {
// 				var outputSources = new List<Asset>();
// 				outputDict[groupKey] = outputSources;
// 			}
			
// 			Output(nodeId, labelToNext, outputDict, new List<string>());
// 		}
		
// 		public void Run (string nodeName, string nodeId, string labelToNext, string package, Dictionary<string, List<Asset>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output) {
// 			var recommendedBundleOutputDir = FileUtility.PathCombine(AssetGraphSettings.BUNDLIZER_CACHE_PLACE, nodeId);
// 			FileUtility.RemakeDirectory(recommendedBundleOutputDir);

// 			var outputDict = new Dictionary<string, List<Asset>>();

// 			foreach (var groupKey in groupedSources.Keys) {
// 				var inputSources = groupedSources[groupKey];

// 				var assets = new List<DepreacatedAssetInfo>();
// 				foreach (var assetData in inputSources) {
// 					var assetName = assetData.fileNameAndExtension;
// 					var assetType = assetData.assetType;
// 					var assetPath = assetData.importFrom;
// 					var assetDatabaseId = assetData.assetDatabaseId;
// 					assets.Add(new DepreacatedAssetInfo(assetName, assetType, assetPath, assetDatabaseId));
// 				}

// 				var localFilePathsBeforeBundlize = FileUtility.FilePathsInFolder(AssetGraphSettings.UNITY_LOCAL_DATAPATH);
// 				try {
// 					In(groupKey, assets, recommendedBundleOutputDir);
// 				} catch (Exception e) {
// 					Debug.LogError("Bundlizer:" + this + " error:" + e);
// 				}

// 				AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
// 				AssetDatabase.SaveAssets();

// 				var localFilePathsAfterBundlize = FileUtility.FilePathsInFolder(AssetGraphSettings.UNITY_LOCAL_DATAPATH);
				
// 				var outputSources = new List<Asset>();

// 				var generatedAssetBundlePaths = localFilePathsAfterBundlize.Except(localFilePathsBeforeBundlize);

// 				foreach (var newAssetPath in generatedAssetBundlePaths) {
// 					if (GraphStackController.IsMetaFile(newAssetPath)) continue;
// 					var newAssetData = Asset.InternalAssetDataGeneratedByImporterOrPrefabricator(
// 						newAssetPath,
// 						AssetDatabase.AssetPathToGUID(newAssetPath),
// 						AssetGraphInternalFunctions.GetTypeOfAsset(newAssetPath),
// 						true,
// 						false
// 					);
// 					outputSources.Add(newAssetData);
// 				}

// 				outputDict[groupKey] = outputSources;
// 			}

// 			Output(nodeId, labelToNext, outputDict, new List<string>());
// 		}

// 		public virtual void In (string groupkey, List<DepreacatedAssetInfo> source, string recommendedBundleOutputDir) {
// 			Debug.LogError("should implement \"public override void In (List<AssetGraph.DepreacatedAssetInfo> source, string recommendedBundleOutputDir)\" in class:" + this);
// 		}
// 	}
// }