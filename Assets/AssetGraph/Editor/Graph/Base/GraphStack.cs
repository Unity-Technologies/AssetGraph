using UnityEngine;

using System;
using System.Collections.Generic;


namespace AssetGraph {
	public class GraphStack {
		// シリアライズが完了した時のデータを保持しておく。型はまだ未定。Listなのは、末尾のぶんだけルートが存在するため。
		List<SOMETHING> serializedRelation;

		// データの樹
		Dictionary<string, Dictionary<string, List<string>>> sourceDict = new Dictionary<string, Dictionary<string, List<string>>>();
		
		// 保持の樹はラベルの親と名前だけで良さそう。
		Dictionary<NodeBase, List<string>> nodeAndLabelsDict = new Dictionary<NodeBase, List<string>>();


		public void AddOut (NodeBase nodeBase, string label, List<string> source) {

			// アウトプットポイントの列挙
			if (!nodeAndLabelsDict.ContainsKey(nodeBase)) nodeAndLabelsDict[nodeBase] = new List<string>();
			nodeAndLabelsDict[nodeBase].Add(label);

			Debug.LogError("インプットポイントは常に一つ。");

			// 実行時に利用する
			{
				if (!sourceDict.ContainsKey(nodeBase.id)) {
					sourceDict[nodeBase.id] = new Dictionary<string, List<string>>();
				}

				sourceDict[nodeBase.id][label] = source;
			}

		}

		public void RunStackedGraph () {
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
			serializedRelation = new List<SOMETHING>();

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
	}
}