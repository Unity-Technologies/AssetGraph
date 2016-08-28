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

		public static Dictionary<string, object> CreateSafeDecerializedJsonData (Dictionary<string, object> deserializedJsonData) {
			var changed = false;

			var nodesSource = deserializedJsonData[AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_NODES] as List<object>;
			var newNodes = new List<Dictionary<string, object>>();

			/*
				delete undetectable node.
			*/
			foreach (var nodeSource in nodesSource) {
				var nodeDict = nodeSource as Dictionary<string, object>;
				
				var nodeId = nodeDict[AssetBundleGraphSettings.NODE_ID] as string;

				var kindSource = nodeDict[AssetBundleGraphSettings.NODE_KIND] as string;

				var kind = AssetBundleGraphSettings.NodeKindFromString(kindSource);
				
				var nodeName = nodeDict[AssetBundleGraphSettings.NODE_NAME] as string;

				// copy all key and value to new Node data dictionary.
				var newNodeDict = new Dictionary<string, object>();
				foreach (var key in nodeDict.Keys) {
					newNodeDict[key] = nodeDict[key];
				}

				switch (kind) {
					case AssetBundleGraphSettings.NodeKind.FILTER_SCRIPT:
					// case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT:
					case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_SCRIPT: {
						var scriptClassName = nodeDict[AssetBundleGraphSettings.NODE_SCRIPT_CLASSNAME] as string;
				
						var nodeScriptInstance = Assembly.GetExecutingAssembly().CreateInstance(scriptClassName);
						
						// warn if no class found.
						if (nodeScriptInstance == null) {
							changed = true;
							Debug.LogError("Node could not be created properly because AssetBundleGraph failed to create script instance for " + scriptClassName + ". No such class found in assembly.");
							continue;
						}

						/*
							during validation, filter script receives only one group with key "0".
						*/
						if (kind == AssetBundleGraphSettings.NodeKind.FILTER_SCRIPT) {
							var outputLabelsSource = nodeDict[AssetBundleGraphSettings.NODE_OUTPUTPOINT_LABELS] as List<object>;
							var outputLabelsSet = new HashSet<string>();
							foreach (var source in outputLabelsSource) {
								outputLabelsSet.Add(source.ToString());
							}

							var latestLabels = new HashSet<string>();
							Action<string, string, Dictionary<string, List<Asset>>, List<string>> Output = (string dataSourceNodeId, string connectionLabel, Dictionary<string, List<Asset>> source, List<string> usedCache) => {
								latestLabels.Add(connectionLabel);
							};

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

							if (!outputLabelsSet.SetEquals(latestLabels)) {
								changed = true;
								newNodeDict[AssetBundleGraphSettings.NODE_OUTPUTPOINT_LABELS] = latestLabels.ToList();
							}
						}
						break;
					}

					case AssetBundleGraphSettings.NodeKind.LOADER_GUI:
					case AssetBundleGraphSettings.NodeKind.FILTER_GUI:
					case AssetBundleGraphSettings.NodeKind.IMPORTSETTING_GUI:
					case AssetBundleGraphSettings.NodeKind.MODIFIER_GUI:
					case AssetBundleGraphSettings.NodeKind.GROUPING_GUI:
					case AssetBundleGraphSettings.NodeKind.EXPORTER_GUI: {
						// nothing to do.
						break;
					}

					/*
						prefabricator GUI node with script.
					*/
					case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_GUI: {
						var scriptClassName = nodeDict[AssetBundleGraphSettings.NODE_SCRIPT_CLASSNAME] as string;
						if (string.IsNullOrEmpty(scriptClassName)) {
							Debug.LogWarning(nodeName  + ": No script name assigned.");
							break;
						}

						var nodeScriptInstance = Assembly.GetExecutingAssembly().CreateInstance(scriptClassName);
						
						// warn if no class found.
						if (nodeScriptInstance == null) {
							Debug.LogError(nodeName  + " could not be created properly because AssetBundleGraph failed to create script instance for " + scriptClassName + ". No such class found in assembly.");
						}
						break;
					}

					case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI: {
						var bundleNameTemplateSource = nodeDict[AssetBundleGraphSettings.NODE_BUNDLIZER_BUNDLENAME_TEMPLATE] as Dictionary<string, object>;
						if (bundleNameTemplateSource == null) {
							Debug.LogWarning(nodeName + " bundleNameTemplateSource is null. This could be caused because of deserialization error.");
							bundleNameTemplateSource = new Dictionary<string, object>();
						}
						foreach (var platform_package_key in bundleNameTemplateSource.Keys) {
							var platform_package_bundleNameTemplate = bundleNameTemplateSource[platform_package_key] as string;
							if (string.IsNullOrEmpty(platform_package_bundleNameTemplate)) {
								Debug.LogWarning(nodeName + " Bundle Name Template is empty. Configure this from editor.");
								break;
							}
						}
						
						var bundleUseOutputSource = nodeDict[AssetBundleGraphSettings.NODE_BUNDLIZER_USE_OUTPUT] as Dictionary<string, object>;
						if (bundleUseOutputSource == null) {
							Debug.LogWarning(nodeName + " bundleUseOutputSource is null. This could be caused because of deserialization error.");
							bundleUseOutputSource = new Dictionary<string, object>();
						}
						break;
					}

					case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
						// nothing to do.
						break;
					}

					default: {
						Debug.LogError(nodeName + " is defined as unknown kind of node. value:" + kind);
						break;
					}
				}

				newNodes.Add(newNodeDict);
			}

			/*
				delete undetectable connection.
					erase no start node connection.
					erase no end node connection.
					erase connection which label does exists in the start node.
			*/
			
			var connectionsSource = deserializedJsonData[AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_CONNECTIONS] as List<object>;
			var newConnections = new List<Dictionary<string, object>>();
			foreach (var connectionSource in connectionsSource) {
				var connectionDict = connectionSource as Dictionary<string, object>;

				var connectionLabel = connectionDict[AssetBundleGraphSettings.CONNECTION_LABEL] as string;
				var fromNodeId = connectionDict[AssetBundleGraphSettings.CONNECTION_FROMNODE] as string;
				var fromNodePointId = connectionDict[AssetBundleGraphSettings.CONNECTION_FROMNODE_CONPOINT_ID] as string;
				var toNodeId = connectionDict[AssetBundleGraphSettings.CONNECTION_TONODE] as string;
				
				// detect start node.
				var fromNodeCandidates = newNodes.Where(
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
				foreach (var candidateOutputPointIdsSource in candidateOutputPointIdsSources) candidateOutputPointIds.Add(candidateOutputPointIdsSource as string);
				if (!candidateOutputPointIdsSources.Contains(fromNodePointId)) {
					changed = true;
					continue;
				}

				// detect end node.
				var toNodeCandidates = newNodes.Where(
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

				newConnections.Add(connectionDict);
			}


			if (changed) {
				var validatedResultDict = new Dictionary<string, object>{
					{AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_LASTMODIFIED, DateTime.Now},
					{AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_NODES, newNodes},
					{AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_CONNECTIONS, newConnections}
				};
				return validatedResultDict;
			}

			return deserializedJsonData;
		}

		public static void ValidateAssertNodeOrder (AssetBundleGraphSettings.NodeKind fromKind, AssetBundleGraphSettings.NodeKind toKind) {
			switch (toKind) {
				case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
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
				case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					switch (toKind) {
						case AssetBundleGraphSettings.NodeKind.FILTER_SCRIPT:
						case AssetBundleGraphSettings.NodeKind.FILTER_GUI:
						case AssetBundleGraphSettings.NodeKind.GROUPING_GUI:
						case AssetBundleGraphSettings.NodeKind.EXPORTER_GUI: {
							// no problem.
							break;
						}

						default: {
							throw new AssetBundleGraphException("BundleBuilder only accepts output to Filter, Grouping and Exporter.");
						}
					}
					break;
				}
			}
		}

	}
}
