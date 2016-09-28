using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;

namespace AssetBundleGraph {
	public class FilterBase : INodeOperationBase {
		public void Setup (BuildTarget target, 
			NodeData node, 
			ConnectionData connection, 
			Dictionary<string, List<Asset>> groupedSources, 
			List<string> alreadyCached, 
			Action<NodeData, ConnectionData, Dictionary<string, List<Asset>>, List<string>> Output) 
		{
			foreach (var groupKey in groupedSources.Keys) {

				var outputDict = new Dictionary<string, List<Asset>>();

				var inputAssets = groupedSources[groupKey];

				Action<ConnectionData, List<Asset>> _PreOutput = (ConnectionData c, List<Asset> outputAssets) => {
					var outputs = new List<Asset>();
					foreach (var asset in inputAssets) {
						if (outputAssets.Contains(asset)) {
							outputs.Add(asset);
						}
					}

					outputDict[groupKey] = outputs;
					Output(node, c, outputDict, new List<string>());
				};
				try {
					In(inputAssets, _PreOutput);
				} catch (Exception e) {
					Debug.LogError(node.Name + " Error:" + e);
				}
			}
		}
		
		public void Run (BuildTarget target, 
			NodeData node, 
			ConnectionData connection, 
			Dictionary<string, List<Asset>> groupedSources, 
			List<string> alreadyCached, 
			Action<NodeData, ConnectionData, Dictionary<string, List<Asset>>, List<string>> Output) 
		{
			foreach (var groupKey in groupedSources.Keys) {
				var outputDict = new Dictionary<string, List<Asset>>();

				var inputSources = groupedSources[groupKey];				

				Action<ConnectionData, List<Asset>> _Output = (ConnectionData c, List<Asset> outputSources) => {
					var outputs = new List<Asset>();
					foreach (var inputSource in inputSources) {
						if (outputSources.Contains(inputSource)) {
							outputs.Add(inputSource);
						}
					}

					outputDict[groupKey] = outputs;
					Output(node, c, outputDict, new List<string>());
				};
				try {
					In(inputSources, _Output);
				} catch (Exception e) {
					Debug.LogError(node.Name + " Error:" + e);
				}
			}
		}


		/**
			フィルタに対して自動的に呼ばれる関数。
		*/
		public virtual void In (List<Asset> source, Action<ConnectionData, List<Asset>> Out) {
			Debug.LogError("The filter class did not have \"In()\" method implemented. Please implement the method to filter:" + this);
		}
	}
}