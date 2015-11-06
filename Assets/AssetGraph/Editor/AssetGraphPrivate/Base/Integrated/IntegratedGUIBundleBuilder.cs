using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace AssetGraph {
	public class IntegratedGUIBundleBuilder : INodeBase {
		private readonly List<string> bundleOptions;
		private readonly List<string> relatedNodeIds;

		public IntegratedGUIBundleBuilder (List<string> bundleOptions, List<string> relatedNodeIds) {
			this.bundleOptions = bundleOptions;
			this.relatedNodeIds = relatedNodeIds;
		}

		public void Setup (string nodeId, string labelToNext, string package, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			var outputDict = new Dictionary<string, List<InternalAssetData>>();
			outputDict["0"] = new List<InternalAssetData>();

			foreach (var groupKey in groupedSources.Keys) {
				var outputSources = groupedSources[groupKey];
				outputDict["0"].AddRange(outputSources);
			}

			RemoveOtherPlatformBundleSettings(relatedNodeIds, package);
			
			Output(nodeId, labelToNext, outputDict, new List<string>());
		}
		
		public void Run (string nodeId, string labelToNext, string package, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			RemoveOtherPlatformBundleSettings(relatedNodeIds, package);

			var recommendedBundleOutputDirSource = FileController.PathCombine(AssetGraphSettings.BUNDLEBUILDER_CACHE_PLACE, nodeId);
			var recommendedBundleOutputDir = FileController.PathCombine(recommendedBundleOutputDirSource, EditorUserBuildSettings.activeBuildTarget.ToString());
			if (!Directory.Exists(recommendedBundleOutputDir)) Directory.CreateDirectory(recommendedBundleOutputDir);

			var outputDict = new Dictionary<string, List<InternalAssetData>>();
			outputDict["0"] = new List<InternalAssetData>();

			var assetBundleOptions = BuildAssetBundleOptions.None;

			foreach (var enabled in bundleOptions) {
				switch (enabled) {
					case "Uncompressed AssetBundle": {
						assetBundleOptions = assetBundleOptions | BuildAssetBundleOptions.UncompressedAssetBundle;
						break;
					}
					case "Disable Write TypeTree": {
						assetBundleOptions = assetBundleOptions | BuildAssetBundleOptions.DisableWriteTypeTree;
						break;
					}
					case "Deterministic AssetBundle": {
						assetBundleOptions = assetBundleOptions | BuildAssetBundleOptions.DeterministicAssetBundle;
						break;
					}
					case "Force Rebuild AssetBundle": {
						assetBundleOptions = assetBundleOptions | BuildAssetBundleOptions.ForceRebuildAssetBundle;
						break;
					}
					case "Ignore TypeTree Changes": {
						assetBundleOptions = assetBundleOptions | BuildAssetBundleOptions.IgnoreTypeTreeChanges;
						break;
					}
					case "Append Hash To AssetBundle Name": {
						assetBundleOptions = assetBundleOptions | BuildAssetBundleOptions.AppendHashToAssetBundleName;
						break;
					}
				}
			}



			BuildPipeline.BuildAssetBundles(recommendedBundleOutputDir, assetBundleOptions, EditorUserBuildSettings.activeBuildTarget);

			var outputSources = new List<InternalAssetData>();

			var generatedAssetBundlePaths = FileController.FilePathsInFolder(recommendedBundleOutputDir);
			foreach (var newAssetPath in generatedAssetBundlePaths) {
				var newAssetData = InternalAssetData.InternalAssetDataGeneratedByBundleBuilder(newAssetPath);
				outputSources.Add(newAssetData);
			}

			outputDict["0"] = outputSources;
			
			var usedCache = new List<string>(alreadyCached);
			Output(nodeId, labelToNext, outputDict, usedCache);
		}

		private void RemoveOtherPlatformBundleSettings (List<string> nodeIds, string package) {
			if (!Directory.Exists(AssetGraphSettings.APPLICATIONDATAPATH_CACHE_PATH)) return;

			var cachedNodeKindFolderPaths = FileController.FolderPathsInFolder(AssetGraphSettings.APPLICATIONDATAPATH_CACHE_PATH);
			foreach (var cachedNodeKindFolderPath in cachedNodeKindFolderPaths) {
				var nodeIdFolderPaths = FileController.FolderPathsInFolder(cachedNodeKindFolderPath);
				foreach (var nodeIdFolderPath in nodeIdFolderPaths) {
					var nodeIdFromFolder = nodeIdFolderPath.Split(AssetGraphSettings.UNITY_FOLDER_SEPARATOR).Last();

					// remove all bundle settings from resources of unrelated nodes.
					if (!nodeIds.Contains(nodeIdFromFolder)) {
						RemoveBundleSettings(nodeIdFolderPath);
						continue;
					}

					// remove all bundle settings from resources of unrelated platforms.
					var platformFolderPaths = FileController.FolderPathsInFolder(nodeIdFolderPath);
					foreach (var platformFolderPath in platformFolderPaths) {
						var platformNameFromFolder = platformFolderPath.Split(AssetGraphSettings.UNITY_FOLDER_SEPARATOR).Last();
						if (platformNameFromFolder == GraphStackController.Current_Platform_Package_Folder(package)) continue;

						RemoveBundleSettings(platformFolderPath);
					}		
				}
			}
		}

		private void RemoveBundleSettings (string nodePath) {
			var filePathsInFolder = FileController.FilePathsInFolder(nodePath);
			foreach (var filePath in filePathsInFolder) {
				var assetImporter = AssetImporter.GetAtPath(filePath);
				assetImporter.assetBundleName = string.Empty;
			}
		}
	}
}