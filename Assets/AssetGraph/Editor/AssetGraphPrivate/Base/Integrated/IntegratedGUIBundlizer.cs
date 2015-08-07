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
				Debug.LogError("no Bundle Name Template set.");
				return;
			}

			if (!bundleNameTemplate.Contains(AssetGraphSettings.KEYWORD_WILDCARD.ToString())) {
				Debug.LogError("no " + AssetGraphSettings.KEYWORD_WILDCARD + "found in Bundle Name Template.");
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
				Debug.LogError("no Bundle Name Template set.");
				return;
			}

			if (!bundleNameTemplate.Contains(AssetGraphSettings.KEYWORD_WILDCARD.ToString())) {
				Debug.LogError("no " + AssetGraphSettings.KEYWORD_WILDCARD + "found in Bundle Name Template.");
				return;
			}
			
			var recommendedBundleOutputDir = Path.Combine(AssetGraphSettings.BUNDLIZER_TEMP_PLACE, nodeId);
			FileController.RemakeDirectory(recommendedBundleOutputDir);

			var outputDict = new Dictionary<string, List<InternalAssetData>>();

			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];

				var localFilePathsBeforeBundlize = FileController.FilePathsInFolderWithoutMeta(AssetGraphSettings.UNITY_LOCAL_DATAPATH);
				try {
					In(groupKey, inputSources, recommendedBundleOutputDir);
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

		public void In (string groupkey, List<InternalAssetData> sources, string recommendedBundleOutputDir) {
			var validation = true;
			foreach (var source in sources) {
				if (string.IsNullOrEmpty(source.importedPath)) {
					Debug.LogError("resource:" + source.pathUnderSourceBase + " is not imported yet, should import before bundlize.");
					validation = false;
				}
			}

			if (!validation) return;

			var mainAssetInfo = sources[0];
			var mainAsset = AssetDatabase.LoadAssetAtPath(mainAssetInfo.importedPath, mainAssetInfo.assetType) as UnityEngine.Object;
			
			var sunAssetInfos = sources.GetRange(1, sources.Count-1);
			var subAssets = new List<UnityEngine.Object>();

			foreach (var subAssetInfo in sunAssetInfos) {
				var loadedSub = AssetDatabase.LoadAssetAtPath(subAssetInfo.importedPath, subAssetInfo.assetType) as UnityEngine.Object;
				subAssets.Add(loadedSub);
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
				Debug.LogError("failed to create AssetBundle:" + targetPath);
			}
		}
	}
}