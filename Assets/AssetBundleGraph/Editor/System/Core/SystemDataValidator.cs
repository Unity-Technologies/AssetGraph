using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography;

/**
	static executor for AssetBundleGraph's data.
*/
namespace AssetBundleGraph {
	public class SystemDataValidator {

		/*
		 * Checks deserialized Json Data, and make some changes if necessary
		 * Returns original Json Data if there is no change necessary, and returns modified Json Data if there is some changes.
		 */
		public static Dictionary<string, object> CreateSafeDecerializedJsonData (Dictionary<string, object> deserializedJsonData) {
			var changed = false;

			var allNodesJson = deserializedJsonData[AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_NODES] as List<object>;
			var sanitizedAllNodesJson = new List<Dictionary<string, object>>();

			/*
				delete undetectable node.
			*/
			foreach (var n in allNodesJson) {
				var nodeJson = n as Dictionary<string, object>;				

				// copy all key and value to new Node data dictionary.
				var sanitizedNodeJson = new Dictionary<string, object>();
				foreach (var key in nodeJson.Keys) {
					sanitizedNodeJson[key] = nodeJson[key];
				}

				var kind = AssetBundleGraphSettings.NodeKindFromString(nodeJson[AssetBundleGraphSettings.NODE_KIND] as string);

				switch (kind) {	
				case AssetBundleGraphSettings.NodeKind.FILTER_SCRIPT:
					if(!ValidateNodeJsonDataForFilterScript(ref nodeJson, ref sanitizedNodeJson, ref changed)) {
						changed = true;
						continue;
					}
					break;			
				case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_GUI: 
					break;
				case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI: 
					if(!ValidateNodeJsonDataForBundlizer(ref nodeJson)) {
						changed = true;
						continue;
					}
					break;
				case AssetBundleGraphSettings.NodeKind.LOADER_GUI:
				case AssetBundleGraphSettings.NodeKind.FILTER_GUI:
				case AssetBundleGraphSettings.NodeKind.IMPORTSETTING_GUI:
				case AssetBundleGraphSettings.NodeKind.MODIFIER_GUI:
				case AssetBundleGraphSettings.NodeKind.GROUPING_GUI:
				case AssetBundleGraphSettings.NodeKind.EXPORTER_GUI: 
				case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI: 
						break;
				default:
					{
						var nodeName = nodeJson[AssetBundleGraphSettings.NODE_NAME] as string;
						Debug.LogError(nodeName + " is defined as unknown kind of node. value:" + kind);
						break;
					}
				}

				sanitizedAllNodesJson.Add(sanitizedNodeJson);
			}

			/*
				delete undetectable connection.
					erase no start node connection.
					erase no end node connection.
					erase connection which label does exists in the start node.
			*/
			
			var allConnectionsJson = deserializedJsonData[AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_CONNECTIONS] as List<object>;
			var sanitizedAllConnectionsJson = new List<Dictionary<string, object>>();
			foreach (var c in allConnectionsJson) {
				var connectionJson = c as Dictionary<string, object>;

				var connectionLabel = connectionJson[AssetBundleGraphSettings.CONNECTION_LABEL] as string;
				var fromNodeId 		= connectionJson[AssetBundleGraphSettings.CONNECTION_FROMNODE] as string;
				var fromNodePointId = connectionJson[AssetBundleGraphSettings.CONNECTION_FROMNODE_CONPOINT_ID] as string;
				var toNodeId 		= connectionJson[AssetBundleGraphSettings.CONNECTION_TONODE] as string;
//				var toNodePointId 	= connectionJson[AssetBundleGraphSettings.CONNECTION_TONODE_CONPOINT_ID] as string;

				// detect start node.
				var fromNodeCandidates = sanitizedAllNodesJson.Where(
					node => {
						var nodeId = node[AssetBundleGraphSettings.NODE_ID] as string;
						return nodeId == fromNodeId;
					}
					).ToList();
				if (!fromNodeCandidates.Any()) {
					changed = true;
					continue;
				}

				
				// start node should contain specific connection point.
				var candidateNode = fromNodeCandidates[0];
				var candidateOutputPointIdsSources = candidateNode[AssetBundleGraphSettings.NODE_OUTPUTPOINT_IDS] as List<object>;
				var candidateOutputPointIds = new List<string>();
				foreach (var candidateOutputPointIdsSource in candidateOutputPointIdsSources) {
					candidateOutputPointIds.Add(candidateOutputPointIdsSource as string);
				}
				if (!candidateOutputPointIdsSources.Contains(fromNodePointId)) {
					changed = true;
					continue;
				}

				// detect end node.
				var toNodeCandidates = sanitizedAllNodesJson.Where(
					node => {
						var nodeId = node[AssetBundleGraphSettings.NODE_ID] as string;
						return nodeId == toNodeId;
					}
					).ToList();
				if (!toNodeCandidates.Any()) {
					changed = true;
					continue;
				}

				// this connection has start node & end node.
				// detect connectionLabel.
				var fromNode = fromNodeCandidates[0];
				var connectionLabelsSource = fromNode[AssetBundleGraphSettings.NODE_OUTPUTPOINT_LABELS] as List<object>;
				var connectionLabels = new List<string>();
				foreach (var connectionLabelSource in connectionLabelsSource) {
					connectionLabels.Add(connectionLabelSource as string);
				}

				if (!connectionLabels.Contains(connectionLabel)) {
					changed = true;
					continue;
				}

				sanitizedAllConnectionsJson.Add(connectionJson);
			}

			if (changed) {
				var validatedResultDict = new Dictionary<string, object>{
					{AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_LASTMODIFIED, DateTime.Now},
					{AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_NODES, sanitizedAllNodesJson},
					{AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_CONNECTIONS, sanitizedAllConnectionsJson}
				};
				return validatedResultDict;
			}

			return deserializedJsonData;
		}

		public static void ValidateAssertNodeOrder (AssetBundleGraphSettings.NodeKind fromKind, AssetBundleGraphSettings.NodeKind toKind) {
			switch (toKind) {
			case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI: 
				{
					switch (fromKind) {
						case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI: {
							// no problem.
							break;
						}
						default: {
							throw new AssetBundleGraphException("BundleBuilder only accepts input from Bundlizer.");
						}
					}
					break;
				}
			}

			switch (fromKind) {
			case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI: 
				{
					switch (toKind) {
					case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI: 
						// no problem.
						break;
					default: {
							throw new AssetBundleGraphException("Bundlizer can only output to BundleBuilder.");
						}
					}
					break;
				}
			case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI: 
				{
					switch (toKind) {
						case AssetBundleGraphSettings.NodeKind.FILTER_SCRIPT:
						case AssetBundleGraphSettings.NodeKind.FILTER_GUI:
						case AssetBundleGraphSettings.NodeKind.GROUPING_GUI:
						case AssetBundleGraphSettings.NodeKind.EXPORTER_GUI: {
							// no problem.
							break;
						}

						default: {
							throw new AssetBundleGraphException("BundleBuilder can only output to Filter, Grouping and Exporter.");
						}
					}
					break;
				}
			}
		}


		private static bool ValidateNodeJsonDataForPrefabricator(ref Dictionary<string, object> nodeJson, bool pureScriptNode) {

			var nodeName = nodeJson[AssetBundleGraphSettings.NODE_NAME] as string;
			var scriptClassName = nodeJson[AssetBundleGraphSettings.NODE_SCRIPT_CLASSNAME] as string;

			if (string.IsNullOrEmpty(scriptClassName)) {
				Debug.LogWarning(nodeName  + ": No script name assigned.");
				// Node should not be removed if not pure script node
				return !pureScriptNode;
			}

			var nodeScriptInstance = Assembly.GetExecutingAssembly().CreateInstance(scriptClassName);

			if (nodeScriptInstance == null) {
				Debug.LogError(nodeName  + ": Node could not be created properly because AssetBundleGraph failed to create script instance for " + 
					scriptClassName + ". No such class found in assembly.");

				// Node should not be removed if not pure script node
				return !pureScriptNode;
			}

			return true;
		}

		private static bool ValidateNodeJsonDataForFilterScript(
			ref Dictionary<string, object> nodeJson, 
			ref Dictionary<string, object> sanitizedNodeJson, 
			ref bool isChanged) 
		{

			var nodeId 		= nodeJson[AssetBundleGraphSettings.NODE_ID] as string;
			var nodeName 	= nodeJson[AssetBundleGraphSettings.NODE_NAME] as string;
			var scriptClassName = nodeJson[AssetBundleGraphSettings.NODE_SCRIPT_CLASSNAME] as string;

			if (string.IsNullOrEmpty(scriptClassName)) {
				Debug.LogWarning(nodeName  + ": No script name assigned.");
				return false;
			}

			var nodeScriptInstance = Assembly.GetExecutingAssembly().CreateInstance(scriptClassName);
			if (nodeScriptInstance == null) {
				Debug.LogError(nodeName  + ": Node could not be created properly because AssetBundleGraph failed to create script instance for " + 
					scriptClassName + ". No such class found in assembly.");
				return false;
			}

			var outputLabelsJson = nodeJson[AssetBundleGraphSettings.NODE_OUTPUTPOINT_LABELS] as List<object>;
			var outputLabelsSet = new HashSet<string>();
			foreach (var label in outputLabelsJson) {
				outputLabelsSet.Add(label.ToString());
			}

			var labelsFromScript = new HashSet<string>();
			Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output = 
				(string dataSourceNodeId, string connectionLabel, Dictionary<string, List<Asset>> source, List<string> usedCache) => 
			{
				labelsFromScript.Add(connectionLabel);
			};

			// Setup() executed with dummy data to collect labels from derived script
			((FilterBase)nodeScriptInstance).Setup(
				nodeName,
				nodeId, 
				string.Empty,
				new Dictionary<string, List<Asset>>{
					{"0", new List<Asset>()}
				},
				new List<string>(),
				Output
			);

			if (!outputLabelsSet.SetEquals(labelsFromScript)) {
				sanitizedNodeJson[AssetBundleGraphSettings.NODE_OUTPUTPOINT_LABELS] = labelsFromScript.ToList();
				isChanged = true;
			}
			return true;
		}

		private static bool ValidateNodeJsonDataForBundlizer(ref Dictionary<string, object> nodeJson) {

			var nodeName 	= nodeJson[AssetBundleGraphSettings.NODE_NAME] as string;

			var bundleNameTemplateSource = nodeJson[AssetBundleGraphSettings.NODE_BUNDLIZER_BUNDLENAME_TEMPLATE] as Dictionary<string, object>;
			if (bundleNameTemplateSource == null) {
				Debug.LogWarning(nodeName + " bundleNameTemplateSource is null. This could be caused because of deserialization error.");
				bundleNameTemplateSource = new Dictionary<string, object>();
			}
			var variantsSource = nodeJson[AssetBundleGraphSettings.NODE_BUNDLIZER_VARIANTS] as Dictionary<string, object>;
			if (variantsSource == null) {
				Debug.LogWarning(nodeName + " variantsSource is null. This could be caused because of deserialization error.");
				variantsSource = new Dictionary<string, object>();
			}
			foreach (var platform_package_key in bundleNameTemplateSource.Keys) {
				var platform_package_bundleNameTemplate = bundleNameTemplateSource[platform_package_key] as string;
				if (string.IsNullOrEmpty(platform_package_bundleNameTemplate)) {
					Debug.LogWarning(nodeName + " Bundle Name Template is empty. Configure this from editor.");
					break;
				}
			}

			return true;
		}
	}
}
