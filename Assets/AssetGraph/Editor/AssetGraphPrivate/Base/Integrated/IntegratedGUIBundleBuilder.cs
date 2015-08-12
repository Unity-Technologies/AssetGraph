using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace AssetGraph {
	public class IntegratedGUIBundleBuilder : INodeBase {
		private readonly Dictionary<string, bool> bundleOptions;
		
		public IntegratedGUIBundleBuilder (Dictionary<string, bool> bundleOptions) {
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
			var recommendedBundleOutputDir = Path.Combine(AssetGraphSettings.BUNDLEBUILDER_TEMP_PLACE, nodeId);
			FileController.RemakeDirectory(recommendedBundleOutputDir);

			var outputDict = new Dictionary<string, List<InternalAssetData>>();
			outputDict["0"] = new List<InternalAssetData>();

			var localFilePathsBeforeBundlize = FileController.FilePathsInFolderWithoutMeta(AssetGraphSettings.UNITY_LOCAL_DATAPATH);
			var assetBundleOptions = BuildAssetBundleOptions.None;
			BuildPipeline.BuildAssetBundles(recommendedBundleOutputDir, assetBundleOptions, BuildTarget.iOS);
			// 	AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
			// 	AssetDatabase.SaveAssets();

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