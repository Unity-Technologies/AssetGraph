using UnityEngine;
using UnityEditor;

namespace AssetBundleGraph {
	public class NodeInspector : ScriptableObject {
		public Node node;

		public void UpdateNode (Node node) {
			this.node = node;
		}
	}
}