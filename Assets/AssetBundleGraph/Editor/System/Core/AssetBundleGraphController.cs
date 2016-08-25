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
		/**
		 * Execute Setup operations using current graph
		 */
		public static Dictionary<string, Dictionary<string, List<DepreacatedThroughputAsset>>> 
		PerformSetup (Dictionary<string, object> deserializedJsonData) {
			var graphDescription = GraphDescriptionBuilder.BuildGraphDescriptionFromJson(deserializedJsonData);
			
			var terminalNodeIds = graphDescription.terminalNodeIds;
			var allNodes = graphDescription.allNodes;
			var allConnections = graphDescription.allConnections;

			/*
				Validation: node names should not overlapping.
			*/
			{
				var nodeNames = allNodes.Select(node => node.nodeName).ToList();
				var overlappings = nodeNames.GroupBy(x => x)
					.Where(group => 1 < group.Count())
					.Select(group => group.Key)
					.ToList();

				if (overlappings.Any()) {
					throw new AssetBundleGraphException("Duplicate node name found:" + overlappings[0] + " please rename and avoid same name.");
				}
			}

			var resultDict = new Dictionary<string, Dictionary<string, List<Asset>>>();
			var cacheDict  = new Dictionary<string, List<string>>();

			foreach (var terminalNodeId in terminalNodeIds) {
				PerformSetupForNode(terminalNodeId, allNodes, allConnections, resultDict, cacheDict);
			}
			
			return CollectResult(resultDict);
		}

		/**
		 * Execute Run operations using current graph
		 */
		public static Dictionary<string, Dictionary<string, List<DepreacatedThroughputAsset>>> 
		PerformRun (
			Dictionary<string, object> deserializedJsonData, 
			Action<string, float> updateHandler=null
		) {
			IntegratedGUIBundleBuilder.RemoveAllAssetBundleSettings();
			
			var graphDescription = GraphDescriptionBuilder.BuildGraphDescriptionFromJson(deserializedJsonData);
			
			var terminalNodeIds = graphDescription.terminalNodeIds;
			var allNodes = graphDescription.allNodes;
			var allConnections = graphDescription.allConnections;

			var resultDict = new Dictionary<string, Dictionary<string, List<Asset>>>();
			var cacheDict  = new Dictionary<string, List<string>>();

			foreach (var terminalNodeId in terminalNodeIds) {
				PerformRunForNode(terminalNodeId, allNodes, allConnections, resultDict, cacheDict, updateHandler);
			}

			return CollectResult(resultDict);
		}

		/**
		 *  Collect build result: connectionId : < groupName : List<Asset> >
		 */
		private static Dictionary<string, Dictionary<string, List<DepreacatedThroughputAsset>>> 
		CollectResult (Dictionary<string, Dictionary<string, List<Asset>>> sourceConId_Group_Throughput) {

			var result = new Dictionary<string, Dictionary<string, List<DepreacatedThroughputAsset>>>();

			foreach (var connectionId in sourceConId_Group_Throughput.Keys) {
				var connectionGroupDict = sourceConId_Group_Throughput[connectionId];

				var newConnectionGroupDict = new Dictionary<string, List<DepreacatedThroughputAsset>>();
				foreach (var groupKey in connectionGroupDict.Keys) {
					var connectionThroughputList = connectionGroupDict[groupKey];

					var sourcePathList = new List<DepreacatedThroughputAsset>();
					foreach (var assetData in connectionThroughputList) {
						var bundled = assetData.isBundled;

						if (!string.IsNullOrEmpty(assetData.importFrom)) {
							sourcePathList.Add(new DepreacatedThroughputAsset(assetData.importFrom, bundled));
							continue;
						} 

						if (!string.IsNullOrEmpty(assetData.absoluteAssetPath)) {
							var relativeAbsolutePath = assetData.absoluteAssetPath.Replace(FileUtility.ProjectPathWithSlash(), string.Empty);
							sourcePathList.Add(new DepreacatedThroughputAsset(relativeAbsolutePath, bundled));
							continue;
						}

						if (!string.IsNullOrEmpty(assetData.exportTo)) {
							sourcePathList.Add(new DepreacatedThroughputAsset(assetData.exportTo, bundled));
							continue;
						}
					}
					newConnectionGroupDict[groupKey] = sourcePathList;
				}
				result[connectionId] = newConnectionGroupDict;
			}
			return result;
		}

		/**
			Perform Setup on all serialized nodes respect to graph structure.
			@result returns ordered connectionIds
		*/
		private static List<string> PerformSetupForNode (
			string endNodeId, 
			List<NodeData> allNodes, 
			List<ConnectionData> connections, 
			Dictionary<string, Dictionary<string, List<Asset>>> resultDict,
			Dictionary<string, List<string>> cacheDict
		) {
			DoNodeOperation(endNodeId, allNodes, connections, resultDict, cacheDict, new List<string>(), false);
			return resultDict.Keys.ToList();
		}

		/**
			Perform Run on all serialized nodes respect to graph structure.
			@result returns ordered connectionIds
		*/
		private static List<string> PerformRunForNode (
			string endNodeId, 
			List<NodeData> allNodes, 
			List<ConnectionData> connections, 
			Dictionary<string, Dictionary<string, List<Asset>>> resultDict,
			Dictionary<string, List<string>> cacheDict,
			Action<string, float> updateHandler=null
		) {

			DoNodeOperation(endNodeId, allNodes, connections, resultDict, cacheDict, new List<string>(), true, updateHandler);
			return resultDict.Keys.ToList();
		}

		/**
			Perform Run or Setup from parent of given terminal node recursively.
		*/
		private static void DoNodeOperation (
			string nodeId, 
			List<NodeData> allNodes, 			
			List<ConnectionData> allConnections, 
			Dictionary<string, Dictionary<string, List<Asset>>> resultDict, 
			Dictionary<string, List<string>> cachedDict,
			List<string> usedConnectionIds,
			bool isActualRun,
			Action<string, float> updateHandler=null
		) {
			var relatedNodes = allNodes.Where(relation => relation.nodeId == nodeId).ToList();
			if (!relatedNodes.Any()) {
				return;
			}

			var currentNodeData = relatedNodes[0];

			if (currentNodeData.IsAlreadyDone()) {
				return;
			}

			var nodeName = currentNodeData.nodeName;
			var nodeKind = currentNodeData.nodeKind;

			/*
			 * Perform prarent node recursively from this node
			*/
			foreach (var connectionToParent in currentNodeData.connectionToParents) {

				var parentNodeId = connectionToParent.fromNodeId;
				var usedConnectionId = connectionToParent.connectionId;
				if (usedConnectionIds.Contains(usedConnectionId)) {
					throw new NodeException("connection loop detected.", parentNodeId);
				}

				usedConnectionIds.Add(usedConnectionId);

				var parentNode = allNodes.Where(node => node.nodeId == parentNodeId).ToList();
				if (!parentNode.Any()) {				
					return;
				}

				var parentNodeKind = parentNode[0].nodeKind;

				// check node kind order.
				SystemDataValidator.ValidateAssertNodeOrder(parentNodeKind, nodeKind);

				DoNodeOperation(parentNodeId, allNodes, allConnections, resultDict, cachedDict, usedConnectionIds, isActualRun, updateHandler);
			}

			/*
			 * Perform node operation for this node
			*/

			// connections Ids from this node to child nodes. non-ordered.
			// actual running order depends on order of Node's OutputPoint order.
			var nonOrderedConnectionsFromThisNodeToChildNode = allConnections
				.Where(con => con.fromNodeId == nodeId)
				.ToList();

			var orderedNodeOutputPointIds = allNodes.Where(node => node.nodeId == nodeId).SelectMany(node => node.outputPointIds).ToList();

			/*
				get connection ids which is orderd by node's outputPoint-order. 
			*/
			var orderedConnectionIds = new List<string>(nonOrderedConnectionsFromThisNodeToChildNode.Count);
			foreach (var orderedNodeOutputPointId in orderedNodeOutputPointIds) {
				foreach (var nonOrderedConnectionFromThisNodeToChildNode in nonOrderedConnectionsFromThisNodeToChildNode) {
					var nonOrderedConnectionOutputPointId = nonOrderedConnectionFromThisNodeToChildNode.fromNodeOutputPointId;
					if (orderedNodeOutputPointId == nonOrderedConnectionOutputPointId) {
						orderedConnectionIds.Add(nonOrderedConnectionFromThisNodeToChildNode.connectionId);
						continue;
					} 
				} 
			}

			/*
				FilterNode and BundlizerNode uses specific multiple output connections.
				ExportNode does not have output.
				but all other nodes has only one output connection and uses first connection.
			*/
			var firstConnectionIdFromThisNodeToChildNode = string.Empty;
			if (orderedConnectionIds.Any()) firstConnectionIdFromThisNodeToChildNode = orderedConnectionIds[0];

			if (updateHandler != null) updateHandler(nodeId, 0f);

			/*
				has next node, run first time.
			*/

			var alreadyCachedPaths = new List<string>();
			if (cachedDict.ContainsKey(nodeId)) alreadyCachedPaths.AddRange(cachedDict[nodeId]);

			/*
				load already exist cache from node.
			*/
			alreadyCachedPaths.AddRange(GetCachedDataByNodeKind(nodeKind, nodeId));

			var inputParentResults = new Dictionary<string, List<Asset>>();

			var receivingConnectionIds = allConnections
				.Where(con => con.toNodeId == nodeId)
				.Select(con => con.connectionId)
				.ToList();

			foreach (var connecionId in receivingConnectionIds) {
				if (!resultDict.ContainsKey(connecionId)) {
					continue;
				}

				var result = resultDict[connecionId];
				foreach (var groupKey in result.Keys) {
					if (!inputParentResults.ContainsKey(groupKey)) inputParentResults[groupKey] = new List<Asset>();
					inputParentResults[groupKey].AddRange(result[groupKey]);	
				}
			}

			/*
				the Action passes to NodeOperaitons.
				It stores result to resultDict.
			*/
			Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output = 
				(string dataSourceNodeId, string targetConnectionId, Dictionary<string, List<Asset>> result, List<string> justCached) => 
			{
				var targetConnectionIds = allConnections
					.Where(con => con.connectionId == targetConnectionId)
					.Select(con => con.connectionId)
					.ToList();

				if (!targetConnectionIds.Any()) {
					// if next connection does not exist, no results for next.
					// save results to resultDict with this endpoint node's id.
					resultDict[dataSourceNodeId] = new Dictionary<string, List<Asset>>();
					foreach (var groupKey in result.Keys) {
						if (!resultDict[dataSourceNodeId].ContainsKey(groupKey)) {
							resultDict[dataSourceNodeId][groupKey] = new List<Asset>();
						}
						resultDict[dataSourceNodeId][groupKey].AddRange(result[groupKey]);
					}
					return;
				}

				if (!resultDict.ContainsKey(targetConnectionId)) {
					resultDict[targetConnectionId] = new Dictionary<string, List<Asset>>();
				}

				/*
					merge connection result by group key.
				*/
				foreach (var groupKey in result.Keys) {
					if (!resultDict[targetConnectionId].ContainsKey(groupKey)) {
						resultDict[targetConnectionId][groupKey] = new List<Asset>();
					}
					resultDict[targetConnectionId][groupKey].AddRange(result[groupKey]);
				}

				if (isActualRun) {
					if (!cachedDict.ContainsKey(nodeId)) {
						cachedDict[nodeId] = new List<string>();
					}
					cachedDict[nodeId].AddRange(justCached);
				}
			};

			try {
				if (isActualRun) {
					switch (nodeKind) {
					/*
							Scripts
						*/
					case AssetBundleGraphSettings.NodeKind.FILTER_SCRIPT: {
							var scriptClassName = currentNodeData.scriptClassName;
							var executor = SystemDataUtility.CreateNodeOperationInstance<FilterBase>(scriptClassName, nodeId);
							executor.Run(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}
					case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_SCRIPT: {
							var scriptClassName = currentNodeData.scriptClassName;
							var executor = SystemDataUtility.CreateNodeOperationInstance<PrefabricatorBase>(scriptClassName, nodeId);
							executor.Run(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}


						/*
							GUIs
						*/
					case AssetBundleGraphSettings.NodeKind.LOADER_GUI: {
							var currentLoadFilePath = SystemDataUtility.GetCurrentPlatformValue(currentNodeData.loadFilePath);
							var executor = new IntegratedGUILoader(FileUtility.GetPathWithAssetsPath(currentLoadFilePath));
							executor.Run(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

					case AssetBundleGraphSettings.NodeKind.FILTER_GUI: {
							/*
								Filter requires "outputPoint ordered exist connection Id and Fake connection Id" for
								exhausting assets by keyword and type correctly.

								outputPoint which has connection can through assets by keyword and keytype,
								also outputPoint which doesn't have connection should take assets by keyword and keytype.
							*/
							var orderedConnectionIdsAndFakeConnectionIds = new string[orderedNodeOutputPointIds.Count];
							for (var i = 0; i < orderedNodeOutputPointIds.Count; i++) {
								var orderedNodeOutputPointId = orderedNodeOutputPointIds[i];

								foreach (var nonOrderedConnectionFromThisNodeToChildNode in nonOrderedConnectionsFromThisNodeToChildNode) {
									var connectionOutputPointId = nonOrderedConnectionFromThisNodeToChildNode.fromNodeOutputPointId;
									if (orderedNodeOutputPointId == connectionOutputPointId) {
										orderedConnectionIdsAndFakeConnectionIds[i] = nonOrderedConnectionFromThisNodeToChildNode.connectionId;
										break;
									} else {
										orderedConnectionIdsAndFakeConnectionIds[i] = AssetBundleGraphSettings.FILTER_FAKE_CONNECTION_ID;
									}
								}
							}
							var executor = new IntegratedGUIFilter(orderedConnectionIdsAndFakeConnectionIds, currentNodeData.containsKeywords, currentNodeData.containsKeytypes);
							executor.Run(nodeName, nodeId, string.Empty, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

					case AssetBundleGraphSettings.NodeKind.IMPORTSETTING_GUI: {
							var executor = new IntegratedGUIImportSetting();
							executor.Run(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

					case AssetBundleGraphSettings.NodeKind.MODIFIER_GUI: {
							var executor = new IntegratedGUIModifier(SystemDataUtility.GetCurrentPlatformShortName());
							executor.Run(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

					case AssetBundleGraphSettings.NodeKind.GROUPING_GUI: {
							var executor = new IntegratedGUIGrouping(SystemDataUtility.GetCurrentPlatformValue(currentNodeData.groupingKeyword));
							executor.Run(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

					case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_GUI: {
							var scriptClassName = currentNodeData.scriptClassName;
							if (string.IsNullOrEmpty(scriptClassName)) {
								Debug.LogError(nodeName + ": Classname is empty. Set valid classname. Configure valid script name from editor.");
								break;
							}
							var executor = SystemDataUtility.CreateNodeOperationInstance<PrefabricatorBase>(scriptClassName, nodeId);
							executor.Run(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

					case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI: {
							/*
								Bundlizer requires assetOutputConnectionId and additional resourceOutputConnectionId.
								both-connected, or both-not-connected, or one of them is connected. 4 patterns exists.
								
								Bundler Node's outputPoint [0] is always the point for assetOutputConnectionId.
								Bundler Node's outputPoint [1] is always the point for resourceOutputConnectionId.
								
								if one of these outputPoint don't have connection, use Fake connection id for correct output.

								
								unorderedConnectionId \
														----> orderedConnectionIdsAndFakeConnectionIds. 
								orderedOutputPointId  / 
							*/
							var orderedConnectionIdsAndFakeConnectionIds = new string[orderedNodeOutputPointIds.Count];
							for (var i = 0; i < orderedNodeOutputPointIds.Count; i++) {
								var orderedNodeOutputPointId = orderedNodeOutputPointIds[i];

								foreach (var nonOrderedConnectionFromThisNodeToChildNode in nonOrderedConnectionsFromThisNodeToChildNode) {
									var connectionOutputPointId = nonOrderedConnectionFromThisNodeToChildNode.fromNodeOutputPointId;
									if (orderedNodeOutputPointId == connectionOutputPointId) {
										orderedConnectionIdsAndFakeConnectionIds[i] = nonOrderedConnectionFromThisNodeToChildNode.connectionId;
										break;
									} else {
										orderedConnectionIdsAndFakeConnectionIds[i] = AssetBundleGraphSettings.BUNDLIZER_FAKE_CONNECTION_ID;
									}
								}
							}

							var bundleNameTemplate = SystemDataUtility.GetCurrentPlatformValue(currentNodeData.bundleNameTemplate);
							var bundleUseOutputResources = SystemDataUtility.GetCurrentPlatformValue(currentNodeData.bundleUseOutput).ToLower();

							var useOutputResources = false;
							var resourcesOutputConnectionId = AssetBundleGraphSettings.BUNDLIZER_FAKE_CONNECTION_ID;
							switch (bundleUseOutputResources) {
							case "true" :{
									useOutputResources = true;
									resourcesOutputConnectionId = orderedConnectionIdsAndFakeConnectionIds[1];
									break;
								}
							}

							var executor = new IntegratedGUIBundlizer(bundleNameTemplate, orderedConnectionIdsAndFakeConnectionIds[0], useOutputResources, resourcesOutputConnectionId);
							executor.Run(nodeName, nodeId, string.Empty, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

					case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
							var bundleOptions = SystemDataUtility.GetCurrentPlatformValue(currentNodeData.enabledBundleOptions);
							var executor = new IntegratedGUIBundleBuilder(bundleOptions, allNodes.Select(nodeData => nodeData.nodeId).ToList());
							executor.Run(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

					case AssetBundleGraphSettings.NodeKind.EXPORTER_GUI: {
							var exportTo = SystemDataUtility.GetCurrentPlatformValue(currentNodeData.exportFilePath);
							var executor = new IntegratedGUIExporter(FileUtility.GetPathWithProjectPath(exportTo));
							executor.Run(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

					default: {
							Debug.LogError(nodeName + " is defined as unknown kind of node. value:" + nodeKind);
							break;
						}
					}
				} else {
					switch (nodeKind) {
					/*
							Scripts
						*/
					case AssetBundleGraphSettings.NodeKind.FILTER_SCRIPT: {
							var scriptClassName = currentNodeData.scriptClassName;
							var executor = SystemDataUtility.CreateNodeOperationInstance<FilterBase>(scriptClassName, nodeId);
							executor.Setup(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}
					case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_SCRIPT: {
							var scriptClassName = currentNodeData.scriptClassName;
							var executor = SystemDataUtility.CreateNodeOperationInstance<PrefabricatorBase>(scriptClassName, nodeId);
							executor.Setup(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}


						/*
							GUIs
						*/
					case AssetBundleGraphSettings.NodeKind.LOADER_GUI: {
							var currentLoadFilePath = SystemDataUtility.GetCurrentPlatformValue(currentNodeData.loadFilePath);

							var executor = new IntegratedGUILoader(FileUtility.GetPathWithAssetsPath(currentLoadFilePath));
							executor.Setup(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

					case AssetBundleGraphSettings.NodeKind.FILTER_GUI: {
							/*
								Filter requires "outputPoint ordered exist connection Id and Fake connection Id" for
								exhausting assets by keyword and type correctly.

								outputPoint which has connection can through assets by keyword and keytype,
								also outputPoint which doesn't have connection should take assets by keyword and keytype.
							*/
							var orderedConnectionIdsAndFakeConnectionIds = new string[orderedNodeOutputPointIds.Count];
							for (var i = 0; i < orderedNodeOutputPointIds.Count; i++) {
								var orderedNodeOutputPointId = orderedNodeOutputPointIds[i];

								foreach (var nonOrderedConnectionFromThisNodeToChildNode in nonOrderedConnectionsFromThisNodeToChildNode) {
									var connectionOutputPointId = nonOrderedConnectionFromThisNodeToChildNode.fromNodeOutputPointId;
									if (orderedNodeOutputPointId == connectionOutputPointId) {
										orderedConnectionIdsAndFakeConnectionIds[i] = nonOrderedConnectionFromThisNodeToChildNode.connectionId;
										break;
									} else {
										orderedConnectionIdsAndFakeConnectionIds[i] = AssetBundleGraphSettings.FILTER_FAKE_CONNECTION_ID;
									}
								}
							}
							var executor = new IntegratedGUIFilter(orderedConnectionIdsAndFakeConnectionIds, currentNodeData.containsKeywords, currentNodeData.containsKeytypes);
							executor.Setup(nodeName, nodeId, string.Empty, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

					case AssetBundleGraphSettings.NodeKind.IMPORTSETTING_GUI: {
							var executor = new IntegratedGUIImportSetting();
							executor.Setup(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

					case AssetBundleGraphSettings.NodeKind.MODIFIER_GUI: {
							var executor = new IntegratedGUIModifier(SystemDataUtility.GetCurrentPlatformShortName());
							executor.Setup(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

					case AssetBundleGraphSettings.NodeKind.GROUPING_GUI: {
							var executor = new IntegratedGUIGrouping(SystemDataUtility.GetCurrentPlatformValue(currentNodeData.groupingKeyword));
							executor.Setup(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

					case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_GUI: {
							var scriptClassName = currentNodeData.scriptClassName;
							if (string.IsNullOrEmpty(scriptClassName)) {
								AssetBundleGraphEditorWindow.AddNodeException(new NodeException(nodeName + ": Classname is empty. Set valid classname.", nodeId));
								break;
							}
							try {
								var executor = SystemDataUtility.CreateNodeOperationInstance<PrefabricatorBase>(scriptClassName, nodeId);
								executor.Setup(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							} catch (NodeException e) {
								AssetBundleGraphEditorWindow.AddNodeException(e);
								break;
							}
							break;
						}

					case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI: {
							/*
								Bundlizer requires assetOutputConnectionId and additional resourceOutputConnectionId.
								both-connected, or both-not-connected, or one of them is connected. 4 patterns exists.
								
								Bundler Node's outputPoint [0] is always the point for assetOutputConnectionId.
								Bundler Node's outputPoint [1] is always the point for resourceOutputConnectionId.
								
								if one of these outputPoint don't have connection, use Fake connection id for correct output.

								
								unorderedConnectionId \
														----> orderedConnectionIdsAndFakeConnectionIds. 
								orderedOutputPointId  / 
							*/
							var orderedConnectionIdsAndFakeConnectionIds = new string[orderedNodeOutputPointIds.Count];
							for (var i = 0; i < orderedNodeOutputPointIds.Count; i++) {
								var orderedNodeOutputPointId = orderedNodeOutputPointIds[i];

								foreach (var nonOrderedConnectionFromThisNodeToChildNode in nonOrderedConnectionsFromThisNodeToChildNode) {
									var connectionOutputPointId = nonOrderedConnectionFromThisNodeToChildNode.fromNodeOutputPointId;
									if (orderedNodeOutputPointId == connectionOutputPointId) {
										orderedConnectionIdsAndFakeConnectionIds[i] = nonOrderedConnectionFromThisNodeToChildNode.connectionId;
										break;
									} else {
										orderedConnectionIdsAndFakeConnectionIds[i] = AssetBundleGraphSettings.BUNDLIZER_FAKE_CONNECTION_ID;
									}
								}
							}

							var bundleNameTemplate = SystemDataUtility.GetCurrentPlatformValue(currentNodeData.bundleNameTemplate);
							var bundleUseOutputResources = SystemDataUtility.GetCurrentPlatformValue(currentNodeData.bundleUseOutput).ToLower();

							var useOutputResources = false;
							var resourcesOutputConnectionId = AssetBundleGraphSettings.BUNDLIZER_FAKE_CONNECTION_ID;
							switch (bundleUseOutputResources) {
							case "true" :{
									useOutputResources = true;
									resourcesOutputConnectionId = orderedConnectionIdsAndFakeConnectionIds[1];
									break;
								}
							}

							var executor = new IntegratedGUIBundlizer(bundleNameTemplate, orderedConnectionIdsAndFakeConnectionIds[0], useOutputResources, resourcesOutputConnectionId);
							executor.Setup(nodeName, nodeId, string.Empty, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

					case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
							var bundleOptions = SystemDataUtility.GetCurrentPlatformValue(currentNodeData.enabledBundleOptions);
							var executor = new IntegratedGUIBundleBuilder(bundleOptions, allNodes.Select(nodeData => nodeData.nodeId).ToList());
							executor.Setup(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

					case AssetBundleGraphSettings.NodeKind.EXPORTER_GUI: {
							var exportTo = SystemDataUtility.GetCurrentPlatformValue(currentNodeData.exportFilePath);
							var executor = new IntegratedGUIExporter(FileUtility.GetPathWithProjectPath(exportTo));
							executor.Setup(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

					default: {
							Debug.LogError(nodeName + " is defined as unknown kind of node. value:" + nodeKind);
							break;
						}
					}
				}
			} catch (NodeException e) {
				AssetBundleGraphEditorWindow.AddNodeException(e);
				//Debug.LogError("error occured:\"" + e.reason + "\", please check information on node.");
				return;
				//throw new AssetBundleGraphException(nodeName + ": " + e.reason);
			}

			currentNodeData.Done();
			if (updateHandler != null) updateHandler(nodeId, 1f);
		}

		public static List<string> GetCachedDataByNodeKind (AssetBundleGraphSettings.NodeKind nodeKind, string nodeId) {
			var platformPackageKeyCandidate = SystemDataUtility.GetCurrentPlatformKey();

			switch (nodeKind) {
				case AssetBundleGraphSettings.NodeKind.IMPORTSETTING_GUI: {
					// no cache file exists for importSetting.
					return new List<string>();
				}
				case AssetBundleGraphSettings.NodeKind.MODIFIER_GUI: {
					// no cache file exists for modifier.
					return new List<string>();
				}
				
				case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
				case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_GUI: {
					var cachedPathBase = FileUtility.PathCombine(
						AssetBundleGraphSettings.PREFABRICATOR_CACHE_PLACE, 
						nodeId,
						platformPackageKeyCandidate
					);

					// no cache folder, no cache.
					if (!Directory.Exists(cachedPathBase)) {
						// search default platform + package
						cachedPathBase = FileUtility.PathCombine(
							AssetBundleGraphSettings.PREFABRICATOR_CACHE_PLACE, 
							nodeId,
							SystemDataUtility.GetDefaultPlatformKey()
						);

						if (!Directory.Exists(cachedPathBase)) {
							return new List<string>();
						}
					}

					return FileUtility.FilePathsInFolder(cachedPathBase);
				}
				 
				case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI: {
					// do nothing.
					break;
				}

				case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					var cachedPathBase = FileUtility.PathCombine(
						AssetBundleGraphSettings.BUNDLEBUILDER_CACHE_PLACE, 
						nodeId,
						platformPackageKeyCandidate
					);

					// no cache folder, no cache.
					if (!Directory.Exists(cachedPathBase)) {
						// search default platform + package
						cachedPathBase = FileUtility.PathCombine(
							AssetBundleGraphSettings.BUNDLEBUILDER_CACHE_PLACE, 
							nodeId,
							SystemDataUtility.GetDefaultPlatformKey()
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
