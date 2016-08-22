using UnityEngine;
using System.Collections.Generic;

namespace AssetBundleGraph {
	/*
	 * ScriptableObject helper object to let ConnectionGUI edit from Inspector
	 */
	public class ConnectionGUIInspectorHelper : ScriptableObject {
		public ConnectionGUI con;
		public Dictionary<string, List<DepreacatedThroughputAsset>> throughputListDict;
		public List<bool> foldouts;

		public void UpdateCon (ConnectionGUI con, Dictionary<string, List<DepreacatedThroughputAsset>> throughputListDict) {
			this.con = con;
			this.throughputListDict = throughputListDict;

			this.foldouts = new List<bool>();
			for (var i = 0; i < this.throughputListDict.Count; i++) {
				foldouts.Add(true);
			}
		}

        public void UpdateThroughputs(Dictionary<string, List<DepreacatedThroughputAsset>> throughputListDict) {
            this.throughputListDict = throughputListDict;
        }
    }
}
