using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;

namespace AssetBundleGraph {
    public class IntegratedGUIBundlizer : INodeBase {
		private readonly string bundleNameTemplate;
		private readonly bool outputResource;
		
		public IntegratedGUIBundlizer (string bundleNameTemplate, bool outputResource) {
			this.bundleNameTemplate = bundleNameTemplate;
			this.outputResource = outputResource;
		}

		public void Setup (string nodeName, string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {			
			ValidateBundleNameTemplate(
				bundleNameTemplate,
				() => {
					throw new OnNodeException(nodeName + ":Bundle Name Template is empty.", nodeId);
				}
			);
			
			var recommendedBundleOutputDir = FileController.PathCombine(AssetBundleGraphSettings.BUNDLIZER_CACHE_PLACE, nodeId, GraphStackController.Current_Platform_Package_Folder());
			
			var outputDict = new Dictionary<string, List<InternalAssetData>>();

			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];
				
				var reservedBundlePath = BundlizeAssets(nodeName, groupKey, inputSources, recommendedBundleOutputDir, false);
				if (string.IsNullOrEmpty(reservedBundlePath)) continue;

				var outputSources = new List<InternalAssetData>();

				var newAssetData = InternalAssetData.InternalAssetDataGeneratedByBundlizer(reservedBundlePath);

				outputSources.Add(newAssetData);
			
				outputDict[groupKey] = outputSources;
			}
			
			Output(nodeId, labelToNext, outputDict, new List<string>());
			
			/*
				generate additional output:
				output bundle resources for next node, for generate another AssetBundles with dependency.
			*/
			if (outputResource) {
				Output(nodeId, AssetBundleGraphSettings.BUNDLIZER_RESOURCES_OUTPUTPOINT_LABEL, groupedSources, new List<string>());
			}
		}
		
		public void Run (string nodeName, string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			ValidateBundleNameTemplate(
				bundleNameTemplate,
				() => {
					throw new AssetBundleGraphBuildException(nodeName + ": Bundle Name Template is empty.");
				}
			);
			
			var recommendedBundleOutputDir = FileController.PathCombine(AssetBundleGraphSettings.BUNDLIZER_CACHE_PLACE, nodeId, GraphStackController.Current_Platform_Package_Folder());
			
			var outputDict = new Dictionary<string, List<InternalAssetData>>();

			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];
				
				var reservedBundlePath = BundlizeAssets(nodeName, groupKey, inputSources, recommendedBundleOutputDir, true);
				if (string.IsNullOrEmpty(reservedBundlePath)) continue;

				var outputSources = new List<InternalAssetData>();

				var newAssetData = InternalAssetData.InternalAssetDataGeneratedByBundlizer(reservedBundlePath);

				outputSources.Add(newAssetData);

				outputDict[groupKey] = outputSources;
			}
			
			Output(nodeId, labelToNext, outputDict, new List<string>());
			
			/*
				generate additional output:
				output bundle resources for next node, for generate another AssetBundles with dependency.
			*/
			if (outputResource) {
				Output(nodeId, AssetBundleGraphSettings.BUNDLIZER_RESOURCES_OUTPUTPOINT_LABEL, groupedSources, new List<string>());
			}
		}

		public string BundlizeAssets (string nodeName, string groupkey, List<InternalAssetData> sources, string recommendedBundleOutputDir, bool isRun) {			
			var invalids = new List<string>();
			foreach (var source in sources) {
				if (string.IsNullOrEmpty(source.importedPath)) {
					invalids.Add(source.pathUnderSourceBase);
				}
			}
			if (invalids.Any()) {
				throw new AssetBundleGraphBuildException(nodeName + ": Invalid files to bundle. Following files need to be imported before bundlize: " + string.Join(", ", invalids.ToArray()) );
			}

			var bundleName = bundleNameTemplate;

			/*
				if contains KEYWORD_WILDCARD, use group identifier to bundlize name.
			*/
			if (bundleNameTemplate.Contains(AssetBundleGraphSettings.KEYWORD_WILDCARD)) {
				var templateHead = bundleNameTemplate.Split(AssetBundleGraphSettings.KEYWORD_WILDCARD)[0];
				var templateTail = bundleNameTemplate.Split(AssetBundleGraphSettings.KEYWORD_WILDCARD)[1];

				bundleName = (templateHead + groupkey + templateTail + "." + GraphStackController.Platform_Dot_Package()).ToLower();
			}
			
			var bundlePath = FileController.PathCombine(recommendedBundleOutputDir, bundleName);
			
			for (var i = 0; i < sources.Count; i++) {
				var source = sources[i];

				// if already bundled in this running, avoid changing that name.
				if (source.isBundled) continue;
				
				if (isRun) {
					if (GraphStackController.IsMetaFile(source.importedPath)) continue;	
					var assetImporter = AssetImporter.GetAtPath(source.importedPath);
					if (assetImporter == null) continue; 
					assetImporter.assetBundleName = bundleName;
				}
				
				// set as this resource is already bundled.
				sources[i] = InternalAssetData.InternalAssetDataBundledByBundlizer(sources[i]);
			}

			return bundlePath;
		}

		public static void ValidateBundleNameTemplate (string bundleNameTemplate, Action NullOrEmpty) {
			if (string.IsNullOrEmpty(bundleNameTemplate)) NullOrEmpty();
		}
	}
}