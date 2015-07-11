using UnityEngine;

using System;
using System.Collections.Generic;

namespace AssetGraph {
	public class FilterBase : NodeBase {
		public FilterBase () : base (AssetGraphSettings.NodeKind.FILTER) {}

		public override void Setup (string id, List<string> inputSource, Action<string, string, List<string>> Output) {
			Action<string, List<string>> _PreOutput = (string label, List<string> outputSource) => {
				Output(id, label, outputSource);
			};
			In(inputSource, _PreOutput);
		}

		/**
			GraphStackから実行される、実際の動作時の処理。mergeとsplitで処理が異なるはず。
			こいつはSplit。出力の内容が複数宛になる。
		*/
		public override void Run (string id, List<string> inputSource, Action<string, string, List<string>> Output) {
			Action<string, List<string>> _Out = (string label, List<string> outputSource) => {
				Output(id, label, outputSource);
			};
			In(inputSource, _Out);
		}


		/**
			フィルタに対して自動的に呼ばれる関数。
		*/
		public virtual void In (List<string> source, Action<string, List<string>> Out) {
			Debug.LogError("オーバーライドしてね！っていうエラーを吐こう");
		}
	}
}