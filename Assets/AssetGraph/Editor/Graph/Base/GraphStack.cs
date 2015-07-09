using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;


namespace AssetGraph {
	public class GraphStack {
		// Setup時に構成されるデータの樹、もちろんRun時に更新される。 Node > Connection > source x n 無くしたい。
		Dictionary<string, Dictionary<string, List<string>>> node_con_sourcesDict = new Dictionary<string, Dictionary<string, List<string>>>();

		/**
			collect Out results per Connection.
		*/
		public void CollectOutput (NodeBase nodeBase, string label, List<string> source) {
			
			if (!node_con_sourcesDict.ContainsKey(nodeBase.id)) {
				node_con_sourcesDict[nodeBase.id] = new Dictionary<string, List<string>>();
			}

			// reject if same label is in the other output.
			if (node_con_sourcesDict[nodeBase.id].ContainsKey(label)) new Exception("same label is already exist:" + label + " in:" + nodeBase + " please use other label.");

			node_con_sourcesDict[nodeBase.id][label] = source;
		}
		

		public List<string> RunStackedGraph (Dictionary<string, object> graphDataDict) {
			var serializedTree = SerializeNodeTree(graphDataDict);
			return RunSerializedTree(serializedTree);
		}
		
		/**
			GUI上に展開されているConnectionsから、接続要素の直列化を行う。
			末尾の数だけ列が作られる。
			列の中身の精査はしない。
				・ループチェックしてない
				・不要なデータも入ってる
		*/
		public Dictionary<string, List<ConnectionData>> SerializeNodeTree (Dictionary<string, object> graphDataDict) {
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
				nodeDatas.Add(new ConnectionData(id, kind));
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
				var targetNodes = nodeDatas.Where(nodeData => nodeData.destNodeId == toNodeId).ToList();
				foreach (var targetNode in targetNodes) targetNode.AddParentNodeIdAndLabel(fromNodeId, connectionId);
			}
			
			// ready endNodeId - ConnectionDatas dictionary.
			var serializedRelationsTree = new Dictionary<string, List<ConnectionData>>();
			foreach (var endNodeId in noChildNodeIds) {
				serializedRelationsTree[endNodeId] = nodeDatas;
			}

			return serializedRelationsTree;
		}

		public void RunUpToParent (string nodeId, List<ConnectionData> relations) {
			var currentConnectionData = relations.Where(relation => relation.destNodeId == nodeId).ToList();
			if (!currentConnectionData.Any()) throw new Exception("failed to find node from relations. nodeId:" + nodeId);

			var parentNodeIdAndLabelDict = currentConnectionData[0].parentNodeIdAndLabelDict;
			foreach (var parentNodeId in parentNodeIdAndLabelDict.Keys) {
				RunUpToParent(parentNodeId, relations);
			}

			Debug.LogError("直前のノードの結果が、connectionのid名でnode_con_sourcesDictに溜まってるはず。うわあ。なので、ここで該当するデータを引き出してinputに叩き込む。");
			// Debug.LogError("runup nodeId:" + nodeId);このへんでSetup
			// Debug.LogError("Execute of this node:" + nodeId);このへんでRun
		}

		/**
			直列化された要素を実行する
		*/
		public List<string> RunSerializedTree (Dictionary<string, List<ConnectionData>> serializedRelationsTree) {
			Debug.LogError("結果の受け渡しに関しては、最終出力の箱をルートごとに持っておく、とかかなあ。だな。このへんグローバルな動作のあとなんでメッチャ気持ちわるい");
			
			// run up serialized node tree data from it's end to first.
			foreach (var routeId in serializedRelationsTree.Keys) {
				RunUpToParent(routeId, serializedRelationsTree[routeId]);
			}

			Debug.LogError("各ノードに溜まった出力結果のクリアリングも行う。タイミングはこのへんな気がする。");

			// clear data.
			serializedRelationsTree.Clear();

			Debug.LogError("まだ偽の返答");
			return new List<string>();
		}

		public List<string> Results (string routeId) {
			Debug.LogError("偽の返答");
			return new List<string>();
		}

		public List<string> ConnectionResults (string nodeId, string label) {
			if (!node_con_sourcesDict.ContainsKey(nodeId)) throw new Exception("nodeId:" + nodeId + " not found in:" + node_con_sourcesDict.Keys);
			if (!node_con_sourcesDict[nodeId].ContainsKey(label)) throw new Exception("label:" + label + " not found in:" + node_con_sourcesDict.Keys);

			return node_con_sourcesDict[nodeId][label];
		}
	}


	public class ConnectionData {
		public readonly string destNodeId;
		public readonly AssetGraphSettings.NodeKind destNodeKind;
		public Dictionary<string, string> parentNodeIdAndLabelDict = new Dictionary<string, string>();

		public ConnectionData (string destNodeId, AssetGraphSettings.NodeKind destNodeKind) {
			this.destNodeId = destNodeId;
			this.destNodeKind = destNodeKind;
		}

		public void AddParentNodeIdAndLabel (string parentNodeId, string connectionLabel) {
			parentNodeIdAndLabelDict[parentNodeId] = connectionLabel;
		}
	}


}