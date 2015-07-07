using UnityEngine;

using System;
using System.Collections.Generic;

namespace AssetGraph {
	public class FilterBase : NodeBase {
		public FilterBase () : base (AssetGraphSettings.NodeKind.FILTER) {}

		public override void Setup (List<string> source, GraphStack stack) {
			this.stack = stack;
			In(source, _DryOut);
		}

		/**
			フィルタに対して自動的に呼ばれる関数。
		*/
		public virtual void In (List<string> source, Action<string, List<string>> Out) {
			Debug.LogError("オーバーライドしてね！っていうエラーを吐こう");
		}

		/**
			GraphStackから実行される、実際の動作時の処理。mergeとsplitで処理が異なる。
		*/
		public override void Run (SOMETHING relation) {
			// run the root nodes of this node. then data will be located in for each results with label.
			var sourceNodes = relation.RootNodesOf(id);

			foreach (var sourceNode in sourceNodes) {
				sourceNode.Run(relation);
				foreach (var label in relation.LabelOfConnectionsFromTo(sourceNode.id, id)) {
					var source = sourceNode.results[label];
					In(source, _Out);
				}
			}
		}



		public void _DryOut (string label, List<string> source) {
			stack.AddOut(this, label, source);
		}

		public void _Out (string label, List<string> source) {
			Debug.LogError("1~複数のOutput箇所について、吐きだす。splitなので、複数のラベルへのデータがまとまるはず。");
			if (!results.ContainsKey(label)) {
				results[label] = new List<string>();
			}

			Debug.Log("重複とか見ないとヤバそう");
			results[label].AddRange(source);
		}
	}
}