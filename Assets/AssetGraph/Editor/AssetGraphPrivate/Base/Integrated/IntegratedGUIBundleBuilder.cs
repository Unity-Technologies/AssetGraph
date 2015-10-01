using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace AssetGraph {
	public class IntegratedGUIBundleBuilder : INodeBase {
		private readonly List<string> bundleOptions;
		
		public IntegratedGUIBundleBuilder (List<string> bundleOptions) {
			this.bundleOptions = bundleOptions;
		}

		public void Setup (string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> groupedSources, Action<string, string, Dictionary<string, List<InternalAssetData>>> Output) {
			var outputDict = new Dictionary<string, List<InternalAssetData>>();
			outputDict["0"] = new List<InternalAssetData>();

			foreach (var groupKey in groupedSources.Keys) {
				var outputSources = groupedSources[groupKey];
				outputDict["0"].AddRange(outputSources);
			}
			
			Output(nodeId, labelToNext, outputDict);
		}
		
		public void Run (string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> groupedSources, Action<string, string, Dictionary<string, List<InternalAssetData>>> Output) {
			var recommendedBundleOutputDirSource = FileController.PathCombine(AssetGraphSettings.BUNDLEBUILDER_TEMP_PLACE, nodeId);
			var recommendedBundleOutputDir = FileController.PathCombine(recommendedBundleOutputDirSource, "iOS");
			FileController.RemakeDirectory(recommendedBundleOutputDir);

			var outputDict = new Dictionary<string, List<InternalAssetData>>();
			outputDict["0"] = new List<InternalAssetData>();

			var localFilePathsBeforeBundlize = FileController.FilePathsInFolderWithoutMeta(AssetGraphSettings.UNITY_LOCAL_DATAPATH);
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

			BuildPipeline.BuildAssetBundles(recommendedBundleOutputDir, assetBundleOptions, BuildTarget.iOS);

			var localFilePathsAfterBundlize = FileController.FilePathsInFolderWithoutMeta(AssetGraphSettings.UNITY_LOCAL_DATAPATH);
				
			var outputSources = new List<InternalAssetData>();

			var generatedAssetBundlePaths = localFilePathsAfterBundlize.Except(localFilePathsBeforeBundlize);
			foreach (var newAssetPath in generatedAssetBundlePaths) {
				var newAssetData = InternalAssetData.InternalAssetDataGeneratedByBundleBuilder(newAssetPath);
				outputSources.Add(newAssetData);
			}

			outputDict["0"] = outputSources;

			Output(nodeId, labelToNext, outputDict);
		}
	}
}