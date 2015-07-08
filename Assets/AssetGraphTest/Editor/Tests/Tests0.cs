using UnityEngine;
using UnityEditor;

using AssetGraph;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using MiniJSONForAssetGraph;

using SublimeSocketAsset;

// Test

public partial class Test {

	public void _0_0_SetupFilter () {
		var stack = new GraphStack();
		var source = new List<string>{
			"1st",
			"2nd"
		};

		var sFilter = new SampleFilter();
		sFilter.Setup(source, stack);

		var resultOf1st = stack.ConnectionResults(sFilter.id, "LabelOf1st");
		var resultOf2nd = stack.ConnectionResults(sFilter.id, "LabelOf2nd");

		if (resultOf1st.Contains("1st")) {
			if (resultOf2nd.Contains("2nd")) {
				return;
			}
		}

		Debug.LogError("failed to split by filter");
	}
	public void _0_1_RunFilter () {
		var sFilter = new SampleFilter();
		var relations = new List<SOMETHING>();

		if (!relations.Any()) {
			Debug.LogError("empty relation");
			return;
		}

		sFilter.Run(relations[0]);

		var resut1st = sFilter.results["LabelOf1st"];
		var resut2nd = sFilter.results["LabelOf2nd"];

		if (resut1st.Contains("1st")) {
			if (resut2nd.Contains("2nd")) {
				return;
			}
		}

		Debug.LogError("failed to split by filter");
	}

	public void _0_2_SetupImporter () {
		Debug.LogError("not yet");
	}
	public void _0_3_RunImporter () {
		Debug.LogError("not yet");
	}

	public void _0_4_SetupPrefabricator () {
		Debug.LogError("not yet");
	}
	public void _0_5_RunPrefabricator () {
		Debug.LogError("not yet");
	}

	public void _0_6_SetupBundlizer () {
		Debug.LogError("not yet");
	}
	public void _0_7_RunBundlizer () {
		Debug.LogError("not yet");
	}

	public void _0_8_SerializeGraph () {
		Debug.LogError("not yet");
	}

	public void _0_9_RunStackedGraph () {
		var basePath = Path.Combine(Application.dataPath, "AssetGraphTest/Editor/TestData");
		var graphDataPath = Path.Combine(basePath, "_0_9_RunStackedGraph.json");
		
		
		
		Debug.LogError("not yet");
	}

}