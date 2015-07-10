using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;


namespace AssetGraph {
	public class GraphStack {

		public struct EndpointNodeIdsAndRelations {
			public List<string> endpointNodeIds;
			public List<ConnectionData> relations;

			public EndpointNodeIdsAndRelations (List<string> endpointNodeIds, List<ConnectionData> relations) {
				this.endpointNodeIds = endpointNodeIds;
				this.relations = relations;
			}
		}
		

		public List<string> RunStackedGraph (Dictionary<string, object> graphDataDict) {
			var endpointNodeIdsAndRelations = SerializeNodeTree(graphDataDict);
			return RunSerializedTree(endpointNodeIdsAndRelations);
		}
		
		/**
			GUI上に展開されているConnectionsから、接続要素の直列化を行う。
			末尾の数だけ列が作られる。
			列の中身の精査はしない。
				・ループチェックしてない
				・不要なデータも入ってる
		*/
		public EndpointNodeIdsAndRelations SerializeNodeTree (Dictionary<string, object> graphDataDict) {
			Debug.LogError("Endの条件を絞れば、不要な、たとえばExportではないNodeが末尾であれば無視する、とか警告だすとかができるはず。");
			var nodeIds = new List<string>();
			var nodesSource = graphDataDict[AssetGraphSettings.ASSETGRAPH_DATA_NODES] as List<object>;
			var connectionsSource = graphDataDict[AssetGraphSettings.ASSETGRAPH_DATA_CONNECTIONS] as List<object>;

			var nodeDatas = new List<ConnectionData>();

			foreach (var nodeSource in nodesSource) {
				var nodeDict = nodeSource as Dictionary<string, object>;
				var id = nodeDict[AssetGraphSettings.NODE_ID] as string;
				nodeIds.Add(id);

				var kindSource = nodeDict[AssetGraphSettings.NODE_KIND] as string;
				var kind = AssetGraphSettings.NodeKindFromString(kindSource);
				var scriptType = nodeDict[AssetGraphSettings.NODE_CLASSNAME] as string;
				nodeDatas.Add(new ConnectionData(id, kind, scriptType));
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
				var connectionId = connectionDict[AssetGraphSettings.CONNECTION_ID] as string;
				var fromNodeId = connectionDict[AssetGraphSettings.CONNECTION_FROMNODE] as string;
				var toNodeId = connectionDict[AssetGraphSettings.CONNECTION_TONODE] as string;

				// collect parent Ids into child node.
				var targetNodes = nodeDatas.Where(nodeData => nodeData.currentNodeId == toNodeId).ToList();
				foreach (var targetNode in targetNodes) targetNode.AddParentNodeIdAndLabel(fromNodeId, connectionId);
			}
			
			return new EndpointNodeIdsAndRelations(noChildNodeIds, nodeDatas);
		}

		public void RunUpToParent (string nodeId, List<ConnectionData> relations, Dictionary<string, List<string>> resultDict) {
			var currentConnectionDatas = relations.Where(relation => relation.currentNodeId == nodeId).ToList();
			if (!currentConnectionDatas.Any()) throw new Exception("failed to find node from relations. nodeId:" + nodeId);

			var currentConnectionData = currentConnectionDatas[0];

			var parentNodeIdAndLabelDict = currentConnectionData.parentNodeIdAndLabelDict;
			foreach (var parentNodeId in parentNodeIdAndLabelDict.Keys) {
				RunUpToParent(parentNodeId, relations, resultDict);
			}

			Debug.LogError("直前のノードの結果が、connectionのid名で node_con_sourcesDict に溜まってるはず。うわあ。なので、ここで該当するデータを引き出してinputに叩き込む。");
			var nodeScriptTypeStr = currentConnectionData.currentNodeClassStr;
			var nodeScriptInstance = Assembly.GetExecutingAssembly().CreateInstance(nodeScriptTypeStr);

			var nodeKind = currentConnectionData.currentNodeKind;
			var result = new List<string>();

			Action<string, string, List<string>> Output = (string dataSourceNodeId, string label, List<string> source) => {
				// この時点での結果を、トップから巻き込んでる箱に入れることができる！
				// resultDict
			};

			switch (nodeKind) {
				case AssetGraphSettings.NodeKind.SOURCE: {
					Debug.LogError("not yet");
					break;
				}
				case AssetGraphSettings.NodeKind.FILTER: {
					((FilterBase)nodeScriptInstance).Setup(result, Output);
					break;
				}
				case AssetGraphSettings.NodeKind.IMPORTER: {
					Debug.LogError("not yet");
					break;
				}
				case AssetGraphSettings.NodeKind.PREFABRICATOR: {
					Debug.LogError("not yet");
					break;
				}
				case AssetGraphSettings.NodeKind.BUNDLIZER: {
					Debug.LogError("not yet");
					break;
				}
				case AssetGraphSettings.NodeKind.DESTINATION: {
					Debug.LogError("not yet");
					break;
				}
			}
			
			Debug.LogError("リザルトの解消,クリアを行って良い");
			// Debug.LogError("runup nodeId:" + nodeId);このへんでSetup
			// Debug.LogError("Execute of this node:" + nodeId);このへんでRun
		}


		/**
			直列化された要素を実行する
		*/
		public List<string> RunSerializedTree (EndpointNodeIdsAndRelations endpointNodeIdsAndRelations) {
			Debug.LogError("データが入る箱の初期化を行う");

			var endpointNodeIds = endpointNodeIdsAndRelations.endpointNodeIds;
			var relations = endpointNodeIdsAndRelations.relations;


			// run up serialized node tree data from it's end to first.
			foreach (var routeId in endpointNodeIds) {
				var resultDict = new Dictionary<string, List<string>>();
				RunUpToParent(routeId, relations, resultDict);
			}

			Debug.LogError("各ノードに溜まった出力結果のクリアリングも行う。タイミングはこのへんな気がする。");

			Debug.LogError("まだ偽の返答");
			return new List<string>();
		}

		public List<string> Results (string routeId) {
			Debug.LogError("偽の返答");
			return new List<string>();
		}
	}


	public class ConnectionData {
		public readonly string currentNodeId;
		public readonly AssetGraphSettings.NodeKind currentNodeKind;
		public readonly string currentNodeClassStr;
		public Dictionary<string, string> parentNodeIdAndLabelDict = new Dictionary<string, string>();

		public ConnectionData (string currentNodeId, AssetGraphSettings.NodeKind currentNodeKind, string currentNodeClassStr) {
			this.currentNodeId = currentNodeId;
			this.currentNodeKind = currentNodeKind;
			this.currentNodeClassStr = currentNodeClassStr;
		}

		public void AddParentNodeIdAndLabel (string parentNodeId, string connectionLabel) {
			parentNodeIdAndLabelDict[parentNodeId] = connectionLabel;
		}
	}


}