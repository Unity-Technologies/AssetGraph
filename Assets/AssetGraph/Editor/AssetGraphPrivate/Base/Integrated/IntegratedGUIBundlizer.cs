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
			var invalids = new List<string>();
			foreach (var source in sources) {
				if (string.IsNullOrEmpty(source.importedPath)) {
					invalids.Add(source.pathUnderSourceBase);
				}
			}
			if (invalids.Any()) throw new Exception("bundlizer:" + string.Join(", ", invalids.ToArray()) + " is not imported yet, should import before bundlize.");


			var bundleName = bundleNameTemplate;

			/*
				if contains KEYWORD_WILDCARD, use group identifier to bundlize name.
			*/
			if (bundleNameTemplate.Contains(AssetGraphSettings.KEYWORD_WILDCARD)) {
				var templateHead = bundleNameTemplate.Split(AssetGraphSettings.KEYWORD_WILDCARD)[0];
				var templateTail = bundleNameTemplate.Split(AssetGraphSettings.KEYWORD_WILDCARD)[1];

				bundleName = (templateHead + groupkey + templateTail + "." + GraphStackController.Platform_Dot_Package(package)).ToLower();
			}

			var bundlePath = FileController.PathCombine(recommendedBundleOutputDir, bundleName);

			if (isRun) {
				foreach (var source in sources) {
					var assetImporter = AssetImporter.GetAtPath(source.importedPath);
					assetImporter.assetBundleName = bundleName;
				}
			}

			return bundlePath;
		}

		public static void ValidateBundleNameTemplate (string bundleNameTemplate, Action NullOrEmpty) {
			if (string.IsNullOrEmpty(bundleNameTemplate)) NullOrEmpty();
		}
	}
}