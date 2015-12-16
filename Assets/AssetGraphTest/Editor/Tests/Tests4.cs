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

		var dataPath = Path.Combine(Application.dataPath, "AssetGraphTest/Editor/TestData");
		var graphDataPath = Path.Combine(dataPath, "_4_0_RunThenCachedGUI.json");
		
		// load
		var dataStr = string.Empty;
		
		using (var sr = new StreamReader(graphDataPath)) {
			dataStr = sr.ReadToEnd();
		}
		var graphDict = Json.Deserialize(dataStr) as Dictionary<string, object>;
		
		// get cached asset dictionary.
		var createdDataDict = new Dictionary<string, List<string>>();
		

		Action act = () => {
			var EndpointNodeIdsAndNodeDatasAndConnectionDatas = GraphStackController.SerializeNodeRoute(graphDict, string.Empty);
		
			var endpointNodeIds = EndpointNodeIdsAndNodeDatasAndConnectionDatas.endpointNodeIds;
			var nodeDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.nodeDatas;
			var connectionDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.connectionDatas;

			var resultDict = new Dictionary<string, Dictionary<string, List<InternalAssetData>>>();

			foreach (var endNodeId in endpointNodeIds) {
				GraphStackController.RunSerializedRoute(endNodeId, nodeDatas, connectionDatas, resultDict, cacheDict, string.Empty);
			}

			/*
				create first data result.
			*/
			foreach (var node in nodeDatas) {
				var nodeId = node.nodeId;
				var nodeKind = node.nodeKind;
				var cachedDataPaths = GraphStackController.GetCachedDataByNodeKind(nodeKind, nodeId, string.Empty);

				createdDataDict[nodeId] = cachedDataPaths;
			}
		};

		act();

		// reset cacheDict for retake.
		cacheDict = new Dictionary<string, List<string>>();


		act();

		/*
			check results.
		*/
		foreach (var nodeId in createdDataDict.Keys) {
			if (!cacheDict.Keys.Contains(nodeId)) {
				if (nodeId == "TestExporter") continue;
				Debug.LogError("cacheDict did not contained:" + nodeId);
			}
		}

		foreach (var nodeId in cacheDict.Keys) {
			if (!createdDataDict.Keys.Contains(nodeId)) {
				Debug.LogError("createdDataDict did not contained:" + nodeId);
			}
		}


		foreach (var key in createdDataDict.Keys) {
			if (!cacheDict.ContainsKey(key)) continue;

			var basePaths = createdDataDict[key];
			var targetPaths = cacheDict[key];
			
			foreach (var basePath in basePaths) {
				// ignore meta files.
				if (GraphStackController.IsMetaFile(basePath)) continue;

				// avoid sub-creating assets. sub-creating assets never appear as cached.
				if (basePath.StartsWith("Assets/AssetGraph/Cache/Imported/Testimporter1/iOS/models/ID_0/Materials")) continue;
				if (basePath.StartsWith("Assets/AssetGraph/Cache/Imported/Testimporter1/iOS/models/ID_1/Materials")) continue;
				if (basePath.StartsWith("Assets/AssetGraph/Cache/Imported/Testimporter1/iOS/models/ID_2/Materials")) continue;

				if (!targetPaths.Contains(basePath)) Debug.LogError("contained in result, but not in cached:" + basePath);
			}

			foreach (var targetPath in targetPaths) {
				// ignore meta files.
				if (GraphStackController.IsMetaFile(targetPath)) continue;

				if (!basePaths.Contains(targetPath)) Debug.LogError("contained in cache, but not in result:" + targetPath);
			}
		}
	}

	public void _4_1_ImporterUnuseCache () {
		GraphStackController.CleanCache();
		GraphStackController.CleanSetting();
	
		var cacheDict = new Dictionary<string, List<string>>();

		var dataPath = Path.Combine(Application.dataPath, "AssetGraphTest/Editor/TestData");
		var graphDataPath = Path.Combine(dataPath, "_4_1_ImporterUnuseCache.json");
		
		// load
		var dataStr = string.Empty;
		
		using (var sr = new StreamReader(graphDataPath)) {
			dataStr = sr.ReadToEnd();
		}
		var graphDict = Json.Deserialize(dataStr) as Dictionary<string, object>;
		
		// setup first.
		{
			var EndpointNodeIdsAndNodeDatasAndConnectionDatas = GraphStackController.SerializeNodeRoute(graphDict, string.Empty);
		
			var endpointNodeIds = EndpointNodeIdsAndNodeDatasAndConnectionDatas.endpointNodeIds;
			var nodeDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.nodeDatas;
			var connectionDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.connectionDatas;

			var resultDict = new Dictionary<string, Dictionary<string, List<InternalAssetData>>>();

			foreach (var endNodeId in endpointNodeIds) {
				GraphStackController.SetupSerializedRoute(endNodeId, nodeDatas, connectionDatas, resultDict, cacheDict, string.Empty);
			}
		}


		Action act = () => {
			var EndpointNodeIdsAndNodeDatasAndConnectionDatas = GraphStackController.SerializeNodeRoute(graphDict, string.Empty);
		
			var endpointNodeIds = EndpointNodeIdsAndNodeDatasAndConnectionDatas.endpointNodeIds;
			var nodeDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.nodeDatas;
			var connectionDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.connectionDatas;

			var resultDict = new Dictionary<string, Dictionary<string, List<InternalAssetData>>>();

			foreach (var endNodeId in endpointNodeIds) {
				GraphStackController.RunSerializedRoute(endNodeId, nodeDatas, connectionDatas, resultDict, cacheDict, string.Empty);
			}
		};

		// set import target data by direct copy.


		// import internal resources.
		act();
		

		// reset cacheDict for retake.
		cacheDict = new Dictionary<string, List<string>>();

		/*
			change import setting, emulate "setting is changed but old caches are already exists."
		*/
		{
			var targetSettingFile = FileController.PathCombine(AssetGraphSettings.IMPORTER_SAMPLING_PLACE, "18139977-3750-4efc-bee0-0351a73f2da7", AssetGraphSettings.PLATFORM_DEFAULT_PACKAGE, "sample.fbx");
			if (!File.Exists(targetSettingFile)) {
				Debug.LogError("failed to read file:" + targetSettingFile);
				return;
			}
			var targetSettingImporter = AssetImporter.GetAtPath(targetSettingFile) as ModelImporter;
			if (!targetSettingImporter) {
				Debug.LogError("failed to read targetSettingImporter:" + targetSettingImporter);
				return;
			}

			targetSettingImporter.meshCompression = ModelImporterMeshCompression.High;
		}

		// act again.
		act();

		// no files should be appeared.
		foreach (var nodeId in cacheDict.Keys) {
			var cachedContents = cacheDict[nodeId];
			foreach (var cached in cachedContents) {
				Debug.LogError("shoud not appear, cached:" + cached);
			}
		}
	}

	public void _4_2_PrefabricatorUnuseCache () {
		GraphStackController.CleanCache();
		GraphStackController.CleanSetting();

		// setup first.
		{
			var cacheDict = new Dictionary<string, List<string>>();

			var dataPath = Path.Combine(Application.dataPath, "AssetGraphTest/Editor/TestData");
			var graphDataPath = Path.Combine(dataPath, "_4_2_PrefabricatorUnuseCache.json");
			
			// load
			var dataStr = string.Empty;
			
			using (var sr = new StreamReader(graphDataPath)) {
				dataStr = sr.ReadToEnd();
			}
			var graphDict = Json.Deserialize(dataStr) as Dictionary<string, object>;
			var EndpointNodeIdsAndNodeDatasAndConnectionDatas = GraphStackController.SerializeNodeRoute(graphDict, string.Empty);
		
			var endpointNodeIds = EndpointNodeIdsAndNodeDatasAndConnectionDatas.endpointNodeIds;
			var nodeDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.nodeDatas;
			var connectionDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.connectionDatas;

			var resultDict = new Dictionary<string, Dictionary<string, List<InternalAssetData>>>();

			foreach (var endNodeId in endpointNodeIds) {
				GraphStackController.SetupSerializedRoute(endNodeId, nodeDatas, connectionDatas, resultDict, cacheDict, string.Empty);
			}
		}

		// create prefab cache.
		{
			var cacheDict = new Dictionary<string, List<string>>();

			var dataPath = Path.Combine(Application.dataPath, "AssetGraphTest/Editor/TestData");
			var graphDataPath = Path.Combine(dataPath, "_4_2_PrefabricatorUnuseCache.json");
			
			// load
			var dataStr = string.Empty;
			
			using (var sr = new StreamReader(graphDataPath)) {
				dataStr = sr.ReadToEnd();
			}
			var graphDict = Json.Deserialize(dataStr) as Dictionary<string, object>;
			var EndpointNodeIdsAndNodeDatasAndConnectionDatas = GraphStackController.SerializeNodeRoute(graphDict, string.Empty);
		
			var endpointNodeIds = EndpointNodeIdsAndNodeDatasAndConnectionDatas.endpointNodeIds;
			var nodeDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.nodeDatas;
			var connectionDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.connectionDatas;

			var resultDict = new Dictionary<string, Dictionary<string, List<InternalAssetData>>>();

			foreach (var endNodeId in endpointNodeIds) {
				GraphStackController.RunSerializedRoute(endNodeId, nodeDatas, connectionDatas, resultDict, cacheDict, string.Empty);
			}
		}

		// change importer setting of file.
		{
			var targetSettingFile = FileController.PathCombine(AssetGraphSettings.IMPORTER_SAMPLING_PLACE, "1b73b22a-41bc-46d3-bbfb-5fe7fa846881", AssetGraphSettings.PLATFORM_DEFAULT_PACKAGE, "sample.fbx");
			var targetSettingImporter = AssetImporter.GetAtPath(targetSettingFile) as ModelImporter;

			targetSettingImporter.meshCompression = ModelImporterMeshCompression.High;;
		}

		// run again. part of prefab is changed, should create new prefab.
		{
			var cacheDict = new Dictionary<string, List<string>>();

			var dataPath = Path.Combine(Application.dataPath, "AssetGraphTest/Editor/TestData");
			var graphDataPath = Path.Combine(dataPath, "_4_2_PrefabricatorUnuseCache.json");
			
			// load
			var dataStr = string.Empty;
			
			using (var sr = new StreamReader(graphDataPath)) {
				dataStr = sr.ReadToEnd();
			}
			var graphDict = Json.Deserialize(dataStr) as Dictionary<string, object>;
			var EndpointNodeIdsAndNodeDatasAndConnectionDatas = GraphStackController.SerializeNodeRoute(graphDict, string.Empty);
		
			var endpointNodeIds = EndpointNodeIdsAndNodeDatasAndConnectionDatas.endpointNodeIds;
			var nodeDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.nodeDatas;
			var connectionDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.connectionDatas;

			var resultDict = new Dictionary<string, Dictionary<string, List<InternalAssetData>>>();

			foreach (var endNodeId in endpointNodeIds) {
				GraphStackController.RunSerializedRoute(endNodeId, nodeDatas, connectionDatas, resultDict, cacheDict, string.Empty);
			}

			// all prefabs are new. cached prefabs should not be appeared.
			
			var cachedContents = cacheDict["eab1a805-4399-4960-bc7f-4d6db602a411"];
			foreach (var cached in cachedContents) {
				Debug.LogError("shoud not appear, cached:" + cached);
			}
		}
	}

	public void _4_3_ImporterFromInside () {
		GraphStackController.CleanCache();
		GraphStackController.CleanSetting();
	
		var cacheDict = new Dictionary<string, List<string>>();

		var dataPath = Path.Combine(Application.dataPath, "AssetGraphTest/Editor/TestData");
		var graphDataPath = Path.Combine(dataPath, "_4_3_ImporterFromInside.json");
		
		// load
		var dataStr = string.Empty;
		
		using (var sr = new StreamReader(graphDataPath)) {
			dataStr = sr.ReadToEnd();
		}
		var graphDict = Json.Deserialize(dataStr) as Dictionary<string, object>;
		

		// setup first.
		{
			var EndpointNodeIdsAndNodeDatasAndConnectionDatas = GraphStackController.SerializeNodeRoute(graphDict, string.Empty);
		
			var endpointNodeIds = EndpointNodeIdsAndNodeDatasAndConnectionDatas.endpointNodeIds;
			var nodeDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.nodeDatas;
			var connectionDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.connectionDatas;

			var resultDict = new Dictionary<string, Dictionary<string, List<InternalAssetData>>>();

			foreach (var endNodeId in endpointNodeIds) {
				GraphStackController.SetupSerializedRoute(endNodeId, nodeDatas, connectionDatas, resultDict, cacheDict, string.Empty);
			}
		}

		Action act = () => {
			var EndpointNodeIdsAndNodeDatasAndConnectionDatas = GraphStackController.SerializeNodeRoute(graphDict, string.Empty);
		
			var endpointNodeIds = EndpointNodeIdsAndNodeDatasAndConnectionDatas.endpointNodeIds;
			var nodeDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.nodeDatas;
			var connectionDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.connectionDatas;

			var resultDict = new Dictionary<string, Dictionary<string, List<InternalAssetData>>>();

			foreach (var endNodeId in endpointNodeIds) {
				GraphStackController.RunSerializedRoute(endNodeId, nodeDatas, connectionDatas, resultDict, cacheDict, string.Empty);
			}
		};

		// import internal resources.
		act();

		// reset cacheDict for retake.
		cacheDict = new Dictionary<string, List<string>>();

		// act again, resources are already inside of unity.
		act();

		int count = 0;//should be 3. 3files will be cached.
		foreach (var nodeId in cacheDict.Keys) {
			var cachedContents = cacheDict[nodeId];
			foreach (var cached in cachedContents) {
				count++;
			}
		}

		if (count == 3) return;

		Debug.LogError("failed to use cache. count:" + count);
	}

}