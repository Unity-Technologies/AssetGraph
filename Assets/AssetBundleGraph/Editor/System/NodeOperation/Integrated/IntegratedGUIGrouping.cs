
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AssetBundleGraph
{
    public class IntegratedGUIGrouping : INodeOperationBase {
		private readonly string groupingKeyword;

		public IntegratedGUIGrouping (string groupingKeyword) {
			this.groupingKeyword = groupingKeyword;
		}

		public void Setup (string nodeName, string nodeId, string connectionIdToNextNode, Dictionary<string, List<Asset>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output) {
			GroupingOutput(nodeName, nodeId, connectionIdToNextNode, groupedSources, Output);
		}

		public void Run (string nodeName, string nodeId, string connectionIdToNextNode, Dictionary<string, List<Asset>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output) {
			GroupingOutput(nodeName, nodeId, connectionIdToNextNode, groupedSources, Output);
		}


		private void GroupingOutput (string nodeName, string nodeId, string connectionIdToNextNode, Dictionary<string, List<Asset>> groupedSources, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output) {

			try {
				ValidateGroupingKeyword(
					groupingKeyword,
					() => {
						throw new NodeException("Grouping Keyword can not be empty.", nodeId);
					},
					() => {
						throw new NodeException(String.Format("Grouping Keyword must contain {0} for numbering: currently {1}", AssetBundleGraphSettings.KEYWORD_WILDCARD, groupingKeyword), nodeId);
					}
				);
			}  catch(NodeException e) {
				AssetBundleGraphEditorWindow.AddNodeException(e);
				return;
			}

			var outputDict = new Dictionary<string, List<Asset>>();

			var mergedGroupedSources = new List<Asset>();

			foreach (var groupKey in groupedSources.Keys) {
				mergedGroupedSources.AddRange(groupedSources[groupKey]);
			}

			foreach (var source in mergedGroupedSources) {
				var targetPath = source.GetAbsolutePathOrImportedPath();

				var groupingKeywordPrefix = groupingKeyword.Split(AssetBundleGraphSettings.KEYWORD_WILDCARD)[0];
				var groupingKeywordPostfix = groupingKeyword.Split(AssetBundleGraphSettings.KEYWORD_WILDCARD)[1];

				var regex = new Regex(groupingKeywordPrefix + "(.*?)" + groupingKeywordPostfix);
				var match = regex.Match(targetPath);

				if (match.Success) {
					var newGroupingKey = match.Groups[1].Value;
					if (!outputDict.ContainsKey(newGroupingKey)) outputDict[newGroupingKey] = new List<Asset>();
					outputDict[newGroupingKey].Add(source);
				}
			}
			
			Output(nodeId, connectionIdToNextNode, outputDict, new List<string>());
		}

		public static void ValidateGroupingKeyword (string currentGroupingKeyword, Action NullOrEmpty, Action ShouldContainWildCardKey) {
			if (string.IsNullOrEmpty(currentGroupingKeyword)) NullOrEmpty();
			if (!currentGroupingKeyword.Contains(AssetBundleGraphSettings.KEYWORD_WILDCARD.ToString())) ShouldContainWildCardKey();
		}
	}
}