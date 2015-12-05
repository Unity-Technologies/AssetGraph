using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;

namespace AssetGraph {
	public class IntegratedGUIFilter : INodeBase {
		private readonly List<string> containsKeywords;
		public IntegratedGUIFilter (List<string> containsKeywords) {
			this.containsKeywords = containsKeywords;
		}

		public void Setup (string nodeId, string noUseLabel, string package, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			var duplicated = containsKeywords.GroupBy(x => x)
				.Where(group => group.Count() > 1)
				.Select(group => group.Key)
				.ToList();
			if (duplicated.Any()) throw new Exception("filter keywords are overlapping:" + duplicated[0]);

			foreach (var groupKey in groupedSources.Keys) {
				var outputDict = new Dictionary<string, List<InternalAssetData>>();

				var inputSources = groupedSources[groupKey];
				var absoluteSourcePaths = inputSources.Select(assetData => assetData.GetAbsolutePathOrImportedPath()).ToList();
				
				Action<string, List<string>> _PreOutput = (string label, List<string> outputSources) => {
					var outputs = new List<InternalAssetData>();
					
					foreach (var outputSource in outputSources) {
						foreach (var inputSource in inputSources) {
							if (outputSource == inputSource.GetAbsolutePathOrImportedPath()) {
								outputs.Add(inputSource);
							}
						}
					}
					
					outputDict[groupKey] = outputs;
					Output(nodeId, label, outputDict, new List<string>());
				};
				try {
					In(absoluteSourcePaths, _PreOutput);
				} catch (Exception e) {
					Debug.LogError("Filter:" + this + " error:" + e);
				}
			}
		}
		
		public void Run (string nodeId, string noUseLabel, string package, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			var duplicated = containsKeywords.GroupBy(x => x)
				.Where(group => group.Count() > 1)
				.Select(group => group.Key)
				.ToList();
			if (duplicated.Any()) throw new Exception("filter keywords are overlapping:" + duplicated[0]);
			
			foreach (var groupKey in groupedSources.Keys) {
				var outputDict = new Dictionary<string, List<InternalAssetData>>();

				var inputSources = groupedSources[groupKey];
				
				var absoluteSourcePaths = inputSources.Select(assetData => assetData.GetAbsolutePathOrImportedPath()).ToList();
				Action<string, List<string>> _Output = (string label, List<string> outputSources) => {
					var outputs = new List<InternalAssetData>();
					
					foreach (var outputSource in outputSources) {
						foreach (var inputSource in inputSources) {
							if (outputSource == inputSource.GetAbsolutePathOrImportedPath()) {
								outputs.Add(inputSource);
							}
						}
					}

					outputDict[groupKey] = outputs;
					Output(nodeId, label, outputDict, new List<string>());
				};
				try {
					In(absoluteSourcePaths, _Output);
				} catch (Exception e) {
					Debug.LogError("Filter:" + this + " error:" + e);
				}
			}
		}

		private void In (List<string> source, Action<string, List<string>> Out) {
			foreach (var containsKeyword in containsKeywords) {
				var contains = source.Where(path => path.Contains(containsKeyword)).ToList();
				Out(containsKeyword, contains);
			}
		}

		public static void ValidateFilter (string currentFilterKeyword, List<string> keywords, Action NullOrEmpty, Action AlreadyContained) {
			if (string.IsNullOrEmpty(currentFilterKeyword)) NullOrEmpty();
			if (keywords.Contains(currentFilterKeyword)) AlreadyContained();
		}
	}
}