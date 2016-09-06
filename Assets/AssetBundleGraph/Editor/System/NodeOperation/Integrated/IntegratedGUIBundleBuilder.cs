using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace AssetBundleGraph {
	public class IntegratedGUIBundleBuilder : INodeOperationBase {
		public void Setup (BuildTarget target, NodeData node, string labelToNext, Dictionary<string, List<Asset>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output) {
			/*
				merge multi group into ["0"] group.
			*/
			var outputDict = new Dictionary<string, List<Asset>>();
			outputDict["0"] = new List<Asset>();

			foreach (var groupKey in groupedSources.Keys) {
				var outputSources = groupedSources[groupKey];
				outputDict["0"].AddRange(outputSources);
			}

			Output(node.Id, labelToNext, outputDict, new List<string>());
		}
		
		public void Run (BuildTarget target, NodeData node, string labelToNext, Dictionary<string, List<Asset>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output) {
			
			var bundleOutputDir = FileUtility.EnsureAssetBundleCacheDirExists(target, node);
			
			/*
				merge multi group into ["0"] group.
			*/
			var assetNames = new List<string>();
			foreach (var groupKey in groupedSources.Keys) {
				var internalAssetsOfCurrentGroup = groupedSources[groupKey];
				foreach (var internalAsset in internalAssetsOfCurrentGroup) {
					assetNames.Add(internalAsset.fileNameAndExtension);
					assetNames.Add(internalAsset.fileNameAndExtension + AssetBundleGraphSettings.MANIFEST_FOOTER);
				}
			}

			/*
				platform's bundle & manifest. 
				e.g. iOS & iOS.manifest.
			*/
			var bundleFileName = SystemDataUtility.GetPathSafeTargetName(target);
			var bundleFileManifest = bundleFileName + AssetBundleGraphSettings.MANIFEST_FOOTER;
			
			assetNames.Add(bundleFileName);
			assetNames.Add(bundleFileManifest);

			/*
				delete not intended assets.
			*/
			foreach (var cachedAsset in alreadyCached) {
				var cachedFileName = Path.GetFileName(cachedAsset);
				if (assetNames.Contains(cachedFileName)) {
					continue;
				}
				File.Delete(cachedAsset);
			}

			BuildPipeline.BuildAssetBundles(bundleOutputDir, (BuildAssetBundleOptions)node.BundleBuilderBundleOptions[target], target);


			/*
				check assumed bundlized resources and actual generated assetbundles.

				"assuned bundlized resources info from bundlizer" are contained by "actual bundlized resources".
			*/
			var outputDict = new Dictionary<string, List<Asset>>();
			var outputSources = new List<Asset>();

			var newAssetPaths = new List<string>();
			var generatedAssetBundlePaths = FileUtility.FilePathsInFolder(bundleOutputDir);
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
				foreach (var bundledName in assetNames) {
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
				Debug.LogWarning(node.Name +": AssetBundle " + diff + " is not intended to build. Check if unnecessary importer or prefabricator exists in the graph.");
			}
		
			outputDict["0"] = outputSources;
			
			var usedCache = new List<string>(alreadyCached);
			Output(node.Id, labelToNext, outputDict, usedCache);
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