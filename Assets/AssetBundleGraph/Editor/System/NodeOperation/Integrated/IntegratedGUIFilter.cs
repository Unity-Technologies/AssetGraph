using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AssetBundleGraph {
    public class IntegratedGUIFilter : INodeOperationBase {
		private readonly List<ConnectionData> connectionsToChild;
		public IntegratedGUIFilter (List<ConnectionData> connectionsToChild) {
			this.connectionsToChild = connectionsToChild;
		}

		public void Setup (BuildTarget target, 
			NodeData node, 
			ConnectionData connectionToOutput, 
			Dictionary<string, List<Asset>> inputGroupAssets, 
			List<string> alreadyCached, 
			Action<ConnectionData, Dictionary<string, List<Asset>>, List<string>> Output) 
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
					Output(connectionToOutput, outputDict, null);
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
			Action<ConnectionData, Dictionary<string, List<Asset>>, List<string>> Output) 
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
					Output(connectionToOutput, outputDict, null);
				};
				
				try {
					Filter(node, inputSources, _Output);
				} catch (Exception e) {
					Debug.LogError(node.Name + " Error:" + e);
				}
			}
		}

		private class FilterableAsset {
			public Asset asset;
			public bool isFiltered = false;

			public FilterableAsset (Asset asset) {
				this.asset = asset;
			}
		}

		private void Filter (NodeData node, List<Asset> assets, Action<string, List<string>> FilterResultReceiver) {
			var filteringAssets = new List<FilterableAsset>();
			foreach (var asset in assets) {
				filteringAssets.Add(new FilterableAsset(asset));
			}

			foreach(var connToChild in connectionsToChild) {
				// these 3 parameters depends on their contents order.
				// TODO: separate connection id order and keyword/keytype.

				var Id = connToChild.Id;

				var filter = node.FilterConditions.Find(fc => fc.ConnectionPoint.Id == connToChild.FromNodeConnectionPointId);
				UnityEngine.Assertions.Assert.IsNotNull(filter);

				// filter by keyword first
				List<FilterableAsset> keywordContainsAssets = filteringAssets.Where(
					assetData => 
					!assetData.isFiltered && 
					Regex.IsMatch(assetData.asset.importFrom, filter.FilterKeyword, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace)
				).ToList();

				var typeMatchedAssetsAbsolutePaths = new List<string>();

				// then, filter by type
				foreach (var containedAssetData in keywordContainsAssets) {
					if (filter.FilterKeytype != AssetBundleGraphSettings.DEFAULT_FILTER_KEYTYPE) {
						var assumedType = TypeUtility.FindTypeOfAsset(containedAssetData.asset.importFrom);
						if (assumedType == null || filter.FilterKeytype != assumedType.ToString()) {
							continue;
						}
					}
					typeMatchedAssetsAbsolutePaths.Add(containedAssetData.asset.absoluteAssetPath);
				}

				// mark assets as exhausted.
				foreach (var a in filteringAssets) {
					if (typeMatchedAssetsAbsolutePaths.Contains(a.asset.absoluteAssetPath)) {
						a.isFiltered = true;
					}
				}

				if (Id != AssetBundleGraphSettings.FILTER_FAKE_CONNECTION_ID) {
					FilterResultReceiver(Id, typeMatchedAssetsAbsolutePaths);
				}
			}
		}
	}
}