using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;

namespace AssetBundleGraph {
    public class IntegratedGUIFilter : INodeBase {
		private readonly List<string> containsKeywords;
		private readonly List<string> containsKeytypes;
		public IntegratedGUIFilter (List<string> containsKeywords, List<string> containsKeytypes) {
			this.containsKeywords = containsKeywords;
			this.containsKeytypes = containsKeytypes;
		}

		public void Setup (string nodeId, string noUseLabel, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			// overlapping test.
			{
				var overlappingCheckList = new List<string>();
				for (var i = 0; i < containsKeywords.Count; i++) {
					var keywordAndKeytypeCombind = containsKeywords[i] + containsKeytypes[i];
					if (overlappingCheckList.Contains(keywordAndKeytypeCombind)) throw new Exception("filter keywords and type combination are overlapping:" + containsKeywords[i] + " type:" + containsKeytypes[i]);
					overlappingCheckList.Add(keywordAndKeytypeCombind);
				}
			}

			foreach (var groupKey in groupedSources.Keys) {
				var outputDict = new Dictionary<string, List<InternalAssetData>>();

				var inputSources = groupedSources[groupKey];
				
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
					Filtering(inputSources, _PreOutput);
				} catch (Exception e) {
					Debug.LogError("Filter:" + this + " error:" + e);
				}
			}
		}
		
		public void Run (string nodeId, string noUseLabel, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			// overlapping test.
			{
				var overlappingCheckList = new List<string>();
				for (var i = 0; i < containsKeywords.Count; i++) {
					var keywordAndKeytypeCombind = containsKeywords[i] + containsKeytypes[i];
					if (overlappingCheckList.Contains(keywordAndKeytypeCombind)) throw new Exception("filter keywords and type combination are overlapping:" + containsKeywords[i] + " type:" + containsKeytypes[i]);
					overlappingCheckList.Add(keywordAndKeytypeCombind);
				}
			}
			
			foreach (var groupKey in groupedSources.Keys) {
				var outputDict = new Dictionary<string, List<InternalAssetData>>();

				var inputSources = groupedSources[groupKey];
				
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
					Filtering(inputSources, _Output);
				} catch (Exception e) {
					Debug.LogError("Filter:" + this + " error:" + e);
				}
			}
		}

		private void Filtering (List<InternalAssetData> assets, Action<string, List<string>> Out) {
			for (var i = 0; i < containsKeywords.Count; i++) {
				var keyword = containsKeywords[i];
				var keytype = containsKeytypes[i];
				
				var contains = assets.Where(assetData => assetData.importedPath.Contains(keyword)).ToList();
				
				// if keyword is wildcard, use type for constraint. pass all assets.
				if (keyword == AssetBundleGraphSettings.FILTER_KEYWORD_WILDCARD) contains = assets; 
				
				// type constraint.
				if (keytype != AssetBundleGraphSettings.DEFAULT_FILTER_KEYTYPE) {
					var typeContains = new List<string>();
					
					foreach (var containedAssetData in contains) {
						var assumedType = TypeBinder.AssumeTypeOfAsset(containedAssetData.importedPath);
						if (keytype == assumedType.ToString()) typeContains.Add(containedAssetData.absoluteSourcePath);
					}
					
					Out(keyword, typeContains);
					continue;
				}
				 
				var containsAssetAbsolutePaths = contains.Select(assetData => assetData.absoluteSourcePath).ToList();
				Out(keyword, containsAssetAbsolutePaths);
			}
		}
		
		

		public static void ValidateFilter (string currentFilterKeyword, List<string> keywords, Action NullOrEmpty, Action AlreadyContained) {
			if (string.IsNullOrEmpty(currentFilterKeyword)) NullOrEmpty();
			if (keywords.Contains(currentFilterKeyword)) AlreadyContained();
		}
	}
}