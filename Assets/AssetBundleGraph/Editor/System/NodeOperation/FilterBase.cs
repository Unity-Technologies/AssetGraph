using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;

namespace AssetBundleGraph {
	public class FilterBase : INodeOperationBase {
		public void Setup (string nodeName, string connectionIdToNextNode, string _, Dictionary<string, List<Asset>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output) {
			foreach (var groupKey in groupedSources.Keys) {

				var outputDict = new Dictionary<string, List<Asset>>();

				var inputSources = groupedSources[groupKey];
				var absoluteSourcePaths = inputSources.Select(assetData => assetData.absoluteAssetPath).ToList();
				
				Action<string, List<string>> _PreOutput = (string label, List<string> outputSources) => {
					var outputs = new List<Asset>();
					foreach (var outputSource in outputSources) {
						foreach (var inputSource in inputSources) {
							if (outputSource == inputSource.absoluteAssetPath) {
								outputs.Add(inputSource);
							}
						}
					}

					outputDict[groupKey] = outputs;
					Output(connectionIdToNextNode, label, outputDict, new List<string>());
				};
				try {
					In(absoluteSourcePaths, _PreOutput);
				} catch (Exception e) {
					Debug.LogError(nodeName + " Error:" + e);
				}
			}
		}
		
		public void Run (string nodeName, string connectionIdToNextNode, string _, Dictionary<string, List<Asset>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output) {
			foreach (var groupKey in groupedSources.Keys) {
				var outputDict = new Dictionary<string, List<Asset>>();

				var inputSources = groupedSources[groupKey];
				
				var absoluteSourcePaths = inputSources.Select(assetData => assetData.absoluteAssetPath).ToList();
				
				Action<string, List<string>> _Output = (string label, List<string> outputSources) => {
					var outputs = new List<Asset>();
					foreach (var outputSource in outputSources) {
						foreach (var inputSource in inputSources) {
							if (outputSource == inputSource.absoluteAssetPath) {
								outputs.Add(inputSource);
							}
						}
					}

					outputDict[groupKey] = outputs;
					Output(connectionIdToNextNode, label, outputDict, new List<string>());
				};
				try {
					In(absoluteSourcePaths, _Output);
				} catch (Exception e) {
					Debug.LogError(nodeName + " Error:" + e);
				}
			}
		}


		/**
			フィルタに対して自動的に呼ばれる関数。
		*/
		public virtual void In (List<string> source, Action<string, List<string>> Out) {
			Debug.LogError("The filter class did not have \"In()\" method implemented. Please implement the method to filter:" + this);
		}
	}
}