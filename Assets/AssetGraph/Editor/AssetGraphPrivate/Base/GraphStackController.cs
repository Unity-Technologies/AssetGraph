using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
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
		
		public Dictionary<string, List<string>> SetupStackedGraph (Dictionary<string, object> graphDataDict) {
			var EndpointNodeIdsAndNodeDatasAndConnectionDatas = SerializeNodeRoute(graphDataDict);
			
			var endpointNodeIds = EndpointNodeIdsAndNodeDatasAndConnectionDatas.endpointNodeIds;
			var nodeDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.nodeDatas;
			var connectionDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.connectionDatas;

			var resultDict = new Dictionary<string, List<InternalAssetData>>();

			foreach (var endNodeId in endpointNodeIds) {
				SetupSerializedRoute(endNodeId, nodeDatas, connectionDatas, resultDict);
			}

			var resultConnectionSourcesDict = new Dictionary<string, List<string>>();

			foreach (var key in resultDict.Keys) {
				var assetDataList = resultDict[key];
				resultConnectionSourcesDict[key] = GetResourcePathList(assetDataList);
			}

			return resultConnectionSourcesDict;
		}

		public Dictionary<string, List<string>> RunStackedGraph (Dictionary<string, object> graphDataDict) {
			var EndpointNodeIdsAndNodeDatasAndConnectionDatas = SerializeNodeRoute(graphDataDict);
			
			var endpointNodeIds = EndpointNodeIdsAndNodeDatasAndConnectionDatas.endpointNodeIds;
			var nodeDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.nodeDatas;
			var connectionDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.connectionDatas;

			var resultDict = new Dictionary<string, List<InternalAssetData>>();

			foreach (var endNodeId in endpointNodeIds) {
				RunSerializedRoute(endNodeId, nodeDatas, connectionDatas, resultDict);
			}

			var resultConnectionSourcesDict = new Dictionary<string, List<string>>();

			foreach (var key in resultDict.Keys) {
				var assetDataList = resultDict[key];
				resultConnectionSourcesDict[key] = GetResourcePathList(assetDataList);
			}

			return resultConnectionSourcesDict;
		}

		private List<string> GetResourcePathList (List<InternalAssetData> assetDatas) {
			var sourcePathList = new List<string>();

			foreach (var assetData in assetDatas) {
				if (assetData.absoluteSourcePath != null) {
					sourcePathList.Add(assetData.absoluteSourcePath);
				} else {
					sourcePathList.Add(assetData.pathUnderConnectionId);
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
		public EndpointNodeIdsAndNodeDatasAndConnectionDatas SerializeNodeRoute (Dictionary<string, object> graphDataDict) {
			Debug.LogWarning("Endの条件を絞れば、不要な、たとえばExportではないNodeが末尾であれば無視する、とか警告だすとかができるはず。");
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
				var kind = AssetGraphSettings.NodeKindFromString(kindSource);
				var scriptType = nodeDict[AssetGraphSettings.NODE_CLASSNAME] as string;

				switch (kind) {
					case AssetGraphSettings.NodeKind.LOADER: {
						var loadFilePath = nodeDict[AssetGraphSettings.LOADERNODE_LOAD_PATH] as string;
						nodeDatas.Add(new NodeData(nodeId, kind, scriptType, loadFilePath));
						break;
					}
					case AssetGraphSettings.NodeKind.EXPORTER: {
						var exportFilePath = nodeDict[AssetGraphSettings.EXPORTERNODE_EXPORT_PATH] as string;
						nodeDatas.Add(new NodeData(nodeId, kind, scriptType, exportFilePath));
						break;
					}
					default: {
						nodeDatas.Add(new NodeData(nodeId, kind, scriptType));
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
				var targetNodes = nodeDatas.Where(nodeData => nodeData.currentNodeId == connection.toNodeId).ToList();
				foreach (var targetNode in targetNodes) targetNode.AddConnectionData(connection);
			}
			
			return new EndpointNodeIdsAndNodeDatasAndConnectionDatas(noChildNodeIds, nodeDatas, connections);
		}

		/**
			setup all serialized nodes in order.
			returns orderd connectionIds
		*/
		public List<string> SetupSerializedRoute (string endNodeId, List<NodeData> nodeDatas, List<ConnectionData> connections, Dictionary<string, List<InternalAssetData>> resultDict) {
			ExecuteParent(endNodeId, nodeDatas, connections, resultDict, false);

			return resultDict.Keys.ToList();
		}

		/**
			run all serialized nodes in order.
			returns orderd connectionIds
		*/
		public List<string> RunSerializedRoute (string endNodeId, List<NodeData> nodeDatas, List<ConnectionData> connections, Dictionary<string, List<InternalAssetData>> resultDict) {
			ExecuteParent(endNodeId, nodeDatas, connections, resultDict, true);

			return resultDict.Keys.ToList();
		}

		/**
			execute Run or Setup for each nodes in order.
		*/
		private void ExecuteParent (string nodeId, List<NodeData> nodeDatas, List<ConnectionData> connectionDatas, Dictionary<string, List<InternalAssetData>> resultDict, bool isActualRun) {
			var currentNodeDatas = nodeDatas.Where(relation => relation.currentNodeId == nodeId).ToList();
			if (!currentNodeDatas.Any()) throw new Exception("failed to find node from relations. nodeId:" + nodeId);

			var currentNodeData = currentNodeDatas[0];

			if (currentNodeData.IsAlreadyDone()) return;

			/*
				run parent nodes of this node.
			*/
			var parentNodeIds = currentNodeData.connectionDataOfParents.Select(conData => conData.fromNodeId).ToList();
			foreach (var parentNodeId in parentNodeIds) {
				ExecuteParent(parentNodeId, nodeDatas, connectionDatas, resultDict, isActualRun);
			}

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


			/*
				has next node, run first time.
			*/

			var classStr = currentNodeData.currentNodeClassStr;
			var nodeKind = currentNodeData.currentNodeKind;
			
			var inputParentResults = new List<InternalAssetData>();
			
			var receivingConnectionIds = connectionDatas
				.Where(con => con.toNodeId == nodeId)
				.Select(con => con.connectionId)
				.ToList();

			foreach (var connecionId in receivingConnectionIds) {
				if (!resultDict.ContainsKey(connecionId)) {
					throw new Exception("failed to detect parentNode's result. searching connectionId:" + connecionId);
				}
				var result = resultDict[connecionId];
				inputParentResults.AddRange(result);
			}

			Action<string, string, List<InternalAssetData>> Output = (string dataSourceNodeId, string connectionLabel, List<InternalAssetData> source) => {
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
				resultDict[targetConnectionId] = source;
			};

			if (isActualRun) {
				switch (nodeKind) {
					case AssetGraphSettings.NodeKind.LOADER: {
						var executor = Executor<IntegratedLoader>(classStr);
						executor.loadFilePath = currentNodeData.loadFilePath;
						executor.Run(nodeId, labelToChild, inputParentResults, Output);
						break;
					}
					case AssetGraphSettings.NodeKind.FILTER: {
						var executor = Executor<FilterBase>(classStr);
						executor.Run(nodeId, labelToChild, inputParentResults, Output);
						break;
					}
					case AssetGraphSettings.NodeKind.IMPORTER: {
						var executor = Executor<ImporterBase>(classStr);
						executor.Run(nodeId, labelToChild, inputParentResults, Output);
						break;
					}
					case AssetGraphSettings.NodeKind.PREFABRICATOR: {
						var executor = Executor<PrefabricatorBase>(classStr);
						executor.Run(nodeId, labelToChild, inputParentResults, Output);
						break;
					}
					case AssetGraphSettings.NodeKind.BUNDLIZER: {
						var executor = Executor<BundlizerBase>(classStr);
						executor.Run(nodeId, labelToChild, inputParentResults, Output);
						break;
					}
					case AssetGraphSettings.NodeKind.EXPORTER: {
						var executor = Executor<IntegratedExporter>(classStr);
						executor.exportFilePath = currentNodeData.exportFilePath;
						executor.Run(nodeId, labelToChild, inputParentResults, Output);
						break;
					}
				}
			} else {
				switch (nodeKind) {
					case AssetGraphSettings.NodeKind.LOADER: {
						var executor = Executor<IntegratedLoader>(classStr);
						executor.loadFilePath = currentNodeData.loadFilePath;
						executor.Setup(nodeId, labelToChild, inputParentResults, Output);
						break;
					}
					case AssetGraphSettings.NodeKind.FILTER: {
						var executor = Executor<FilterBase>(classStr);
						executor.Setup(nodeId, labelToChild, inputParentResults, Output);
						break;
					}
					case AssetGraphSettings.NodeKind.IMPORTER: {
						var executor = Executor<ImporterBase>(classStr);
						executor.Setup(nodeId, labelToChild, inputParentResults, Output);
						break;
					}
					case AssetGraphSettings.NodeKind.PREFABRICATOR: {
						var executor = Executor<PrefabricatorBase>(classStr);
						executor.Setup(nodeId, labelToChild, inputParentResults, Output);
						break;
					}
					case AssetGraphSettings.NodeKind.BUNDLIZER: {
						var executor = Executor<BundlizerBase>(classStr);
						executor.Setup(nodeId, labelToChild, inputParentResults, Output);
						break;
					}
					case AssetGraphSettings.NodeKind.EXPORTER: {
						var executor = Executor<IntegratedExporter>(classStr);
						executor.exportFilePath = currentNodeData.exportFilePath;
						executor.Setup(nodeId, labelToChild, inputParentResults, Output);
						break;
					}
				}
			}

			currentNodeData.Done();
		}

		public T Executor<T> (string classStr) where T : INodeBase {
			var nodeScriptTypeStr = classStr;
			var nodeScriptInstance = Assembly.GetExecutingAssembly().CreateInstance(nodeScriptTypeStr);
			if (nodeScriptInstance == null) throw new Exception("failed to generate class information of class:" + nodeScriptTypeStr);
			return ((T)nodeScriptInstance);
		}
	}


	public class NodeData {
		public readonly string currentNodeId;
		public readonly AssetGraphSettings.NodeKind currentNodeKind;
		public readonly string currentNodeClassStr;
		public List<ConnectionData> connectionDataOfParents = new List<ConnectionData>();

		// for Loader
		public readonly string loadFilePath;

		// for Exporter
		public readonly string exportFilePath;

		private bool done;

		public NodeData (string currentNodeId, AssetGraphSettings.NodeKind currentNodeKind, string currentNodeClassStr, string loaderOrExporterPath=null) {
			this.currentNodeId = currentNodeId;
			this.currentNodeKind = currentNodeKind;

			this.currentNodeClassStr = currentNodeClassStr;
			
			switch (currentNodeKind) {
				case AssetGraphSettings.NodeKind.LOADER: {
					this.loadFilePath = loaderOrExporterPath;
					this.exportFilePath = null;
					break;
				}
				case AssetGraphSettings.NodeKind.EXPORTER: {
					this.loadFilePath = null;
					this.exportFilePath = loaderOrExporterPath;
					break;
				}
				default: {
					this.loadFilePath = null;
					this.exportFilePath = null;
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