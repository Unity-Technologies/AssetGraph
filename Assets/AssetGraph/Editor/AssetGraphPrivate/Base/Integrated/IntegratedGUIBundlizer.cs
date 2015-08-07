using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace AssetGraph {
	public class IntegratedGUIBundlizer : INodeBase {
		private readonly string bundleNameTemplate;
		
		public IntegratedGUIBundlizer (string bundleNameTemplate) {
			this.bundleNameTemplate = bundleNameTemplate;
		}

		public void Setup (string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> groupedSources, Action<string, string, Dictionary<string, List<InternalAssetData>>> Output) {
			if (string.IsNullOrEmpty(bundleNameTemplate)) {
				Debug.LogWarning("no Bundle Name Template set.");
				return;
			}

			if (!bundleNameTemplate.Contains(AssetGraphSettings.KEYWORD_WILDCARD.ToString())) {
				Debug.LogWarning("no " + AssetGraphSettings.KEYWORD_WILDCARD + "found in Bundle Name Template.");
				return;
			}

			var outputDict = new Dictionary<string, List<InternalAssetData>>();

			foreach (var groupKey in groupedSources.Keys) {
				var outputSources = new List<InternalAssetData>();// cause of 0.
				outputDict[groupKey] = outputSources;
			}
			
			Output(nodeId, labelToNext, outputDict);
		}
		
		public void Run (string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> groupedSources, Action<string, string, Dictionary<string, List<InternalAssetData>>> Output) {
			if (string.IsNullOrEmpty(bundleNameTemplate)) {
				Debug.LogWarning("no Bundle Name Template set.");
				return;
			}

			if (!bundleNameTemplate.Contains(AssetGraphSettings.KEYWORD_WILDCARD.ToString())) {
				Debug.LogWarning("no " + AssetGraphSettings.KEYWORD_WILDCARD + "found in Bundle Name Template.");
				return;
			}
			
			var recommendedBundleOutputDir = Path.Combine(AssetGraphSettings.BUNDLIZER_TEMP_PLACE, nodeId);
			FileController.RemakeDirectory(recommendedBundleOutputDir);

			var outputDict = new Dictionary<string, List<InternalAssetData>>();

			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];

				var assets = new List<AssetInfo>();
				foreach (var assetData in inputSources) {
					var assetName = assetData.fileNameAndExtension;
					var assetType = assetData.assetType;
					var assetPath = assetData.importedPath;
					var assetId = assetData.assetId;
					assets.Add(new AssetInfo(assetName, assetType, assetPath, assetId));
				}

				var localFilePathsBeforeBundlize = FileController.FilePathsInFolderWithoutMeta(AssetGraphSettings.UNITY_LOCAL_DATAPATH);
				try {
					In(groupKey, assets, recommendedBundleOutputDir);
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

				outputDict[groupKey] = outputSources;
			}

			Output(nodeId, labelToNext, outputDict);
		}

		public void In (string groupkey, List<AssetInfo> source, string recommendedBundleOutputDir) {
			var mainAssetInfo = source[0];
			var mainAsset = AssetDatabase.LoadAssetAtPath(mainAssetInfo.assetPath, mainAssetInfo.assetType) as UnityEngine.Object;

			var sunAssetInfos = source.GetRange(1, source.Count-1);
			var subAssets = new List<UnityEngine.Object>();

			foreach (var subAssetInfo in sunAssetInfos) {
				subAssets.Add(
					AssetDatabase.LoadAssetAtPath(subAssetInfo.assetPath, subAssetInfo.assetType) as UnityEngine.Object
				);
			}

			var templateHead = bundleNameTemplate.Split(AssetGraphSettings.KEYWORD_WILDCARD)[0];
			var templateTail = bundleNameTemplate.Split(AssetGraphSettings.KEYWORD_WILDCARD)[1];

			// create AssetBundle from assets.
			var targetPath = Path.Combine(recommendedBundleOutputDir, templateHead + groupkey + templateTail);
			
			uint crc = 0;
			try {
				BuildPipeline.BuildAssetBundle(
					mainAsset,
					subAssets.ToArray(),
					targetPath,
					out crc,
					BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets,
					BuildTarget.iOS
				);
			} catch (Exception e) {
				Debug.Log("failed to create AssetBundle:" + targetPath);
			}
		}
	}
}