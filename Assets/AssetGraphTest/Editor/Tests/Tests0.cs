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
		// contains 2 resources.
		var projectFolderPath = Directory.GetParent(Application.dataPath).ToString();
		var definedSourcePath = Path.Combine(projectFolderPath, "TestResources/");

		var emptySource = new List<AssetData>();

		var results = new Dictionary<string, List<AssetData>>();

		var integratedLoader = new IntegratedLoader();
		Action<string, string, List<AssetData>> Out = (string nodeId, string connectionId, List<AssetData> output) => {
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

		var emptySource = new List<AssetData>();

		var results = new Dictionary<string, List<AssetData>>();

		var integratedLoader = new IntegratedLoader();
		Action<string, string, List<AssetData>> Out = (string nodeId, string connectionId, List<AssetData> output) => {
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
		var source = new List<AssetData>{
			AssetData.AssetDataByLoader("A/1st", "A"),
			AssetData.AssetDataByLoader("A/2nd", "A")
		};

		var results = new Dictionary<string, List<AssetData>>();

		var sFilter = new SampleFilter_0();
		Action<string, string, List<AssetData>> Out = (string nodeId, string connectionId, List<AssetData> output) => {
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
		var source = new List<AssetData>{
			AssetData.AssetDataByLoader("A/1st", "A"),
			AssetData.AssetDataByLoader("A/2nd", "A")
		};

		var results = new Dictionary<string, List<AssetData>>();

		var sFilter = new SampleFilter_0();
		Action<string, string, List<AssetData>> Out = (string nodeId, string connectionId, List<AssetData> output) => {
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

		var source = new List<AssetData>{
			AssetData.AssetDataByLoader(definedSourcePath + "dummy.png", definedSourcePath),
			AssetData.AssetDataByLoader(definedSourcePath + "model/sample.fbx", definedSourcePath)
		};

		var results = new Dictionary<string, List<AssetData>>();

		var sImporter = new SampleImporter_0();
		Action<string, string, List<AssetData>> Out = (string nodeId, string connectionId, List<AssetData> output) => {
			results[connectionId] = output;
		};

		sImporter.Setup("ID_0_2_SetupImporter", "CONNECTION_0_2_SetupImporter", source, Out);
		// do nothing in this test yet.
	}
	public void _0_3_RunImporter () {
		var projectFolderPath = Directory.GetParent(Application.dataPath).ToString();
		var definedSourcePath = Path.Combine(projectFolderPath, "TestResources/");
		
		var source = new List<AssetData>{
			AssetData.AssetDataByLoader(definedSourcePath + "dummy.png", definedSourcePath),
			AssetData.AssetDataByLoader(definedSourcePath + "model/sample.fbx", definedSourcePath)
		};

		var results = new Dictionary<string, List<AssetData>>();

		var sImporter = new SampleImporter_0();
		Action<string, string, List<AssetData>> Out = (string nodeId, string connectionId, List<AssetData> output) => {
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
		var endpointNodeIdsAndNodeDatas = stack.SerializeNodeRoute(graphDict);
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
		
		var stack = new GraphStack();
		var endpointNodeIdsAndNodeDatasAndConnectionDatas = stack.SerializeNodeRoute(graphDict);

		var endPoint0 = endpointNodeIdsAndNodeDatasAndConnectionDatas.endpointNodeIds[0];
		var nodeDatas = endpointNodeIdsAndNodeDatasAndConnectionDatas.nodeDatas;
		var connectionDatas = endpointNodeIdsAndNodeDatasAndConnectionDatas.connectionDatas;

		var orderedConnectionIds = stack.RunSerializedRoute(endPoint0, nodeDatas, connectionDatas);
		
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
		
		var stack = new GraphStack();
		stack.RunStackedGraph(graphDict);
		
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


	public void _0_10_SetupExport () {
		var projectFolderPath = Directory.GetParent(Application.dataPath).ToString();
		var exportFilePath = Path.Combine(projectFolderPath, "TestExportPlace/For_0_10_SetupExport");

		// delete all if exist
		if (Directory.Exists(exportFilePath)) {
			Directory.Delete(exportFilePath, true);
		}

		Directory.CreateDirectory(exportFilePath);

		var importedPath = "Assets/AssetGraphTest/ExporterTestResource/a.png";
		var assetId = AssetDatabase.AssetPathToGUID(importedPath);

		var exportTargets = new List<AssetData>{
			AssetData.AssetDataGeneratedByImporterOrPrefabricatorOrBundlizer(importedPath, assetId),
		};
		
		var integratedExporter = new IntegratedExporter();
		Action<string, string, List<AssetData>> Out = (string nodeId, string connectionId, List<AssetData> output) => {
			
		};

		integratedExporter.exportFilePath = exportFilePath;
		integratedExporter.Setup("ID_0_10_SetupExport", "CONNECTION_0_10_SetupExport", exportTargets, Out);
		// nothing for check yet.
	}

	public void _0_11_RunExport () {
		var projectFolderPath = Directory.GetParent(Application.dataPath).ToString();
		var exportFilePath = Path.Combine(projectFolderPath, "TestExportPlace/For_0_11_RunExport");

		// delete all if exist
		if (Directory.Exists(exportFilePath)) {
			Directory.Delete(exportFilePath, true);
		}

		Directory.CreateDirectory(exportFilePath);

		var importedPath = "Assets/AssetGraphTest/ExporterTestResource/a.png";
		var assetId = AssetDatabase.AssetPathToGUID(importedPath);
		
		var exportTargets = new List<AssetData>{
			AssetData.AssetDataGeneratedByImporterOrPrefabricatorOrBundlizer(importedPath, assetId),
		};
		
		var integratedExporter = new IntegratedExporter();
		Action<string, string, List<AssetData>> Out = (string nodeId, string connectionId, List<AssetData> output) => {
			
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
		Debug.LogError("not yet");
	}
}