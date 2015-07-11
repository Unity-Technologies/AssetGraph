using System;
using System.Collections.Generic;

namespace AssetGraph {
	/**
		全てのNodeクラスの基礎
	*/
	public class NodeBase {

		public readonly AssetGraphSettings.NodeKind kind;

		public NodeBase (AssetGraphSettings.NodeKind kind) {
			this.kind = kind;
		}

		/**
			起動時に走るメソッド
		*/
		public virtual void Setup (string id, List<string> source, Action<string, string, List<string>> Output) {}

		/**
			実行時に走るメソッド
		*/
		public virtual void Run (string id, List<string> source, Action<string, string, List<string>> Output) {}
	}
}