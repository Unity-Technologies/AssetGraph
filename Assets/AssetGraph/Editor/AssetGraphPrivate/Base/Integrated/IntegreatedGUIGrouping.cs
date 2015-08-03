using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AssetGraph {
	public class IntegreatedGUIGrouping : INodeBase {
		private readonly string groupingKeyword;

		public IntegreatedGUIGrouping (string groupingKeyword) {
			this.groupingKeyword = groupingKeyword;
		}

		public void Setup (string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> groupedSources, Action<string, string, Dictionary<string, List<InternalAssetData>>> Output) {
			if (string.IsNullOrEmpty(groupingKeyword)) {
				Debug.LogWarning("groupingKeyword is empty.");
				return;
			}

			var outputDict = new Dictionary<string, List<InternalAssetData>>();

			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];

				foreach (var source in inputSources) {
					var targetPath = source.importedPath;

					var groupingKeywordPrefix = groupingKeyword.Split('*')[0];
					var groupingKeywordPostfix = groupingKeyword.Split('*')[1];

					var regex = new Regex(groupingKeywordPrefix + "(.*?)" + groupingKeywordPostfix);
					var match = regex.Match(targetPath);

					if (match.Success) {
						var newGroupingKey = match.Groups[1].Value;
						if (!outputDict.ContainsKey(newGroupingKey)) outputDict[newGroupingKey] = new List<InternalAssetData>();
						outputDict[newGroupingKey].Add(source);
					}
				}
			}
			
			Output(nodeId, labelToNext, outputDict);
		}

		public void Run (string nodeId, string labelToNext, Dictionary<string, List<InternalAssetData>> groupedSources, Action<string, string, Dictionary<string, List<InternalAssetData>>> Output) {
			if (string.IsNullOrEmpty(groupingKeyword)) {
				Debug.LogWarning("groupingKeyword is empty.");
				return;
			}
			
			var outputDict = new Dictionary<string, List<InternalAssetData>>();

			foreach (var groupKey in groupedSources.Keys) {
				var inputSources = groupedSources[groupKey];

				foreach (var source in inputSources) {
					var targetPath = source.importedPath;

					var groupingKeywordPrefix = groupingKeyword.Split('*')[0];
					var groupingKeywordPostfix = groupingKeyword.Split('*')[1];

					var regex = new Regex(groupingKeywordPrefix + "(.*?)" + groupingKeywordPostfix);
					var match = regex.Match(targetPath);

					if (match.Success) {
						var newGroupingKey = match.Groups[1].Value;
						if (!outputDict.ContainsKey(newGroupingKey)) outputDict[newGroupingKey] = new List<InternalAssetData>();
						outputDict[newGroupingKey].Add(source);
					}
				}
			}
			
			Output(nodeId, labelToNext, outputDict);
		}
	}
}