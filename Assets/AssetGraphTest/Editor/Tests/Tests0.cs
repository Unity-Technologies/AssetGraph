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

		// stackにいろいろ入ってるはず

		Debug.LogError("not yet");
	}

	public void _0_1_RunFilter () {
		// フィルタだけが入っているグラフを実行してみる。
		Debug.LogError("not yet");
	}
}