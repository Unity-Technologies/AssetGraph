using UnityEngine;

using System;
using System.Collections.Generic;

namespace AssetBundleGraph {
	public class PostprocessBase {
		public virtual void Run (Dictionary<NodeData, Dictionary<string, List<DepreacatedThroughputAsset>>> throughputs, bool isRun) {
			Debug.Log("The Postprocess class did not have \"Run()\" method implemented. Please implement the method to do post process:" + this);
		}
	}
}