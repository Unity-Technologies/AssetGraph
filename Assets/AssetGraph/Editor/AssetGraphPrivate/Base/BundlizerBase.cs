using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace AssetGraph {
	public class BundlizerBase : INodeBase {
		public void Setup (string nodeId, string labelToNext, List<InternalAssetData> inputSources, Action<string, string, List<InternalAssetData>> Output) {
			var outputSources = new List<InternalAssetData>();
			Output(nodeId, labelToNext, outputSources);
		}
		
		public void Run (string nodeId, string labelToNext, List<InternalAssetData> inputSources, Action<string, string, List<InternalAssetData>> Output) {

			var assets = new List<AssetInfo>();
			foreach (var assetData in inputSources) {
				var assetName = assetData.fileNameAndExtension;
				var assetType = assetData.assetType;
				var assetPath = assetData.importedPath;
				var assetId = assetData.assetId;
				assets.Add(new AssetInfo(assetName, assetType, assetPath, assetId));
			}

			var recommendedBundleOutputDir = Path.Combine(AssetGraphSettings.BUNDLIZER_TEMP_PLACE, nodeId);
			FileController.RemakeDirectory(recommendedBundleOutputDir);

			var localFilePathsBeforeBundlize = FileController.FilePathsInFolderWithoutMeta(AssetGraphSettings.UNITY_LOCAL_DATAPATH);
			try {
				In(assets, recommendedBundleOutputDir);
			} catch (Exception e) {
				Debug.LogError("Bundlizer:" + this + " error:" + e);
			}

			AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
			AssetDatabase.SaveAssets();

			var localFilePathsAfterBundlize = FileController.FilePathsInFolderWithoutMeta(AssetGraphSettings.UNITY_LOCAL_DATAPATH);
			
			var outputSources = new List<InternalAssetData>();

			var generatedAssetBundlePaths = localFilePathsAfterBundlize.Except(localFilePathsBeforeBundlize);
			foreach (var newAssetPath in generatedAssetBundlePaths) {
				var newAssetData = InternalAssetData.InternalAssetDataGeneratedByImporterOrPrefabricatorOrBundlizer(
					newAssetPath,
					AssetDatabase.AssetPathToGUID(newAssetPath),
					AssetGraphInternalFunctions.GetAssetType(newAssetPath)
				);
				outputSources.Add(newAssetData);
			}
			
			Output(nodeId, labelToNext, outputSources);
		}

		public virtual void In (List<AssetInfo> source, string recommendedBundleOutputDir) {
			Debug.LogError("should implement \"public override void In (List<AssetGraph.AssetInfo> source, string recommendedBundleOutputDir)\" in class:" + this);
		}
	}
}