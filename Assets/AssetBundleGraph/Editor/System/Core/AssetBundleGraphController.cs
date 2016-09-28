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
//		/**
//		 * Execute Setup operations using current graph
//		 */
//		public static Dictionary<string, Dictionary<string, List<DepreacatedThroughputAsset>>> 
//		PerformSetup (SaveData saveData) {
//			var graphDescription = GraphDescriptionBuilder.BuildGraphDescriptionFromJson(deserializedJsonData);
//			
//			var terminalNodeIds = graphDescription.terminalNodeIds;
//			var allNodes = graphDescription.allNodes;
//			var allConnections = graphDescription.allConnections;
//
//			/*
//				Validation: node names should not overlapping.
//			*/
//			{
//				var nodeNames = allNodes.Select(node => node.nodeName).ToList();
//				var overlappings = nodeNames.GroupBy(x => x)
//					.Where(group => 1 < group.Count())
//					.Select(group => group.Key)
//					.ToList();
//
//				if (overlappings.Any()) {
//					throw new AssetBundleGraphException("Duplicate node name found:" + overlappings[0] + " please rename and avoid same name.");
//				}
//			}
//
//			var resultDict = new Dictionary<string, Dictionary<string, List<Asset>>>();
//			var cacheDict  = new Dictionary<string, List<string>>();
//
//			foreach (var terminalNodeId in terminalNodeIds) {
//				PerformSetupForNode(terminalNodeId, allNodes, allConnections, resultDict, cacheDict);
//			}
//			
//			return CollectResult(resultDict);
//		}

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
			/*
				Validation: node names should not overlapping.
				perform only when setup
			*/
			if(!isRun){
				var nodeNames = saveData.Nodes.Select(node => node.Name).ToList();
				var overlappings = nodeNames.GroupBy(x => x)
					.Where(group => 1 < group.Count())
					.Select(group => group.Key)
					.ToList();

				if (overlappings.Any()) {
					throw new AssetBundleGraphException("Duplicate node name found:" + overlappings[0] + " please rename and avoid same name.");
				}
			} else {
				IntegratedGUIBundleBuilder.RemoveAllAssetBundleSettings();
			}

			var resultDict = new Dictionary<ConnectionData, Dictionary<string, List<Asset>>>();
			var cacheDict  = new Dictionary<NodeData, List<string>>();

			var leaf = saveData.CollectAllLeafNodes();

			foreach (var leafNode in leaf) {
				DoNodeOperation(target, leafNode, null, saveData, resultDict, cacheDict, new List<ConnectionData>(), isRun, updateHandler);
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
			ConnectionData currentConnectionData,
			SaveData saveData,
			Dictionary<ConnectionData, Dictionary<string, List<Asset>>> resultDict, 
			Dictionary<NodeData, List<string>> cachedDict,
			List<ConnectionData> visitedConnections,
			bool isActualRun,
			Action<NodeData, float> updateHandler=null
		) {
			if (currentNodeData.IsNodeOperationPerformed) {
				return;
			}

			/*
			 * Perform prarent node recursively from this node
			*/
			foreach (var c in currentNodeData.ConnectionsToParent) {

				if (visitedConnections.Contains(c)) {
					throw new NodeException("connection loop detected:", c.FromNodeId);
				}
				visitedConnections.Add(c);

				var parentNode = saveData.Nodes.Find(node => node.Id == c.FromNodeId);
				UnityEngine.Assertions.Assert.IsNotNull(parentNode);

				// check if nodes can connect together
				ConnectionData.ValidateConnection(parentNode, currentNodeData);

				DoNodeOperation(target, parentNode, c, saveData, resultDict, cachedDict, visitedConnections, isActualRun, updateHandler);
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

			var inputParentResults = new Dictionary<string, List<Asset>>();

			var connToParents = saveData.Connections.FindAll(con => con.ToNodeId == currentNodeData.Id);
			foreach (var rCon in connToParents) {
				if (!resultDict.ContainsKey(rCon)) {
					continue;
				}

				var result = resultDict[rCon];
				foreach (var groupKey in result.Keys) {
					if (!inputParentResults.ContainsKey(groupKey)) {
						inputParentResults[groupKey] = new List<Asset>();
					}
					inputParentResults[groupKey].AddRange(result[groupKey]);	
				}
			}

			/*
				the Action passes to NodeOperaitons.
				It stores result to resultDict.
			*/
			Action<NodeData, ConnectionData, Dictionary<string, List<Asset>>, List<string>> Output = 
				(NodeData sourceNode, ConnectionData targetConnection, Dictionary<string, List<Asset>> result, List<string> justCached) => 
			{
				// this should not happen
//				var targetConnectionIds = saveData.Connections
//					.Where(con => con.Id == targetConnectionId)
//					.Select(con => con.Id)
//					.ToList();
//
//				if (!targetConnectionIds.Any()) {
//					// if next connection does not exist, no results for next.
//					// save results to resultDict with this endpoint node's id.
//					resultDict[dataSourceNodeId] = new Dictionary<string, List<Asset>>();
//					foreach (var groupKey in result.Keys) {
//						if (!resultDict[dataSourceNodeId].ContainsKey(groupKey)) {
//							resultDict[dataSourceNodeId][groupKey] = new List<Asset>();
//						}
//						resultDict[dataSourceNodeId][groupKey].AddRange(result[groupKey]);
//					}
//					return;
//				}

				if(targetConnection != null ) {
					if (!resultDict.ContainsKey(targetConnection)) {
						resultDict[targetConnection] = new Dictionary<string, List<Asset>>();
					}
					/*
					merge connection result by group key.
					*/
					foreach (var groupKey in result.Keys) {
						if (!resultDict[targetConnection].ContainsKey(groupKey)) {
							resultDict[targetConnection][groupKey] = new List<Asset>();
						}
						resultDict[targetConnection][groupKey].AddRange(result[groupKey]);
					}
				}

				if (isActualRun) {
					if (!cachedDict.ContainsKey(currentNodeData)) {
						cachedDict[currentNodeData] = new List<string>();
					}
					cachedDict[currentNodeData].AddRange(justCached);
				}
			};

			try {
				INodeOperationBase executor = CreateOperation(saveData, currentNodeData);
				if(executor != null) {
					if(isActualRun) {
						executor.Run(target, currentNodeData, currentConnectionData, inputParentResults, alreadyCachedPaths, Output);
					}
					else {
						executor.Setup(target, currentNodeData, currentConnectionData, inputParentResults, alreadyCachedPaths, Output);
					}
				}

			} catch (NodeException e) {
				AssetBundleGraphEditorWindow.AddNodeException(e);
				//Debug.LogError("error occured:\"" + e.reason + "\", please check information on node.");
				return;
				//throw new AssetBundleGraphException(node.Name + ": " + e.reason);
			}

			currentNodeData.IsNodeOperationPerformed = true;
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
				case NodeKind.PREFABRICATOR_SCRIPT: {
						var scriptClassName = currentNodeData.ScriptClassName;
						executor = SystemDataUtility.CreateNodeOperationInstance<PrefabricatorBase>(scriptClassName, currentNodeData);
						break;
					}
				case NodeKind.LOADER_GUI: {
						executor = new IntegratedGUILoader();
						break;
					}
				case NodeKind.FILTER_GUI: {
						/**
								Filter requires "outputPoint ordered exist connection Id and Fake connection Id" for
								exhausting assets by keyword and type correctly.

								outputPoint which has connection can through assets by keyword and keytype,
								also outputPoint which doesn't have connection should take assets by keyword and keytype.
						*/
						var orderedNodeOutputPointIds = saveData.Nodes.Where(node => node.Id == currentNodeData.Id).SelectMany(node => node.OutputPoints).Select(p => p.Id).ToList();
						var nonOrderedConnectionsFromThisNodeToChildNode = saveData.Connections.Where(con => con.FromNodeId == currentNodeData.Id).ToList();
						var orderedConnectionIdsAndFakeConnectionIds = new string[orderedNodeOutputPointIds.Count];

						for (var i = 0; i < orderedNodeOutputPointIds.Count; i++) {
							var orderedNodeOutputPointId = orderedNodeOutputPointIds[i];

							foreach (var nonOrderedConnectionFromThisNodeToChildNode in nonOrderedConnectionsFromThisNodeToChildNode) {
								var connectionOutputPointId = nonOrderedConnectionFromThisNodeToChildNode.FromNodeConnectionPointId;
								if (orderedNodeOutputPointId == connectionOutputPointId) {
									orderedConnectionIdsAndFakeConnectionIds[i] = nonOrderedConnectionFromThisNodeToChildNode.Id;
									break;
								} else {
									orderedConnectionIdsAndFakeConnectionIds[i] = AssetBundleGraphSettings.FILTER_FAKE_CONNECTION_ID;
								}
							}
						}
						executor = new IntegratedGUIFilter(orderedConnectionIdsAndFakeConnectionIds);
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
				case NodeKind.PREFABRICATOR_GUI: {
						var scriptClassName = currentNodeData.ScriptClassName;
						if (string.IsNullOrEmpty(scriptClassName)) {
							throw new NodeException(currentNodeData.Name + ": Classname is empty. Set valid classname. Configure valid script name from editor.", currentNodeData.Id);
						}
						executor = SystemDataUtility.CreateNodeOperationInstance<PrefabricatorBase>(scriptClassName, currentNodeData);
						break;
					}

				case NodeKind.BUNDLIZER_GUI: {
//						/*
//								Bundlizer requires assetOutputConnectionId and additional resourceOutputConnectionId.
//								both-connected, or both-not-connected, or one of them is connected. 4 patterns exists.
//								
//								Bundler Node's outputPoint [0] is always the point for assetOutputConnectionId.
//								Bundler Node's outputPoint [1] is always the point for resourceOutputConnectionId.
//								
//								if one of these outputPoint don't have connection, use Fake connection id for correct output.
//
//								
//								unorderedConnectionId \
//														----> orderedConnectionIdsAndFakeConnectionIds. 
//								orderedOutputPointId  / 
//							*/
//						var orderedNodeOutputPointIds = saveData.Nodes.Where(node => node.Id == currentNodeData.Id).SelectMany(node => node.OutputPoints).Select(p => p.Id).ToList();
//						var nonOrderedConnectionsFromThisNodeToChildNode = saveData.Connections.Where(con => con.FromNodeId == currentNodeData.Id).ToList();
//						var orderedConnectionIdsAndFakeConnectionIds = new string[orderedNodeOutputPointIds.Count];
//						for (var i = 0; i < orderedNodeOutputPointIds.Count; i++) {
//							var orderedNodeOutputPointId = orderedNodeOutputPointIds[i];
//
//							foreach (var nonOrderedConnectionFromThisNodeToChildNode in nonOrderedConnectionsFromThisNodeToChildNode) {
//								var connectionOutputPointId = nonOrderedConnectionFromThisNodeToChildNode.FromNodeConnectionPointId;
//								if (orderedNodeOutputPointId == connectionOutputPointId) {
//									orderedConnectionIdsAndFakeConnectionIds[i] = nonOrderedConnectionFromThisNodeToChildNode.Id;
//									break;
//								} else {
//									orderedConnectionIdsAndFakeConnectionIds[i] = AssetBundleGraphSettings.BUNDLIZER_FAKE_CONNECTION_ID;
//								}
//							}
//						}

						var connections = saveData.Connections.Where(c => c.FromNodeId == currentNodeData.Id).ToList();
						ConnectionData assetOutputConnection = null;
						foreach(var c in connections) {
							if(currentNodeData.OutputPoints.Find(p => p.Id == c.FromNodeConnectionPointId) != null) {
								assetOutputConnection = c;
								break;
							}
						}

						executor = new IntegratedGUIBundlizer(assetOutputConnection);
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
				
				case NodeKind.PREFABRICATOR_SCRIPT:
				case NodeKind.PREFABRICATOR_GUI: {
					var cachedPathBase = FileUtility.PathCombine(
						AssetBundleGraphSettings.PREFABRICATOR_CACHE_PLACE, 
						node.Id,
						SystemDataUtility.GetPathSafeTargetName(t)
					);

					// no cache folder, no cache.
					if (!Directory.Exists(cachedPathBase)) {
						// search default platform + package
						cachedPathBase = FileUtility.PathCombine(
							AssetBundleGraphSettings.PREFABRICATOR_CACHE_PLACE, 
							node.Id,
							SystemDataUtility.GetPathSafeDefaultTargetName()
						);

						if (!Directory.Exists(cachedPathBase)) {
							return new List<string>();
						}
					}

					return FileUtility.FilePathsInFolder(cachedPathBase);
				}
				 
				case NodeKind.BUNDLIZER_GUI: {
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

					return FileUtility.FilePathsInFolder(cachedPathBase);
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
