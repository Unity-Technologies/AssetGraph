using UnityEngine;
using System.Collections.Generic;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {
	/*
	 * ScriptableObject helper object to let ConnectionGUI edit from Inspector
	 */
	public class ConnectionGUIInspectorHelper : ScriptableObject {
		public ConnectionGUI connectionGUI;
		public Dictionary<string, List<AssetReference>> assetGroups;
        public GroupViewContext groupViewContext;

		public void UpdateInspector (ConnectionGUI con, Dictionary<string, List<AssetReference>> assetGroups) {
			this.connectionGUI = con;
			this.assetGroups = assetGroups;

            if (groupViewContext == null) {
                groupViewContext = new GroupViewContext ();
            }
		}

		public void UpdateAssetGroups(Dictionary<string, List<AssetReference>> assetGroups) {
			this.assetGroups = assetGroups;
        }
    }
}
