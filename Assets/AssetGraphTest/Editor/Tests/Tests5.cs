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

		GraphStackController.CleanCache();
		GraphStackController.CleanSetting();
		
		EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTarget.iOS);

		var dataPath = Path.Combine(Application.dataPath, "AssetGraphTest/Editor/TestData");
		var graphDataPath = Path.Combine(dataPath, "_5_0_PlatformChanging.json");
		
		// load
		var dataStr = string.Empty;
		
		using (var sr = new StreamReader(graphDataPath)) {
			dataStr = sr.ReadToEnd();
		}
		var graphDict = Json.Deserialize(dataStr) as Dictionary<string, object>;
		
		Action setup = () => {
			var EndpointNodeIdsAndNodeDatasAndConnectionDatas = GraphStackController.SerializeNodeRoute(graphDict);
		
			var endpointNodeIds = EndpointNodeIdsAndNodeDatasAndConnectionDatas.endpointNodeIds;
			var nodeDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.nodeDatas;
			var connectionDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.connectionDatas;

			var cacheDict = new Dictionary<string, List<string>>();
			var resultDict = new Dictionary<string, Dictionary<string, List<InternalAssetData>>>();

			foreach (var endNodeId in endpointNodeIds) {
				GraphStackController.SetupSerializedRoute(endNodeId, nodeDatas, connectionDatas, resultDict, cacheDict, string.Empty);
			}
		};

		Action run = () => {
			var EndpointNodeIdsAndNodeDatasAndConnectionDatas = GraphStackController.SerializeNodeRoute(graphDict);
		
			var endpointNodeIds = EndpointNodeIdsAndNodeDatasAndConnectionDatas.endpointNodeIds;
			var nodeDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.nodeDatas;
			var connectionDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.connectionDatas;

			var cacheDict = new Dictionary<string, List<string>>();
			var resultDict = new Dictionary<string, Dictionary<string, List<InternalAssetData>>>();

			foreach (var endNodeId in endpointNodeIds) {
				GraphStackController.RunSerializedRoute(endNodeId, nodeDatas, connectionDatas, resultDict, cacheDict, string.Empty);
			}
		};

		setup();
		run();

		// cache generated.
		uint before_iOSAssetGUID;
		BuildPipeline.GetCRCForAssetBundle("Assets/AssetGraph/Cache/BundleBuilt/c464cf25-acf0-4678-aae3-d598e44dcc60/iOS/chara_0.assetbundle", out before_iOSAssetGUID);

		// change platform.
		EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTarget.StandaloneOSXIntel);
		setup();
		run();


		// change platform again.
		EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTarget.iOS);

		// should cache.

		setup();
		run();
		// the GUID of assetBundle for iOS platform should be keep. = cached.

		uint after_iOSAssetGUID;
		BuildPipeline.GetCRCForAssetBundle("Assets/AssetGraph/Cache/BundleBuilt/c464cf25-acf0-4678-aae3-d598e44dcc60/iOS/chara_0.assetbundle", out after_iOSAssetGUID);

		if (after_iOSAssetGUID != before_iOSAssetGUID) Debug.LogError("failed to cache after_iOSAssetGUID:" + after_iOSAssetGUID + " before_iOSAssetGUID:" + before_iOSAssetGUID);
	}

}