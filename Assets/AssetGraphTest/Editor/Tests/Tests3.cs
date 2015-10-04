using UnityEngine;
using UnityEditor;

using AssetGraph;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using MiniJSONForAssetGraph;

// 同じFilterの結果を複数のノードが使用する場合、すでに一度終わったファイルを使用できるかどうか

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
		
		var EndpointNodeIdsAndNodeDatasAndConnectionDatas = GraphStackController.SerializeNodeRoute(graphDict);
		
		var endpointNodeIds = EndpointNodeIdsAndNodeDatasAndConnectionDatas.endpointNodeIds;
		var nodeDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.nodeDatas;
		var connectionDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.connectionDatas;

		var resultDict = new Dictionary<string, Dictionary<string, List<InternalAssetData>>>();

		var cacheDict = new Dictionary<string, List<string>>();

		foreach (var endNodeId in endpointNodeIds) {
			GraphStackController.RunSerializedRoute(endNodeId, nodeDatas, connectionDatas, resultDict, cacheDict);
		}

		var connectionIds = resultDict.Keys.ToList();
		
		if (connectionIds.Contains("ロードからフィルタへ")) {
			connectionIds.Remove("ロードからフィルタへ");
		}

		if (!connectionIds.Contains("ロードからフィルタへ")) {
			return;
		}

		Debug.LogError("multiple same connectionId contains.");
	}
}