using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;

namespace AssetBundleGraph {
    public class IntegratedGUIFilter : INodeBase {
		private readonly string[] connectionIdsFromThisNodeToChildNodesOrFakeIds;
		private readonly List<string> containsKeywords;
		private readonly List<string> containsKeytypes;
		public IntegratedGUIFilter (string[] connectionIdsFromThisNodeToChildNodes, List<string> containsKeywords, List<string> containsKeytypes) {
			this.connectionIdsFromThisNodeToChildNodesOrFakeIds = connectionIdsFromThisNodeToChildNodes;
			this.containsKeywords = containsKeywords;
			this.containsKeytypes = containsKeytypes;
		}

		public void Setup (string nodeName, string nodeId, string noUseLabel, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			// overlapping test.
			try {
				var overlappingCheckList = new List<string>();
				for (var i = 0; i < containsKeywords.Count; i++) {
					var keywordAndKeytypeCombind = containsKeywords[i] + containsKeytypes[i];
					if (overlappingCheckList.Contains(keywordAndKeytypeCombind)) {
						throw new NodeException(String.Format("Duplicated filter condition found for [Keyword:{0} Type:{1}]", containsKeywords[i], containsKeytypes[i]), nodeId);
					}
					overlappingCheckList.Add(keywordAndKeytypeCombind);
				}
			} catch(NodeException e) {
				AssetBundleGraph.AddNodeException(e);
				return;
			}

			foreach (var groupKey in groupedSources.Keys) {
				var outputDict = new Dictionary<string, List<InternalAssetData>>();

				var inputSources = groupedSources[groupKey];
				
				Action<string, List<string>> _PreOutput = (string connectionId, List<string> outputSources) => {
					var outputs = new List<InternalAssetData>();
					
					foreach (var outputSource in outputSources) {
						foreach (var inputSource in inputSources) {
							if (outputSource == inputSource.GetAbsolutePathOrImportedPath()) {
								outputs.Add(inputSource);
							}
						}
					}
					
					outputDict[groupKey] = outputs;
					Output(nodeId, connectionId, outputDict, new List<string>());
				};
				
				try {
					Filter(inputSources, _PreOutput);
				} catch (Exception e) {
					Debug.LogError("Filter:" + this + " error:" + e);
				}
			}
		}
		
		public void Run (string nodeName, string nodeId, string noUseLabel, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			// overlapping test.
			{
				var overlappingCheckList = new List<string>();
				for (var i = 0; i < containsKeywords.Count; i++) {
					var keywordAndKeytypeCombind = containsKeywords[i] + containsKeytypes[i];
					if (overlappingCheckList.Contains(keywordAndKeytypeCombind)) throw new Exception("filter keywords and type combination are overlapping:" + containsKeywords[i] + " type:" + containsKeytypes[i]);
					overlappingCheckList.Add(keywordAndKeytypeCombind);
				}
			}
			
			foreach (var groupKey in groupedSources.Keys) {
				var outputDict = new Dictionary<string, List<InternalAssetData>>();

				var inputSources = groupedSources[groupKey];
				
				Action<string, List<string>> _Output = (string connectionId, List<string> outputSources) => {
					var outputs = new List<InternalAssetData>();
					
					foreach (var outputSource in outputSources) {
						foreach (var inputSource in inputSources) {
							if (outputSource == inputSource.GetAbsolutePathOrImportedPath()) {
								outputs.Add(inputSource);
							}
						}
					}

					outputDict[groupKey] = outputs;
					Output(nodeId, connectionId, outputDict, new List<string>());
				};
				
				try {
					Filter(inputSources, _Output);
				} catch (Exception e) {
					Debug.LogError("Filter:" + this + " error:" + e);
				}
			}
		}

		private class ExhaustiveAssetPathData {
			public readonly string importedPath;
			public readonly string absoluteSourcePath;
			public bool isFilterExhausted = false;

			public ExhaustiveAssetPathData (string absoluteSourcePath, string importedPath) {
				this.importedPath = importedPath;
				this.absoluteSourcePath = absoluteSourcePath;
			}
		}

		private void Filter (List<InternalAssetData> assets, Action<string, List<string>> FilterResultReceiver) {
			var exhaustiveAssets = new List<ExhaustiveAssetPathData>();
			foreach (var asset in assets) {
				exhaustiveAssets.Add(new ExhaustiveAssetPathData(asset.absoluteSourcePath, asset.importedPath));
			}

			for (var i = 0; i < connectionIdsFromThisNodeToChildNodesOrFakeIds.Length; i++) {
				// these 3 parameters depends on their contents order.
				var connectionId = connectionIdsFromThisNodeToChildNodesOrFakeIds[i];
				var keyword = containsKeywords[i];
				var keytype = containsKeytypes[i];
				
				var keywordContainsAssets = exhaustiveAssets.Where(assetData => !assetData.isFilterExhausted && assetData.importedPath.Contains(keyword)).ToList();
				
				// if keyword is wildcard, use type for constraint. pass all remaining assets.
				if (keyword == AssetBundleGraphSettings.FILTER_KEYWORD_WILDCARD) keywordContainsAssets = exhaustiveAssets.Where(assetData => !assetData.isFilterExhausted).ToList(); 
				
				// type constraint.
				if (keytype != AssetBundleGraphSettings.DEFAULT_FILTER_KEYTYPE) {
					var typeMatchedAssetsAbsolutePaths = new List<string>();
					
					foreach (var containedAssetData in keywordContainsAssets) {
						var assumedType = TypeBinder.AssumeTypeOfAsset(containedAssetData.importedPath);
						if (assumedType != null && keytype == assumedType.ToString()) typeMatchedAssetsAbsolutePaths.Add(containedAssetData.absoluteSourcePath);
					}
					
					// these assets are exhausted.
					foreach (var exhaustiveAsset in exhaustiveAssets) {
						if (typeMatchedAssetsAbsolutePaths.Contains(exhaustiveAsset.absoluteSourcePath)) exhaustiveAsset.isFilterExhausted = true;
					}

					if (connectionId != AssetBundleGraphSettings.FILTER_FAKE_CONNECTION_ID) FilterResultReceiver(connectionId, typeMatchedAssetsAbsolutePaths);
					continue;
				}
				
				var keywordMatchedAssetAbsolutePaths = keywordContainsAssets.Select(assetData => assetData.absoluteSourcePath).ToList();
				// these assets are exhausted.
				foreach (var exhaustiveAsset in exhaustiveAssets) {
					if (keywordMatchedAssetAbsolutePaths.Contains(exhaustiveAsset.absoluteSourcePath)) exhaustiveAsset.isFilterExhausted = true;
				}

				if (connectionId != AssetBundleGraphSettings.FILTER_FAKE_CONNECTION_ID) FilterResultReceiver(connectionId, keywordMatchedAssetAbsolutePaths);
			}
		}
		
		

		public static void ValidateFilter (string currentFilterKeyword, List<string> keywords, Action NullOrEmpty, Action AlreadyContained) {
			if (string.IsNullOrEmpty(currentFilterKeyword)) NullOrEmpty();
			if (keywords.Contains(currentFilterKeyword)) AlreadyContained();
		}
	}
}