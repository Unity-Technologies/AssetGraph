using UnityEngine;

using System;
using System.Collections.Generic;

namespace AssetBundleGraph {
	public class FinallyBase {
		public virtual void Run (Dictionary<NodeData, Dictionary<string, List<DepreacatedThroughputAsset>>> throughputs, bool isRun) {
			Debug.Log("The Finally class did not have \"Run()\" method implemented. Please implement the method to do post process:" + this);
		}
	}
}