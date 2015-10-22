using UnityEngine;
using UnityEditor;

using AssetGraph;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using MiniJSONForAssetGraph;

public partial class Test {
	public void _5_0_PlatformChanging () {
		// GraphStackController.CleanCache();
		
		// var cacheDict = new Dictionary<string, List<string>>();

		// var dataPath = Path.Combine(Application.dataPath, "AssetGraphTest/Editor/TestData");
		// var graphDataPath = Path.Combine(dataPath, "_4_0_RunThenCachedGUI.json");
		
		// // load
		// var dataStr = string.Empty;
		
		// using (var sr = new StreamReader(graphDataPath)) {
		// 	dataStr = sr.ReadToEnd();
		// }
		// var graphDict = Json.Deserialize(dataStr) as Dictionary<string, object>;
		
		// // get cached asset dictionary.
		// var createdDataDict = new Dictionary<string, List<string>>();
		

		// Action act = () => {
		// 	var EndpointNodeIdsAndNodeDatasAndConnectionDatas = GraphStackController.SerializeNodeRoute(graphDict);
		
		// 	var endpointNodeIds = EndpointNodeIdsAndNodeDatasAndConnectionDatas.endpointNodeIds;
		// 	var nodeDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.nodeDatas;
		// 	var connectionDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.connectionDatas;

		// 	var resultDict = new Dictionary<string, Dictionary<string, List<InternalAssetData>>>();

		// 	foreach (var endNodeId in endpointNodeIds) {
		// 		GraphStackController.RunSerializedRoute(endNodeId, nodeDatas, connectionDatas, resultDict, cacheDict);
		// 	}

		// 	/*
		// 		create first data result.
		// 	*/
		// 	foreach (var node in nodeDatas) {
		// 		var nodeId = node.nodeId;
		// 		var nodeKind = node.nodeKind;
		// 		var cachedDataPaths = GraphStackController.GetCachedData(nodeKind, nodeId);

		// 		createdDataDict[nodeId] = cachedDataPaths;
		// 	}
		// };

		// act();

		// // reset cacheDict for retake.
		// cacheDict = new Dictionary<string, List<string>>();


		// act();

		// /*
		// 	check results.
		// */
		// foreach (var nodeId in createdDataDict.Keys) {
		// 	if (!cacheDict.Keys.Contains(nodeId)) {
		// 		if (nodeId == "TestExporter") continue;
		// 		Debug.LogError("cacheDict did not contained:" + nodeId);
		// 	}
		// }

		// foreach (var nodeId in cacheDict.Keys) {
		// 	if (!createdDataDict.Keys.Contains(nodeId)) {
		// 		Debug.LogError("createdDataDict did not contained:" + nodeId);
		// 	}
		// }


		// foreach (var key in createdDataDict.Keys) {
		// 	if (!cacheDict.ContainsKey(key)) continue;

		// 	var basePaths = createdDataDict[key];
		// 	var targetPaths = cacheDict[key];
			
		// 	foreach (var basePath in basePaths) {

		// 		// avoid sub-creating assets. sub-creating assets never appear as cached.
		// 		if (basePath.StartsWith("Assets/AssetGraph/Cache/Imported/Testimporter1/models/ID_0/Materials")) continue;
		// 		if (basePath.StartsWith("Assets/AssetGraph/Cache/Imported/Testimporter1/models/ID_1/Materials")) continue;
		// 		if (basePath.StartsWith("Assets/AssetGraph/Cache/Imported/Testimporter1/models/ID_2/Materials")) continue;

		// 		if (!targetPaths.Contains(basePath)) Debug.LogError("contained in result, but not in cached:" + basePath);
		// 	}

		// 	foreach (var targetPath in targetPaths) {
		// 		if (!basePaths.Contains(targetPath)) Debug.LogError("contained in cache, but not in result:" + targetPath);
		// 	}
		// }

		Debug.LogError("not yet");
	}

}