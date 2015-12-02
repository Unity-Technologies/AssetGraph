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

		public void Setup (string nodeId, string labelToNext, string package, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			ValidateBundleNameTemplate(
				bundleNameTemplate,
				() => {
					throw new Exception("no Bundle Name Template set.");
				},
				() => {
					throw new Exception("no " + AssetGraphSettings.KEYWORD_WILDCARD + "found in Bundle Name Template:" + bundleNameTemplate);
				}
			);
			
			var recommendedBundleOutputDir = FileController.PathCombine(AssetGraphSettings.BUNDLIZER_CACHE_PLACE, nodeId, GraphStackController.Current_Platform_Package_Folder(package));
			
			var outputDict = new Dictionary<string, List<InternalAssetData>>();

			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];
				
				var reservedBundlePath = BundlizeAssets(package, groupKey, inputSources, recommendedBundleOutputDir, false);
				if (string.IsNullOrEmpty(reservedBundlePath)) continue;

				var outputSources = new List<InternalAssetData>();

				var newAssetData = InternalAssetData.InternalAssetDataGeneratedByBundlizer(reservedBundlePath);

				outputSources.Add(newAssetData);
			
				outputDict[groupKey] = outputSources;
			}

			Output(nodeId, labelToNext, outputDict, new List<string>());
		}
		
		public void Run (string nodeId, string labelToNext, string package, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			ValidateBundleNameTemplate(
				bundleNameTemplate,
				() => {
					throw new Exception("no Bundle Name Template set.");
				},
				() => {
					throw new Exception("no " + AssetGraphSettings.KEYWORD_WILDCARD + "found in Bundle Name Template:" + bundleNameTemplate);
				}
			);
			
			var recommendedBundleOutputDir = FileController.PathCombine(AssetGraphSettings.BUNDLIZER_CACHE_PLACE, nodeId, GraphStackController.Current_Platform_Package_Folder(package));
			
			var outputDict = new Dictionary<string, List<InternalAssetData>>();

			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];
				
				var reservedBundlePath = BundlizeAssets(package, groupKey, inputSources, recommendedBundleOutputDir, true);
				if (string.IsNullOrEmpty(reservedBundlePath)) continue;

				var outputSources = new List<InternalAssetData>();

				var newAssetData = InternalAssetData.InternalAssetDataGeneratedByBundlizer(reservedBundlePath);

				outputSources.Add(newAssetData);

				outputDict[groupKey] = outputSources;
			}

			Output(nodeId, labelToNext, outputDict, new List<string>());
		}

		public string BundlizeAssets (string package, string groupkey, List<InternalAssetData> sources, string recommendedBundleOutputDir, bool isRun) {			
			foreach (var source in sources) {
				if (string.IsNullOrEmpty(source.importedPath)) {
					Debug.LogError("resource:" + source.pathUnderSourceBase + " is not imported yet, should import before bundlize.");
				}
			}

			var templateHead = bundleNameTemplate.Split(AssetGraphSettings.KEYWORD_WILDCARD)[0];
			var templateTail = bundleNameTemplate.Split(AssetGraphSettings.KEYWORD_WILDCARD)[1];

			var bundleName = templateHead + groupkey + templateTail;
			if (!string.IsNullOrEmpty(package)) bundleName = bundleName + "." + package;
			var bundlePath = FileController.PathCombine(recommendedBundleOutputDir, bundleName);

			if (isRun) {
				foreach (var source in sources) {
					var assetImporter = AssetImporter.GetAtPath(source.importedPath);
					assetImporter.assetBundleName = bundleName;
				}
			}

			return bundlePath;
		}

		public static void ValidateBundleNameTemplate (string bundleNameTemplate, Action NullOrEmpty, Action UnlessKeyword) {
			if (string.IsNullOrEmpty(bundleNameTemplate)) NullOrEmpty();
			if (!bundleNameTemplate.Contains(AssetGraphSettings.KEYWORD_WILDCARD.ToString())) UnlessKeyword();
		}
	}
}