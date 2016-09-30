using UnityEngine;

using System;
using System.Collections.Generic;

namespace AssetBundleGraph {
	public interface IPostprocess {
		void Run (Dictionary<NodeData, Dictionary<string, List<Asset>>> assetGroups, bool isRun);
	}
}
