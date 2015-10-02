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

		public void Setup (string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			if (string.IsNullOrEmpty(bundleNameTemplate)) {
				Debug.LogError("no Bundle Name Template set.");
				return;
			}

			if (!bundleNameTemplate.Contains(AssetGraphSettings.KEYWORD_WILDCARD.ToString())) {
				Debug.LogError("no " + AssetGraphSettings.KEYWORD_WILDCARD + "found in Bundle Name Template.");
				return;
			}
			
			var recommendedBundleOutputDir = FileController.PathCombine(AssetGraphSettings.BUNDLIZER_TEMP_PLACE, nodeId);
			FileController.RemakeDirectory(recommendedBundleOutputDir);

			var outputDict = new Dictionary<string, List<InternalAssetData>>();

			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];
				
				var reservedBundlePath = BundlizeAssets(groupKey, inputSources, recommendedBundleOutputDir, false);
				if (string.IsNullOrEmpty(reservedBundlePath)) continue;

				var outputSources = new List<InternalAssetData>();

				var newAssetData = InternalAssetData.InternalAssetDataGeneratedByBundlizer(reservedBundlePath);

				outputSources.Add(newAssetData);
			
				outputDict[groupKey] = outputSources;
			}

			Output(nodeId, labelToNext, outputDict, alreadyCached);
		}
		
		public void Run (string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			if (string.IsNullOrEmpty(bundleNameTemplate)) {
				Debug.LogError("no Bundle Name Template set.");
				return;
			}

			if (!bundleNameTemplate.Contains(AssetGraphSettings.KEYWORD_WILDCARD.ToString())) {
				Debug.LogError("no " + AssetGraphSettings.KEYWORD_WILDCARD + "found in Bundle Name Template.");
				return;
			}
			
			var recommendedBundleOutputDir = FileController.PathCombine(AssetGraphSettings.BUNDLIZER_TEMP_PLACE, nodeId);
			FileController.RemakeDirectory(recommendedBundleOutputDir);

			var outputDict = new Dictionary<string, List<InternalAssetData>>();

			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];
				
				var reservedBundlePath = BundlizeAssets(groupKey, inputSources, recommendedBundleOutputDir, true);
				if (string.IsNullOrEmpty(reservedBundlePath)) continue;

				var outputSources = new List<InternalAssetData>();

				var newAssetData = InternalAssetData.InternalAssetDataGeneratedByBundlizer(reservedBundlePath);

				outputSources.Add(newAssetData);
			
				outputDict[groupKey] = outputSources;
			}

			Output(nodeId, labelToNext, outputDict, alreadyCached);
		}

		public string BundlizeAssets (string groupkey, List<InternalAssetData> sources, string recommendedBundleOutputDir, bool isRun) {
			var validation = true;
			foreach (var source in sources) {
				if (string.IsNullOrEmpty(source.importedPath)) {
					Debug.LogError("resource:" + source.pathUnderSourceBase + " is not imported yet, should import before bundlize.");
					validation = false;
				}
			}

			if (!validation) return string.Empty;

			var templateHead = bundleNameTemplate.Split(AssetGraphSettings.KEYWORD_WILDCARD)[0];
			var templateTail = bundleNameTemplate.Split(AssetGraphSettings.KEYWORD_WILDCARD)[1];

			var bundleName = templateHead + groupkey + templateTail;
			var bundlePath = FileController.PathCombine(recommendedBundleOutputDir, bundleName);

			if (isRun) {
				foreach (var source in sources) {
					var assetImporter = AssetImporter.GetAtPath(source.importedPath);
					assetImporter.assetBundleName = bundleName;
				}
			}

			return bundlePath;
		}
	}
}