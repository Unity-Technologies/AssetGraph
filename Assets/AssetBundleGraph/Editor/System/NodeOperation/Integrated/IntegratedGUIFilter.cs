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
			var output = new Dictionary<string, List<AssetReference>>();

			foreach(var groupKey in inputGroupAssets.Keys) {

				var assets = new List<FilterableAsset>();
				inputGroupAssets[groupKey].ForEach(a => assets.Add(new FilterableAsset(a)));

				foreach(var a in assets) {
					foreach(var filter in node.FilterConditions) {
						if(a.isFiltered) {
							continue;
						}
						bool isTargetFilter = false;
						if(connectionToOutput != null) {
							isTargetFilter = connectionToOutput.FromNodeConnectionPointId == filter.ConnectionPoint.Id;
						}

						bool keywordMatch = Regex.IsMatch(a.asset.importFrom, filter.FilterKeyword, 
							RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

						bool match = keywordMatch;

						if(keywordMatch && filter.FilterKeytype != AssetBundleGraphSettings.DEFAULT_FILTER_KEYTYPE) 
						{
							var assumedType = a.asset.filterType;
							match = assumedType != null && filter.FilterKeytype == assumedType.ToString();
						}

						if(match) {
							a.isFiltered = true;
							if(isTargetFilter) {
								if(!output.ContainsKey(groupKey)) {
									output[groupKey] = new List<AssetReference>();
								}
								output[groupKey].Add(a.asset);
							}
						}
					}
				}
			}

			Output(output);
		}
	}
}