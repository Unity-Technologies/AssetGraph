using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;


namespace AssetGraph {
	public class GraphStack {

		public struct EndpointNodeIdsAndNodeDatas {
			public List<string> endpointNodeIds;
			public List<NodeData> nodeDatas;

			public EndpointNodeIdsAndNodeDatas (List<string> endpointNodeIds, List<NodeData> nodeDatas) {
				this.endpointNodeIds = endpointNodeIds;
				this.nodeDatas = nodeDatas;
			}
		}
		

		public Dictionary<string, List<string>> RunStackedGraph (Dictionary<string, object> graphDataDict) {
			var endpointNodeIdsAndNodeDatas = SerializeNodeTree(graphDataDict);
			
			var endpointNodeIds = endpointNodeIdsAndNodeDatas.endpointNodeIds;
			var nodeDatas = endpointNodeIdsAndNodeDatas.nodeDatas;

			var routeIdsListDicts = new Dictionary<string, List<string>>();

			foreach (var endNodeId in endpointNodeIds) {
				var orderedResultNodeIds = RunSerializedTree(endNodeId, nodeDatas);
				routeIdsListDicts[endNodeId] = orderedResultNodeIds;
			}

			return routeIdsListDicts;
		}

		private class ConnectionDict {
			public readonly string connectionId;
			public readonly string connectionLabel;
			public readonly string fromNodeId;
			public readonly string toNodeId;

			public ConnectionDict (string connectionId, string connectionLabel, string fromNodeId, string toNodeId) {
				this.connectionId = connectionId;
				this.connectionLabel = connectionLabel;
				this.fromNodeId = fromNodeId;
				this.toNodeId = toNodeId;
			}
		}
		
		/**
			GUI上に展開されているConnectionsから、接続要素の直列化を行う。
			末尾の数だけ列が作られる。
			列の中身の精査はしない。
				・ループチェックしてない
				・不要なデータも入ってる
		*/
		public EndpointNodeIdsAndNodeDatas SerializeNodeTree (Dictionary<string, object> graphDataDict) {
			Debug.LogWarning("Endの条件を絞れば、不要な、たとえばExportではないNodeが末尾であれば無視する、とか警告だすとかができるはず。");
			var nodeIds = new List<string>();
			var nodesSource = graphDataDict[AssetGraphSettings.ASSETGRAPH_DATA_NODES] as List<object>;
			
			var connectionsSource = graphDataDict[AssetGraphSettings.ASSETGRAPH_DATA_CONNECTIONS] as List<object>;
			var connections = new List<ConnectionDict>();
			foreach (var connectionSource in connectionsSource) {
				var connectionDict = connectionSource as Dictionary<string, object>;
				
				var connectionId = connectionDict[AssetGraphSettings.CONNECTION_ID] as string;
				var connectionLabel = connectionDict[AssetGraphSettings.CONNECTION_LABEL] as string;
				var fromNodeId = connectionDict[AssetGraphSettings.CONNECTION_FROMNODE] as string;
				var toNodeId = connectionDict[AssetGraphSettings.CONNECTION_TONODE] as string;
				connections.Add(new ConnectionDict(connectionId, connectionLabel, fromNodeId, toNodeId));
			}

			var nodeDatas = new List<NodeData>();

			foreach (var nodeSource in nodesSource) {
				var nodeDict = nodeSource as Dictionary<string, object>;
				var nodeId = nodeDict[AssetGraphSettings.NODE_ID] as string;
				nodeIds.Add(nodeId);

				var kindSource = nodeDict[AssetGraphSettings.NODE_KIND] as string;
				var kind = AssetGraphSettings.NodeKindFromString(kindSource);
				var scriptType = nodeDict[AssetGraphSettings.NODE_CLASSNAME] as string;

				/*
					get the label of a connection which is connecting from this node to the next node.
					if node's kind is FILTER, it will generate labels by themselves in Setup and Run.
					set single dummy label.
				*/
				var labelToNextCandidates = connections.Where(con => con.fromNodeId == nodeId).Select(con => con.connectionLabel).ToList();
				if (labelToNextCandidates.Any()) {
					var labelToNext = labelToNextCandidates[0];
					if (kind == AssetGraphSettings.NodeKind.FILTER) labelToNext = AssetGraphSettings.DUMMY_IMPORTER_LABELTONEXT;
					nodeDatas.Add(new NodeData(nodeId, labelToNext, kind, scriptType));
				} else {
					nodeDatas.Add(new NodeData(nodeId, null, kind, scriptType));
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
				foreach (var targetNode in targetNodes) targetNode.AddConnectionData(connection.fromNodeId, connection.connectionLabel, connection.connectionId);
			}
			
			return new EndpointNodeIdsAndNodeDatas(noChildNodeIds, nodeDatas);
		}

		/**
			直列化された要素を実行する
		*/
		public List<string> RunSerializedTree (string endNodeId, List<NodeData> nodeDatas) {
			var resultDict = new Dictionary<string, List<AssetData>>();
			RunUpToParent(endNodeId, nodeDatas, resultDict);
			// foreach (var resultKey in resultDict.Keys) {
			// 	Debug.LogError("resultKey:" + resultKey);
			// 	Debug.LogError("val:" + resultDict[resultKey].Count);
			// 	foreach (var src in resultDict[resultKey]) {
			// 		Debug.LogError("src:" + src);
			// 	}
			// }
			Debug.LogWarning("まだ偽の返答");
			return new List<string>();
		}

		private void RunUpToParent (string nodeId, List<NodeData> nodeDatas, Dictionary<string, List<AssetData>> resultDict) {
			var currentNodeDatas = nodeDatas.Where(relation => relation.currentNodeId == nodeId).ToList();
			if (!currentNodeDatas.Any()) throw new Exception("failed to find node from relations. nodeId:" + nodeId);

			var currentNodeData = currentNodeDatas[0];

			var parentNodeIds = currentNodeData.connectionDataOfParents.Select(conData => conData.nodeId).ToList();
			foreach (var parentNodeId in parentNodeIds) {
				RunUpToParent(parentNodeId, nodeDatas, resultDict);
			}

			// var nodeId = currentNodeData.currentNodeId;
			var labelToNext = currentNodeData.labelToNext;
			var classStr = currentNodeData.currentNodeClassStr;
			var nodeKind = currentNodeData.currentNodeKind;
			
			var inputParentResults = new List<AssetData>();


			Action<string, string, List<AssetData>> Output = (string dataSourceNodeId, string connectionLabel, List<AssetData> source) => {
				// たとえばOutが2つあったら、ここ2回まわるわな、
				// あってる。んで、Nodeの特定と、ラベルからコネクションのIDの選定ができるはず。
				// あーできるな、データが出てるってことは誰かの親なので、親のリストとConnectionIDの辞書があるんで、、なんだけど、
				// うーーん、、親が持ってるのを、コネクションのIDだけじゃなくて、コネクションとラベルとその出元のId、っていうふうにしたほうがよさげ。
				// ・ラベルと親のIdからコネクションIdを割り出す
				// ・そのために必要なのは、親のId > ラベル > コネクションId、っていう3つをくくりつけること。
				foreach (var nodeData in nodeDatas) {
					var connectionDataOfParents = nodeData.connectionDataOfParents;
					foreach (var connectionDataOfParent in connectionDataOfParents) {
						if (connectionDataOfParent.nodeId == dataSourceNodeId
							&& connectionDataOfParent.connectionLabel == connectionLabel
						) {
							var connectionId = connectionDataOfParent.connectionId;
							resultDict[connectionId] = source;
						}
					}
				}
			};

			// Debug.LogError("runup nodeId:" + nodeId);このへんでSetup
			// Debug.LogError("Execute of this node:" + nodeId);このへんでRun
			switch (nodeKind) {
				case AssetGraphSettings.NodeKind.LOADER: {
					Execute<IntegratedLoader>(nodeId, labelToNext, classStr, inputParentResults, Output);
					break;
				}
				case AssetGraphSettings.NodeKind.FILTER: {
					Execute<FilterBase>(nodeId, labelToNext, classStr, inputParentResults, Output);
					break;
				}
				case AssetGraphSettings.NodeKind.IMPORTER: {
					Execute<ImporterBase>(nodeId, labelToNext, classStr, inputParentResults, Output);
					break;
				}
				case AssetGraphSettings.NodeKind.PREFABRICATOR: {
					Debug.LogError("not yet applied node kind, Prefabricator");
					break;
				}
				case AssetGraphSettings.NodeKind.BUNDLIZER: {
					Debug.LogError("not yet applied node kind, Bundlizer");
					break;
				}
				case AssetGraphSettings.NodeKind.EXPORTER: {
					Debug.LogError("not yet applied node kind, Exporter");
					break;
				}
			}
		}

		public void Execute<T> (string nodeId, string labelToNext, string classStr, List<AssetData> inputParentResults, Action<string, string, List<AssetData>> Output) where T : INodeBase {
			var nodeScriptTypeStr = classStr;
			var nodeScriptInstance = Assembly.GetExecutingAssembly().CreateInstance(nodeScriptTypeStr);
			if (nodeScriptInstance == null) throw new Exception("failed to generate class information of class:" + nodeScriptTypeStr);

			((T)nodeScriptInstance).Run(nodeId, labelToNext, inputParentResults, Output);
		}
	}


	public class NodeData {
		public readonly string currentNodeId;
		public readonly string labelToNext;
		public readonly AssetGraphSettings.NodeKind currentNodeKind;
		public readonly string currentNodeClassStr;
		public List<ConnectionData> connectionDataOfParents = new List<ConnectionData>();

		public NodeData (string currentNodeId, string labelToNext, AssetGraphSettings.NodeKind currentNodeKind, string currentNodeClassStr) {
			this.currentNodeId = currentNodeId;
			this.labelToNext = labelToNext;
			this.currentNodeKind = currentNodeKind;
			this.currentNodeClassStr = currentNodeClassStr;
		}

		public void AddConnectionData (string parentNodeId, string connectionLabel, string connectionId) {
			connectionDataOfParents.Add(new ConnectionData(parentNodeId, connectionLabel, connectionId));
		}
	}

	public class ConnectionData {
		public readonly string nodeId;
		public readonly string connectionLabel;
		public readonly string connectionId;

		public ConnectionData (string nodeId, string connectionLabel, string connectionId) {
			this.nodeId = nodeId;
			this.connectionLabel = connectionLabel;
			this.connectionId = connectionId;
		}
	}


}