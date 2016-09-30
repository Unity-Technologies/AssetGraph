
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

namespace AssetBundleGraph
{
    public class IntegratedGUIGrouping : INodeOperationBase {

		public void Setup (BuildTarget target, 
			NodeData node, 
			ConnectionPointData inputPoint,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<Asset>> inputGroupAssets, 
			List<string> alreadyCached, 
			Action<ConnectionData, Dictionary<string, List<Asset>>, List<string>> Output) 
		{
			GroupingOutput(target, node, inputPoint, connectionToOutput, inputGroupAssets, Output);
		}

		public void Run (BuildTarget target, 
			NodeData node, 
			ConnectionPointData inputPoint,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<Asset>> inputGroupAssets, 
			List<string> alreadyCached, 
			Action<ConnectionData, Dictionary<string, List<Asset>>, List<string>> Output) 
		{
			GroupingOutput(target, node, inputPoint, connectionToOutput, inputGroupAssets, Output);
		}


		private void GroupingOutput (BuildTarget target, 
			NodeData node, 
			ConnectionPointData inputPoint,
			ConnectionData connectionToOutput, 
			Dictionary<string, List<Asset>> inputGroupAssets, 
			Action<ConnectionData, Dictionary<string, List<Asset>>, List<string>> Output) 
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

			var outputDict = new Dictionary<string, List<Asset>>();

			var mergedGroupedSources = new List<Asset>();

			foreach (var groupKey in inputGroupAssets.Keys) {
				mergedGroupedSources.AddRange(inputGroupAssets[groupKey]);
			}

			var groupingKeyword = node.GroupingKeywords[target];
			var split = groupingKeyword.Split(AssetBundleGraphSettings.KEYWORD_WILDCARD);
			var groupingKeywordPrefix  = split[0];
			var groupingKeywordPostfix = split[1];

			foreach (var source in mergedGroupedSources) {
				var targetPath = source.GetAbsolutePathOrImportedPath();

				var regex = new Regex(groupingKeywordPrefix + "(.*?)" + groupingKeywordPostfix);
				var match = regex.Match(targetPath);

				if (match.Success) {
					var newGroupingKey = match.Groups[1].Value;
					if (!outputDict.ContainsKey(newGroupingKey)) outputDict[newGroupingKey] = new List<Asset>();
					outputDict[newGroupingKey].Add(source);
				}
			}
			
			Output(connectionToOutput, outputDict, null);
		}

		public static void ValidateGroupingKeyword (string currentGroupingKeyword, Action NullOrEmpty, Action ShouldContainWildCardKey) {
			if (string.IsNullOrEmpty(currentGroupingKeyword)) NullOrEmpty();
			if (!currentGroupingKeyword.Contains(AssetBundleGraphSettings.KEYWORD_WILDCARD.ToString())) ShouldContainWildCardKey();
		}
	}
}