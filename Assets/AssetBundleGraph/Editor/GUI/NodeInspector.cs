using UnityEngine;
using System.Collections.Generic;

namespace AssetBundleGraph {
    public class NodeInspector : ScriptableObject {
		public Node node;
		public List<string> errors;

		public void UpdateNode (Node node) {
			this.node = node;
		}

		public void UpdateErrors (List<string> errors) {
			this.errors = errors;
		}
	}
}