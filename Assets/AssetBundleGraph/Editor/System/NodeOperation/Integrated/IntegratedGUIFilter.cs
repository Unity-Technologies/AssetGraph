using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AssetBundleGraph {
    public class IntegratedGUIFilter : INodeOperationBase {
		private readonly string[] connectionIdsFromThisNodeToChildNodesOrFakeIds;
		private readonly List<string> containsKeywords;
		private readonly List<string> containsKeytypes;
		public IntegratedGUIFilter (string[] connectionIdsFromThisNodeToChildNodes, List<string> containsKeywords, List<string> containsKeytypes) {
			this.connectionIdsFromThisNodeToChildNodesOrFakeIds = connectionIdsFromThisNodeToChildNodes;
			this.containsKeywords = containsKeywords;
			this.containsKeytypes = containsKeytypes;
		}

		public void Setup (string nodeName, string nodeId, string unused_connectionIdToNextNode, Dictionary<string, List<Asset>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output) {
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
				AssetBundleGraphEditorWindow.AddNodeException(e);
				return;
			}

			foreach (var groupKey in groupedSources.Keys) {
				var outputDict = new Dictionary<string, List<Asset>>();

				var inputSources = groupedSources[groupKey];
				
				Action<string, List<string>> _PreOutput = (string connectionId, List<string> outputSources) => {
					var outputs = new List<Asset>();
					
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
					Debug.LogError(nodeName + " Error:" + e);
				}
			}
		}
		
		public void Run (string nodeName, string nodeId, string nused_connectionIdToNextNode, Dictionary<string, List<Asset>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output) {
			// overlapping test.
			{
				var overlappingCheckList = new List<string>();
				for (var i = 0; i < containsKeywords.Count; i++) {
					var keywordAndKeytypeCombind = containsKeywords[i] + containsKeytypes[i];
					if (overlappingCheckList.Contains(keywordAndKeytypeCombind)) {
						throw new NodeException(String.Format("Duplicated filter condition found for [Keyword:{0} Type:{1}]", containsKeywords[i], containsKeytypes[i]), nodeId);
					}
					overlappingCheckList.Add(keywordAndKeytypeCombind);
				}
			}
			
			foreach (var groupKey in groupedSources.Keys) {
				var outputDict = new Dictionary<string, List<Asset>>();

				var inputSources = groupedSources[groupKey];
				
				Action<string, List<string>> _Output = (string connectionId, List<string> outputSources) => {
					var outputs = new List<Asset>();
					
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
					Debug.LogError(nodeName + " Error:" + e);
				}
			}
		}

		private class ExhaustiveAssetPathData {
			public readonly string importFrom;
			public readonly string absoluteAssetPath;
			public bool isFilterExhausted = false;

			public ExhaustiveAssetPathData (string absoluteAssetPath, string importFrom) {
				this.importFrom = importFrom;
				this.absoluteAssetPath = absoluteAssetPath;
			}
		}

		private void Filter (List<Asset> assets, Action<string, List<string>> FilterResultReceiver) {
			var exhaustiveAssets = new List<ExhaustiveAssetPathData>();
			foreach (var asset in assets) {
				exhaustiveAssets.Add(new ExhaustiveAssetPathData(asset.absoluteAssetPath, asset.importFrom));
			}

			for (var i = 0; i < connectionIdsFromThisNodeToChildNodesOrFakeIds.Length; i++) {
				// these 3 parameters depends on their contents order.
				var connectionId = connectionIdsFromThisNodeToChildNodesOrFakeIds[i];
				var keyword = containsKeywords[i];
				var keytype = containsKeytypes[i];

				// filter by keyword first
				List<ExhaustiveAssetPathData> keywordContainsAssets = exhaustiveAssets.Where(
					assetData => 
					!assetData.isFilterExhausted && 
					Regex.IsMatch(assetData.importFrom, keyword, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace)
				).ToList();
				
				var typeMatchedAssetsAbsolutePaths = new List<string>();

				// then, filter by type
				foreach (var containedAssetData in keywordContainsAssets) {
					if (keytype != AssetBundleGraphSettings.DEFAULT_FILTER_KEYTYPE) {
						var assumedType = TypeUtility.FindTypeOfAsset(containedAssetData.importFrom);
						if (assumedType == null || keytype != assumedType.ToString()) {
							continue;
						}
					}
					typeMatchedAssetsAbsolutePaths.Add(containedAssetData.absoluteAssetPath);
				}

				// mark assets as exhausted.
				foreach (var exhaustiveAsset in exhaustiveAssets) {
					if (typeMatchedAssetsAbsolutePaths.Contains(exhaustiveAsset.absoluteAssetPath)) {
						exhaustiveAsset.isFilterExhausted = true;
					}
				}

				if (connectionId != AssetBundleGraphSettings.FILTER_FAKE_CONNECTION_ID) {
					FilterResultReceiver(connectionId, typeMatchedAssetsAbsolutePaths);
				}
			}
		}
		
		

		public static void ValidateFilter (string currentFilterKeyword, List<string> keywords, Action NullOrEmpty, Action AlreadyContained) {
			if (string.IsNullOrEmpty(currentFilterKeyword)) NullOrEmpty();
			if (keywords.Contains(currentFilterKeyword)) AlreadyContained();
		}
	}
}