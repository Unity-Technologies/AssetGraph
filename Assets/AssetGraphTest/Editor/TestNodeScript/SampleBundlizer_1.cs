using UnityEngine;

using System;
using System.Collections.Generic;

public class SampleBundlizer_1 : AssetGraph.BundlizerBase {
	public override void In (string groupkey, List<AssetGraph.AssetInfo> source, string recommendedBundleOutputDir) {
		foreach (var s in source) {
			Debug.LogError("SampleBundlizer_1:" + s);
		}
	}
}