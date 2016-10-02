using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AssetBundleGraph {
    public class IntegratedGUIFilter : INodeOperation {
		private readonly List<ConnectionData> connectionsToChild;
		public IntegratedGUIFilter (List<ConnectionData> connectionsToChild) {
			this.connectionsToChild = connectionsToChild;
		}

		public void Setup (BuildTarget target, 
			NodeData node, 
			ConnectionPointData inputPoint,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<Asset>> inputGroupAssets, 
			List<string> alreadyCached, 
			Action<ConnectionData, Dictionary<string, List<Asset>>, List<string>> Output) 
		{
			node.ValidateOverlappingFilterCondition(true);
			Filter(node, inputGroupAssets, Output);
		}
		
		public void Run (BuildTarget target, 
			NodeData node, 
			ConnectionPointData inputPoint,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<Asset>> inputGroupAssets, 
			List<string> alreadyCached, 
			Action<ConnectionData, Dictionary<string, List<Asset>>, List<string>> Output) 
		{
			Filter(node, inputGroupAssets, Output);
		}

		private class FilterableAsset {
			public Asset asset;
			public bool isFiltered = false;

			public FilterableAsset (Asset asset) {
				this.asset = asset;
			}
		}

		private void Filter (NodeData node, Dictionary<string, List<Asset>> inputGroupAssets, Action<ConnectionData, Dictionary<string, List<Asset>>, List<string>> Output) {

			foreach(var connToChild in connectionsToChild) {

				var filter = node.FilterConditions.Find(fc => fc.ConnectionPoint.Id == connToChild.FromNodeConnectionPointId);
				UnityEngine.Assertions.Assert.IsNotNull(filter);

				var output = new Dictionary<string, List<Asset>>();

				foreach(var groupKey in inputGroupAssets.Keys) {
					var assets = inputGroupAssets[groupKey];
					var filteringAssets = new List<FilterableAsset>();
					assets.ForEach(a => filteringAssets.Add(new FilterableAsset(a)));


					// filter by keyword first
					List<FilterableAsset> keywordContainsAssets = filteringAssets.Where(
						assetData => 
						!assetData.isFiltered && 
						Regex.IsMatch(assetData.asset.importFrom, filter.FilterKeyword, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace)
					).ToList();

					List<FilterableAsset> finalFilteredAsset = new List<FilterableAsset>();

					// then, filter by type
					foreach (var a in keywordContainsAssets) {
						if (filter.FilterKeytype != AssetBundleGraphSettings.DEFAULT_FILTER_KEYTYPE) {
							var assumedType = TypeUtility.FindTypeOfAsset(a.asset.importFrom);
							if (assumedType == null || filter.FilterKeytype != assumedType.ToString()) {
								continue;
							}
						}
						finalFilteredAsset.Add(a);
					}

					// mark assets as exhausted.
					foreach (var a in finalFilteredAsset) {
						a.isFiltered = true;
					}

					output[groupKey] = finalFilteredAsset.Select(v => v.asset).ToList();
				}

				Output(connToChild, output, null);
			}
		}
	}
}