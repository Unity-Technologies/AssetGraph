using UnityEngine;
using UnityEditor;
using System.Collections;

using Model=Unity.AssetGraph.DataModel.Version2;

namespace Unity.AssetGraph {
	public class SaveScope : GUI.Scope {

		private NodeGUI node;

		public SaveScope (NodeGUI node) {
			this.node = node;
		}

		protected override void CloseScope () {
			if(node != null) {
				node.ResetErrorStatus();
			}
			NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_SAVE));
		}
	}
}
