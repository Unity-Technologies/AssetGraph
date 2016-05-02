using UnityEngine;
using UnityEditor;

using System.Collections.Generic;

namespace AssetBundleGraph {
	public class ConnectionInspector : ScriptableObject {
		public Connection con;
		public Dictionary<string, List<string>> throughputListDict;
		public List<bool> foldouts;

		public void UpdateCon (Connection con, Dictionary<string, List<string>> throughputListDict) {
			this.con = con;
			this.throughputListDict = throughputListDict;

			this.foldouts = new List<bool>();
			for (var i = 0; i < this.throughputListDict.Count; i++) {
				foldouts.Add(true);
			}
		}
	}
}