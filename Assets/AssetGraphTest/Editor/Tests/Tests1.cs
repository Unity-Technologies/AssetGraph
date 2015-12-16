using UnityEngine;
using UnityEditor;

using AssetGraph;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using MiniJSONForAssetGraph;

// gui version of _0 series.

public partial class Test {

	public void _1_0_0_SetupLoader () {
		GraphStackController.CleanCache();

		// contains 2 resources.
		var projectFolderPath = Directory.GetParent(Application.dataPath).ToString();
		var definedSourcePath = Path.Combine(projectFolderPath, "TestResources/TestResources0/");

		var emptySource = new Dictionary<string, List<InternalAssetData>>();

		var results = new Dictionary<string, List<InternalAssetData>>();

		var integratedGUILoader = new IntegratedGUILoader(definedSourcePath);
		Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Out = (string nodeId, string connectionId, Dictionary<string, List<InternalAssetData>> output, List<string> cached) => {
			results[connectionId] = output["0"];
		};

		integratedGUILoader.Setup("ID_1_0_0_SetupLoader", "CONNECTION_1_0_0_SetupLoader", string.Empty, emptySource, new List<string>(), Out);

		var outputs = results["CONNECTION_1_0_0_SetupLoader"];
		if (outputs.Count == 2) {
			Debug.Log("passed _1_0_0_SetupLoader");
			return;
		}

		Debug.LogError("not match 2, actual:" + outputs.Count);
	}

	public void _1_0_1_RunLoader () {
		GraphStackController.CleanCache();

		// contains 2 resources.
		var projectFolderPath = Directory.GetParent(Application.dataPath).ToString();
		var definedSourcePath = Path.Combine(projectFolderPath, "TestResources/TestResources0/");

		var emptySource = new Dictionary<string, List<InternalAssetData>>();

		var results = new Dictionary<string, List<InternalAssetData>>();

		var integratedGUILoader = new IntegratedGUILoader(definedSourcePath);
		Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Out = (string nodeId, string connectionId, Dictionary<string, List<InternalAssetData>> output, List<string> cached) => {
			results[connectionId] = output["0"];
		};

		integratedGUILoader.Run("ID_1_0_1_RunLoader", "CONNECTION_1_0_1_RunLoader", string.Empty, emptySource, new List<string>(), Out);

		var outputs = results["CONNECTION_1_0_1_RunLoader"];
		if (outputs.Count == 2) {
			Debug.Log("passed _1_0_1_RunLoader");
			return;
		}

		Debug.LogError("not match 2, actual:" + outputs.Count);
	}

	public void _1_0_SetupFilter () {
		GraphStackController.CleanCache();

		var source = new Dictionary<string, List<InternalAssetData>> {
			{"0", 
				new List<InternalAssetData> {
					InternalAssetData.InternalAssetDataByLoader("A/1st", "A"),
					InternalAssetData.InternalAssetDataByLoader("A/2nd", "A")
				}
			}
		};

		var results = new Dictionary<string, List<InternalAssetData>>();
		
		var keywords = new List<string>{
			"A/1st", "A/2nd"
		};

		var integratedGUIFilter = new IntegratedGUIFilter(keywords);
		Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Out = (string nodeId, string connectionId, Dictionary<string, List<InternalAssetData>> output, List<string> cached) => {
			results[connectionId] = output["0"];
		};

		integratedGUIFilter.Setup("ID_1_0_SetupFilter", "CONNECTION_1_0_SetupFilter", string.Empty, source, new List<string>(), Out);

		/*
			in GUI Filter, output result connection id is it's keyword.
		*/
		if (results.ContainsKey("A/1st")) {
			var result1 = results["A/1st"];
			
			if (result1[0].absoluteSourcePath == "A/1st") {
				if (results.ContainsKey("A/2nd")) {
					var resut2 = results["A/2nd"];
					if (resut2[0].absoluteSourcePath == "A/2nd") {
						Debug.Log("passed _1_0_SetupFilter");
						return;
					}
				}	
			}
		}

		Debug.LogError("failed to split by filter");
	}
	public void _1_1_RunFilter () {
		GraphStackController.CleanCache();

		var source = new Dictionary<string, List<InternalAssetData>> {
			{"0", 
				new List<InternalAssetData> {
					InternalAssetData.InternalAssetDataByLoader("A/1st", "A"),
					InternalAssetData.InternalAssetDataByLoader("A/2nd", "A")
				}
			}
		};

		var results = new Dictionary<string, List<InternalAssetData>>();

		var keywords = new List<string>{
			"A/1st", "A/2nd"
		};

		var integratedGUIFilter = new IntegratedGUIFilter(keywords);
		Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Out = (string nodeId, string connectionId, Dictionary<string, List<InternalAssetData>> output, List<string> cached) => {
			results[connectionId] = output["0"];
		};

		integratedGUIFilter.Run("ID_1_1_RunFilter", "CONNECTION_1_1_RunFilter", string.Empty, source, new List<string>(), Out);

		/*
			in GUI Filter, output result connection id is it's keyword.
		*/
		if (results.ContainsKey("A/1st")) {
			var result1 = results["A/1st"];
			if (result1[0].absoluteSourcePath == "A/1st") {
				if (results.ContainsKey("A/2nd")) {
					var resut2 = results["A/2nd"];
					if (resut2[0].absoluteSourcePath == "A/2nd") {
						Debug.Log("passed _1_1_RunFilter");
						return;
					}
				}	
			}
		}

		Debug.LogError("failed to split by filter");
	}

	public void _1_2_SetupImporter () {
		GraphStackController.CleanCache();

		var projectFolderPath = Directory.GetParent(Application.dataPath).ToString();
		var definedSourcePath = Path.Combine(projectFolderPath, "TestResources/TestResources0/");

		var source = new Dictionary<string, List<InternalAssetData>> {
			{"0", 
				new List<InternalAssetData> {
					InternalAssetData.InternalAssetDataByLoader(Path.Combine(definedSourcePath, "dummy.png"), definedSourcePath),
					InternalAssetData.InternalAssetDataByLoader(Path.Combine(definedSourcePath, "model/sample.fbx"), definedSourcePath)
				}
			}
		};

		var results = new Dictionary<string, List<InternalAssetData>>();

		var integratedGUIImporter = new IntegratedGUIImporter(AssetGraphSettings.PLATFORM_DEFAULT_NAME);
		Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Out = (string nodeId, string connectionId, Dictionary<string, List<InternalAssetData>> output, List<string> cached) => {
			results[connectionId] = output["0"];
		};

		integratedGUIImporter.Setup("ID_1_2_SetupImporter", "CONNECTION_1_2_SetupImporter", string.Empty, source, new List<string>(), Out);
		Debug.Log("passed _1_2_SetupImporter");
	}
	public void _1_3_RunImporter () {
		var projectFolderPath = Directory.GetParent(Application.dataPath).ToString();
		var definedSourcePath = Path.Combine(projectFolderPath, "TestResources/TestResources0/");
		
		var source = new Dictionary<string, List<InternalAssetData>> {
			{"0", 
				new List<InternalAssetData> {
					InternalAssetData.InternalAssetDataByLoader(Path.Combine(definedSourcePath, "dummy.png"), definedSourcePath),
					InternalAssetData.InternalAssetDataByLoader(Path.Combine(definedSourcePath, "model/sample.fbx"), definedSourcePath)
				}
			}
		};

		var results = new Dictionary<string, List<InternalAssetData>>();

		var integratedGUIImporter = new IntegratedGUIImporter(AssetGraphSettings.PLATFORM_DEFAULT_NAME);
		Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Out = (string nodeId, string connectionId, Dictionary<string, List<InternalAssetData>> output, List<string> cached) => {
			results[connectionId] = output["0"];
		};

		integratedGUIImporter.Run("ID_1_3_RunImporter", "CONNECTION_1_3_RunImporter", string.Empty, source, new List<string>(), Out);

		var currentOutputs = results["CONNECTION_1_3_RunImporter"].Where(path => !GraphStackController.IsMetaFile(path.importedPath)).ToList();
		if (currentOutputs.Count == 3) {
			Debug.Log("passed _1_3_RunImporter");
			return;
		}

		Debug.LogError("failed to collect importerd resource:" + currentOutputs.Count);
	}

	// there is no GUI Prefabricator.

	public void _1_6_SetupBundlizer () {
		GraphStackController.CleanCache();

		var importedPath = "Assets/AssetGraphTest/PrefabricatorTestResource/SpanPath/a.png";

		var source = new Dictionary<string, List<InternalAssetData>> {
			{"0", 
				new List<InternalAssetData> {
					InternalAssetData.InternalAssetDataByImporter(
						"traceId_1_6_SetupBundlizer",
						Path.Combine(Application.dataPath, importedPath),
						Application.dataPath,
						Path.GetFileName(importedPath),
						string.Empty,
						importedPath,
						AssetDatabase.AssetPathToGUID(importedPath),
						AssetGraphInternalFunctions.GetAssetType(importedPath)
					)
				}

			}
		};

		var results = new Dictionary<string, List<InternalAssetData>>();

		var bundleNameTemplate = "a_*.bundle";

		var integratedGUIBundlizer = new IntegratedGUIBundlizer(bundleNameTemplate);
		Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Out = (string nodeId, string connectionId, Dictionary<string, List<InternalAssetData>> output, List<string> cached) => {
			results[connectionId] = output["0"];
		};

		integratedGUIBundlizer.Setup("ID_1_6_SetupBundlizer", "CONNECTION_1_6_SetupBundlizer", string.Empty, source, new List<string>(), Out);
		Debug.Log("passed _1_6_SetupBundlizer");
	}
	public void _1_7_RunBundlizer () {
		GraphStackController.CleanCache();
		EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTarget.iOS);

		var importedPath = "Assets/AssetGraphTest/PrefabricatorTestResource/SpanPath/a.png";

		var source = new Dictionary<string, List<InternalAssetData>> {
			{"0", 
				new List<InternalAssetData> {
					InternalAssetData.InternalAssetDataByImporter(
						"traceId_1_7_RunBundlizer",
						Path.Combine(Application.dataPath, importedPath),
						Application.dataPath,
						Path.GetFileName(importedPath),
						string.Empty,
						importedPath,
						AssetDatabase.AssetPathToGUID(importedPath),
						AssetGraphInternalFunctions.GetAssetType(importedPath)
					)
				}
			}
		};

		var results = new Dictionary<string, List<InternalAssetData>>();

		var bundleNameTemplate = "a_*.bundle";

		var integratedGUIBundlizer = new IntegratedGUIBundlizer(bundleNameTemplate);
		Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Out = (string nodeId, string connectionId, Dictionary<string, List<InternalAssetData>> output, List<string> cached) => {
			results[connectionId] = output["0"];
		};

		integratedGUIBundlizer.Run("ID_1_7_RunBundlizer", "CONNECTION_1_7_RunBundlizer", string.Empty, source, new List<string>(), Out);

		var currentOutputs = results["CONNECTION_1_7_RunBundlizer"];
		if (currentOutputs.Count == 1) {
			// should be a_0.bundle
			if (currentOutputs[0].pathUnderConnectionId != "iOS/a_0.bundle.ios") {
				Debug.LogError("failed to bundlize, name not match:" + currentOutputs[0].pathUnderConnectionId);
				return;
			}

			// passed, erase bundle name setting.
			var bundledAssetSourcePath = "Assets/AssetGraphTest/PrefabricatorTestResource/SpanPath/a.png";
			if (!File.Exists(bundledAssetSourcePath)) {
				Debug.LogError("failed to delete bundle setting. bundledAssetSourcePath:" + bundledAssetSourcePath);
				return;
			}

			var assetImporter = AssetImporter.GetAtPath(bundledAssetSourcePath);
			assetImporter.assetBundleName = string.Empty;
			return;
		}
		
		Debug.LogError("failed to bundlize");
	}

	public void _1_8_0_SerializeGraph_hasValidEndpoint () {
		GraphStackController.CleanCache();

		var basePath = Path.Combine(Application.dataPath, "AssetGraphTest/Editor/TestData");
		var graphDataPath = Path.Combine(basePath, "_1_8_SerializeGraph.json");
		
		// load
		var dataStr = string.Empty;
		
		using (var sr = new StreamReader(graphDataPath)) {
			dataStr = sr.ReadToEnd();
		}

		var graphDict = Json.Deserialize(dataStr) as Dictionary<string, object>;
		
		var endpointNodeIdsAndNodeDatas = GraphStackController.SerializeNodeRoute(graphDict, string.Empty);
		if (endpointNodeIdsAndNodeDatas.endpointNodeIds.Contains("2nd_Importer")) {
			Debug.Log("passed _1_8_0_SerializeGraph_hasValidEndpoint");
			return;
		}

		Debug.LogError("not valid endpoint");
	}

	public void _1_8_1_SerializeGraph_hasValidOrder () {
		GraphStackController.CleanCache();

		var basePath = Path.Combine(Application.dataPath, "AssetGraphTest/Editor/TestData");
		var graphDataPath = Path.Combine(basePath, "_1_8_SerializeGraph.json");
		
		// load
		var dataStr = string.Empty;
		
		using (var sr = new StreamReader(graphDataPath)) {
			dataStr = sr.ReadToEnd();
		}


		var graphDict = Json.Deserialize(dataStr) as Dictionary<string, object>;
		
		var endpointNodeIdsAndNodeDatasAndConnectionDatas = GraphStackController.SerializeNodeRoute(graphDict, string.Empty);

		var endPoint0 = endpointNodeIdsAndNodeDatasAndConnectionDatas.endpointNodeIds[0];
		var nodeDatas = endpointNodeIdsAndNodeDatasAndConnectionDatas.nodeDatas;
		var connectionDatas = endpointNodeIdsAndNodeDatasAndConnectionDatas.connectionDatas;

		var resultDict = new Dictionary<string, Dictionary<string, List<InternalAssetData>>>();
		var cacheDict = new Dictionary<string, List<string>>();
		var orderedConnectionIds = GraphStackController.RunSerializedRoute(endPoint0, nodeDatas, connectionDatas, resultDict, cacheDict, string.Empty);
		
		if (orderedConnectionIds.Count == 0) {
			Debug.LogError("list is empty");
			return;
		}

		if (orderedConnectionIds[0] == "ローダーからフィルタへ" &&
			orderedConnectionIds[1] == "フィルタからインポータへ1" &&
			orderedConnectionIds[2] == "フィルタからインポータへ2") {
			Debug.Log("passed _1_8_1_SerializeGraph_hasValidOrder");
			return;
		}

		Debug.LogError("failed to validate order");
	}

	public void _1_9_RunStackedGraph () {
		GraphStackController.CleanCache();

		var basePath = Path.Combine(Application.dataPath, "AssetGraphTest/Editor/TestData");
		var graphDataPath = Path.Combine(basePath, "_1_9_RunStackedGraph.json");
		
		// load
		var dataStr = string.Empty;
		
		using (var sr = new StreamReader(graphDataPath)) {
			dataStr = sr.ReadToEnd();
		}
		var graphDict = Json.Deserialize(dataStr) as Dictionary<string, object>;
		
		GraphStackController.RunStackedGraph(graphDict, string.Empty);
		
		var projectFolderPath = Directory.GetParent(Application.dataPath).ToString();
		var expectedExportDestPath = Path.Combine(projectFolderPath, "TestExportPlace/For_1_9_SerializedGraphJSONByExporter");

		if (File.Exists(Path.Combine(expectedExportDestPath, "iOS/model/Materials/kiosk_0001.mat")) &&
			File.Exists(Path.Combine(expectedExportDestPath, "iOS/model/sample.fbx")) &&
			File.Exists(Path.Combine(expectedExportDestPath, "iOS/dummy.png"))
		) {
			Debug.Log("passed _1_9_RunStackedGraph");
			return;
		}

		Debug.LogError("failed to export");
	}


	public void _1_10_SetupExporter () {
		GraphStackController.CleanCache();

		var projectFolderPath = Directory.GetParent(Application.dataPath).ToString();
		var exportFilePath = Path.Combine(projectFolderPath, "TestExportPlace/For_1_10_SetupExport");

		// delete all if exist
		if (Directory.Exists(exportFilePath)) {
			Directory.Delete(exportFilePath, true);
		}

		Directory.CreateDirectory(exportFilePath);

		var importedPath = "Assets/AssetGraphTest/ExporterTestResource/SpanTempPath/SpanPath/a.png";
		var assetId = AssetDatabase.AssetPathToGUID(importedPath);
		var assetType = AssetGraphInternalFunctions.GetAssetType(importedPath);

		var exportTargets = new Dictionary<string, List<InternalAssetData>> {
			{"0", 
				new List<InternalAssetData> {
					InternalAssetData.InternalAssetDataGeneratedByImporterOrPrefabricator(importedPath, assetId, assetType, true),
				}
			}
		};
		
		var integratedGUIExporter = new IntegratedGUIExporter(exportFilePath);
		Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Out = (string nodeId, string connectionId, Dictionary<string, List<InternalAssetData>> output, List<string> cached) => {
			
		};

		integratedGUIExporter.Setup("ID_1_10_SetupExport", "CONNECTION_1_10_SetupExport", string.Empty, exportTargets, new List<string>(), Out);
		Debug.Log("passed _1_10_SetupExporter");
	}

	public void _1_11_RunExporter () {
		GraphStackController.CleanCache();

		var projectFolderPath = Directory.GetParent(Application.dataPath).ToString();
		var exportFilePath = Path.Combine(projectFolderPath, "TestExportPlace/For_1_11_RunExport");

		// delete all if exist
		if (Directory.Exists(exportFilePath)) {
			Directory.Delete(exportFilePath, true);
		}

		Directory.CreateDirectory(exportFilePath);

		var importedPath = "Assets/AssetGraphTest/ExporterTestResource/SpanTempPath/SpanPath/a.png";
		var assetId = AssetDatabase.AssetPathToGUID(importedPath);
		var assetType = AssetGraphInternalFunctions.GetAssetType(importedPath);
		
		var exportTargets = new Dictionary<string, List<InternalAssetData>> {
			{"0", 
				new List<InternalAssetData> {
					InternalAssetData.InternalAssetDataGeneratedByImporterOrPrefabricator(importedPath, assetId, assetType, true),
				}
			}
		};
		
		var integratedGUIExporter = new IntegratedGUIExporter(exportFilePath);
		Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Out = (string nodeId, string connectionId, Dictionary<string, List<InternalAssetData>> output, List<string> cached) => {
			
		};

		integratedGUIExporter.Run("ID_1_11_RunExport", "CONNECTION_1_11_RunExport", string.Empty, exportTargets, new List<string>(), Out);

		var assumeedExportedFilePath = Path.Combine(exportFilePath, "a.png");

		if (File.Exists(assumeedExportedFilePath)) {
			Debug.Log("passed _1_11_RunExporter");
			return;
		}

		Debug.LogError("failed to export");
	}

	public void _1_12_RunStackedGraph_FullStacked () {
		GraphStackController.CleanCache();
		
		var basePath = Path.Combine(Application.dataPath, "AssetGraphTest/Editor/TestData");
		var graphDataPath = Path.Combine(basePath, "_1_12_RunStackedGraph_FullStacked.json");
		
		// load
		var dataStr = string.Empty;
		
		using (var sr = new StreamReader(graphDataPath)) {
			dataStr = sr.ReadToEnd();
		}
		var graphDict = Json.Deserialize(dataStr) as Dictionary<string, object>;
		
		GraphStackController.RunStackedGraph(graphDict, string.Empty);
		
		var projectFolderPath = Directory.GetParent(Application.dataPath).ToString();
		var expectedExportDestPath = Path.Combine(projectFolderPath, "TestExportPlace/TestExportFor_1_12_SerializedGraphJSON");

		var the1stBundlePath = Path.Combine(expectedExportDestPath, "iOS/chara0.assetbundle");
		var the2ndBundlePath = Path.Combine(expectedExportDestPath, "iOS/chara1.assetbundle");
		if (File.Exists(the1stBundlePath) && File.Exists(the2ndBundlePath)) {
			Debug.Log("passed _1_12_RunStackedGraph_FullStacked");
			return;
		}

		Debug.LogError("failed to generate");
	}

	public void _1_13_SetupStackedGraph_FullStacked () {
		GraphStackController.CleanCache();
		
		var basePath = Path.Combine(Application.dataPath, "AssetGraphTest/Editor/TestData");
		var graphDataPath = Path.Combine(basePath, "_1_12_RunStackedGraph_FullStacked.json");
		
		// load
		var dataStr = string.Empty;
		
		using (var sr = new StreamReader(graphDataPath)) {
			dataStr = sr.ReadToEnd();
		}
		var graphDict = Json.Deserialize(dataStr) as Dictionary<string, object>;
		
		var resultDict = GraphStackController.SetupStackedGraph(graphDict, string.Empty);

		// 11 is count of connections. 3 is count of end nodes.
		if (resultDict.Count == 11 + 3) {
			Debug.Log("passed _1_13_SetupStackedGraph_FullStacked");
			return;
		}

		Debug.LogError("shortage or excess of connections:" + resultDict.Count);
	}

	public void _1_14_SetupStackedGraph_Sample () {
		GraphStackController.CleanCache();
		
		var basePath = Path.Combine(Application.dataPath, "AssetGraphTest/Editor/TestData");
		var graphDataPath = Path.Combine(basePath, "_1_14_RunStackedGraph_Sample.json");
		
		// load
		var dataStr = string.Empty;
		
		using (var sr = new StreamReader(graphDataPath)) {
			dataStr = sr.ReadToEnd();
		}

		var graphDict = Json.Deserialize(dataStr) as Dictionary<string, object>;

		GraphStackController.SetupStackedGraph(graphDict, string.Empty);

		Debug.Log("passed _1_14_SetupStackedGraph_Sample");
	}

	public void _1_15_RunStackedGraph_Sample () {
		GraphStackController.CleanCache();

		var basePath = Path.Combine(Application.dataPath, "AssetGraphTest/Editor/TestData");
		var graphDataPath = Path.Combine(basePath, "_1_14_RunStackedGraph_Sample.json");
		
		// load
		var dataStr = string.Empty;
		
		using (var sr = new StreamReader(graphDataPath)) {
			dataStr = sr.ReadToEnd();
		}
		var graphDict = Json.Deserialize(dataStr) as Dictionary<string, object>;
		
		GraphStackController.RunStackedGraph(graphDict, string.Empty);

		var projectFolderPath = Directory.GetParent(Application.dataPath).ToString();
		var expectedExportDestPath = Path.Combine(projectFolderPath, "TestExportPlace/TestExportFor_1_14_RunStackedGraph_Sample");

		var the1stBundlePath = Path.Combine(expectedExportDestPath, "iOS/chara_0.assetbundle");
		var the2ndBundlePath = Path.Combine(expectedExportDestPath, "iOS/chara_1.assetbundle");
		var soundBundlePath = Path.Combine(expectedExportDestPath, "iOS/sounds_0.assetbundle");
		if (
			File.Exists(the1stBundlePath) && 
			File.Exists(the2ndBundlePath) &&
			File.Exists(soundBundlePath)) {
			Debug.Log("passed _1_15_RunStackedGraph_Sample");
			return;
		}

		Debug.LogError("failed to generate");
	}
}