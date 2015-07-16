using UnityEngine;
using UnityEditor;

using AssetGraph;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using MiniJSONForAssetGraph;

// 同じFilterの結果を複数のノードが使用する場合、キャッシュが効くかどうか

public partial class Test {
	public void _3_0_OrderWithCache0 () {
		// 根っこあたりにフィルタがあり、4つ又のimportの結果が再度読まれないかどうか
		var basePath = Path.Combine(Application.dataPath, "AssetGraphTest/Editor/TestData");
		var graphDataPath = Path.Combine(basePath, "_3_0_OrderWithCache0.json");
		
		// load
		var dataStr = string.Empty;
		
		using (var sr = new StreamReader(graphDataPath)) {
			dataStr = sr.ReadToEnd();
		}
		var graphDict = Json.Deserialize(dataStr) as Dictionary<string, object>;
		
		var stack = new GraphStack();
		stack.RunStackedGraph(graphDict);
		
		Debug.LogError("not yet");
	}

	public void _3_1_OrderWithCache1 () {
		// 端っこ
		Debug.LogError("not yet");
	}

	public void _3_2_OrderWithCache2 () {
		// 根っこと端っこ
		Debug.LogError("not yet");
	}

	public void _3_3_OrderWithCache3 () {
		// カオス
		Debug.LogError("not yet");
	}

}