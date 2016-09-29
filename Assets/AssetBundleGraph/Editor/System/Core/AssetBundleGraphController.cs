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
		 * Verify node name has no collision
		 */
		private static void ValidateNameCollision(SaveData saveData) {
			var nodeNames = saveData.Nodes.Select(node => node.Name).ToList();
			var overlappings = nodeNames.GroupBy(x => x)
				.Where(group => 1 < group.Count())
				.Select(group => group.Key)
				.ToList();

			if (overlappings.Any()) {
				throw new NodeException("Duplicate node name found:" + overlappings[0] + " please rename and avoid name collision.", 
					saveData.Nodes.Find(n=>n.Name == overlappings[0]).Id);
			}
		}

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
		public static Dictionary<ConnectionData, Dictionary<string, List<DepreacatedThroughputAsset>>> 
		Perform (
			SaveData saveData, 
			BuildTarget target,
			bool isRun,
			Action<NodeData, float> updateHandler=null
		) {
			bool validateFailed = false;
			try {
				ValidateNameCollision(saveData);
				ValidateLoopConnection(saveData);
			} catch (NodeException e) {
				AssetBundleGraphEditorWindow.AddNodeException(e);
				validateFailed = true;
			}

			var resultDict = new Dictionary<ConnectionData, Dictionary<string, List<Asset>>>();
			var cacheDict  = new Dictionary<NodeData, List<string>>();

			// if validation failed, node may contain looped connections, so we are not going to 
			// go into each operations.
			if(!validateFailed) {
				var leaf = saveData.CollectAllLeafNodes();

				foreach (var leafNode in leaf) {
					if( leafNode.InputPoints.Count == 0 ) {
						DoNodeOperation(target, leafNode, null, null, saveData, resultDict, cacheDict, new List<ConnectionPointData>(), isRun, updateHandler);
					} else {
						foreach(var inputPoint in leafNode.InputPoints) {
							DoNodeOperation(target, leafNode, inputPoint, null, saveData, resultDict, cacheDict, new List<ConnectionPointData>(), isRun, updateHandler);
						}
					}
				}
			}
			return CollectResult(resultDict);
		}

		/**
		 *  Collect build result: connectionId : < groupName : List<Asset> >
		 */
		private static Dictionary<ConnectionData, Dictionary<string, List<DepreacatedThroughputAsset>>> 
		CollectResult (Dictionary<ConnectionData, Dictionary<string, List<Asset>>> buildResult) {

			var finalResult = new Dictionary<ConnectionData, Dictionary<string, List<DepreacatedThroughputAsset>>>();

			foreach (var connection in buildResult.Keys) {
				var groupDict = buildResult[connection];
				var finalGroupDict = new Dictionary<string, List<DepreacatedThroughputAsset>>();

				foreach (var groupKey in groupDict.Keys) {
					var assets = groupDict[groupKey];
					var finalAssets = new List<DepreacatedThroughputAsset>();

					foreach (var assetData in assets) {
						var bundled = assetData.isBundled;

						if (!string.IsNullOrEmpty(assetData.importFrom)) {
							finalAssets.Add(new DepreacatedThroughputAsset(assetData.importFrom, bundled));
							continue;
						} 

						if (!string.IsNullOrEmpty(assetData.absoluteAssetPath)) {
							var relativeAbsolutePath = assetData.absoluteAssetPath.Replace(FileUtility.ProjectPathWithSlash(), string.Empty);
							finalAssets.Add(new DepreacatedThroughputAsset(relativeAbsolutePath, bundled));
							continue;
						}

						if (!string.IsNullOrEmpty(assetData.exportTo)) {
							finalAssets.Add(new DepreacatedThroughputAsset(assetData.exportTo, bundled));
							continue;
						}
					}
					finalGroupDict[groupKey] = finalAssets;
				}
				finalResult[connection] = finalGroupDict;
			}
			return finalResult;
		}

//		/**
//			Perform Setup on all serialized nodes respect to graph structure.
//			@result returns ordered connectionIds
//		*/
//		private static List<string> PerformSetupForNode (
//			string endNodeId, 
//			List<NodeData> allNodes, 
//			List<ConnectionData> connections, 
//			Dictionary<string, Dictionary<string, List<Asset>>> resultDict,
//			Dictionary<string, List<string>> cacheDict
//		) {
//			DoNodeOperation(endNodeId, allNodes, connections, resultDict, cacheDict, new List<string>(), false);
//			return resultDict.Keys.ToList();
//		}
//
//		/**
//			Perform Run on all serialized nodes respect to graph structure.
//			@result returns ordered connectionIds
//		*/
//		private static List<string> PerformForNode (
//			string endNodeId, 
//			List<NodeData> allNodes, 
//			List<ConnectionData> connections, 
//			Dictionary<string, Dictionary<string, List<Asset>>> resultDict,
//			Dictionary<string, List<string>> cacheDict,
//			bool isRun,
//			Action<string, float> updateHandler=null
//		) {
//			DoNodeOperation(endNodeId, allNodes, connections, resultDict, cacheDict, new List<string>(), isRun, updateHandler);
//			return resultDict.Keys.ToList();
//		}

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
			List<ConnectionPointData> performedPoints,
			bool isActualRun,
			Action<NodeData, float> updateHandler=null
		) {
			if (currentInputPoint != null && performedPoints.Contains(currentInputPoint)) {
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
						DoNodeOperation(target, parentNode, parentInputPoint, c, saveData, resultDict, cachedDict, performedPoints, isActualRun, updateHandler);
					}
				} 
				// if parent does not have input point, call with inputPoint==null
				else {
					DoNodeOperation(target, parentNode, null, c, saveData, resultDict, cachedDict, performedPoints, isActualRun, updateHandler);
				}
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
				INodeOperationBase executor = CreateOperation(saveData, currentNodeData);
				if(executor != null) {
					if(isActualRun) {
						executor.Run(target, currentNodeData, currentInputPoint, connectionToOutput, inputGroupAssets, alreadyCachedPaths, Output);
					}
					else {
						executor.Setup(target, currentNodeData, currentInputPoint, connectionToOutput, inputGroupAssets, alreadyCachedPaths, Output);
					}
				}

			} catch (NodeException e) {
				AssetBundleGraphEditorWindow.AddNodeException(e);
			}

			// mark this point as performed
			performedPoints.Add(currentInputPoint);

			if (updateHandler != null) {
				updateHandler(currentNodeData, 1f);
			}
		}

		public static INodeOperationBase CreateOperation(SaveData saveData, NodeData currentNodeData) {
			INodeOperationBase executor = null;

			try {
				switch (currentNodeData.Kind) {
				case NodeKind.FILTER_SCRIPT: {
						var scriptClassName = currentNodeData.ScriptClassName;
						executor = SystemDataUtility.CreateNodeOperationInstance<FilterBase>(scriptClassName, currentNodeData);
						break;
					}
				case NodeKind.PREFABBUILDER_SCRIPT: {
						var scriptClassName = currentNodeData.ScriptClassName;
						executor = SystemDataUtility.CreateNodeOperationInstance<PrefabBuilderBase>(scriptClassName, currentNodeData);
						break;
					}
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
						var scriptClassName = currentNodeData.ScriptClassName;
						if (string.IsNullOrEmpty(scriptClassName)) {
							throw new NodeException(currentNodeData.Name + ": Classname is empty. Set valid classname. Configure valid script name from editor.", currentNodeData.Id);
						}
						executor = SystemDataUtility.CreateNodeOperationInstance<PrefabBuilderBase>(scriptClassName, currentNodeData);
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
				AssetBundleGraphEditorWindow.AddNodeException(e);
				//Debug.LogError("error occured:\"" + e.reason + "\", please check information on node.");
				//throw new AssetBundleGraphException(node.Name + ": " + e.reason);
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
				
				case NodeKind.PREFABBUILDER_SCRIPT:
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
		

	}
}
