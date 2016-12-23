
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

namespace AssetBundleGraph
{
    public class IntegratedGUIGrouping : INodeOperation {

		public void Setup (BuildTarget target, 
			NodeData node, 
			ConnectionData connectionFromInput,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<AssetReference>> inputGroupAssets, 
			PerformGraph.Output Output) 
		{
			Profiler.BeginSample("AssetBundleGraph.GUIGrouping.Setup");
			GroupingOutput(target, node, connectionFromInput, connectionToOutput, inputGroupAssets, Output);
			Profiler.EndSample();
		}

		public void Run (BuildTarget target, 
			NodeData node, 
			ConnectionData connectionFromInput,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<AssetReference>> inputGroupAssets, 
			PerformGraph.Output Output) 
		{
			Profiler.BeginSample("AssetBundleGraph.GUIGrouping.Run");
			GroupingOutput(target, node, connectionFromInput, connectionToOutput, inputGroupAssets, Output);
			Profiler.EndSample();
		}


		private void GroupingOutput (BuildTarget target, 
			NodeData node, 
			ConnectionData connectionFromInput,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<AssetReference>> inputGroupAssets, 
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

			var outputDict = new Dictionary<string, List<AssetReference>>();

			var mergedGroupedSources = new List<AssetReference>();

			if(inputGroupAssets != null) {
				foreach (var groupKey in inputGroupAssets.Keys) {
					mergedGroupedSources.AddRange(inputGroupAssets[groupKey]);
				}

				var groupingKeyword = node.GroupingKeywords[target];
				var split = groupingKeyword.Split(AssetBundleGraphSettings.KEYWORD_WILDCARD);
				var groupingKeywordPrefix  = split[0];
				var groupingKeywordPostfix = split[1];

				foreach (var source in mergedGroupedSources) {
					var targetPath = source.path;

					var regex = new Regex(groupingKeywordPrefix + "(.*?)" + groupingKeywordPostfix);
					var match = regex.Match(targetPath);

					if (match.Success) {
						var newGroupingKey = match.Groups[1].Value;
						if (!outputDict.ContainsKey(newGroupingKey)) outputDict[newGroupingKey] = new List<AssetReference>();
						outputDict[newGroupingKey].Add(source);
					}
				}

				Output(outputDict);
			}
		}

		public static void ValidateGroupingKeyword (string currentGroupingKeyword, Action NullOrEmpty, Action ShouldContainWildCardKey) {
			if (string.IsNullOrEmpty(currentGroupingKeyword)) NullOrEmpty();
			if (!currentGroupingKeyword.Contains(AssetBundleGraphSettings.KEYWORD_WILDCARD.ToString())) ShouldContainWildCardKey();
		}
	}
}