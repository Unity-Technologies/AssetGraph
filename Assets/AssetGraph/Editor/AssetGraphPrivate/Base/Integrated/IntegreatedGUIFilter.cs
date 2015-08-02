using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;

namespace AssetGraph {
	public class IntegreatedGUIFilter : INodeBase {
		private readonly List<string> containsKeywords;
		public IntegreatedGUIFilter (List<string> containsKeywords) {
			this.containsKeywords = containsKeywords;
		}

		public void Setup (string nodeId, string noUseLabel, List<InternalAssetData> inputSources, Action<string, string, List<InternalAssetData>> Output) {
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
				
				Output(nodeId, label, outputs);
			};
			try {
				In(absoluteSourcePaths, _PreOutput);
			} catch (Exception e) {
				Debug.LogError("Filter:" + this + " error:" + e);
			}
		}
		
		public void Run (string nodeId, string noUseLabel, List<InternalAssetData> inputSources, Action<string, string, List<InternalAssetData>> Output) {
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

				Output(nodeId, label, outputs);
			};
			try {
				In(absoluteSourcePaths, _Output);
			} catch (Exception e) {
				Debug.LogError("Filter:" + this + " error:" + e);
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