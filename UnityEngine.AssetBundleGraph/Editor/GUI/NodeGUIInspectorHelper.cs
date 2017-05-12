using UnityEngine;
using System.Collections.Generic;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {
	/*
	 * ScriptableObject helper object to let NodeGUI edit from Inspector
	 */
    public class NodeGUIInspectorHelper : ScriptableObject {
		public NodeGUI node;
		public AssetBundleGraphController controller;
		public List<string> errors = new List<string>();

		public void UpdateNode (AssetBundleGraphController c, NodeGUI node) {
			this.controller = c;
			this.node = node;
		}

		public void UpdateErrors (List<string> errorsSource) {
			this.errors = errorsSource;
		}
	}
}