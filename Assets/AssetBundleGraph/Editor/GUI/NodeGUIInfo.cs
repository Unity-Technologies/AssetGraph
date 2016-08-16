using UnityEngine;
using System.Collections.Generic;

namespace AssetBundleGraph {
    public class NodeGUIInfo : ScriptableObject {
		public Node node;
		public List<string> errors = new List<string>();

		public void UpdateNode (Node node) {
			this.node = node;
		}

		public void UpdateErrors (List<string> errorsSource) {
			this.errors = errorsSource;
		}
	}
}