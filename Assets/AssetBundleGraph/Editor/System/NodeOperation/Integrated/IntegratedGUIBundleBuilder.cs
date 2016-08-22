using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace AssetBundleGraph {
	public class IntegratedGUIBundleBuilder : INodeOperationBase {
		private readonly List<string> bundleOptions;
//		private readonly List<string> relatedNodeIds;

		public IntegratedGUIBundleBuilder (List<string> bundleOptions, List<string> relatedNodeIds) {
			this.bundleOptions = bundleOptions;
//			this.relatedNodeIds = relatedNodeIds;
		}

		public void Setup (string nodeName, string connectionIdToNextNode, string labelToNext, Dictionary<string, List<Asset>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output) {
			/*
				merge multi group into ["0"] group.
			*/
			var outputDict = new Dictionary<string, List<Asset>>();
			outputDict["0"] = new List<Asset>();

			foreach (var groupKey in groupedSources.Keys) {
				var outputSources = groupedSources[groupKey];
				outputDict["0"].AddRange(outputSources);
			}

			Output(connectionIdToNextNode, labelToNext, outputDict, new List<string>());
		}
		
		public void Run (string nodeName, string connectionIdToNextNode, string labelToNext, Dictionary<string, List<Asset>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output) {
			
			var recommendedBundleOutputDirSource = FileUtility.PathCombine(AssetBundleGraphSettings.BUNDLEBUILDER_CACHE_PLACE, connectionIdToNextNode);
			var recommendedBundleOutputDir = FileUtility.PathCombine(recommendedBundleOutputDirSource, SystemDataUtility.GetCurrentPlatformKey());
			if (!Directory.Exists(recommendedBundleOutputDir)) Directory.CreateDirectory(recommendedBundleOutputDir);

			
			/*
				merge multi group into ["0"] group.
			*/
			var intendedAssetNames = new List<string>();
			foreach (var groupKey in groupedSources.Keys) {
				var internalAssetsOfCurrentGroup = groupedSources[groupKey];
				foreach (var internalAsset in internalAssetsOfCurrentGroup) {
					intendedAssetNames.Add(internalAsset.fileNameAndExtension);
					intendedAssetNames.Add(internalAsset.fileNameAndExtension + AssetBundleGraphSettings.MANIFEST_FOOTER);
				}
			}
			


			/*
				platform's bundle & manifest. 
				e.g. iOS & iOS.manifest.
			*/
			var currentPlatform_Package_BundleFile = SystemDataUtility.GetCurrentPlatformKey();
			var currentPlatform_Package_BundleFileManifest = currentPlatform_Package_BundleFile + AssetBundleGraphSettings.MANIFEST_FOOTER;
			
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
				check assumed bundlized resources and actual generated assetbundles.

				"assuned bundlized resources info from bundlizer" are contained by "actual bundlized resources".
			*/
			var outputDict = new Dictionary<string, List<Asset>>();
			var outputSources = new List<Asset>();

			var newAssetPaths = new List<string>();
			var generatedAssetBundlePaths = FileUtility.FilePathsInFolder(recommendedBundleOutputDir);
			foreach (var newAssetPath in generatedAssetBundlePaths) {
				newAssetPaths.Add(newAssetPath);
				var newAssetData = Asset.CreateAssetWithImportPath(newAssetPath);
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
				
					var bundleManifestName = bundledName + AssetBundleGraphSettings.MANIFEST_FOOTER;
					if (generatedAssetName == bundleManifestName) {
						containedAssetBundles.Add(generatedAssetPath);
						continue;
					}
				}
			}

			var diffs = newAssetPaths.Except(containedAssetBundles);
			foreach (var diff in diffs) {
				Debug.LogWarning(nodeName +": AssetBundle " + diff + " is not intended to build. Check if unnecessary importer or prefabricator exists in the graph.");
			}
		
			outputDict["0"] = outputSources;
			
			var usedCache = new List<string>(alreadyCached);
			Output(connectionIdToNextNode, labelToNext, outputDict, usedCache);
		}
		
		
		public static void RemoveAllAssetBundleSettings () {
			RemoveBundleSettings(AssetBundleGraphSettings.ASSETS_PATH);
		}
		
		public static void RemoveBundleSettings (string nodePath) {
			EditorUtility.DisplayProgressBar("AssetBundleGraph unbundlize all resources...", nodePath, 0);
			var filePathsInFolder = FileUtility.FilePathsInFolder(nodePath);
			foreach (var filePath in filePathsInFolder) {
				if (FileUtility.IsMetaFile(filePath)) {
					continue;
				}
				if (FileUtility.ContainsHiddenFiles(filePath)) {
					continue;
				}
				var assetImporter = AssetImporter.GetAtPath(filePath);
				
				// assetImporter is null when the asset is not accepted by Unity.
				// e.g. file.my_new_extension is ignored by Unity.
				if (assetImporter == null) continue;

				if (assetImporter.GetType() == typeof(UnityEditor.MonoImporter)) continue;
				
				assetImporter.assetBundleName = string.Empty;
			}
			EditorUtility.ClearProgressBar();
		}
	}
}