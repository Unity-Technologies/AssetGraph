using UnityEngine;

using System;
using System.Collections.Generic;

public class SamplePrefabricator_2 : AssetGraph.PrefabricatorBase {
	public override void In (List<AssetGraph.AssetInfo> source, string recommendedPrefabOutputDir) {
		foreach (var s in source) {
			Debug.LogError("SamplePrefabricator_2:" + s);
		}
	}
}