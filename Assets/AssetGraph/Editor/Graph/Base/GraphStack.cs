using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;


namespace AssetGraph {
	public class GraphStack {

		public struct EndpointNodeIdsAndRelations {
			public List<string> endpointNodeIds;
			public List<NodeData> relations;

			public EndpointNodeIdsAndRelations (List<string> endpointNodeIds, List<NodeData> relations) {
				this.endpointNodeIds = endpointNodeIds;
				this.relations = relations;
			}
		}
		

		public Dictionary<string, List<string>> RunStackedGraph (Dictionary<string, object> graphDataDict) {
			var endpointNodeIdsAndRelations = SerializeNodeTree(graphDataDict);
			
			var endpointNodeIds = endpointNodeIdsAndRelations.endpointNodeIds;
			var relations = endpointNodeIdsAndRelations.relations;

			var routeIdsListDicts = new Dictionary<string, List<string>>();

			foreach (var endNodeId in endpointNodeIds) {
				var orderedResultNodeIds = RunSerializedTree(endNodeId, relations);
				routeIdsListDicts[endNodeId] = orderedResultNodeIds;
			}

			return routeIdsListDicts;
		}
		
		/**
			GUI上に展開されているConnectionsから、接続要素の直列化を行う。
			末尾の数だけ列が作られる。
			列の中身の精査はしない。
				・ループチェックしてない
				・不要なデータも入ってる
		*/
		public EndpointNodeIdsAndRelations SerializeNodeTree (Dictionary<string, object> graphDataDict) {
			Debug.LogWarning("Endの条件を絞れば、不要な、たとえばExportではないNodeが末尾であれば無視する、とか警告だすとかができるはず。");
			var nodeIds = new List<string>();
			var nodesSource = graphDataDict[AssetGraphSettings.ASSETGRAPH_DATA_NODES] as List<object>;
			var connectionsSource = graphDataDict[AssetGraphSettings.ASSETGRAPH_DATA_CONNECTIONS] as List<object>;

			var nodeDatas = new List<NodeData>();

			foreach (var nodeSource in nodesSource) {
				var nodeDict = nodeSource as Dictionary<string, object>;
				var id = nodeDict[AssetGraphSettings.NODE_ID] as string;
				nodeIds.Add(id);

				var kindSource = nodeDict[AssetGraphSettings.NODE_KIND] as string;
				var kind = AssetGraphSettings.NodeKindFromString(kindSource);
				var scriptType = nodeDict[AssetGraphSettings.NODE_CLASSNAME] as string;
				nodeDatas.Add(new NodeData(id, kind, scriptType));
			}

			
			/*
				collect node's child. for detecting endpoint of relationship.
			*/
			var nodeIdListWhichHasChild = new List<string>();

			foreach (var connectionDictSource in connectionsSource) {
				var connectionDict = connectionDictSource as Dictionary<string, object>;
				var fromNodeId = connectionDict[AssetGraphSettings.CONNECTION_FROMNODE] as string;
				
				nodeIdListWhichHasChild.Add(fromNodeId);
			}
			var noChildNodeIds = nodeIds.Except(nodeIdListWhichHasChild).ToList();

			
			/*
				adding parentNode id x n into childNode for run up relationship from childNode.
			*/
			foreach (var connectionSource in connectionsSource) {
				var connectionDict = connectionSource as Dictionary<string, object>;
				var connectionLabel = connectionDict[AssetGraphSettings.CONNECTION_LABEL] as string;
				var connectionId = connectionDict[AssetGraphSettings.CONNECTION_ID] as string;
				var fromNodeId = connectionDict[AssetGraphSettings.CONNECTION_FROMNODE] as string;
				var toNodeId = connectionDict[AssetGraphSettings.CONNECTION_TONODE] as string;

				// collect parent Ids into child node.
				var targetNodes = nodeDatas.Where(nodeData => nodeData.currentNodeId == toNodeId).ToList();
				foreach (var targetNode in targetNodes) targetNode.AddConnectionData(fromNodeId, connectionLabel, connectionId);
			}
			
			return new EndpointNodeIdsAndRelations(noChildNodeIds, nodeDatas);
		}

		/**
			直列化された要素を実行する
		*/
		public List<string> RunSerializedTree (string endNodeId, List<NodeData> nodeDatas) {
			var resultDict = new Dictionary<string, List<string>>();
			RunUpToParent(endNodeId, nodeDatas, resultDict);
			foreach (var resultKey in resultDict.Keys) {
				Debug.LogError("resultKey:" + resultKey);
				Debug.LogError("val:" + resultDict[resultKey].Count);
				foreach (var src in resultDict[resultKey]) {
					Debug.LogError("src:" + src);
				}
			}
			Debug.LogWarning("まだ偽の返答");
			return new List<string>();
		}

		private void RunUpToParent (string nodeId, List<NodeData> nodeDatas, Dictionary<string, List<string>> resultDict) {
			var currentNodeDatas = nodeDatas.Where(relation => relation.currentNodeId == nodeId).ToList();
			if (!currentNodeDatas.Any()) throw new Exception("failed to find node from relations. nodeId:" + nodeId);

			var currentNodeData = currentNodeDatas[0];

			var parentNodeIds = currentNodeData.connectionDataOfParents.Select(conData => conData.nodeId).ToList();
			foreach (var parentNodeId in parentNodeIds) {
				RunUpToParent(parentNodeId, nodeDatas, resultDict);
			}

			var id = currentNodeData.currentNodeId;
			var classStr = currentNodeData.currentNodeClassStr;
			var nodeKind = currentNodeData.currentNodeKind;
			
			var inputParentResults = new List<string>();


			Action<string, string, List<string>> Output = (string dataSourceNodeId, string connectionLabel, List<string> source) => {
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
				case AssetGraphSettings.NodeKind.SOURCE: {
					// Debug.LogError("not yet applied node kind, Source");
					break;
				}
				case AssetGraphSettings.NodeKind.FILTER: {
					Execute<FilterBase>(id, classStr, inputParentResults, Output);
					break;
				}
				case AssetGraphSettings.NodeKind.IMPORTER: {
					// Debug.LogError("not yet");
					break;
				}
				case AssetGraphSettings.NodeKind.PREFABRICATOR: {
					// Debug.LogError("not yet");
					break;
				}
				case AssetGraphSettings.NodeKind.BUNDLIZER: {
					// Debug.LogError("not yet");
					break;
				}
				case AssetGraphSettings.NodeKind.DESTINATION: {
					// Debug.LogError("not yet");
					break;
				}
			}
		}

		public void Execute<T> (string id, string classStr, List<string> inputParentResults, Action<string, string, List<string>> Output) where T : NodeBase {
			var nodeScriptTypeStr = classStr;
			var nodeScriptInstance = Assembly.GetExecutingAssembly().CreateInstance(nodeScriptTypeStr);
			if (nodeScriptInstance == null) throw new Exception("failed to generate class information of class:" + nodeScriptTypeStr);

			((T)nodeScriptInstance).Setup(id, inputParentResults, Output);
			Debug.LogWarning("Runが必要");
		}
	}


	public class NodeData {
		public readonly string currentNodeId;
		public readonly AssetGraphSettings.NodeKind currentNodeKind;
		public readonly string currentNodeClassStr;
		public List<ConnectionData> connectionDataOfParents = new List<ConnectionData>();

		public NodeData (string currentNodeId, AssetGraphSettings.NodeKind currentNodeKind, string currentNodeClassStr) {
			this.currentNodeId = currentNodeId;
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