using UnityEngine;

using System;
using System.Collections.Generic;

public class SamplePrefabricator_2 : AssetGraph.PrefabricatorBase {
	public override void In (string groupKey, List<AssetGraph.AssetInfo> source, string recommendedPrefabOutputDir, Func<GameObject, string, string> Prefabricate) {
		foreach (var s in source) {
			Debug.LogError("SamplePrefabricator_2:" + s);
		}
	}
}