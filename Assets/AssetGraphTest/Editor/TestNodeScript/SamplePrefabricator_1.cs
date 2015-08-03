using UnityEngine;

using System;
using System.Collections.Generic;

public class SamplePrefabricator_1 : AssetGraph.PrefabricatorBase {
	public override void In (string groupkey, List<AssetGraph.AssetInfo> source, string recommendedPrefabOutputDir) {
		foreach (var s in source) {
			Debug.LogError("SamplePrefabricator_1:" + s);
		}
	}
}