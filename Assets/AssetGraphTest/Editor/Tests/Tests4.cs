using UnityEngine;
using UnityEditor;

using AssetGraph;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using MiniJSONForAssetGraph;

public partial class Test {
	public void _4_0_RunThenCachedGUI () {
		GraphStackController.CleanCache();
		
		var cacheDict = new Dictionary<string, List<string>>();

		Action act = () => {
			var basePath = Path.Combine(Application.dataPath, "AssetGraphTest/Editor/TestData");
			var graphDataPath = Path.Combine(basePath, "_4_0_RunThenCachedGUI.json");
			
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

			foreach (var endNodeId in endpointNodeIds) {
				GraphStackController.RunSerializedRoute(endNodeId, nodeDatas, connectionDatas, resultDict, cacheDict);
			}
		};

		act();

		Debug.LogError("二回目の実行では、doneスイッチはひっくり返ってる。なので、まあ、これがシンプルでいいかなあ。");
		
		act();

		Debug.LogError("なんのcacheを使ったか、を取り出して観測する。");

		foreach (var cached in cacheDict) {
			Debug.LogError("cached key node id:" + cached.Key);
			foreach (var cachedResInfo in cached.Value) {
				Debug.LogError("cachedResInfo:" + cachedResInfo);
			}
		}
	}

}