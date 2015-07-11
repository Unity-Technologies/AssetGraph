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
		var source = new List<string>{
			"1st",
			"2nd"
		};

		var results = new Dictionary<string, List<string>>();

		var sFilter = new SampleFilter();
		Action<string, string, List<string>> Out = (string nodeId, string connectionLabel, List<string> output) => {
			/*
				テスト用なのでラベルをidentityが担保されてる値かのように使っているが、
				実際にデータを保持する際にはuniqueが保証されているconnectionIdを使用する。
			*/
			results[connectionLabel] = output;
		};

		sFilter.Setup("テストで使わないId", source, Out);

		if (results.ContainsKey("LabelOf1st")) {
			var result1 = results["LabelOf1st"];
			if (result1.Contains("1st")) {
				if (results.ContainsKey("LabelOf2nd")) {
					var resut2 = results["LabelOf2nd"];
					if (resut2.Contains("2nd")) {
						return;
					}
				}	
			}
		}

		Debug.LogError("failed to split by filter");
	}
	public void _0_1_RunFilter () {
		var source = new List<string>{
			"1st",
			"2nd"
		};

		var results = new Dictionary<string, List<string>>();

		var sFilter = new SampleFilter();
		Action<string, string, List<string>> Out = (string nodeId, string connectionLabel, List<string> output) => {
			/*
				テスト用なのでラベルをidentityが担保されてる値かのように使っているが、
				実際にデータを保持する際にはuniqueが保証されているconnectionIdを使用する。
			*/
			results[connectionLabel] = output;
		};

		sFilter.Run("テストで使わないId", source, Out);

		if (results.ContainsKey("LabelOf1st")) {
			var result1 = results["LabelOf1st"];
			if (result1.Contains("1st")) {
				if (results.ContainsKey("LabelOf2nd")) {
					var resut2 = results["LabelOf2nd"];
					if (resut2.Contains("2nd")) {
						return;
					}
				}	
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

	public void _0_8_0_SerializeGraph_hasValidEndpoint () {
		var basePath = Path.Combine(Application.dataPath, "AssetGraphTest/Editor/TestData");
		var graphDataPath = Path.Combine(basePath, "_0_8_SerializeGraph.json");
		
		// load
		var dataStr = string.Empty;
		
		using (var sr = new StreamReader(graphDataPath)) {
			dataStr = sr.ReadToEnd();
		}


		var graphDict = Json.Deserialize(dataStr) as Dictionary<string, object>;
		
		var stack = new GraphStack();
		var endpointNodeIdsAndRelations = stack.SerializeNodeTree(graphDict);
		if (endpointNodeIdsAndRelations.endpointNodeIds.Contains("最後のFilter")) {
			return;
		}

		Debug.LogError("not yet");
	}

	public void _0_8_1_SerializeGraph_hasValidOrder () {
		var basePath = Path.Combine(Application.dataPath, "AssetGraphTest/Editor/TestData");
		var graphDataPath = Path.Combine(basePath, "_0_8_SerializeGraph.json");
		
		// load
		var dataStr = string.Empty;
		
		using (var sr = new StreamReader(graphDataPath)) {
			dataStr = sr.ReadToEnd();
		}


		var graphDict = Json.Deserialize(dataStr) as Dictionary<string, object>;
		
		var stack = new GraphStack();
		var endpointNodeIdsAndRelations = stack.SerializeNodeTree(graphDict);

		var endPoint0 = endpointNodeIdsAndRelations.endpointNodeIds[0];
		var relations = endpointNodeIdsAndRelations.relations;
		
		var orders = stack.RunSerializedTree(endPoint0, relations);
		foreach (var orderedNodeId in orders) {
			Debug.LogError("orderedNodeId:" + orderedNodeId);
		}

		if (orders.Count == 0) {
			Debug.LogError("list is empty");
			return;
		}

		if (orders[0] == "原点" &&
			orders[1] == "最初のFilter" &&
			orders[2] == "最後のFilter") {
			return;
		}

		Debug.LogError("failed to validate order");
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
		stack.RunStackedGraph(graphDict);
		// 直感としては、クラスを文字列からnewする、っていうのが活躍しそう。んでデータの受け渡しにgraphStackを渡すスタイルはこのままで良さそう。

		// 適当なGraphなので完遂してもうーんっていう感じだが。
		// Filterだけだから出力する方向にもっていくのはできると思う。
		Debug.LogWarning("Filterのコード、Script名からクラスを呼び出す仕掛けを作ろう。");

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