using UnityEngine;
using UnityEditor;

namespace AssetGraph {
	public class NodeInspector : ScriptableObject {
		public Node node;
		public void UpdateNode (Node node) {
			this.node = node;
		}
	}
}