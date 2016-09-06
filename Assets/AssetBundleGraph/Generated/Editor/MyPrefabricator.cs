using UnityEngine;
using UnityEditor;

using System;
using System.Collections.Generic;

using AssetBundleGraph;

public class MyPrefabricator : AssetBundleGraph.PrefabricatorBase {
	public override void ValidateCanCreatePrefab (BuildTarget target, NodeData node, string groupKey, List<AssetBundleGraph.DepreacatedAssetInfo> sources, string recommendedPrefabOutputDir, Func<string, string> Prefabricate) {
		// Test and see if Prefab can be created
		// use Prefabricate deledate to create prefab.
	}

	public override void CreatePrefab (BuildTarget target, NodeData node, string groupKey, List<AssetBundleGraph.DepreacatedAssetInfo> source, string recommendedPrefabOutputDir, Func<GameObject, string, bool, string> Prefabricate) {
		// use Prefabricate deledate to create prefab.
	}
}
