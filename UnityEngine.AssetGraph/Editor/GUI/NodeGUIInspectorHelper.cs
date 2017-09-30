using UnityEngine;
using System.Collections.Generic;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
	/*
	 * ScriptableObject helper object to let NodeGUI edit from Inspector
	 */
    public class NodeGUIInspectorHelper : ScriptableObject {
		public NodeGUI node;
		public AssetGraphController controller;
		public List<string> errors = new List<string>();

		public void UpdateNode (AssetGraphController c, NodeGUI node) {
			this.controller = c;
			this.node = node;
		}

		public void UpdateErrors (List<string> errorsSource) {
			this.errors = errorsSource;
		}
	}
}