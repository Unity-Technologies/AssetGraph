
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

namespace AssetBundleGraph
{
    public class IntegratedGUIGrouping : INodeOperation {

		public void Setup (BuildTarget target, 
			NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			GroupingOutput(target, node, incoming, connectionsToOutput, Output);
		}

		public void Run (BuildTarget target, 
			NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output,
			Action<NodeData, string, float> progressFunc) 
		{
			//Operation is completed furing Setup() phase, so do nothing in Run.
		}


		private void GroupingOutput (BuildTarget target, 
			NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{

			ValidateGroupingKeyword(
				node.GroupingKeywords[target],
				() => {
					throw new NodeException("Grouping Keyword can not be empty.", node.Id);
				},
				() => {
					throw new NodeException(String.Format("Grouping Keyword must contain {0} for numbering: currently {1}", AssetBundleGraphSettings.KEYWORD_WILDCARD, node.GroupingKeywords[target]), node.Id);
				}
			);

			if(incoming == null || connectionsToOutput == null || Output == null) {
				return;
			}

			var outputDict = new Dictionary<string, List<AssetReference>>();
			var groupingKeyword = node.GroupingKeywords[target];
			var split = groupingKeyword.Split(AssetBundleGraphSettings.KEYWORD_WILDCARD);
			var groupingKeywordPrefix  = split[0];
			var groupingKeywordPostfix = split[1];
			var regex = new Regex(groupingKeywordPrefix + "(.*?)" + groupingKeywordPostfix);

			foreach(var ag in incoming) {
				foreach (var assets in ag.assetGroups.Values) {
					foreach(var a in assets) {
						var targetPath = a.path;

						var match = regex.Match(targetPath);

						if (match.Success) {
							var newGroupingKey = match.Groups[1].Value;
							if (!outputDict.ContainsKey(newGroupingKey)) {
								outputDict[newGroupingKey] = new List<AssetReference>();
							}
							outputDict[newGroupingKey].Add(a);
						}
					}
				}
			}

			var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
				null : connectionsToOutput.First();
			Output(dst, outputDict);
		}

		public static void ValidateGroupingKeyword (string currentGroupingKeyword, Action NullOrEmpty, Action ShouldContainWildCardKey) {
			if (string.IsNullOrEmpty(currentGroupingKeyword)) NullOrEmpty();
			if (!currentGroupingKeyword.Contains(AssetBundleGraphSettings.KEYWORD_WILDCARD.ToString())) ShouldContainWildCardKey();
		}
	}
}