using UnityEngine;
using System.Collections.Generic;

namespace UnityEngine.AssetBundles.GraphTool {
	/*
	 * ScriptableObject helper object to let NodeGUI edit from Inspector
	 */
    public class NodeGUIInspectorHelper : ScriptableObject {
		public NodeGUI node;
		public List<string> errors = new List<string>();

		public void UpdateNode (NodeGUI node) {
			this.node = node;
		}

		public void UpdateErrors (List<string> errorsSource) {
			this.errors = errorsSource;
		}
	}
}