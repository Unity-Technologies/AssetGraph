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
			/*
				forcely merge to group ["0"].
				these are came from bundlizer.
			*/
			var outputDict = new Dictionary<string, List<InternalAssetData>>();
			outputDict["0"] = new List<InternalAssetData>();

			foreach (var groupKey in groupedSources.Keys) {
				var outputSources = groupedSources[groupKey];
				outputDict["0"].AddRange(outputSources);
			}

			Output(nodeId, labelToNext, outputDict, new List<string>());
		}
		
		public void Run (string nodeId, string labelToNext, string package, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			RemoveOtherPlatformAndPackageBundleSettings(relatedNodeIds, package);

			var recommendedBundleOutputDirSource = FileController.PathCombine(AssetGraphSettings.BUNDLEBUILDER_CACHE_PLACE, nodeId);
			var recommendedBundleOutputDir = FileController.PathCombine(recommendedBundleOutputDirSource, GraphStackController.Current_Platform_Package_Folder(package));
			if (!Directory.Exists(recommendedBundleOutputDir)) Directory.CreateDirectory(recommendedBundleOutputDir);

			
			/*
				merge multi group into ["0"] group.
			*/
			var intendedAssetNames = new List<string>();
			foreach (var groupKey in groupedSources.Keys) {
				var internalAssetsOfCurrentGroup = groupedSources[groupKey];
				foreach (var internalAsset in internalAssetsOfCurrentGroup) {
					intendedAssetNames.Add(internalAsset.fileNameAndExtension);
					intendedAssetNames.Add(internalAsset.fileNameAndExtension + AssetGraphSettings.MANIFEST_FOOTER);
				}
			}
			


			/*
				platform's bundle & manifest. 
				e.g. iOS & iOS.manifest.
			*/
			var currentPlatform_Package_BundleFile = GraphStackController.Current_Platform_Package_Folder(package);
			var currentPlatform_Package_BundleFileManifest = currentPlatform_Package_BundleFile + AssetGraphSettings.MANIFEST_FOOTER;
			
			intendedAssetNames.Add(currentPlatform_Package_BundleFile);
			intendedAssetNames.Add(currentPlatform_Package_BundleFileManifest);

			/*
				delete not intended assets.
			*/
			foreach (var alreadyCachedPath in alreadyCached) {
				var cachedFileName = Path.GetFileName(alreadyCachedPath);
				if (intendedAssetNames.Contains(cachedFileName)) continue;
				File.Delete(alreadyCachedPath);
			}

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
#if UNITY_5_3
                    case "ChunkBased Compression": {
                        assetBundleOptions = assetBundleOptions | BuildAssetBundleOptions.ChunkBasedCompression;
                        break;
                    }
#endif
				}
			}

			BuildPipeline.BuildAssetBundles(recommendedBundleOutputDir, assetBundleOptions, EditorUserBuildSettings.activeBuildTarget);


			/*
				check assumed bundlized resources and actual generated assetbunles.

				"assuned bundlized resources info from bundlizer" are contained by "actual bundlized resources".
			*/
			var outputDict = new Dictionary<string, List<InternalAssetData>>();
			var outputSources = new List<InternalAssetData>();

			var newAssetPaths = new List<string>();
			var generatedAssetBundlePaths = FileController.FilePathsInFolder(recommendedBundleOutputDir);
			foreach (var newAssetPath in generatedAssetBundlePaths) {
				newAssetPaths.Add(newAssetPath);
				var newAssetData = InternalAssetData.InternalAssetDataGeneratedByBundleBuilder(newAssetPath);
				outputSources.Add(newAssetData);
			}

			// compare, erase & notice.
			var containedAssetBundles = new List<string>();
			
			// collect intended output.
			foreach (var generatedAssetPath in newAssetPaths) {
				var generatedAssetName = Path.GetFileName(generatedAssetPath);

				// collect intended assetBundle & assetBundleManifest file.
				foreach (var bundledName in intendedAssetNames) {
					if (generatedAssetName == bundledName) {
						containedAssetBundles.Add(generatedAssetPath);
						continue;
					}
				
					var bundleManifestName = bundledName + AssetGraphSettings.MANIFEST_FOOTER;
					if (generatedAssetName == bundleManifestName) {
						containedAssetBundles.Add(generatedAssetPath);
						continue;
					}
				}
			}

			var diffs = newAssetPaths.Except(containedAssetBundles);
			foreach (var diff in diffs) {
				Debug.LogWarning("bundleBuilder:AssetBundle:" + diff + " is not intended bundle. please check if unnecessary importer or prefabricator node is exists in graph.");
			}
		
			outputDict["0"] = outputSources;
			
			var usedCache = new List<string>(alreadyCached);
			Output(nodeId, labelToNext, outputDict, usedCache);
		}

		private void RemoveOtherPlatformAndPackageBundleSettings (List<string> nodeIds, string package) {
			if (!Directory.Exists(AssetGraphSettings.APPLICATIONDATAPATH_CACHE_PATH)) return;

			/*
				get all cache folder of node from cache path.
			*/
			var cachedNodeKindFolderPaths = FileController.FolderPathsInFolder(AssetGraphSettings.APPLICATIONDATAPATH_CACHE_PATH);
			foreach (var cachedNodeKindFolderPath in cachedNodeKindFolderPaths) {
				var nodeIdFolderPaths = FileController.FolderPathsInFolder(cachedNodeKindFolderPath);
				foreach (var nodeIdFolderPath in nodeIdFolderPaths) {
					var nodeIdFromFolder = nodeIdFolderPath.Split(AssetGraphSettings.UNITY_FOLDER_SEPARATOR).Last();

					// remove all bundle settings from unrelated nodes.
					if (!nodeIds.Contains(nodeIdFromFolder)) {
						RemoveBundleSettings(nodeIdFolderPath);
						continue;
					}

					// related nodes, contains platform_package folder.
					
					// remove all bundle settings from unrelated platforms + packages.
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
			EditorUtility.DisplayProgressBar("AssetGraph BundleBuilder unbundlize resources...", nodePath, 0);
			var filePathsInFolder = FileController.FilePathsInFolder(nodePath);
			foreach (var filePath in filePathsInFolder) {
				if (GraphStackController.IsMetaFile(filePath)) continue;
				var assetImporter = AssetImporter.GetAtPath(filePath);
				assetImporter.assetBundleName = string.Empty;
			}
			EditorUtility.ClearProgressBar();
		}
	}
}