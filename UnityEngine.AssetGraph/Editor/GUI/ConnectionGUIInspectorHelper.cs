using UnityEngine;
using System.Collections.Generic;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
	/*
	 * ScriptableObject helper object to let ConnectionGUI edit from Inspector
	 */
	public class ConnectionGUIInspectorHelper : ScriptableObject {
		public ConnectionGUI connectionGUI;
		public Dictionary<string, List<AssetReference>> assetGroups;
        public GroupViewContext groupViewContext = new GroupViewContext ();

		public void UpdateInspector (ConnectionGUI con, Dictionary<string, List<AssetReference>> assetGroups) {
			this.connectionGUI = con;
			this.assetGroups = assetGroups;
            this.name = con.Label;
		}

		public void UpdateAssetGroups(Dictionary<string, List<AssetReference>> assetGroups) {
			this.assetGroups = assetGroups;
        }
    }
}
