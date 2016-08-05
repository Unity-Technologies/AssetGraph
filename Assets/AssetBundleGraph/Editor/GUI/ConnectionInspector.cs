using UnityEngine;

using System.Collections.Generic;
using System;

namespace AssetBundleGraph {
	public class ConnectionInspector : ScriptableObject {
		public Connection con;
		public Dictionary<string, List<ThroughputAsset>> throughputListDict;
		public List<bool> foldouts;

		public void UpdateCon (Connection con, Dictionary<string, List<ThroughputAsset>> throughputListDict) {
			this.con = con;
			this.throughputListDict = throughputListDict;

			this.foldouts = new List<bool>();
			for (var i = 0; i < this.throughputListDict.Count; i++) {
				foldouts.Add(true);
			}
		}

        public void UpdateThroughputs(Dictionary<string, List<ThroughputAsset>> throughputListDict) {
            this.throughputListDict = throughputListDict;
        }
    }
}