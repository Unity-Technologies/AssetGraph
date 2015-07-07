using System;
using System.Collections.Generic;

namespace AssetGraph {
	/**
		全てのNodeクラスの基礎
	*/
	public class NodeBase {

		public readonly AssetGraphSettings.NodeKind kind;
		public readonly string id;
		
		public GraphStack stack;

		public Dictionary<string, List<string>> results = new Dictionary<string, List<string>>();

		public NodeBase (AssetGraphSettings.NodeKind kind) {
			this.kind = kind;
			this.id = Guid.NewGuid().ToString();
		}

		/**
			起動時に走るメソッド
		*/
		public virtual void Setup (List<string> source, GraphStack stack) {}

		/**
			実行時に走るメソッド
		*/
		public virtual void Run (SOMETHING relation) {}
	}
}