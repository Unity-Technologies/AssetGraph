using UnityEngine;
using UnityEditor;

using AssetGraph;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using MiniJSONForAssetGraph;

// Test

public partial class Test {

	public void _0_0_0_SetupLoader () {
		// contains 2 resources.
		var projectFolderPath = Directory.GetParent(Application.dataPath).ToString();
		var definedSourcePath = Path.Combine(projectFolderPath, "TestResources/");

		var emptySource = new List<InternalAssetData>();

		var results = new Dictionary<string, List<InternalAssetData>>();

		var integratedLoader = new IntegratedLoader();
		Action<string, string, List<InternalAssetData>> Out = (string nodeId, string connectionId, List<InternalAssetData> output) => {
			results[connectionId] = output;
		};

		integratedLoader.loadFilePath = definedSourcePath;
		integratedLoader.Setup("ID_0_0_0_SetupLoader", "CONNECTION_0_0_0_SetupLoader", emptySource, Out);

		var outputs = results["CONNECTION_0_0_0_SetupLoader"];
		if (outputs.Count == 2) {
			return;
		}

		Debug.LogError("not match 2, actual:" + outputs.Count);
	}

	public void _0_0_1_RunLoader () {
		// contains 2 resources.
		var projectFolderPath = Directory.GetParent(Application.dataPath).ToString();
		var definedSourcePath = Path.Combine(projectFolderPath, "TestResources/");

		var emptySource = new List<InternalAssetData>();

		var results = new Dictionary<string, List<InternalAssetData>>();

		var integratedLoader = new IntegratedLoader();
		Action<string, string, List<InternalAssetData>> Out = (string nodeId, string connectionId, List<InternalAssetData> output) => {
			results[connectionId] = output;
		};

		integratedLoader.loadFilePath = definedSourcePath;
		integratedLoader.Run("ID_0_0_1_RunLoader", "CONNECTION_0_0_1_RunLoader", emptySource, Out);

		var outputs = results["CONNECTION_0_0_1_RunLoader"];
		if (outputs.Count == 2) {
			return;
		}

		Debug.LogError("not match 2, actual:" + outputs.Count);
	}

	public void _0_0_SetupFilter () {
		var source = new List<InternalAssetData>{
			InternalAssetData.InternalAssetDataByLoader("A/1st", "A"),
			InternalAssetData.InternalAssetDataByLoader("A/2nd", "A")
		};

		var results = new Dictionary<string, List<InternalAssetData>>();

		var sFilter = new SampleFilter_0();
		Action<string, string, List<InternalAssetData>> Out = (string nodeId, string connectionId, List<InternalAssetData> output) => {
			results[connectionId] = output;
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
		var source = new List<InternalAssetData>{
			InternalAssetData.InternalAssetDataByLoader("A/1st", "A"),
			InternalAssetData.InternalAssetDataByLoader("A/2nd", "A")
		};

		var results = new Dictionary<string, List<InternalAssetData>>();

		var sFilter = new SampleFilter_0();
		Action<string, string, List<InternalAssetData>> Out = (string nodeId, string connectionId, List<InternalAssetData> output) => {
			results[connectionId] = output;
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
		var projectFolderPath = Directory.GetParent(Application.dataPath).ToString();
		var definedSourcePath = Path.Combine(projectFolderPath, "TestResources/");

		var source = new List<InternalAssetData>{
			InternalAssetData.InternalAssetDataByLoader(Path.Combine(definedSourcePath, "dummy.png"), definedSourcePath),
			InternalAssetData.InternalAssetDataByLoader(Path.Combine(definedSourcePath, "model/sample.fbx"), definedSourcePath)
		};

		var results = new Dictionary<string, List<InternalAssetData>>();

		var sImporter = new SampleImporter_0();
		Action<string, string, List<InternalAssetData>> Out = (string nodeId, string connectionId, List<InternalAssetData> output) => {
			results[connectionId] = output;
		};

		sImporter.Setup("ID_0_2_SetupImporter", "CONNECTION_0_2_SetupImporter", source, Out);
		// do nothing in this test yet.
	}
	public void _0_3_RunImporter () {
		var projectFolderPath = Directory.GetParent(Application.dataPath).ToString();
		var definedSourcePath = Path.Combine(projectFolderPath, "TestResources/");
		
		var source = new List<InternalAssetData>{
			InternalAssetData.InternalAssetDataByLoader(Path.Combine(definedSourcePath, "dummy.png"), definedSourcePath),
			InternalAssetData.InternalAssetDataByLoader(Path.Combine(definedSourcePath, "model/sample.fbx"), definedSourcePath)
		};

		var results = new Dictionary<string, List<InternalAssetData>>();

		var sImporter = new SampleImporter_0();
		Action<string, string, List<InternalAssetData>> Out = (string nodeId, string connectionId, List<InternalAssetData> output) => {
			results[connectionId] = output;
		};

		sImporter.Run("ID_0_3_RunImporter", "CONNECTION_0_3_RunImporter", source, Out);

		var currentOutputs = results["CONNECTION_0_3_RunImporter"];
		if (currentOutputs.Count == 3) {
			return;
		}

		Debug.LogError("failed to collect importerd resource");
	}

	public void _0_4_SetupPrefabricator () {
		var importedPath = "Assets/AssetGraphTest/PrefabricatorTestResource/SpanPath/a.png";

		var source = new List<InternalAssetData>{
			InternalAssetData.InternalAssetDataByImporter(
				"traceId_0_4_SetupPrefabricator",
				Path.Combine(Application.dataPath, importedPath),
				Application.dataPath,
				Path.GetFileName(importedPath),
				string.Empty,
				importedPath,
				AssetDatabase.AssetPathToGUID(importedPath),
				AssetGraphInternalFunctions.GetAssetType(importedPath)
			)
		};

		var results = new Dictionary<string, List<InternalAssetData>>();

		var sPrefabricator = new SamplePrefabricator_0();
		Action<string, string, List<InternalAssetData>> Out = (string nodeId, string connectionId, List<InternalAssetData> output) => {
			results[connectionId] = output;
		};

		sPrefabricator.Setup("ID_0_4_SetupPrefabricator", "CONNECTION_0_4_SetupPrefabricator", source, Out);
		// do nothing in this test yet.
	}
	public void _0_5_RunPrefabricator () {
		var importedPath = "Assets/AssetGraphTest/PrefabricatorTestResource/SpanPath/a.png";

		var source = new List<InternalAssetData>{
			InternalAssetData.InternalAssetDataByImporter(
				"traceId_0_5_RunPrefabricator",
				Path.Combine(Application.dataPath, importedPath),
				Application.dataPath,
				Path.GetFileName(importedPath),
				string.Empty,
				importedPath,
				AssetDatabase.AssetPathToGUID(importedPath),
				AssetGraphInternalFunctions.GetAssetType(importedPath)
			)
		};

		var results = new Dictionary<string, List<InternalAssetData>>();

		var sPrefabricator = new SamplePrefabricator_0();
		Action<string, string, List<InternalAssetData>> Out = (string nodeId, string connectionId, List<InternalAssetData> output) => {
			results[connectionId] = output;
		};

		sPrefabricator.Run("ID_0_5_RunPrefabricator", "CONNECTION_0_5_RunPrefabricator", source, Out);

		var currentOutputs = results["CONNECTION_0_5_RunPrefabricator"];
		if (currentOutputs.Count == 3) {
			// a.png
			// material.mat
			// prefab.prefab
			return;
		}

		Debug.LogError("failed to prefabricate");
	}

	public void _0_6_SetupBundlizer () {
		var importedPath = "Assets/AssetGraphTest/PrefabricatorTestResource/SpanPath/a.png";

		var source = new List<InternalAssetData>{
			InternalAssetData.InternalAssetDataByImporter(
				"traceId_0_6_SetupBundlizer",
				Path.Combine(Application.dataPath, importedPath),
				Application.dataPath,
				Path.GetFileName(importedPath),
				string.Empty,
				importedPath,
				AssetDatabase.AssetPathToGUID(importedPath),
				AssetGraphInternalFunctions.GetAssetType(importedPath)
			)
		};

		var results = new Dictionary<string, List<InternalAssetData>>();

		var sBundlizer = new SampleBundlizer_0();
		Action<string, string, List<InternalAssetData>> Out = (string nodeId, string connectionId, List<InternalAssetData> output) => {
			results[connectionId] = output;
		};

		sBundlizer.Setup("ID_0_6_SetupBundlizer", "CONNECTION_0_6_SetupBundlizer", source, Out);
		// do nothing in this test yet.
	}
	public void _0_7_RunBundlizer () {
		var importedPath = "Assets/AssetGraphTest/PrefabricatorTestResource/SpanPath/a.png";

		var source = new List<InternalAssetData>{
			InternalAssetData.InternalAssetDataByImporter(
				"traceId_0_7_RunBundlizer",
				Path.Combine(Application.dataPath, importedPath),
				Application.dataPath,
				Path.GetFileName(importedPath),
				string.Empty,
				importedPath,
				AssetDatabase.AssetPathToGUID(importedPath),
				AssetGraphInternalFunctions.GetAssetType(importedPath)
			)
		};

		var results = new Dictionary<string, List<InternalAssetData>>();

		var sBundlizer = new SampleBundlizer_0();
		Action<string, string, List<InternalAssetData>> Out = (string nodeId, string connectionId, List<InternalAssetData> output) => {
			results[connectionId] = output;
		};

		sBundlizer.Run("ID_0_7_RunBundlizer", "CONNECTION_0_7_RunBundlizer", source, Out);

		var currentOutputs = results["CONNECTION_0_7_RunBundlizer"];
		if (currentOutputs.Count == 1) {
			// a.bundle
			return;
		}
		
		Debug.LogError("failed to bundlize");
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
		
		var endpointNodeIdsAndNodeDatas = GraphStackController.SerializeNodeRoute(graphDict);
		if (endpointNodeIdsAndNodeDatas.endpointNodeIds.Contains("2nd_Importer")) {
			return;
		}

		Debug.LogError("not valid endpoint");
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
		
		var endpointNodeIdsAndNodeDatasAndConnectionDatas = GraphStackController.SerializeNodeRoute(graphDict);

		var endPoint0 = endpointNodeIdsAndNodeDatasAndConnectionDatas.endpointNodeIds[0];
		var nodeDatas = endpointNodeIdsAndNodeDatasAndConnectionDatas.nodeDatas;
		var connectionDatas = endpointNodeIdsAndNodeDatasAndConnectionDatas.connectionDatas;

		var resultDict = new Dictionary<string, List<InternalAssetData>>();
		var orderedConnectionIds = GraphStackController.RunSerializedRoute(endPoint0, nodeDatas, connectionDatas, resultDict);
		
		if (orderedConnectionIds.Count == 0) {
			Debug.LogError("list is empty");
			return;
		}

		if (orderedConnectionIds[0] == "ローダーからフィルタへ" &&
			orderedConnectionIds[1] == "フィルタからインポータへ") {
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
		
		GraphStackController.RunStackedGraph(graphDict);
		
		var projectFolderPath = Directory.GetParent(Application.dataPath).ToString();
		var expectedExportDestPath = Path.Combine(projectFolderPath, "TestExportPlace/For_0_9_SerializedGraphJSONByExporter");

		if (File.Exists(Path.Combine(expectedExportDestPath, "kiosk_0001.mat")) &&
			File.Exists(Path.Combine(expectedExportDestPath, "sample.fbx")) &&
			File.Exists(Path.Combine(expectedExportDestPath, "dummy.png"))
		) {
			return;
		}

		Debug.LogError("not yet");
	}


	public void _0_10_SetupExporter () {
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

		var exportTargets = new List<InternalAssetData>{
			InternalAssetData.InternalAssetDataGeneratedByImporterOrPrefabricatorOrBundlizer(importedPath, assetId, assetType),
		};
		
		var integratedExporter = new IntegratedExporter();
		Action<string, string, List<InternalAssetData>> Out = (string nodeId, string connectionId, List<InternalAssetData> output) => {
			
		};

		integratedExporter.exportFilePath = exportFilePath;
		integratedExporter.Setup("ID_0_10_SetupExport", "CONNECTION_0_10_SetupExport", exportTargets, Out);
		// nothing for check yet.
	}

	public void _0_11_RunExporter () {
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
		
		var exportTargets = new List<InternalAssetData>{
			InternalAssetData.InternalAssetDataGeneratedByImporterOrPrefabricatorOrBundlizer(importedPath, assetId, assetType),
		};
		
		var integratedExporter = new IntegratedExporter();
		Action<string, string, List<InternalAssetData>> Out = (string nodeId, string connectionId, List<InternalAssetData> output) => {
			
		};

		integratedExporter.exportFilePath = exportFilePath;
		integratedExporter.Run("ID_0_11_RunExport", "CONNECTION_0_11_RunExport", exportTargets, Out);

		var assumeedExportedFilePath = Path.Combine(exportFilePath, "a.png");

		if (File.Exists(assumeedExportedFilePath)) {
			return;
		}

		Debug.LogError("failed to export");
	}

	public void _0_12_RunStackedGraph_FullStacked () {
		var basePath = Path.Combine(Application.dataPath, "AssetGraphTest/Editor/TestData");
		var graphDataPath = Path.Combine(basePath, "_0_12_RunStackedGraph_FullStacked.json");
		
		// load
		var dataStr = string.Empty;
		
		using (var sr = new StreamReader(graphDataPath)) {
			dataStr = sr.ReadToEnd();
		}
		var graphDict = Json.Deserialize(dataStr) as Dictionary<string, object>;
		
		GraphStackController.RunStackedGraph(graphDict);
		
		var projectFolderPath = Directory.GetParent(Application.dataPath).ToString();
		var expectedExportDestPath = Path.Combine(projectFolderPath, "TestExportPlace/TestExportFor_0_12_SerializedGraphJSON");

		var the1stBundlePath = Path.Combine(expectedExportDestPath, "chara0/chara0.assetbundle");
		var the2ndBundlePath = Path.Combine(expectedExportDestPath, "chara1/chara1.assetbundle");
		if (File.Exists(the1stBundlePath) && File.Exists(the2ndBundlePath)) {
			return;
		}

		Debug.LogError("failed to generate");
	}

	public void _0_13_SetupStackedGraph_FullStacked () {
		var basePath = Path.Combine(Application.dataPath, "AssetGraphTest/Editor/TestData");
		var graphDataPath = Path.Combine(basePath, "_0_12_RunStackedGraph_FullStacked.json");
		
		// load
		var dataStr = string.Empty;
		
		using (var sr = new StreamReader(graphDataPath)) {
			dataStr = sr.ReadToEnd();
		}
		var graphDict = Json.Deserialize(dataStr) as Dictionary<string, object>;
		
		var resultDict = GraphStackController.SetupStackedGraph(graphDict);
		
		foreach (var connectionId in resultDict.Keys) {
			var sourceArray = resultDict[connectionId].ToArray();
			var str = string.Join("\n	source:", sourceArray);
			Debug.Log("connectionId:" + connectionId + "\n	source:" + str);
		}

		if (resultDict.Count == 11) {
			return;
		}

		Debug.LogError("shortage of connections");
	}

	public void _0_14_SetupStackedGraph_Sample () {
		var basePath = Path.Combine(Application.dataPath, "AssetGraphTest/Editor/TestData");
		var graphDataPath = Path.Combine(basePath, "_0_14_RunStackedGraph_Sample.json");
		
		// load
		var dataStr = string.Empty;
		
		using (var sr = new StreamReader(graphDataPath)) {
			dataStr = sr.ReadToEnd();
		}
		var graphDict = Json.Deserialize(dataStr) as Dictionary<string, object>;
		
		var setupResultDict = GraphStackController.SetupStackedGraph(graphDict);
		
		foreach (var connectionId in setupResultDict.Keys) {
			var sourceArray = setupResultDict[connectionId].ToArray();
			if (0 < sourceArray.Length) {
				var str = string.Join("\n	source:", sourceArray);
				Debug.Log("connectionId:" + connectionId + "\n	source:" + str);	
			} else {
				Debug.Log("connectionId:" + connectionId + " is empty.");
			}
		}
	}

	public void _0_15_RunStackedGraph_Sample () {
		var basePath = Path.Combine(Application.dataPath, "AssetGraphTest/Editor/TestData");
		var graphDataPath = Path.Combine(basePath, "_0_14_RunStackedGraph_Sample.json");
		
		// load
		var dataStr = string.Empty;
		
		using (var sr = new StreamReader(graphDataPath)) {
			dataStr = sr.ReadToEnd();
		}
		var graphDict = Json.Deserialize(dataStr) as Dictionary<string, object>;
		
		GraphStackController.RunStackedGraph(graphDict);
		
		var projectFolderPath = Directory.GetParent(Application.dataPath).ToString();
		var expectedExportDestPath = Path.Combine(projectFolderPath, "TestExportPlace/TestExportFor_0_14_RunStackedGraph_Sample");

		var the1stBundlePath = Path.Combine(expectedExportDestPath, "chara_0.assetbundle");
		var the2ndBundlePath = Path.Combine(expectedExportDestPath, "chara_1.assetbundle");
		var soundBundlePath = Path.Combine(expectedExportDestPath, "sounds.assetbundle");
		if (
			File.Exists(the1stBundlePath) && 
			File.Exists(the2ndBundlePath) &&
			File.Exists(soundBundlePath)) {
			return;
		}

		Debug.LogError("failed to generate");
	}
}