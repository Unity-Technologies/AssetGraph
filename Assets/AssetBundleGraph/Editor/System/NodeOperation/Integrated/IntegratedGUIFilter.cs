using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AssetBundleGraph {
    public class IntegratedGUIFilter : INodeOperationBase {
		private readonly string[] connectionIdsFromThisNodeToChildNodesOrFakeIds;
		public IntegratedGUIFilter (string[] connectionIdsFromThisNodeToChildNodes) {
			this.connectionIdsFromThisNodeToChildNodesOrFakeIds = connectionIdsFromThisNodeToChildNodes;
		}

		public void Setup (BuildTarget target, 
			NodeData node, 
			ConnectionData connectionToOutput, 
			Dictionary<string, List<Asset>> inputGroupAssets, 
			List<string> alreadyCached, 
			Action<NodeData, ConnectionData, Dictionary<string, List<Asset>>, List<string>> Output) 
		{
			// overlapping test.
			try {
				node.ValidateOverlappingFilterCondition(true);
			} catch(NodeException e) {
				AssetBundleGraphEditorWindow.AddNodeException(e);
				return;
			}

			foreach (var groupKey in inputGroupAssets.Keys) {
				var outputDict = new Dictionary<string, List<Asset>>();

				var inputSources = inputGroupAssets[groupKey];
				
				Action<string, List<string>> _PreOutput = (string Id, List<string> outputSources) => {
					var outputs = new List<Asset>();
					
					foreach (var outputSource in outputSources) {
						foreach (var inputSource in inputSources) {
							if (outputSource == inputSource.GetAbsolutePathOrImportedPath()) {
								outputs.Add(inputSource);
							}
						}
					}
					
					outputDict[groupKey] = outputs;
					Output(node, connectionToOutput, outputDict, new List<string>());
				};
				
				try {
					Filter(node, inputSources, _PreOutput);
				} catch (Exception e) {
					Debug.LogError(node.Name + " Error:" + e);
				}
			}
		}
		
		public void Run (BuildTarget target, 
			NodeData node, 
			ConnectionData connectionToOutput, 
			Dictionary<string, List<Asset>> inputGroupAssets, 
			List<string> alreadyCached, 
			Action<NodeData, ConnectionData, Dictionary<string, List<Asset>>, List<string>> Output) 
		{

			// overlapping test.
			node.ValidateOverlappingFilterCondition(true);

			foreach (var groupKey in inputGroupAssets.Keys) {
				var outputDict = new Dictionary<string, List<Asset>>();

				var inputSources = inputGroupAssets[groupKey];
				
				Action<string, List<string>> _Output = (string Id, List<string> outputSources) => {
					var outputs = new List<Asset>();
					
					foreach (var outputSource in outputSources) {
						foreach (var inputSource in inputSources) {
							if (outputSource == inputSource.GetAbsolutePathOrImportedPath()) {
								outputs.Add(inputSource);
							}
						}
					}

					outputDict[groupKey] = outputs;
					Output(node, connectionToOutput, outputDict, new List<string>());
				};
				
				try {
					Filter(node, inputSources, _Output);
				} catch (Exception e) {
					Debug.LogError(node.Name + " Error:" + e);
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

		private void Filter (NodeData node, List<Asset> assets, Action<string, List<string>> FilterResultReceiver) {
			var exhaustiveAssets = new List<ExhaustiveAssetPathData>();
			foreach (var asset in assets) {
				exhaustiveAssets.Add(new ExhaustiveAssetPathData(asset.absoluteAssetPath, asset.importFrom));
			}

			for (var i = 0; i < connectionIdsFromThisNodeToChildNodesOrFakeIds.Length; i++) {

				// these 3 parameters depends on their contents order.
				// TODO: separate connection id order and keyword/keytype.

				var Id = connectionIdsFromThisNodeToChildNodesOrFakeIds[i];
				var keyword = node.FilterConditions[i].FilterKeyword;
				var keytype = node.FilterConditions[i].FilterKeytype;

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

				if (Id != AssetBundleGraphSettings.FILTER_FAKE_CONNECTION_ID) {
					FilterResultReceiver(Id, typeMatchedAssetsAbsolutePaths);
				}
			}
		}
	}
}