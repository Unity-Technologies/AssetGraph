using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;


namespace AssetGraph {
	public class GraphStackController {

		public struct EndpointNodeIdsAndNodeDatasAndConnectionDatas {
			public List<string> endpointNodeIds;
			public List<NodeData> nodeDatas;
			public List<ConnectionData> connectionDatas;

			public EndpointNodeIdsAndNodeDatasAndConnectionDatas (List<string> endpointNodeIds, List<NodeData> nodeDatas, List<ConnectionData> connectionDatas) {
				this.endpointNodeIds = endpointNodeIds;
				this.nodeDatas = nodeDatas;
				this.connectionDatas = connectionDatas;
			}
		}

		public static List<string> GetLabelsFromSetupFilter (string scriptType) {
			var nodeScriptInstance = Assembly.GetExecutingAssembly().CreateInstance(scriptType);
			if (nodeScriptInstance == null) {
				throw new Exception("no class found:" + scriptType);
			}

			var labels = new List<string>();
			Action<string, string, Dictionary<string, List<InternalAssetData>>> Output = (string dataSourceNodeId, string connectionLabel, Dictionary<string, List<InternalAssetData>> source) => {
				labels.Add(connectionLabel);
			};

			((FilterBase)nodeScriptInstance).Setup("GetLabelsFromSetupFilter_dummy_nodeId", string.Empty, 
				new Dictionary<string, List<InternalAssetData>>{
					{"0", new List<InternalAssetData>()}
				},
			Output);
			return labels;

		}

		public static Dictionary<string, object> ValidateStackedGraph (Dictionary<string, object> graphDataDict) {
			var changed = false;


			var nodesSource = graphDataDict[AssetGraphSettings.ASSETGRAPH_DATA_NODES] as List<object>;
			var newNodes = new List<Dictionary<string, object>>();

			/*
				delete undetectable node.
			*/
			foreach (var nodeSource in nodesSource) {
				var nodeDict = nodeSource as Dictionary<string, object>;
				
				var nodeId = nodeDict[AssetGraphSettings.NODE_ID] as string;

				var kindSource = nodeDict[AssetGraphSettings.NODE_KIND] as string;
				var kind = AssetGraphSettings.NodeKindFromString(kindSource);
				
				var nodeName = nodeDict[AssetGraphSettings.NODE_NAME] as string;

				// copy all key and value to new Node data dictionary.
				var newNodeDict = new Dictionary<string, object>();
				foreach (var key in nodeDict.Keys) {
					newNodeDict[key] = nodeDict[key];
				}

				switch (kind) {
					case AssetGraphSettings.NodeKind.LOADER_SCRIPT:
					case AssetGraphSettings.NodeKind.FILTER_SCRIPT:
					case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT:
					case AssetGraphSettings.NodeKind.GROUPING_SCRIPT:
					case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
					case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT:
					case AssetGraphSettings.NodeKind.EXPORTER_SCRIPT: {
						var scriptType = nodeDict[AssetGraphSettings.NODE_SCRIPT_TYPE] as string;
				
						var nodeScriptInstance = Assembly.GetExecutingAssembly().CreateInstance(scriptType);
						
						// warn if no class found.
						if (nodeScriptInstance == null) {
							changed = true;
							Debug.LogWarning("no class found:" + scriptType + " kind:" + kind + ", rebuildfing AssetGraph...");
							continue;
						}

						if (kind == AssetGraphSettings.NodeKind.FILTER_SCRIPT) {
							var outoutLabelsSource = nodeDict[AssetGraphSettings.NODE_OUTPUT_LABELS] as List<object>;
							var outoutLabelsSet = new HashSet<string>();
							foreach (var source in outoutLabelsSource) {
								outoutLabelsSet.Add(source.ToString());
							}

							var latestLabels = new HashSet<string>();
							Action<string, string, Dictionary<string, List<InternalAssetData>>> Output = (string dataSourceNodeId, string connectionLabel, Dictionary<string, List<InternalAssetData>> source) => {
								latestLabels.Add(connectionLabel);
							};

							((FilterBase)nodeScriptInstance).Setup(nodeId, string.Empty, 
								new Dictionary<string, List<InternalAssetData>>{
									{"0", new List<InternalAssetData>()}
								},
							Output);

							if (!outoutLabelsSet.SetEquals(latestLabels)) {
								changed = true;
								newNodeDict[AssetGraphSettings.NODE_OUTPUT_LABELS] = latestLabels.ToList();
							}
						}
						break;
					}

					case AssetGraphSettings.NodeKind.LOADER_GUI:
					case AssetGraphSettings.NodeKind.FILTER_GUI:
					case AssetGraphSettings.NodeKind.IMPORTER_GUI:
					case AssetGraphSettings.NodeKind.GROUPING_GUI:
					case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
						// nothing to do.
						break;
					}

					/*
						GUI node with script.
					*/
					case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI:
					case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
						var scriptType = nodeDict[AssetGraphSettings.NODE_SCRIPT_TYPE] as string;
						if (string.IsNullOrEmpty(scriptType)) {
							Debug.LogWarning("node:" + kind + ", script path is empty, please set prefer script to node:" + nodeName);
							break;
						}

						var nodeScriptInstance = Assembly.GetExecutingAssembly().CreateInstance(scriptType);
						
						// warn if no class found.
						if (nodeScriptInstance == null) Debug.LogWarning("no class found:" + scriptType + ", please set prefer script to node:" + nodeName);
						break;
					}

					default: {
						Debug.LogError("not match kind:" + kind);
						break;
					}
				}

				newNodes.Add(newNodeDict);
			}

			/*
				delete undetectable connection.
					erase no start node connection.
					erase no end node connection.
					erase connection which label does exists in the start node.
			*/
			
			var connectionsSource = graphDataDict[AssetGraphSettings.ASSETGRAPH_DATA_CONNECTIONS] as List<object>;
			var newConnections = new List<Dictionary<string, object>>();
			foreach (var connectionSource in connectionsSource) {
				var connectionDict = connectionSource as Dictionary<string, object>;

				var connectionLabel = connectionDict[AssetGraphSettings.CONNECTION_LABEL] as string;
				var fromNodeId = connectionDict[AssetGraphSettings.CONNECTION_FROMNODE] as string;
				var toNodeId = connectionDict[AssetGraphSettings.CONNECTION_TONODE] as string;
				
				// detect start node.
				var fromNodeCandidates = newNodes.Where(
					node => {
						var nodeId = node[AssetGraphSettings.NODE_ID] as string;
						return nodeId == fromNodeId;
					}
					).ToList();
				if (!fromNodeCandidates.Any()) {
					Debug.LogError("c3");
					changed = true;
					continue;
				}

				// detect end node.
				var toNodeCandidates = newNodes.Where(
					node => {
						var nodeId = node[AssetGraphSettings.NODE_ID] as string;
						return nodeId == toNodeId;
					}
					).ToList();
				if (!toNodeCandidates.Any()) {
					Debug.LogError("c4");
					changed = true;
					continue;
				}

				// this connection has start node & end node.
				// detect connectionLabel.
				var fromNode = fromNodeCandidates[0];
				var connectionLabelsSource = fromNode[AssetGraphSettings.NODE_OUTPUT_LABELS] as List<object>;
				var connectionLabels = new List<string>();
				foreach (var connectionLabelSource in connectionLabelsSource) {
					connectionLabels.Add(connectionLabelSource as string);
				}

				if (!connectionLabels.Contains(connectionLabel)) {
					Debug.LogError("c5");
					changed = true;
					continue;
				}

				newConnections.Add(connectionDict);
			}


			if (changed) {
				Debug.LogError("changed!");
				var validatedResultDict = new Dictionary<string, object>{
					{AssetGraphSettings.ASSETGRAPH_DATA_LASTMODIFIED, DateTime.Now},
					{AssetGraphSettings.ASSETGRAPH_DATA_NODES, newNodes},
					{AssetGraphSettings.ASSETGRAPH_DATA_CONNECTIONS, newConnections}
				};
				return validatedResultDict;
			}

			return graphDataDict;
		}
		
		public static Dictionary<string, List<string>> SetupStackedGraph (Dictionary<string, object> graphDataDict) {
			var EndpointNodeIdsAndNodeDatasAndConnectionDatas = SerializeNodeRoute(graphDataDict);
			
			var endpointNodeIds = EndpointNodeIdsAndNodeDatasAndConnectionDatas.endpointNodeIds;
			var nodeDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.nodeDatas;
			var connectionDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.connectionDatas;

			var resultDict = new Dictionary<string, Dictionary<string, List<InternalAssetData>>>();

			foreach (var endNodeId in endpointNodeIds) {
				SetupSerializedRoute(endNodeId, nodeDatas, connectionDatas, resultDict);
			}

			var resultConnectionSourcesDict = new Dictionary<string, List<string>>();

			foreach (var key in resultDict.Keys) {
				var assetDataDictList = resultDict[key];
				resultConnectionSourcesDict[key] = GetResourcePathList(assetDataDictList);
			}

			return resultConnectionSourcesDict;
		}

		public static Dictionary<string, List<string>> RunStackedGraph (
			Dictionary<string, object> graphDataDict, 
			Action<string, float> updateHandler=null
		) {
			var EndpointNodeIdsAndNodeDatasAndConnectionDatas = SerializeNodeRoute(graphDataDict);
			
			var endpointNodeIds = EndpointNodeIdsAndNodeDatasAndConnectionDatas.endpointNodeIds;
			var nodeDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.nodeDatas;
			var connectionDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.connectionDatas;

			var resultDict = new Dictionary<string, Dictionary<string, List<InternalAssetData>>>();

			foreach (var endNodeId in endpointNodeIds) {
				RunSerializedRoute(endNodeId, nodeDatas, connectionDatas, resultDict, updateHandler);
			}

			var resultConnectionSourcesDict = new Dictionary<string, List<string>>();

			foreach (var key in resultDict.Keys) {
				var assetDataDictList = resultDict[key];
				resultConnectionSourcesDict[key] = GetResourcePathList(assetDataDictList);
			}

			return resultConnectionSourcesDict;
		}

		private static List<string> GetResourcePathList (Dictionary<string, List<InternalAssetData>> assetDataDictList) {
			var sourcePathList = new List<string>();

			foreach (var assetDatas in assetDataDictList.Values) {
				foreach (var assetData in assetDatas) {
					if (assetData.absoluteSourcePath != null) {
						sourcePathList.Add(assetData.absoluteSourcePath);
					} else {
						sourcePathList.Add(assetData.pathUnderConnectionId);
					}
				}
			}
			
			return sourcePathList;
		}
		
		/**
			GUI上に展開されているConnectionsから、接続要素の直列化を行う。
			末尾の数だけ列が作られる。
			列の中身の精査はしない。
				・ループチェックしてない
				・不要なデータも入ってる
		*/
		public static EndpointNodeIdsAndNodeDatasAndConnectionDatas SerializeNodeRoute (Dictionary<string, object> graphDataDict) {
			var nodeIds = new List<string>();
			var nodesSource = graphDataDict[AssetGraphSettings.ASSETGRAPH_DATA_NODES] as List<object>;
			
			var connectionsSource = graphDataDict[AssetGraphSettings.ASSETGRAPH_DATA_CONNECTIONS] as List<object>;
			var connections = new List<ConnectionData>();
			foreach (var connectionSource in connectionsSource) {
				var connectionDict = connectionSource as Dictionary<string, object>;
				
				var connectionId = connectionDict[AssetGraphSettings.CONNECTION_ID] as string;
				var connectionLabel = connectionDict[AssetGraphSettings.CONNECTION_LABEL] as string;
				var fromNodeId = connectionDict[AssetGraphSettings.CONNECTION_FROMNODE] as string;
				var toNodeId = connectionDict[AssetGraphSettings.CONNECTION_TONODE] as string;
				connections.Add(new ConnectionData(connectionId, connectionLabel, fromNodeId, toNodeId));
			}

			var nodeDatas = new List<NodeData>();

			foreach (var nodeSource in nodesSource) {
				var nodeDict = nodeSource as Dictionary<string, object>;
				var nodeId = nodeDict[AssetGraphSettings.NODE_ID] as string;
				nodeIds.Add(nodeId);

				var kindSource = nodeDict[AssetGraphSettings.NODE_KIND] as string;
				var nodeKind = AssetGraphSettings.NodeKindFromString(kindSource);
				
				var nodeName = nodeDict[AssetGraphSettings.NODE_NAME] as string;

				switch (nodeKind) {
					case AssetGraphSettings.NodeKind.LOADER_GUI:
					case AssetGraphSettings.NodeKind.LOADER_SCRIPT: {
						var loadFilePath = nodeDict[AssetGraphSettings.LOADERNODE_LOAD_PATH] as string;
						nodeDatas.Add(new NodeData(nodeId, nodeKind, nodeName, null, loadFilePath, null, null, null));
						break;
					}
					case AssetGraphSettings.NodeKind.EXPORTER_GUI:
					case AssetGraphSettings.NodeKind.EXPORTER_SCRIPT: {
						var exportFilePath = nodeDict[AssetGraphSettings.EXPORTERNODE_EXPORT_PATH] as string;
						nodeDatas.Add(new NodeData(nodeId, nodeKind, nodeName, null, null, exportFilePath, null, null));
						break;
					}

					case AssetGraphSettings.NodeKind.FILTER_SCRIPT:
					case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT:
					case AssetGraphSettings.NodeKind.GROUPING_SCRIPT:

					case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
					case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI:

					case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT:
					case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
						var scriptType = nodeDict[AssetGraphSettings.NODE_SCRIPT_TYPE] as string;
						nodeDatas.Add(new NodeData(nodeId, nodeKind, nodeName, scriptType, null, null, null, null));
						break;
					}

					case AssetGraphSettings.NodeKind.FILTER_GUI: {
						var containsKeywordsSource = nodeDict[AssetGraphSettings.NODE_FILTER_CONTAINS_KEYWORDS] as List<object>;
						var containsKeywords = new List<string>();
						foreach (var containsKeywordSource in containsKeywordsSource) {
							containsKeywords.Add(containsKeywordSource.ToString());
						}
						nodeDatas.Add(new NodeData(nodeId, nodeKind, nodeName, null, null, null, containsKeywords, null));
						break;
					}

					case AssetGraphSettings.NodeKind.IMPORTER_GUI: {
						nodeDatas.Add(new NodeData(nodeId, nodeKind, nodeName, null, null, null, null, null));
						break;
					}

					case AssetGraphSettings.NodeKind.GROUPING_GUI: {
						var groupingKeyword = nodeDict[AssetGraphSettings.NODE_GROUPING_KEYWORD] as string;
						nodeDatas.Add(new NodeData(nodeId, nodeKind, nodeName, null, null, null, null, groupingKeyword));
						break;
					}

					default: {
						Debug.LogError("failed to match:" + nodeKind);
						break;
					}
				}
			}
			
			/*
				collect node's child. for detecting endpoint of relationship.
			*/
			var nodeIdListWhichHasChild = new List<string>();

			foreach (var connection in connections) {
				nodeIdListWhichHasChild.Add(connection.fromNodeId);
			}
			var noChildNodeIds = nodeIds.Except(nodeIdListWhichHasChild).ToList();

			/*
				adding parentNode id x n into childNode for run up relationship from childNode.
			*/
			foreach (var connection in connections) {
				// collect parent Ids into child node.
				var targetNodes = nodeDatas.Where(nodeData => nodeData.nodeId == connection.toNodeId).ToList();
				foreach (var targetNode in targetNodes) targetNode.AddConnectionData(connection);
			}
			
			return new EndpointNodeIdsAndNodeDatasAndConnectionDatas(noChildNodeIds, nodeDatas, connections);
		}

		/**
			setup all serialized nodes in order.
			returns orderd connectionIds
		*/
		public static List<string> SetupSerializedRoute (
			string endNodeId, 
			List<NodeData> nodeDatas, 
			List<ConnectionData> connections, 
			Dictionary<string, Dictionary<string, List<InternalAssetData>>> resultDict
		) {
			ExecuteParent(endNodeId, nodeDatas, connections, resultDict, false);

			return resultDict.Keys.ToList();
		}

		/**
			run all serialized nodes in order.
			returns orderd connectionIds
		*/
		public static List<string> RunSerializedRoute (
			string endNodeId, 
			List<NodeData> nodeDatas, 
			List<ConnectionData> connections, 
			Dictionary<string, Dictionary<string, List<InternalAssetData>>> resultDict,
			Action<string, float> updateHandler=null
		) {
			ExecuteParent(endNodeId, nodeDatas, connections, resultDict, true, updateHandler);

			return resultDict.Keys.ToList();
		}

		/**
			execute Run or Setup for each nodes in order.
		*/
		private static void ExecuteParent (
			string nodeId, 
			List<NodeData> nodeDatas, 
			List<ConnectionData> connectionDatas, 
			Dictionary<string, Dictionary<string, List<InternalAssetData>>> resultDict, 
			bool isActualRun,
			Action<string, float> updateHandler=null
		) {
			var currentNodeDatas = nodeDatas.Where(relation => relation.nodeId == nodeId).ToList();
			if (!currentNodeDatas.Any()) throw new Exception("failed to find node from relations. nodeId:" + nodeId);

			var currentNodeData = currentNodeDatas[0];

			if (currentNodeData.IsAlreadyDone()) return;

			/*
				run parent nodes of this node.
			*/
			var parentNodeIds = currentNodeData.connectionDataOfParents.Select(conData => conData.fromNodeId).ToList();
			foreach (var parentNodeId in parentNodeIds) {
				ExecuteParent(parentNodeId, nodeDatas, connectionDatas, resultDict, isActualRun, updateHandler);
			}

			/*
				run after parent run.
			*/


			var connectionLabelsFromThisNodeToChildNode = connectionDatas
				.Where(con => con.fromNodeId == nodeId)
				.Select(con => con.connectionLabel)
				.ToList();

			/*
				this is label of connection.

				will be ignored in Filter node,
				because the Filter node will generate new label of connection by itself.
			*/
			var labelToChild = string.Empty;
			if (connectionLabelsFromThisNodeToChildNode.Any()) {
				labelToChild = connectionLabelsFromThisNodeToChildNode[0];
			}

			if (updateHandler != null) updateHandler(nodeId, 0f);

			/*
				has next node, run first time.
			*/
			var nodeName = currentNodeData.nodeName;
			var nodeKind = currentNodeData.nodeKind;
			
			var inputParentResults = new Dictionary<string, List<InternalAssetData>>();
			
			var receivingConnectionIds = connectionDatas
				.Where(con => con.toNodeId == nodeId)
				.Select(con => con.connectionId)
				.ToList();

			foreach (var connecionId in receivingConnectionIds) {
				if (!resultDict.ContainsKey(connecionId)) {
					Debug.LogWarning("failed to detect parentNode's result. searching connectionId:" + connecionId);
					continue;
				}
				var result = resultDict[connecionId];
				foreach (var groupKey in result.Keys) {
					if (!inputParentResults.ContainsKey(groupKey)) inputParentResults[groupKey] = new List<InternalAssetData>();
					inputParentResults[groupKey].AddRange(result[groupKey]);	
				}
			}

			Action<string, string, Dictionary<string, List<InternalAssetData>>> Output = (string dataSourceNodeId, string connectionLabel, Dictionary<string, List<InternalAssetData>> result) => {
				var targetConnectionIds = connectionDatas
					.Where(con => con.fromNodeId == dataSourceNodeId) // from this node
					.Where(con => con.connectionLabel == connectionLabel) // from this label
					.Select(con => con.connectionId)
					.ToList();
				
				if (!targetConnectionIds.Any()) {
					Debug.LogWarning("this dataSourceNodeId:" + dataSourceNodeId + " is endpointint このログの代わりに何か出したいところ。");
					return;
				}
				
				var targetConnectionId = targetConnectionIds[0];
				if (!resultDict.ContainsKey(targetConnectionId)) resultDict[targetConnectionId] = new Dictionary<string, List<InternalAssetData>>();
				
				var connectionResult = resultDict[targetConnectionId];

				foreach (var groupKey in result.Keys) {
					if (!connectionResult.ContainsKey(groupKey)) connectionResult[groupKey] = new List<InternalAssetData>();
					connectionResult[groupKey].AddRange(result[groupKey]);
				}
			};

			if (isActualRun) {
				switch (nodeKind) {
					case AssetGraphSettings.NodeKind.LOADER_SCRIPT: {
						var executor = new IntegratedScriptLoader(currentNodeData.loadFilePath);
						executor.Run(nodeId, labelToChild, inputParentResults, Output);
						break;
					}
					case AssetGraphSettings.NodeKind.FILTER_SCRIPT: {
						var scriptType = currentNodeData.scriptType;
						var executor = Executor<FilterBase>(scriptType);
						executor.Run(nodeId, labelToChild, inputParentResults, Output);
						break;
					}
					case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT: {
						var scriptType = currentNodeData.scriptType;
						var executor = Executor<ImporterBase>(scriptType);
						executor.Run(nodeId, labelToChild, inputParentResults, Output);
						break;
					}
					case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT: {
						var scriptType = currentNodeData.scriptType;
						var executor = Executor<PrefabricatorBase>(scriptType);
						executor.Run(nodeId, labelToChild, inputParentResults, Output);
						break;
					}
					case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT: {
						var scriptType = currentNodeData.scriptType;
						var executor = Executor<BundlizerBase>(scriptType);
						executor.Run(nodeId, labelToChild, inputParentResults, Output);
						break;
					}
					case AssetGraphSettings.NodeKind.EXPORTER_SCRIPT: {
						var executor = new IntegratedScriptExporter(currentNodeData.exportFilePath);
						executor.Run(nodeId, labelToChild, inputParentResults, Output);
						break;
					}

					/*
						GUIs
					*/
					case AssetGraphSettings.NodeKind.LOADER_GUI: {
						var executor = new IntegratedGUILoader(currentNodeData.loadFilePath);
						executor.Run(nodeId, labelToChild, inputParentResults, Output);
						break;
					}

					case AssetGraphSettings.NodeKind.FILTER_GUI: {
						var executor = new IntegreatedGUIFilter(currentNodeData.containsKeywords);
						executor.Run(nodeId, labelToChild, inputParentResults, Output);
						break;
					}

					case AssetGraphSettings.NodeKind.IMPORTER_GUI: {
						var executor = new IntegreatedGUIImporter();
						executor.Run(nodeId, labelToChild, inputParentResults, Output);
						break;
					}

					case AssetGraphSettings.NodeKind.GROUPING_GUI: {
						var executor = new IntegreatedGUIGrouping(currentNodeData.groupingKeyword);
						executor.Run(nodeId, labelToChild, inputParentResults, Output);
						break;
					}

					case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI: {
						var scriptType = currentNodeData.scriptType;
						if (string.IsNullOrEmpty(scriptType)) {
							throw new Exception("failed to detect prefabriator class at node:" + nodeName + ", maybe script are empty or not matched. please set valid script.");
						}
						var executor = Executor<PrefabricatorBase>(scriptType);
						executor.Run(nodeId, labelToChild, inputParentResults, Output);
						break;
					}

					case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
						var scriptType = currentNodeData.scriptType;
						if (string.IsNullOrEmpty(scriptType)) {
							throw new Exception("failed to detect bundlizer class at node:" + nodeName + ", maybe script are empty or not matched. please set valid script.");
						}
						var executor = Executor<BundlizerBase>(scriptType);
						executor.Run(nodeId, labelToChild, inputParentResults, Output);
						break;
					}

					case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
						var executor = new IntegratedGUIExporter(currentNodeData.exportFilePath);
						executor.Run(nodeId, labelToChild, inputParentResults, Output);
						break;
					}

					default: {
						Debug.LogError("kind not found:" + nodeKind);
						break;
					}
				}
			} else {
				switch (nodeKind) {
					/*
						Script version
					*/
					case AssetGraphSettings.NodeKind.LOADER_SCRIPT: {
						var executor = new IntegratedScriptLoader(currentNodeData.loadFilePath);
						executor.Setup(nodeId, labelToChild, inputParentResults, Output);
						break;
					}
					case AssetGraphSettings.NodeKind.FILTER_SCRIPT: {
						var scriptType = currentNodeData.scriptType;
						var executor = Executor<FilterBase>(scriptType);
						executor.Setup(nodeId, labelToChild, inputParentResults, Output);
						break;
					}
					case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT: {
						var scriptType = currentNodeData.scriptType;
						var executor = Executor<ImporterBase>(scriptType);
						executor.Setup(nodeId, labelToChild, inputParentResults, Output);
						break;
					}
					case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT: {
						var scriptType = currentNodeData.scriptType;
						var executor = Executor<PrefabricatorBase>(scriptType);
						executor.Setup(nodeId, labelToChild, inputParentResults, Output);
						break;
					}
					case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT: {
						var scriptType = currentNodeData.scriptType;
						var executor = Executor<BundlizerBase>(scriptType);
						executor.Setup(nodeId, labelToChild, inputParentResults, Output);
						break;
					}
					case AssetGraphSettings.NodeKind.EXPORTER_SCRIPT: {
						var executor = new IntegratedScriptExporter(currentNodeData.exportFilePath);
						executor.Setup(nodeId, labelToChild, inputParentResults, Output);
						break;
					}

					/*
						GUIs
					*/
					case AssetGraphSettings.NodeKind.LOADER_GUI: {
						var executor = new IntegratedGUILoader(currentNodeData.loadFilePath);
						executor.Setup(nodeId, labelToChild, inputParentResults, Output);
						break;
					}

					case AssetGraphSettings.NodeKind.FILTER_GUI: {
						var executor = new IntegreatedGUIFilter(currentNodeData.containsKeywords);
						executor.Setup(nodeId, labelToChild, inputParentResults, Output);
						break;
					}

					case AssetGraphSettings.NodeKind.IMPORTER_GUI: {
						var executor = new IntegreatedGUIImporter();
						executor.Setup(nodeId, labelToChild, inputParentResults, Output);
						break;
					}

					case AssetGraphSettings.NodeKind.GROUPING_GUI: {
						var executor = new IntegreatedGUIGrouping(currentNodeData.groupingKeyword);
						executor.Setup(nodeId, labelToChild, inputParentResults, Output);
						break;
					}

					case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI: {
						var scriptType = currentNodeData.scriptType;
						if (string.IsNullOrEmpty(scriptType)) {
							throw new Exception("failed to detect prefabriator class at node:" + nodeName + ", maybe script are empty or not matched. please set valid script.");
						}
						var executor = Executor<PrefabricatorBase>(scriptType);
						executor.Setup(nodeId, labelToChild, inputParentResults, Output);
						break;
					}

					case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
						var scriptType = currentNodeData.scriptType;
						if (string.IsNullOrEmpty(scriptType)) {
							throw new Exception("failed to detect bundlizer class at node:" + nodeName + ", maybe script are empty or not matched. please set valid script.");
						}
						var executor = Executor<BundlizerBase>(scriptType);
						executor.Setup(nodeId, labelToChild, inputParentResults, Output);
						break;
					}

					case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
						var executor = new IntegratedGUIExporter(currentNodeData.exportFilePath);
						executor.Setup(nodeId, labelToChild, inputParentResults, Output);
						break;
					}

					default: {
						Debug.LogError("kind not found:" + nodeKind);
						break;
					}
				}
			}

			currentNodeData.Done();
			if (updateHandler != null) updateHandler(nodeId, 1f);
		}

		public static T Executor<T> (string typeStr) where T : INodeBase {
			var nodeScriptInstance = Assembly.GetExecutingAssembly().CreateInstance(typeStr);
			if (nodeScriptInstance == null) throw new Exception("failed to generate class information of class:" + typeStr + " which is based on Type:" + typeof(T));
			return ((T)nodeScriptInstance);
		}
	}


	public class NodeData {
		public readonly string nodeName;
		public readonly string nodeId;
		public readonly AssetGraphSettings.NodeKind nodeKind;
		
		public List<ConnectionData> connectionDataOfParents = new List<ConnectionData>();

		// for All script nodes & prefabricator, bundlizer GUI.
		public readonly string scriptType;

		// for Loader Script
		public readonly string loadFilePath;

		// for Exporter Script
		public readonly string exportFilePath;

		// for filter GUI data
		public readonly List<string> containsKeywords;

		// for importer GUI data
		public readonly string groupingKeyword;

		private bool done;

		public NodeData (
			string currentNodeId, 
			AssetGraphSettings.NodeKind currentNodeKind, 
			string nodeName,
			string scriptType,
			string loadPath,
			string exportPath,
			List<string> filterContainsList,
			string groupingKeyword
		) {
			this.nodeId = currentNodeId;
			this.nodeKind = currentNodeKind;
			this.nodeName = nodeName;
			
			switch (currentNodeKind) {
				case AssetGraphSettings.NodeKind.LOADER_SCRIPT:
				case AssetGraphSettings.NodeKind.LOADER_GUI: {
					this.scriptType = null;
					this.loadFilePath = loadPath;
					this.exportFilePath = null;
					this.containsKeywords = null;
					this.groupingKeyword = null;
					break;
				}
				case AssetGraphSettings.NodeKind.EXPORTER_SCRIPT:
				case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
					this.scriptType = null;
					this.loadFilePath = null;
					this.exportFilePath = exportPath;
					this.containsKeywords = null;
					this.groupingKeyword = null;
					break;
				}

				case AssetGraphSettings.NodeKind.FILTER_SCRIPT:
				case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT:

				case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
				case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI:

				case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT:
				case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
					this.scriptType = scriptType;
					this.loadFilePath = null;
					this.exportFilePath = null;
					this.containsKeywords = null;
					this.groupingKeyword = null;
					break;
				}

				case AssetGraphSettings.NodeKind.FILTER_GUI: {
					this.scriptType = null;
					this.loadFilePath = null;
					this.exportFilePath = null;
					this.containsKeywords = filterContainsList;
					this.groupingKeyword = null;
					break;
				}

				case AssetGraphSettings.NodeKind.IMPORTER_GUI: {
					this.scriptType = null;
					this.loadFilePath = null;
					this.exportFilePath = null;
					this.containsKeywords = null;
					this.groupingKeyword = null;
					break;
				}

				case AssetGraphSettings.NodeKind.GROUPING_GUI: {
					this.scriptType = null;
					this.loadFilePath = null;
					this.exportFilePath = null;
					this.containsKeywords = null;
					this.groupingKeyword = groupingKeyword;
					break;
				}

				default: {
					Debug.LogError("failed to match kind:" + nodeKind);
					break;
				}
			}
		}

		public void AddConnectionData (ConnectionData connection) {
			connectionDataOfParents.Add(new ConnectionData(connection));
		}

		public void Done () {
			done = true;
		}

		public bool IsAlreadyDone () {
			return done;
		}
	}

	public class ConnectionData {
		public readonly string connectionId;
		public readonly string connectionLabel;
		public readonly string fromNodeId;
		public readonly string toNodeId;

		public ConnectionData (string connectionId, string connectionLabel, string fromNodeId, string toNodeId) {
			this.connectionId = connectionId;
			this.connectionLabel = connectionLabel;
			this.fromNodeId = fromNodeId;
			this.toNodeId = toNodeId;
		}

		public ConnectionData (ConnectionData connection) {
			this.connectionId = connection.connectionId;
			this.connectionLabel = connection.connectionLabel;
			this.fromNodeId = connection.fromNodeId;
			this.toNodeId = connection.toNodeId;
		}
	}
}