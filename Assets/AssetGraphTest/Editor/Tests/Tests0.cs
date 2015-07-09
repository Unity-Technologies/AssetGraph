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
		var basePath = Path.Combine(Application.dataPath, "AssetGraphTest/Editor/TestData");
		var graphDataPath = Path.Combine(basePath, "_0_8_SerializeGraph.json");
		
		// load
		var dataStr = string.Empty;
		
		using (var sr = new StreamReader(graphDataPath)) {
			dataStr = sr.ReadToEnd();
		}


		var graphDict = Json.Deserialize(dataStr) as Dictionary<string, object>;
		
		var stack = new GraphStack();
		var serializedNodeTree = stack.SerializeNodeTree(graphDict);
		if (serializedNodeTree.Keys.Contains("最後のFilter")) {
			return;
		}

		Debug.LogError("not yet");
	}

	public void _0_9_RunStackedGraph () {
		var basePath = Path.Combine(Application.dataPath, "AssetGraphTest/Editor/TestData");
		var graphDataPath = Path.Combine(basePath, "_0_9_RunStackedGraph.json");
		
		// load
		var dataStr = string.Empty;
		
		using (var sr = new StreamReader(graphDataPath)) {
			dataStr = sr.ReadToEnd();
		}
		var graphDict = Json.Deserialize(dataStr) as Dictionary<string, object>;
		
		var stack = new GraphStack();
		// stack.RunStackedGraph(graphDict);このコードではなくSetup/Runから呼び出す、、のか？どうやってクラス決定するのがいいんだろう。
		// 直感としては、クラスを文字列からnewする、っていうのが活躍しそう。んでデータの受け渡しにgraphStackを渡すスタイルはこのままで良さそう。

		// 適当なGraphなので完遂してもうーんっていう感じだが。
		// Filterだけだから出力する方向にもっていくのはできると思う。
		Debug.LogError("Filterのコード、Script名からクラスを呼び出す仕掛けを作ろう。");

		// // 終われば、resultsに入ってるはず。ファイルもでるけどそれは後。
		// var results = stack.Results(routeIds[0]);
		// if (results.Any()) {
		// 	Debug.Log("やったぜ！");
		// } else {
		// 	Debug.LogError("no result found");
		// }

		Debug.LogError("not yet");
	}

	public void _0_10_SetupSource () {
		Debug.LogError("not yet");
	}

	public void _0_11_RunSource () {
		Debug.LogError("not yet");
	}

	public void _0_12_SetupDestination () {
		Debug.LogError("not yet");
	}

	public void _0_13_RunDestination () {
		Debug.LogError("not yet");
	}

}