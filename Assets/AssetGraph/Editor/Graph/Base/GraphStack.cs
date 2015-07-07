using UnityEngine;

using System;
using System.Collections.Generic;


namespace AssetGraph {
	public class GraphStack {
		// シリアライズが完了した時のデータを保持しておく。型はまだ未定。Listなのは、末尾のぶんだけルートが存在するため。
		List<SOMETHING> serializedRelations;

		// Setup時に構成されるデータの樹、もちろんRun時に更新される。 Node > Connection > source x n
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

		public void RunStackedGraph (string graphDataPath) {
			// JSON経由で、グラフを起動する。
			var connections = new List<Connection>();
			Serialize(connections);
			RunSerialized();
		}
		
		/**
			GUI上に展開されているConnectionsから、接続要素の直列化を行う。
			末尾の数だけ列が作られる。
		*/
		public void Serialize (List<Connection> connections) {
			Debug.LogError("not yet、まずは末尾を探そう。Outを持っていない、次がないNodeを洗い出す == connectionの中でInにはあるけどOutには無いやつを探すとそれでOK");
			serializedRelations = new List<SOMETHING>();

			Debug.Log("その中で、Sourceまで繋がっている = Lastがsourceなもの、のみを残す。");

			// foreach () {// unserializedなrelationを、末尾の数から直列化する。mergeがある場合も、上から順に要素に仕上げる。うん、いけるな。
			// たとえばA,B,CがDに入っている場合、A,B,Cを関連親ノードとして纏めて保持できると良い。
			// もっと複雑な場合はどうなんだろう。

			// }
		}

		/**
			直列化された要素を実行する
		*/
		public void RunSerialized () {
			Debug.LogError("not yet、relationの末尾から、relationを部分的に渡しながら遡上していく。元のrelationはconnection単位の辞書でいいのか。");
			Debug.LogError("末尾のNodeを上から順に実行する。");
			Debug.LogError("各ノードに溜まった出力結果のクリアリングも行う。");

		}

		public List<string> ConnectionResults (string nodeId, string label) {
			if (!node_con_sourcesDict.ContainsKey(nodeId)) throw new Exception("nodeId:" + nodeId + " not found in:" + node_con_sourcesDict.Keys);
			if (!node_con_sourcesDict[nodeId].ContainsKey(label)) throw new Exception("label:" + label + " not found in:" + node_con_sourcesDict.Keys);

			return node_con_sourcesDict[nodeId][label];
		}
	}
}