using UnityEngine;
using UnityEditor;

using System.Collections.Generic;

namespace AssetGraph {
	public class ConnectionInspector : ScriptableObject {
		public Connection con;
		public Dictionary<string, List<string>> throughputListDict;
		public void UpdateCon (Connection con, Dictionary<string, List<string>> throughputListDict) {
			this.con = con;
			this.throughputListDict = throughputListDict;
		}
	}
}