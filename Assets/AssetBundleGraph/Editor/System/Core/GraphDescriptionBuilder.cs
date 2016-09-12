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

	public struct GraphDescription {
		public List<string> terminalNodeIds;
		public List<NodeData> allNodes;
		public List<ConnectionData> allConnections;

		public GraphDescription (List<string> terminalNodeIds, List<NodeData> allNodes, List<ConnectionData> allConnections) {
			this.terminalNodeIds = terminalNodeIds;
			this.allNodes = allNodes;
			this.allConnections = allConnections;
		}
	}

	public class GraphDescriptionBuilder {			
		public static GraphDescription BuildGraphDescriptionFromJson (Dictionary<string, object> deserializedJsonData) {
			var nodeIds = new List<string>();
			var nodesSource = deserializedJsonData[AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_NODES] as List<object>;

			var connectionsSource = deserializedJsonData[AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_CONNECTIONS] as List<object>;
			var allConnections = new List<ConnectionData>();
			foreach (var connectionSource in connectionsSource) {
				var connectionDict = connectionSource as Dictionary<string, object>;

				var connectionId = connectionDict[AssetBundleGraphSettings.CONNECTION_ID] as string;
				var connectionLabel = connectionDict[AssetBundleGraphSettings.CONNECTION_LABEL] as string;
				var fromNodeId = connectionDict[AssetBundleGraphSettings.CONNECTION_FROMNODE] as string;
				var fromNodeOutputPointId = connectionDict[AssetBundleGraphSettings.CONNECTION_FROMNODE_CONPOINT_ID] as string;
				var toNodeId = connectionDict[AssetBundleGraphSettings.CONNECTION_TONODE] as string;
				var toNodeInputPointId = connectionDict[AssetBundleGraphSettings.CONNECTION_TONODE_CONPOINT_ID] as string;
				allConnections.Add(new ConnectionData(connectionId, connectionLabel, fromNodeId, fromNodeOutputPointId, toNodeId, toNodeInputPointId));
			}

			var allNodes = new List<NodeData>();

			foreach (var nodeSource in nodesSource) {
				var nodeDict = nodeSource as Dictionary<string, object>;
				var nodeId = nodeDict[AssetBundleGraphSettings.NODE_ID] as string;
				nodeIds.Add(nodeId);

				var kindSource = nodeDict[AssetBundleGraphSettings.NODE_KIND] as string;
				var nodeKind = AssetBundleGraphSettings.NodeKindFromString(kindSource);

				var nodeName = nodeDict[AssetBundleGraphSettings.NODE_NAME] as string;

				var nodeOutputPointIdsSources = nodeDict[AssetBundleGraphSettings.NODE_OUTPUTPOINT_IDS] as List<object>;
				var outputPointIds = new List<string>();
				foreach (var nodeOutputPointIdsSource in nodeOutputPointIdsSources) {
					outputPointIds.Add(nodeOutputPointIdsSource as string);
				}

				switch (nodeKind) {
				case AssetBundleGraphSettings.NodeKind.LOADER_GUI: {
						var loadPathSource = nodeDict[AssetBundleGraphSettings.NODE_LOADER_LOAD_PATH] as Dictionary<string, object>;
						var loadPath = new Dictionary<string, string>();
						if (loadPathSource == null) {

							loadPathSource = new Dictionary<string, object>();
						}
						foreach (var platform_package_key in loadPathSource.Keys) {
							loadPath[platform_package_key] = loadPathSource[platform_package_key] as string;
						}

						allNodes.Add(
							new NodeData(
								nodeId:nodeId, 
								nodeKind:nodeKind, 
								nodeName:nodeName, 
								outputPointIds:outputPointIds,
								loadPath:loadPath
							)
						);
						break;
					}
				case AssetBundleGraphSettings.NodeKind.EXPORTER_GUI: {
						var exportPathSource = nodeDict[AssetBundleGraphSettings.NODE_EXPORTER_EXPORT_PATH] as Dictionary<string, object>;
						var exportTo = new Dictionary<string, string>();

						if (exportPathSource == null) {

							exportPathSource = new Dictionary<string, object>();
						}
						foreach (var platform_package_key in exportPathSource.Keys) {
							exportTo[platform_package_key] = exportPathSource[platform_package_key] as string;
						}

						allNodes.Add(
							new NodeData(
								nodeId:nodeId, 
								nodeKind:nodeKind, 
								nodeName:nodeName,
								outputPointIds:outputPointIds,
								exportTo:exportTo
							)
						);
						break;
					}

				case AssetBundleGraphSettings.NodeKind.FILTER_SCRIPT:
					// case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT:

				case AssetBundleGraphSettings.NodeKind.MODIFIER_GUI:
				
				case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_GUI: {
						var scriptClassName = nodeDict[AssetBundleGraphSettings.NODE_SCRIPT_CLASSNAME] as string;
						allNodes.Add(
							new NodeData(
								nodeId:nodeId, 
								nodeKind:nodeKind, 
								nodeName:nodeName, 
								outputPointIds:outputPointIds,
								scriptClassName:scriptClassName
							)
						);
						break;
					}

				case AssetBundleGraphSettings.NodeKind.FILTER_GUI: {
						var containsKeywordsSource = nodeDict[AssetBundleGraphSettings.NODE_FILTER_CONTAINS_KEYWORDS] as List<object>;
						var filterContainsKeywords = new List<string>();
						foreach (var containsKeywordSource in containsKeywordsSource) {
							filterContainsKeywords.Add(containsKeywordSource.ToString());
						}

						var containsKeytypesSource = nodeDict[AssetBundleGraphSettings.NODE_FILTER_CONTAINS_KEYTYPES] as List<object>;
						var filterContainsKeytypes = new List<string>();
						foreach (var containsKeytypeSource in containsKeytypesSource) {
							filterContainsKeytypes.Add(containsKeytypeSource.ToString());
						}

						allNodes.Add(
							new NodeData(
								nodeId:nodeId, 
								nodeKind:nodeKind, 
								nodeName:nodeName, 
								outputPointIds:outputPointIds,
								filterContainsKeywords:filterContainsKeywords,
								filterContainsKeytypes:filterContainsKeytypes
							)
						);
						break;
					}

				case AssetBundleGraphSettings.NodeKind.IMPORTSETTING_GUI: {
						var importerPackagesSource = nodeDict[AssetBundleGraphSettings.NODE_IMPORTER_PACKAGES] as Dictionary<string, object>;
						var importerPackages = new Dictionary<string, string>();

						if (importerPackagesSource == null) {

							importerPackagesSource = new Dictionary<string, object>();
						}
						foreach (var platform_package_key in importerPackagesSource.Keys) {
							importerPackages[platform_package_key] = string.Empty;
						}

						allNodes.Add(
							new NodeData(
								nodeId:nodeId, 
								nodeKind:nodeKind, 
								nodeName:nodeName,
								outputPointIds:outputPointIds,
								importerPackages:importerPackages
							)
						);
						break;
					}

				 {
						allNodes.Add(
							new NodeData(
								nodeId:nodeId, 
								nodeKind:nodeKind, 
								nodeName:nodeName,
								outputPointIds:outputPointIds
							)
						);
						break;
					}

				case AssetBundleGraphSettings.NodeKind.GROUPING_GUI: {
						var groupingKeywordSource = nodeDict[AssetBundleGraphSettings.NODE_GROUPING_KEYWORD] as Dictionary<string, object>;
						var groupingKeyword = new Dictionary<string, string>();

						if (groupingKeywordSource == null) {

							groupingKeywordSource = new Dictionary<string, object>();
						}
						foreach (var platform_package_key in groupingKeywordSource.Keys) {
							groupingKeyword[platform_package_key] = groupingKeywordSource[platform_package_key] as string;
						}

						allNodes.Add(
							new NodeData(
								nodeId:nodeId, 
								nodeKind:nodeKind, 
								nodeName:nodeName, 
								outputPointIds:outputPointIds,
								groupingKeyword:groupingKeyword
							)
						);
						break;
					}

				case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI: {
						var bundleNameTemplateSource = nodeDict[AssetBundleGraphSettings.NODE_BUNDLIZER_BUNDLENAME_TEMPLATE] as Dictionary<string, object>;
						var bundleNameTemplate = new Dictionary<string, string>();
						if (bundleNameTemplateSource == null) {
							bundleNameTemplateSource = new Dictionary<string, object>();
						}
						foreach (var platform_package_key in bundleNameTemplateSource.Keys) {
							bundleNameTemplate[platform_package_key] = bundleNameTemplateSource[platform_package_key] as string;
						}

						var variantsSource = nodeDict[AssetBundleGraphSettings.NODE_BUNDLIZER_VARIANTS] as Dictionary<string, object>;
						var variants = new Dictionary<string, string>();
						if (variantsSource == null) {
							variantsSource = new Dictionary<string, object>();
						}
						foreach (var inputPointId in variantsSource.Keys) {
							variants[inputPointId] = variantsSource[inputPointId] as string;
						}

						allNodes.Add(
							new NodeData(
								nodeId:nodeId, 
								nodeKind:nodeKind, 
								nodeName:nodeName,
								outputPointIds:outputPointIds,
								bundleNameTemplate:bundleNameTemplate,
								variants:variants
							)
						);
						break;
					}

				case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
						var enabledBundleOptionsSource = nodeDict[AssetBundleGraphSettings.NODE_BUNDLEBUILDER_ENABLEDBUNDLEOPTIONS] as Dictionary<string, object>;

						// default is empty. all settings are disabled.
						var enabledBundleOptions = new Dictionary<string, List<string>>();

						if (enabledBundleOptionsSource == null) {

							enabledBundleOptionsSource = new Dictionary<string, object>();
						}
						foreach (var platform_package_key in enabledBundleOptionsSource.Keys) {
							enabledBundleOptions[platform_package_key] = new List<string>();

							var enabledBundleOptionsListSource = enabledBundleOptionsSource[platform_package_key] as List<object>;

							// adopt enabled option.
							foreach (var enabledBundleOption in enabledBundleOptionsListSource) {
								enabledBundleOptions[platform_package_key].Add(enabledBundleOption as string);
							}
						}

						allNodes.Add(
							new NodeData(
								nodeId:nodeId, 
								nodeKind:nodeKind, 
								nodeName:nodeName, 
								outputPointIds:outputPointIds,
								enabledBundleOptions:enabledBundleOptions
							)
						);
						break;
					}

				default: {
						Debug.LogError(nodeName + " is defined as unknown kind of node. value:" + nodeKind);
						break;
					}
				}
			}

			/*
				collect node's child. for detecting endpoint of relationship.
			*/
			var nodeIdListWhichHasChild = new List<string>();

			foreach (var connection in allConnections) {
				nodeIdListWhichHasChild.Add(connection.fromNodeId);
			}
			var noChildNodeIds = nodeIds.Except(nodeIdListWhichHasChild).ToList();

			/*
				adding parentNode id x n into childNode for run up relationship from childNode.
			*/
			foreach (var connection in allConnections) {
				var targetNodes = allNodes.Where(nodeData => nodeData.nodeId == connection.toNodeId).ToList();
				foreach (var targetNode in targetNodes) {
					targetNode.AddConnectionToParent(connection);
				}
			}

			return new GraphDescription(noChildNodeIds, allNodes, allConnections);
		}
	}
}
