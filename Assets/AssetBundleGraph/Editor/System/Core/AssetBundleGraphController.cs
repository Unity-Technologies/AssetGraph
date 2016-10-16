using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace AssetBundleGraph {
	/*
	 * AssetBundleGraphController executes operations based on graph 
	 */
	public class AssetBundleGraphController {

		/*
		 * Verify nodes does not create cycle
		 */
		private static void ValidateLoopConnection(SaveData saveData) {
			var leaf = saveData.CollectAllLeafNodes();
			foreach (var leafNode in leaf) {
				MarkAndTraverseParent(saveData, leafNode, new List<ConnectionData>(), new List<NodeData>());
			}
		}

		private static void MarkAndTraverseParent(SaveData saveData, NodeData current, List<ConnectionData> visitedConnections, List<NodeData> visitedNode) {

			// if node is visited from other route, just quit
			if(visitedNode.Contains(current)) {
				return;
			}

			var connectionsToParents = saveData.Connections.FindAll(con => con.ToNodeId == current.Id);
			foreach(var c in connectionsToParents) {
				if(visitedConnections.Contains(c)) {
					throw new NodeException("Looped connection detected. Please fix connections to avoid loop.", current.Id);
				}

				var parentNode = saveData.Nodes.Find(node => node.Id == c.FromNodeId);
				UnityEngine.Assertions.Assert.IsNotNull(parentNode);

				visitedConnections.Add(c);
				MarkAndTraverseParent(saveData, parentNode, visitedConnections, visitedNode);
			}

			visitedNode.Add(current);
		}

		/**
		 * Execute Run operations using current graph
		 */
		public static Dictionary<ConnectionData, Dictionary<string, List<Asset>>> 
		Perform (
			SaveData saveData, 
			BuildTarget target,
			bool isRun,
			Action<NodeException> errorHandler,
			Action<NodeData, float> updateHandler) 
		{
			bool validateFailed = false;
			try {
				ValidateLoopConnection(saveData);
			} catch (NodeException e) {
				errorHandler(e);
				validateFailed = true;
			}

			var resultDict = new Dictionary<ConnectionData, Dictionary<string, List<Asset>>>();
			var performedIds = new List<string>();
			var cacheDict  = new Dictionary<NodeData, List<string>>();

			// if validation failed, node may contain looped connections, so we are not going to 
			// go into each operations.
			if(!validateFailed) {
				var leaf = saveData.CollectAllLeafNodes();

				foreach (var leafNode in leaf) {
					if( leafNode.InputPoints.Count == 0 ) {
						DoNodeOperation(target, leafNode, null, null, saveData, resultDict, cacheDict, performedIds, isRun, errorHandler, updateHandler);
					} else {
						foreach(var inputPoint in leafNode.InputPoints) {
							DoNodeOperation(target, leafNode, inputPoint, null, saveData, resultDict, cacheDict, performedIds, isRun, errorHandler, updateHandler);
						}
					}
				}
			}
			return resultDict;
		}
			
		/**
			Perform Run or Setup from parent of given terminal node recursively.
		*/
		private static void DoNodeOperation (
			BuildTarget target,
			NodeData currentNodeData,
			ConnectionPointData currentInputPoint,
			ConnectionData connectionToOutput,
			SaveData saveData,
			Dictionary<ConnectionData, Dictionary<string, List<Asset>>> resultDict, 
			Dictionary<NodeData, List<string>> cachedDict,
			List<string> performedIds,
			bool isActualRun,
			Action<NodeException> errorHandler,
			Action<NodeData, float> updateHandler
		) {
			if (performedIds.Contains(currentNodeData.Id) || (currentInputPoint != null && performedIds.Contains(currentInputPoint.Id))) {
				return;
			}

			/*
			 * Find connections coming into this node from parent node, and traverse recursively
			*/
			var connectionsToParents = saveData.Connections.FindAll(con => con.ToNodeId == currentNodeData.Id);

			foreach (var c in connectionsToParents) {

				var parentNode = saveData.Nodes.Find(node => node.Id == c.FromNodeId);
				UnityEngine.Assertions.Assert.IsNotNull(parentNode);

				// check if nodes can connect together
				ConnectionData.ValidateConnection(parentNode, currentNodeData);
				if( parentNode.InputPoints.Count > 0 ) {
					// if node has multiple input, node is operated per input
					foreach(var parentInputPoint in parentNode.InputPoints) {
						DoNodeOperation(target, parentNode, parentInputPoint, c, saveData, resultDict, cachedDict, performedIds, isActualRun, errorHandler, updateHandler);
					}
				} 
				// if parent does not have input point, call with inputPoint==null
				else {
					DoNodeOperation(target, parentNode, null, c, saveData, resultDict, cachedDict, performedIds, isActualRun, errorHandler, updateHandler);
				}
			}

			// mark this point as performed
			if(currentInputPoint != null) {
				performedIds.Add(currentInputPoint.Id);
			} 
			// Root node does not have input point, so we are storing node id instead.
			else {
				performedIds.Add(currentNodeData.Id);
			}

			/*
			 * Perform node operation for this node
			*/

			if (updateHandler != null) {
				updateHandler(currentNodeData, 0f);
			}

			/*
				has next node, run first time.
			*/

			var alreadyCachedPaths = new List<string>();
			if (cachedDict.ContainsKey(currentNodeData)) {
				alreadyCachedPaths.AddRange(cachedDict[currentNodeData]);
			}
			// load already exist cache from node.
			alreadyCachedPaths.AddRange(GetCachedDataByNode(target, currentNodeData));

			// Grab incoming assets from result by refering connections to parents
			var inputGroupAssets = new Dictionary<string, List<Asset>>();
			if(currentInputPoint != null) {
				// aggregates all input assets coming from current inputPoint
				var connToParentsFromCurrentInput = saveData.Connections.FindAll(con => con.ToNodeConnectionPointId == currentInputPoint.Id);
				foreach (var rCon in connToParentsFromCurrentInput) {
					if (!resultDict.ContainsKey(rCon)) {
						continue;
					}

					var result = resultDict[rCon];
					foreach (var groupKey in result.Keys) {
						if (!inputGroupAssets.ContainsKey(groupKey)) {
							inputGroupAssets[groupKey] = new List<Asset>();
						}
						inputGroupAssets[groupKey].AddRange(result[groupKey]);	
					}
				}
			}

			/*
				the Action passes to NodeOperaitons.
				It stores result to resultDict.
			*/
			Action<ConnectionData, Dictionary<string, List<Asset>>, List<string>> Output = 
				(ConnectionData destinationConnection, Dictionary<string, List<Asset>> outputGroupAsset, List<string> cachedItems) => 
			{
				if(destinationConnection != null ) {
					if (!resultDict.ContainsKey(destinationConnection)) {
						resultDict[destinationConnection] = new Dictionary<string, List<Asset>>();
					}
					/*
					merge connection result by group key.
					*/
					foreach (var groupKey in outputGroupAsset.Keys) {
						if (!resultDict[destinationConnection].ContainsKey(groupKey)) {
							resultDict[destinationConnection][groupKey] = new List<Asset>();
						}
						resultDict[destinationConnection][groupKey].AddRange(outputGroupAsset[groupKey]);
					}
				}

				if (isActualRun) {
					if (!cachedDict.ContainsKey(currentNodeData)) {
						cachedDict[currentNodeData] = new List<string>();
					}
					if(cachedItems != null) {
						cachedDict[currentNodeData].AddRange(cachedItems);
					}
				}
			};

			try {
				INodeOperation executor = CreateOperation(saveData, currentNodeData, errorHandler);
				if(executor != null) {
					if(isActualRun) {
						executor.Run(target, currentNodeData, currentInputPoint, connectionToOutput, inputGroupAssets, alreadyCachedPaths, Output);
					}
					else {
						executor.Setup(target, currentNodeData, currentInputPoint, connectionToOutput, inputGroupAssets, alreadyCachedPaths, Output);
					}
				}

			} catch (NodeException e) {
				errorHandler(e);
				// since error occured, this node should stop running for other inputpoints. Adding node id to stop.
				if(!performedIds.Contains(currentNodeData.Id)) {
					performedIds.Add(currentNodeData.Id);
				}
			}

			if (updateHandler != null) {
				updateHandler(currentNodeData, 1f);
			}
		}

		public static INodeOperation CreateOperation(SaveData saveData, NodeData currentNodeData, Action<NodeException> errorHandler) {
			INodeOperation executor = null;

			try {
				switch (currentNodeData.Kind) {
				case NodeKind.LOADER_GUI: {
						executor = new IntegratedGUILoader();
						break;
					}
				case NodeKind.FILTER_GUI: {
						// Filter requires multiple output connections
						var connectionsToChild = saveData.Connections.FindAll(c => c.FromNodeId == currentNodeData.Id);
						executor = new IntegratedGUIFilter(connectionsToChild);
						break;
					}

				case NodeKind.IMPORTSETTING_GUI: {
						executor = new IntegratedGUIImportSetting();
						break;
					}
				case NodeKind.MODIFIER_GUI: {
						executor = new IntegratedGUIModifier();
						break;
					}
				case NodeKind.GROUPING_GUI: {
						executor = new IntegratedGUIGrouping();
						break;
					}
				case NodeKind.PREFABBUILDER_GUI: {
						executor = new IntegratedPrefabBuilder();
						break;
					}

				case NodeKind.BUNDLECONFIG_GUI: {
						executor = new IntegratedGUIBundleConfigurator();
						break;
					}

				case NodeKind.BUNDLEBUILDER_GUI: {
						executor = new IntegratedGUIBundleBuilder();
						break;
					}

				case NodeKind.EXPORTER_GUI: {
						executor = new IntegratedGUIExporter();
						break;
					}

				default: {
						Debug.LogError(currentNodeData.Name + " is defined as unknown kind of node. value:" + currentNodeData.Kind);
						break;
					}
				} 
			} catch (NodeException e) {
				errorHandler(e);
			}

			return executor;
		}

		public static List<string> GetCachedDataByNode (BuildTarget t, NodeData node) {
			switch (node.Kind) {
				case NodeKind.IMPORTSETTING_GUI: {
					// no cache file exists for importSetting.
					return new List<string>();
				}
				case NodeKind.MODIFIER_GUI: {
					// no cache file exists for modifier.
					return new List<string>();
				}
				
				case NodeKind.PREFABBUILDER_GUI: {
					var cachedPathBase = FileUtility.PathCombine(
						AssetBundleGraphSettings.PREFABBUILDER_CACHE_PLACE, 
						node.Id,
						SystemDataUtility.GetPathSafeTargetName(t)
					);

					// no cache folder, no cache.
					if (!Directory.Exists(cachedPathBase)) {
						// search default platform + package
						cachedPathBase = FileUtility.PathCombine(
							AssetBundleGraphSettings.PREFABBUILDER_CACHE_PLACE, 
							node.Id,
							SystemDataUtility.GetPathSafeDefaultTargetName()
						);

						if (!Directory.Exists(cachedPathBase)) {
							return new List<string>();
						}
					}

					return FileUtility.GetFilePathsInFolder(cachedPathBase);
				}
				 
				case NodeKind.BUNDLECONFIG_GUI: {
					// do nothing.
					break;
				}

				case NodeKind.BUNDLEBUILDER_GUI: {
					var cachedPathBase = FileUtility.PathCombine(
						AssetBundleGraphSettings.BUNDLEBUILDER_CACHE_PLACE, 
						node.Id,
						SystemDataUtility.GetPathSafeTargetName(t)
					);

					// no cache folder, no cache.
					if (!Directory.Exists(cachedPathBase)) {
						// search default platform + package
						cachedPathBase = FileUtility.PathCombine(
							AssetBundleGraphSettings.BUNDLEBUILDER_CACHE_PLACE, 
							node.Id,
							SystemDataUtility.GetPathSafeDefaultTargetName()
						);

						if (!Directory.Exists(cachedPathBase)) {
							return new List<string>();
						}
					}

					return FileUtility.GetFilePathsInFolder(cachedPathBase);
				}

				default: {
					// nothing to do.
					break;
				}
			}
			return new List<string>();
		}
		
		public static void Postprocess (SaveData saveData, Dictionary<ConnectionData, Dictionary<string, List<Asset>>> result, bool isBuild) 
		{
			var nodeResult = CollectNodeGroupAndAssets(saveData, result);

			var postprocessType = typeof(IPostprocess);
			var ppTypes = Assembly.GetExecutingAssembly().GetTypes().Select(v => v).Where(v => v != postprocessType && postprocessType.IsAssignableFrom(v)).ToList();
			foreach (var t in ppTypes) {
				var postprocessScriptInstance = Assembly.GetExecutingAssembly().CreateInstance(t.Name);
				if (postprocessScriptInstance == null) {
					throw new AssetBundleGraphException("Postprocess " + t.Name + " failed to run (failed to create instance from assembly).");
				}
				var postprocessInstance = (IPostprocess)postprocessScriptInstance;

				postprocessInstance.Run(nodeResult, isBuild);
			}
		}

		private static Dictionary<NodeData, Dictionary<string, List<Asset>>> CollectNodeGroupAndAssets (
			SaveData data,
			Dictionary<ConnectionData, Dictionary<string, List<Asset>>> result
		) {
			var nodeDatas = new Dictionary<NodeData, Dictionary<string, List<Asset>>>();

			foreach (var c in result.Keys) {
				var targetNode = data.Nodes.Find(node => node.Id == c.FromNodeId);
				var groupDict = result[c];

				if (!nodeDatas.ContainsKey(targetNode)) {
					nodeDatas[targetNode] = new Dictionary<string, List<Asset>>();
				}
				foreach (var groupKey in groupDict.Keys) {
					if (!nodeDatas[targetNode].ContainsKey(groupKey)) {
						nodeDatas[targetNode][groupKey] = new List<Asset>();
					}
					nodeDatas[targetNode][groupKey].AddRange(groupDict[groupKey]);
				}
			}

			return nodeDatas;
		}

	}
}
