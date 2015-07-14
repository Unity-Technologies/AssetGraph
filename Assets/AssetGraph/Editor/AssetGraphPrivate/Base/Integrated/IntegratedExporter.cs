using UnityEngine;

using System;
using System.Collections.Generic;

namespace AssetGraph {
	public class IntegratedExporter : INodeBase {
		public string exportFilePath;
		
		public void Setup (string nodeId, string labelToNext, List<AssetData> inputSource, Action<string, string, List<AssetData>> Output) {
			Debug.LogError("Exporter setup");
		}
		
		public void Run (string nodeId, string labelToNext, List<AssetData> inputSource, Action<string, string, List<AssetData>> Output) {
			Debug.LogError("Exporter run");
		}
	}
}