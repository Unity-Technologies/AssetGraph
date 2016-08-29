using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;

namespace AssetBundleGraph {
    public class IntegratedGUIBundlizer : INodeOperationBase {
		private readonly string bundleNameTemplate;
		private readonly string assetsOutputConnectionId;

		public IntegratedGUIBundlizer (string bundleNameTemplate, string assetsConnectionId) {
			this.bundleNameTemplate = bundleNameTemplate;
			this.assetsOutputConnectionId = assetsConnectionId;
		}

		public void Setup (string nodeName, string nodeId, string unused_connectionIdToNextNode, Dictionary<string, List<Asset>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output) {			

			try {
				ValidateBundleNameTemplate(
					bundleNameTemplate,
					() => {
						throw new NodeException(nodeName + ":Bundle Name Template is empty.", nodeId);
					}
				);
			} catch (NodeException e) {
				AssetBundleGraphEditorWindow.AddNodeException(e);
				return;
			}
			
			var recommendedBundleOutputDir = FileUtility.PathCombine(AssetBundleGraphSettings.BUNDLIZER_CACHE_PLACE, nodeId, SystemDataUtility.GetCurrentPlatformKey());
			
			var outputDict = new Dictionary<string, List<Asset>>();

			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];
				
				var reservedBundlePath = BundlizeAssets(nodeName, groupKey, inputSources, recommendedBundleOutputDir, false);
				if (string.IsNullOrEmpty(reservedBundlePath)) {
					continue;
				}

				var outputSources = new List<Asset>();

				var newAssetData = Asset.CreateAssetWithImportPath(reservedBundlePath);

				outputSources.Add(newAssetData);
			
				outputDict[groupKey] = outputSources;
			}
			
			if (assetsOutputConnectionId != AssetBundleGraphSettings.BUNDLIZER_FAKE_CONNECTION_ID) {
				Output(nodeId, assetsOutputConnectionId, outputDict, new List<string>());
			}
			
		}
		
		public void Run (string nodeName, string nodeId, string unused_connectionIdToNextNode, Dictionary<string, List<Asset>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output) {
			ValidateBundleNameTemplate(
				bundleNameTemplate,
				() => {
					throw new AssetBundleGraphBuildException(nodeName + ": Bundle Name Template is empty.");
				}
			);
			
			var recommendedBundleOutputDir = FileUtility.PathCombine(AssetBundleGraphSettings.BUNDLIZER_CACHE_PLACE, nodeId, SystemDataUtility.GetCurrentPlatformKey());
			
			var outputDict = new Dictionary<string, List<Asset>>();

			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];
				
				var reservedBundlePath = BundlizeAssets(nodeName, groupKey, inputSources, recommendedBundleOutputDir, true);
				if (string.IsNullOrEmpty(reservedBundlePath)) continue;

				var outputSources = new List<Asset>();

				var newAssetData = Asset.CreateAssetWithImportPath(reservedBundlePath);

				outputSources.Add(newAssetData);

				outputDict[groupKey] = outputSources;
			}
			
			if (assetsOutputConnectionId != AssetBundleGraphSettings.BUNDLIZER_FAKE_CONNECTION_ID) {
				Output(nodeId, assetsOutputConnectionId, outputDict, new List<string>());
			}
			
		}

		public string BundlizeAssets (string nodeName, string groupkey, List<Asset> sources, string recommendedBundleOutputDir, bool isRun) {			
			var invalids = new List<string>();
			foreach (var source in sources) {
				if (string.IsNullOrEmpty(source.importFrom)) {
					invalids.Add(source.absoluteAssetPath);
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

				bundleName = (templateHead + groupkey + templateTail + "." + SystemDataUtility.GetCurrentPlatformShortName()).ToLower();
			}
			
			var bundlePath = FileUtility.PathCombine(recommendedBundleOutputDir, bundleName);
			
			for (var i = 0; i < sources.Count; i++) {
				var source = sources[i];

				// if already bundled in this running, avoid changing that name.
				if (source.isBundled) {
					continue;
				}
				
				if (isRun) {
					if (FileUtility.IsMetaFile(source.importFrom)) continue;	
					var assetImporter = AssetImporter.GetAtPath(source.importFrom);
					if (assetImporter == null) continue; 
					assetImporter.assetBundleName = bundleName;
				}
				
				// set as this resource is already bundled.
				sources[i] = Asset.DuplicateAssetWithNewStatus(sources[i], sources[i].isNew, true);
			}

			return bundlePath;
		}

		public static void ValidateBundleNameTemplate (string bundleNameTemplate, Action NullOrEmpty) {
			if (string.IsNullOrEmpty(bundleNameTemplate)) NullOrEmpty();
		}
	}
}