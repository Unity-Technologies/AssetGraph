using UnityEngine;

using System;
using System.Collections.Generic;

public class MyPrefabricator : AssetBundleGraph.PrefabricatorBase {
	public override void Estimate (string groupKey, List<AssetBundleGraph.AssetInfo> source, string recommendedPrefabOutputDir, Func<string, string> Prefabricate) {
		// let's generate Prefab with "Prefabricate" method.
	}
	
	public override void Run (string groupKey, List<AssetBundleGraph.AssetInfo> source, string recommendedPrefabOutputDir, Func<GameObject, string, bool, string> Prefabricate) {
		// let's generate Prefab with "Prefabricate" method.
	}
}