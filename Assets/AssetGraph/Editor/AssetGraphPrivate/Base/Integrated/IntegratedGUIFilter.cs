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

		public void Setup (string nodeId, string noUseLabel, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			foreach (var groupKey in groupedSources.Keys) {
				var outputDict = new Dictionary<string, List<InternalAssetData>>();

				var inputSources = groupedSources[groupKey];
				var absoluteSourcePaths = inputSources.Select(assetData => assetData.absoluteSourcePath).ToList();
				
				Action<string, List<string>> _PreOutput = (string label, List<string> outputSources) => {
					var outputs = new List<InternalAssetData>();
					foreach (var outputSource in outputSources) {
						foreach (var inputSource in inputSources) {
							if (outputSource == inputSource.absoluteSourcePath) {
								outputs.Add(inputSource);
							}
						}
					}
					
					outputDict[groupKey] = outputs;
					Output(nodeId, label, outputDict, alreadyCached);
				};
				try {
					In(absoluteSourcePaths, _PreOutput);
				} catch (Exception e) {
					Debug.LogError("Filter:" + this + " error:" + e);
				}
			}
		}
		
		public void Run (string nodeId, string noUseLabel,  Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			foreach (var groupKey in groupedSources.Keys) {
				var outputDict = new Dictionary<string, List<InternalAssetData>>();

				var inputSources = groupedSources[groupKey];
				
				var absoluteSourcePaths = inputSources.Select(assetData => assetData.absoluteSourcePath).ToList();
				
				Action<string, List<string>> _Output = (string label, List<string> outputSources) => {
					var outputs = new List<InternalAssetData>();
					foreach (var outputSource in outputSources) {
						foreach (var inputSource in inputSources) {
							if (outputSource == inputSource.absoluteSourcePath) {
								outputs.Add(inputSource);
							}
						}
					}

					outputDict[groupKey] = outputs;
					Output(nodeId, label, outputDict, alreadyCached);
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
	}
}