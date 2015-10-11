using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AssetGraph {
	public class IntegratedGUIGrouping : INodeBase {
		private readonly string groupingKeyword;

		public IntegratedGUIGrouping (string groupingKeyword) {
			this.groupingKeyword = groupingKeyword;
		}

		public void Setup (string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			GroupingOutput(nodeId, labelToNext, groupedSources, Output);
		}

		public void Run (string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			GroupingOutput(nodeId, labelToNext, groupedSources, Output);
		}


		private void GroupingOutput (string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> groupedSources, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			if (!groupingKeyword.Contains(AssetGraphSettings.KEYWORD_WILDCARD.ToString())) {
				Debug.LogWarning("grouping keyword does not contain " + AssetGraphSettings.KEYWORD_WILDCARD + ", will return empty throughput.");
				return;
			}

			var outputDict = new Dictionary<string, List<InternalAssetData>>();

			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];

				foreach (var source in inputSources) {
					var targetPath = source.GetAbsolutePathOrImportedPath();

					var groupingKeywordPrefix = groupingKeyword.Split(AssetGraphSettings.KEYWORD_WILDCARD)[0];
					var groupingKeywordPostfix = groupingKeyword.Split(AssetGraphSettings.KEYWORD_WILDCARD)[1];

					var regex = new Regex(groupingKeywordPrefix + "(.*?)" + groupingKeywordPostfix);
					var match = regex.Match(targetPath);

					if (match.Success) {
						var newGroupingKey = match.Groups[1].Value;
						if (!outputDict.ContainsKey(newGroupingKey)) outputDict[newGroupingKey] = new List<InternalAssetData>();
						outputDict[newGroupingKey].Add(source);
					}
				}
			}
			
			Output(nodeId, labelToNext, outputDict, new List<string>());
		}
	}
}