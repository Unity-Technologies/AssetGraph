using UnityEngine;
using UnityEditor;

using System.Collections.Generic;

namespace AssetGraph {
	public class ConnectionInspector : ScriptableObject {
		public Connection con;
		public List<string> throughputDataList;
		public void UpdateCon (Connection con, List<string> throughputDataList) {
			this.con = con;
			this.throughputDataList = throughputDataList;
		}
	}
}