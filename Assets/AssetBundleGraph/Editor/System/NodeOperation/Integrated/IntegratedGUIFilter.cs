using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AssetBundleGraph {
    public class IntegratedGUIFilter : INodeOperation {

		public void Setup (BuildTarget target, 
			NodeData node, 
			ConnectionData connectionFromInput,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<AssetReference>> inputGroupAssets, 
			PerformGraph.Output Output) 
		{
			Profiler.BeginSample("AssetBundleGraph.GUIFilter.Setup");
			node.ValidateOverlappingFilterCondition(true);
			Filter(node, connectionFromInput, connectionToOutput, inputGroupAssets, Output);
			Profiler.EndSample();
		}
		
		public void Run (BuildTarget target, 
			NodeData node, 
			ConnectionData connectionFromInput,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<AssetReference>> inputGroupAssets, 
			PerformGraph.Output Output) 
		{
			Profiler.BeginSample("AssetBundleGraph.GUIFilter.Run");
			Filter(node, connectionFromInput, connectionToOutput, inputGroupAssets, Output);
			Profiler.EndSample();
		}

		private class FilterableAsset {
			public AssetReference asset;
			public bool isFiltered = false;

			public FilterableAsset (AssetReference asset) {
				this.asset = asset;
			}
		}

		private void Filter (NodeData node, 
			ConnectionData connectionFromInput,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<AssetReference>> inputGroupAssets, 
			PerformGraph.Output Output) 
		{

			//TODO:

//			foreach(var connToChild in connectionsToChild) {
//
//				var filter = node.FilterConditions.Find(fc => fc.ConnectionPoint.Id == connToChild.FromNodeConnectionPointId);
//				UnityEngine.Assertions.Assert.IsNotNull(filter);
//
//				var output = new Dictionary<string, List<AssetReference>>();
//
//				foreach(var groupKey in inputGroupAssets.Keys) {
//					var assets = inputGroupAssets[groupKey];
//					var filteringAssets = new List<FilterableAsset>();
//					assets.ForEach(a => filteringAssets.Add(new FilterableAsset(a)));
//
//
//					// filter by keyword first
//					List<FilterableAsset> keywordContainsAssets = filteringAssets.Where(
//						assetData => 
//						!assetData.isFiltered && 
//						Regex.IsMatch(assetData.asset.importFrom, filter.FilterKeyword, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace)
//					).ToList();
//
//					List<FilterableAsset> finalFilteredAsset = new List<FilterableAsset>();
//
//					// then, filter by type
//					foreach (var a in keywordContainsAssets) {
//						if (filter.FilterKeytype != AssetBundleGraphSettings.DEFAULT_FILTER_KEYTYPE) {
//							var assumedType = a.asset.filterType;
//							if (assumedType == null || filter.FilterKeytype != assumedType.ToString()) {
//								continue;
//							}
//						}
//						finalFilteredAsset.Add(a);
//					}
//
//					// mark assets as exhausted.
//					foreach (var a in finalFilteredAsset) {
//						a.isFiltered = true;
//					}
//
//					output[groupKey] = finalFilteredAsset.Select(v => v.asset).ToList();
//				}


//				Output(connToChild, output, null);
//			}
		}
	}
}