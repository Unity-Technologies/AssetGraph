using UnityEngine;

using System;
using System.Collections.Generic;

namespace AssetBundleGraph {
	public class FinallyBase {
		public virtual void Run (Dictionary<string, Dictionary<string, List<string>>> throughputs, bool isRun) {
			Debug.Log("please generate some class : FinallyBase if you need do finally.");
		}
	}
}