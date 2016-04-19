using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;

namespace AssetGraph {
	public class IntegratedGUIFilter : INodeBase {
		private readonly List<string> containsKeywords;
		private readonly List<string> containsKeytypes;
		public IntegratedGUIFilter (List<string> containsKeywords, List<string> containsKeytypes) {
			this.containsKeywords = containsKeywords;
			this.containsKeytypes = containsKeytypes;
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
			for (var i = 0; i < containsKeywords.Count; i++) {
				var keyword = containsKeywords[i];
				var keytype = containsKeytypes[i];
				
				var contains = source.Where(path => path.Contains(keyword)).ToList();
				
				// type constraint.
				if (keytype != AssetGraphSettings.DEFAULT_FILTER_KEYTYPE) {
					var typeContains = new List<string>();
					
					foreach (var contain in contains) {
						var assumedType = TypeBinder.AssumeTypeFromExtension(contain);
						if (keytype == assumedType.ToString()) typeContains.Add(contain);
					}
					
					Out(keyword, typeContains);
					continue;
				}
				 
				Out(keyword, contains);
			}
		}
		
		

		public static void ValidateFilter (string currentFilterKeyword, List<string> keywords, Action NullOrEmpty, Action AlreadyContained) {
			if (string.IsNullOrEmpty(currentFilterKeyword)) NullOrEmpty();
			if (keywords.Contains(currentFilterKeyword)) AlreadyContained();
		}
	}
}