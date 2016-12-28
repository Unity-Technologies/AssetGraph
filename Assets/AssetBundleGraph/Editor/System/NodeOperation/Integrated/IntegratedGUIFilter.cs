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
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			Profiler.BeginSample("AssetBundleGraph.GUIFilter.Setup");
			node.ValidateOverlappingFilterCondition(true);
			Filter(node, incoming, connectionsToOutput, Output);
			Profiler.EndSample();
		}
		
		public void Run (BuildTarget target, 
			NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			//Operation is completed furing Setup() phase, so do nothing in Run.
		}

		private void Filter (NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			if(connectionsToOutput == null || incoming == null) {
				return;
			}

			var allOutput = new Dictionary<string, Dictionary<string, List<AssetReference>>>();

			foreach(var outPoints in node.OutputPoints) {
				allOutput[outPoints.Id] = new Dictionary<string, List<AssetReference>>();
			}

			foreach(var ag in incoming) {
				foreach(var groupKey in ag.assetGroups.Keys) {

					foreach(var a in ag.assetGroups[groupKey]) {
						foreach(var filter in node.FilterConditions) {
							bool keywordMatch = Regex.IsMatch(a.importFrom, filter.FilterKeyword, 
								RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

							bool match = keywordMatch;

							if(keywordMatch && filter.FilterKeytype != AssetBundleGraphSettings.DEFAULT_FILTER_KEYTYPE) 
							{
								var assumedType = a.filterType;
								match = assumedType != null && filter.FilterKeytype == assumedType.ToString();
							}

							if(match) {
								var output = allOutput[filter.ConnectionPoint.Id];
								if(!output.ContainsKey(groupKey)) {
									output[groupKey] = new List<AssetReference>();
								}
								output[groupKey].Add(a);
								// consume this asset with this output
								break;
							}
						}
					}
				}
			}

			foreach(var dst in connectionsToOutput) {
				if(allOutput.ContainsKey(dst.FromNodeConnectionPointId)) {
					Output(dst, allOutput[dst.FromNodeConnectionPointId]);
				}
			}
		}
	}
}