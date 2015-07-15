using System;
using System.Collections.Generic;

namespace AssetGraph {
	public class BundlizerBase : INodeBase {
		public void Setup (string nodeId, string labelToNext, List<AssetData> inputSources, Action<string, string, List<AssetData>> Output) {
		}
		public void Run (string nodeId, string labelToNext, List<AssetData> inputSources, Action<string, string, List<AssetData>> Output) {
		}
	}
}