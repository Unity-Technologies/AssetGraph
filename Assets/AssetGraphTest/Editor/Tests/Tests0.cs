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

	public void _0_0_0_SetupLoader () {
		Debug.LogError("not yet");
	}

	public void _0_0_1_RunLoader () {
		Debug.LogError("not yet");
	}

	public void _0_0_SetupFilter () {
		var source = new List<AssetData>{
			new AssetData("A/1st", "A"),
			new AssetData("A/2nd", "A")
		};

		var results = new Dictionary<string, List<AssetData>>();

		var sFilter = new SampleFilter_0();
		Action<string, string, List<AssetData>> Out = (string nodeId, string connectionLabel, List<AssetData> output) => {
			results[connectionLabel] = output;
		};

		sFilter.Setup("ID_0_0_SetupFilter", "CONNECTION_0_0_SetupFilter", source, Out);

		if (results.ContainsKey("SampleFilter_0_LabelOf1st")) {
			var result1 = results["SampleFilter_0_LabelOf1st"];
			if (result1[0].absoluteSourcePath == "A/1st") {
				if (results.ContainsKey("SampleFilter_0_LabelOf2nd")) {
					var resut2 = results["SampleFilter_0_LabelOf2nd"];
					if (resut2[0].absoluteSourcePath == "A/2nd") {
						return;
					}
				}	
			}
		}

		Debug.LogError("failed to split by filter");
	}
	public void _0_1_RunFilter () {
		var source = new List<AssetData>{
			new AssetData("A/1st", "A"),
			new AssetData("A/2nd", "A")
		};

		var results = new Dictionary<string, List<AssetData>>();

		var sFilter = new SampleFilter_0();
		Action<string, string, List<AssetData>> Out = (string nodeId, string connectionLabel, List<AssetData> output) => {
			/*
				テスト用なのでラベルをidentityが担保されてる値かのように使っているが、
				実際にデータを保持する際にはuniqueが保証されているconnectionIdを使用する。
			*/
			results[connectionLabel] = output;
		};

		sFilter.Run("ID_0_1_RunFilter", "CONNECTION_0_1_RunFilter", source, Out);

		if (results.ContainsKey("SampleFilter_0_LabelOf1st")) {
			var result1 = results["SampleFilter_0_LabelOf1st"];
			if (result1[0].absoluteSourcePath == "A/1st") {
				if (results.ContainsKey("SampleFilter_0_LabelOf2nd")) {
					var resut2 = results["SampleFilter_0_LabelOf2nd"];
					if (resut2[0].absoluteSourcePath == "A/2nd") {
						return;
					}
				}	
			}
		}

		Debug.LogError("failed to split by filter");
	}

	public void _0_2_SetupImporter () {
		var definedSourcePath = "/Users/runnershigh/Desktop/AssetGraph/TestResources/";
		var source = new List<AssetData>{
			new AssetData(definedSourcePath + "dummy.png", definedSourcePath),
			new AssetData(definedSourcePath + "model/FBX_Biker.fbx", definedSourcePath)
		};

		var results = new Dictionary<string, List<AssetData>>();

		var sImporter = new SampleImporter_0();
		Action<string, string, List<AssetData>> Out = (string nodeId, string connectionLabel, List<AssetData> output) => {
			results[connectionLabel] = output;
		};

		sImporter.Setup("ID_0_2_SetupImporter", "CONNECTION_0_2_SetupImporter", source, Out);
		Debug.LogError("not yet");

	}
	public void _0_3_RunImporter () {
		var definedSourcePath = "/Users/runnershigh/Desktop/AssetGraph/TestResources/";
		var source = new List<AssetData>{
			new AssetData(definedSourcePath + "dummy.png", definedSourcePath),
			new AssetData(definedSourcePath + "model/FBX_Biker.fbx", definedSourcePath)
		};

		var results = new Dictionary<string, List<AssetData>>();

		var sImporter = new SampleImporter_0();
		Action<string, string, List<AssetData>> Out = (string nodeId, string connectionLabel, List<AssetData> output) => {
			results[connectionLabel] = output;
		};

		sImporter.Run("ID_0_3_RunImporter", "CONNECTION_0_3_RunImporter", source, Out);

		var currentOutputs = results["CONNECTION_0_3_RunImporter"];
		if (currentOutputs.Count == 5) {
			return;
		}

		Debug.LogError("failed to collect importerd resource");
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
		var endpointNodeIdsAndNodeDatas = stack.SerializeNodeTree(graphDict);
		if (endpointNodeIdsAndNodeDatas.endpointNodeIds.Contains("最後のFilter")) {
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
		var endpointNodeIdsAndNodeDatas = stack.SerializeNodeTree(graphDict);

		var endPoint0 = endpointNodeIdsAndNodeDatas.endpointNodeIds[0];
		var nodeDatas = endpointNodeIdsAndNodeDatas.nodeDatas;
		
		var orders = stack.RunSerializedTree(endPoint0, nodeDatas);
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