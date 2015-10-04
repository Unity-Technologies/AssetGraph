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
		// contains 2 resources.
		var projectFolderPath = Directory.GetParent(Application.dataPath).ToString();
		var definedSourcePath = Path.Combine(projectFolderPath, "TestResources/");

		var emptySource = new Dictionary<string, List<InternalAssetData>>();

		var results = new Dictionary<string, List<InternalAssetData>>();

		var integratedGUILoader = new IntegratedGUILoader(definedSourcePath);
		Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Out = (string nodeId, string connectionId, Dictionary<string, List<InternalAssetData>> output, List<string> cached) => {
			results[connectionId] = output["0"];
		};

		integratedGUILoader.Setup("ID_1_0_0_SetupLoader", "CONNECTION_1_0_0_SetupLoader", emptySource, new List<string>(), Out);

		var outputs = results["CONNECTION_1_0_0_SetupLoader"];
		if (outputs.Count == 2) {
			Debug.Log("passed _1_0_0_SetupLoader");
			return;
		}

		Debug.LogError("not match 2, actual:" + outputs.Count);
	}

	public void _1_0_1_RunLoader () {
		// contains 2 resources.
		var projectFolderPath = Directory.GetParent(Application.dataPath).ToString();
		var definedSourcePath = Path.Combine(projectFolderPath, "TestResources/");

		var emptySource = new Dictionary<string, List<InternalAssetData>>();

		var results = new Dictionary<string, List<InternalAssetData>>();

		var integratedGUILoader = new IntegratedGUILoader(definedSourcePath);
		Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Out = (string nodeId, string connectionId, Dictionary<string, List<InternalAssetData>> output, List<string> cached) => {
			results[connectionId] = output["0"];
		};

		integratedGUILoader.Run("ID_1_0_1_RunLoader", "CONNECTION_1_0_1_RunLoader", emptySource, new List<string>(), Out);

		var outputs = results["CONNECTION_1_0_1_RunLoader"];
		if (outputs.Count == 2) {
			Debug.Log("passed _1_0_1_RunLoader");
			return;
		}

		Debug.LogError("not match 2, actual:" + outputs.Count);
	}

	public void _1_0_SetupFilter () {
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

		integratedGUIFilter.Setup("ID_1_0_SetupFilter", "CONNECTION_1_0_SetupFilter", source, new List<string>(), Out);

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

		integratedGUIFilter.Run("ID_1_1_RunFilter", "CONNECTION_1_1_RunFilter", source, new List<string>(), Out);

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
		var projectFolderPath = Directory.GetParent(Application.dataPath).ToString();
		var definedSourcePath = Path.Combine(projectFolderPath, "TestResources/");

		var source = new Dictionary<string, List<InternalAssetData>> {
			{"0", 
				new List<InternalAssetData> {
					InternalAssetData.InternalAssetDataByLoader(Path.Combine(definedSourcePath, "dummy.png"), definedSourcePath),
					InternalAssetData.InternalAssetDataByLoader(Path.Combine(definedSourcePath, "model/sample.fbx"), definedSourcePath)
				}
			}
		};

		var results = new Dictionary<string, List<InternalAssetData>>();

		var integratedGUIImporter = new IntegratedGUIImporter();
		Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Out = (string nodeId, string connectionId, Dictionary<string, List<InternalAssetData>> output, List<string> cached) => {
			results[connectionId] = output["0"];
		};

		integratedGUIImporter.Setup("ID_1_2_SetupImporter", "CONNECTION_1_2_SetupImporter", source, new List<string>(), Out);
		Debug.Log("passed _1_2_SetupImporter");
	}
	public void _1_3_RunImporter () {
		var projectFolderPath = Directory.GetParent(Application.dataPath).ToString();
		var definedSourcePath = Path.Combine(projectFolderPath, "TestResources/");
		
		var source = new Dictionary<string, List<InternalAssetData>> {
			{"0", 
				new List<InternalAssetData> {
					InternalAssetData.InternalAssetDataByLoader(Path.Combine(definedSourcePath, "dummy.png"), definedSourcePath),
					InternalAssetData.InternalAssetDataByLoader(Path.Combine(definedSourcePath, "model/sample.fbx"), definedSourcePath)
				}
			}
		};

		var results = new Dictionary<string, List<InternalAssetData>>();

		var integratedGUIImporter = new IntegratedGUIImporter();
		Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Out = (string nodeId, string connectionId, Dictionary<string, List<InternalAssetData>> output, List<string> cached) => {
			results[connectionId] = output["0"];
		};

		integratedGUIImporter.Run("ID_1_3_RunImporter", "CONNECTION_1_3_RunImporter", source, new List<string>(), Out);

		var currentOutputs = results["CONNECTION_1_3_RunImporter"];
		if (currentOutputs.Count == 3) {
			Debug.Log("passed _1_3_RunImporter");
			return;
		}

		Debug.LogError("failed to collect importerd resource");
	}

	// there is no GUI Prefabricator.

	public void _1_6_SetupBundlizer () {
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

		integratedGUIBundlizer.Setup("ID_1_6_SetupBundlizer", "CONNECTION_1_6_SetupBundlizer", source, new List<string>(), Out);
		Debug.Log("passed _1_6_SetupBundlizer");
	}
	public void _1_7_RunBundlizer () {
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

		integratedGUIBundlizer.Run("ID_1_7_RunBundlizer", "CONNECTION_1_7_RunBundlizer", source, new List<string>(), Out);

		var currentOutputs = results["CONNECTION_1_7_RunBundlizer"];
		if (currentOutputs.Count == 1) {
			// should be a_0.bundle
			if (currentOutputs[0].pathUnderConnectionId != "a_0.bundle") {
				Debug.LogError("failed to bundlize, name not match:" + currentOutputs[0].pathUnderConnectionId);
				return;
			}

			Debug.Log("passed _1_7_RunBundlizer");
			return;
		}
		
		Debug.LogError("failed to bundlize");
	}

	public void _1_8_0_SerializeGraph_hasValidEndpoint () {
		var basePath = Path.Combine(Application.dataPath, "AssetGraphTest/Editor/TestData");
		var graphDataPath = Path.Combine(basePath, "_1_8_SerializeGraph.json");
		
		// load
		var dataStr = string.Empty;
		
		using (var sr = new StreamReader(graphDataPath)) {
			dataStr = sr.ReadToEnd();
		}

		var graphDict = Json.Deserialize(dataStr) as Dictionary<string, object>;
		
		var endpointNodeIdsAndNodeDatas = GraphStackController.SerializeNodeRoute(graphDict);
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
		
		var endpointNodeIdsAndNodeDatasAndConnectionDatas = GraphStackController.SerializeNodeRoute(graphDict);

		var endPoint0 = endpointNodeIdsAndNodeDatasAndConnectionDatas.endpointNodeIds[0];
		var nodeDatas = endpointNodeIdsAndNodeDatasAndConnectionDatas.nodeDatas;
		var connectionDatas = endpointNodeIdsAndNodeDatasAndConnectionDatas.connectionDatas;

		var resultDict = new Dictionary<string, Dictionary<string, List<InternalAssetData>>>();
		var cacheDict = new Dictionary<string, List<string>>();
		var orderedConnectionIds = GraphStackController.RunSerializedRoute(endPoint0, nodeDatas, connectionDatas, resultDict, cacheDict);
		
		if (orderedConnectionIds.Count == 0) {
			Debug.LogError("list is empty");
			return;
		}

		if (orderedConnectionIds[0] == "ローダーからフィルタへ" &&
			orderedConnectionIds[1] == "フィルタからインポータへ") {
			Debug.Log("passed _1_8_1_SerializeGraph_hasValidOrder");
			return;
		}

		Debug.LogError("failed to validate order");
	}

	public void _1_9_RunStackedGraph () {
		var basePath = Path.Combine(Application.dataPath, "AssetGraphTest/Editor/TestData");
		var graphDataPath = Path.Combine(basePath, "_1_9_RunStackedGraph.json");
		
		// load
		var dataStr = string.Empty;
		
		using (var sr = new StreamReader(graphDataPath)) {
			dataStr = sr.ReadToEnd();
		}
		var graphDict = Json.Deserialize(dataStr) as Dictionary<string, object>;
		
		GraphStackController.RunStackedGraph(graphDict);
		
		var projectFolderPath = Directory.GetParent(Application.dataPath).ToString();
		var expectedExportDestPath = Path.Combine(projectFolderPath, "TestExportPlace/For_0_9_SerializedGraphJSONByExporter");

		if (File.Exists(Path.Combine(expectedExportDestPath, "kiosk_0001.mat")) &&
			File.Exists(Path.Combine(expectedExportDestPath, "sample.fbx")) &&
			File.Exists(Path.Combine(expectedExportDestPath, "dummy.png"))
		) {
			Debug.Log("passed _1_9_RunStackedGraph");
			return;
		}

		Debug.LogError("not yet");
	}


	public void _1_10_SetupExporter () {
		var projectFolderPath = Directory.GetParent(Application.dataPath).ToString();
		var exportFilePath = Path.Combine(projectFolderPath, "TestExportPlace/For_0_10_SetupExport");

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
					InternalAssetData.InternalAssetDataGeneratedByImporterOrPrefabricator(importedPath, assetId, assetType),
				}
			}
		};
		
		var integratedScriptExporter = new IntegratedScriptExporter(exportFilePath);
		Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Out = (string nodeId, string connectionId, Dictionary<string, List<InternalAssetData>> output, List<string> cached) => {
			
		};

		integratedScriptExporter.Setup("ID_0_10_SetupExport", "CONNECTION_0_10_SetupExport", exportTargets, new List<string>(), Out);
		Debug.Log("passed _1_10_SetupExporter");
	}

	public void _1_11_RunExporter () {
		var projectFolderPath = Directory.GetParent(Application.dataPath).ToString();
		var exportFilePath = Path.Combine(projectFolderPath, "TestExportPlace/For_0_11_RunExport");

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
					InternalAssetData.InternalAssetDataGeneratedByImporterOrPrefabricator(importedPath, assetId, assetType),
				}
			}
		};
		
		var integratedScriptExporter = new IntegratedScriptExporter(exportFilePath);
		Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Out = (string nodeId, string connectionId, Dictionary<string, List<InternalAssetData>> output, List<string> cached) => {
			
		};

		integratedScriptExporter.Run("ID_0_11_RunExport", "CONNECTION_0_11_RunExport", exportTargets, new List<string>(), Out);

		var assumeedExportedFilePath = Path.Combine(exportFilePath, "a.png");

		if (File.Exists(assumeedExportedFilePath)) {
			Debug.Log("passed _1_11_RunExporter");
			return;
		}

		Debug.LogError("failed to export");
	}
}