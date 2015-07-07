using UnityEngine;

using AssetGraph;

using System;
using System.Collections.Generic;

using SublimeSocketAsset;

// Test

public partial class Test {

	/**
		実行方法の一つとして、

			Setup実行
			メソッド呼び出しの収集
		っていう方向と、

			Run実行
			フローの制御

		の二つを見ないといけない。
		ここではFilterのSetup実行を追う。
	*/
	public void _0_0_SetupFilter () {
		var stack = new GraphStack();
		var source = new List<string>{
			"1st",
			"2nd"
		};

		var sFilter = new SampleFilter();
		sFilter.Setup(source, stack);

		var resut1st = sFilter.results["LabelOf1st"];
		var resut2nd = sFilter.results["LabelOf2nd"];

		if (resut1st.Contains("1st")) {
			if (resut2nd.Contains("2nd")) {
				return;
			}
		}

		Debug.LogError("failed to split by filter");
	}

	public void _0_1_RunFilter () {
		// フィルタだけが入っているグラフを実行してみる。
		Debug.LogError("not yet");
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
		Debug.LogError("not yet");
	}

}