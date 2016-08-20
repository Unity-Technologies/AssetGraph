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
	public class GraphStackController {
		public struct EndpointNodeIdsAndNodeDatasAndConnectionDatas {
			public List<string> endpointNodeIds;
			public List<NodeData> nodeDatas;
			public List<ConnectionData> connectionDatas;

			public EndpointNodeIdsAndNodeDatasAndConnectionDatas (List<string> endpointNodeIds, List<NodeData> nodeDatas, List<ConnectionData> connectionDatas) {
				this.endpointNodeIds = endpointNodeIds;
				this.nodeDatas = nodeDatas;
				this.connectionDatas = connectionDatas;
			}
		}

		/**
			check if cache is exist at local path.
		*/
		public static bool IsCached (InternalAssetData relatedAsset, List<string> alreadyCachedPath, string localAssetPath) {
			if (alreadyCachedPath.Contains(localAssetPath)) {
				// if source is exists, check hash.
				var sourceHash = GetHash(relatedAsset.absoluteSourcePath);
				var destHash = GetHash(localAssetPath);

				// completely hit.
				if (sourceHash.SequenceEqual(destHash)) {
					return true;
				}
			}

			return false;
		}

		/**
			check if cache is exist and nothing changes.
		*/
		public static bool IsCachedForEachSource (List<InternalAssetData> relatedAssets, List<string> alreadyCachedPath, string localAssetPath) {
			// check prefab-out file is exist or not.
			if (alreadyCachedPath.Contains(localAssetPath)) {
				
				// cached. check if 
				var changed = false;
				foreach (var relatedAsset in relatedAssets) {
					if (relatedAsset.isNew) {
						changed = true;
						break;
					}
				}
				
				if (changed) return false;
				return true;
			}
			return false;
		}

		public static byte[] GetHash (string filePath) {
			using (var md5 = MD5.Create()) {
				using (var stream = File.OpenRead(filePath)) {
					return md5.ComputeHash(stream);
				}
			}
		}

		public static List<string> CreateCustomFilterInstanceForScript (string scriptClassName) {
			var nodeScriptInstance = Assembly.LoadFile("Library/ScriptAssemblies/Assembly-CSharp-Editor.dll").CreateInstance(scriptClassName);
			if (nodeScriptInstance == null) {
				Debug.LogError("Failed to create instance for " + scriptClassName + ". No such class found in assembly.");
				return new List<string>();
			}

			var labels = new List<string>();
			Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output = (string dataSourceNodeId, string connectionLabel, Dictionary<string, List<InternalAssetData>> source, List<string> usedCache) => {
				labels.Add(connectionLabel);
			};

			((FilterBase)nodeScriptInstance).Setup(
				"GetLabelsFromSetupFilter_dummy_nodeName",
				"GetLabelsFromSetupFilter_dummy_nodeId", 
				string.Empty,
				new Dictionary<string, List<InternalAssetData>>{
					{"0", new List<InternalAssetData>()}
				},
				new List<string>(),
				Output
			);
			return labels;

		}

		public static Dictionary<string, object> ValidateStackedGraph (Dictionary<string, object> graphDataDict) {
			var changed = false;


			var nodesSource = graphDataDict[AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_NODES] as List<object>;
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
							Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output = (string dataSourceNodeId, string connectionLabel, Dictionary<string, List<InternalAssetData>> source, List<string> usedCache) => {
								latestLabels.Add(connectionLabel);
							};

							((FilterBase)nodeScriptInstance).Setup(
								nodeName,
								nodeId, 
								string.Empty,
								new Dictionary<string, List<InternalAssetData>>{
									{"0", new List<InternalAssetData>()}
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
			
			var connectionsSource = graphDataDict[AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_CONNECTIONS] as List<object>;
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

			return graphDataDict;
		}
		
		public static Dictionary<string, Dictionary<string, List<ThroughputAsset>>> SetupStackedGraph (Dictionary<string, object> graphDataDict) {
			var endpointNodeIdsAndNodeDatasAndConnectionDatas = SerializeNodeRoute(graphDataDict);
			
			var endpointNodeIds = endpointNodeIdsAndNodeDatasAndConnectionDatas.endpointNodeIds;
			var nodeDatas = endpointNodeIdsAndNodeDatasAndConnectionDatas.nodeDatas;
			var connectionDatas = endpointNodeIdsAndNodeDatasAndConnectionDatas.connectionDatas;

			/*
				node names should not overlapping.
			*/
			{
				var nodeNames = nodeDatas.Select(node => node.nodeName).ToList();
				var overlappings = nodeNames.GroupBy(x => x)
					.Where(group => 1 < group.Count())
					.Select(group => group.Key)
					.ToList();

				if (overlappings.Any()) {
					throw new AssetBundleGraphException("Duplicate node name found:" + overlappings[0] + " please rename and avoid same name.");
				}
			}

			var resultDict = new Dictionary<string, Dictionary<string, List<InternalAssetData>>>();
			var cacheDict = new Dictionary<string, List<string>>();

			foreach (var endNodeId in endpointNodeIds) {
				SetupSerializedRoute(endNodeId, nodeDatas, connectionDatas, resultDict, cacheDict);
			}
			
			return ConId_Group_Throughput(resultDict);
		}

		public static Dictionary<string, Dictionary<string, List<ThroughputAsset>>> RunStackedGraph (
			Dictionary<string, object> graphDataDict, 
			Action<string, float> updateHandler=null
		) {
			IntegratedGUIBundleBuilder.RemoveAllAssetBundleSettings();
			
			var EndpointNodeIdsAndNodeDatasAndConnectionDatas = SerializeNodeRoute(graphDataDict);
			
			var endpointNodeIds = EndpointNodeIdsAndNodeDatasAndConnectionDatas.endpointNodeIds;
			var nodeDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.nodeDatas;
			var connectionDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.connectionDatas;

			var resultDict = new Dictionary<string, Dictionary<string, List<InternalAssetData>>>();
			var cacheDict = new Dictionary<string, List<string>>();

			foreach (var endNodeId in endpointNodeIds) {
				RunSerializedRoute(endNodeId, nodeDatas, connectionDatas, resultDict, cacheDict, updateHandler);
			}

			return ConId_Group_Throughput(resultDict);
		}

		private static Dictionary<string, Dictionary<string, List<ThroughputAsset>>> ConId_Group_Throughput (Dictionary<string, Dictionary<string, List<InternalAssetData>>> sourceConId_Group_Throughput) {
			var result = new Dictionary<string, Dictionary<string, List<ThroughputAsset>>>();
			foreach (var connectionId in sourceConId_Group_Throughput.Keys) {
				var connectionGroupDict = sourceConId_Group_Throughput[connectionId];
				
				var newConnectionGroupDict = new Dictionary<string, List<ThroughputAsset>>();
				foreach (var groupKey in connectionGroupDict.Keys) {
					var connectionThroughputList = connectionGroupDict[groupKey];

					var sourcePathList = new List<ThroughputAsset>();
					foreach (var assetData in connectionThroughputList) {
						var bundled = assetData.isBundled;
						
						if (!string.IsNullOrEmpty(assetData.importedPath)) {
							sourcePathList.Add(new ThroughputAsset(assetData.importedPath, bundled));
							continue;
						} 
						
						if (!string.IsNullOrEmpty(assetData.absoluteSourcePath)) {
							var relativeAbsolutePath = assetData.absoluteSourcePath.Replace(ProjectPathWithSlash(), string.Empty);
							sourcePathList.Add(new ThroughputAsset(relativeAbsolutePath, bundled));
							continue;
						}

						if (!string.IsNullOrEmpty(assetData.exportedPath)) {
							sourcePathList.Add(new ThroughputAsset(assetData.exportedPath, bundled));
							continue;
						}
					}
					newConnectionGroupDict[groupKey] = sourcePathList;
				}
				result[connectionId] = newConnectionGroupDict;
			}
			return result;
		}

		private static string ProjectPathWithSlash () {
			var assetPath = Application.dataPath;
			return Directory.GetParent(assetPath).ToString() + AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR;
		}
		
		public static EndpointNodeIdsAndNodeDatasAndConnectionDatas SerializeNodeRoute (Dictionary<string, object> graphDataDict) {
			var nodeIds = new List<string>();
			var nodesSource = graphDataDict[AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_NODES] as List<object>;
			
			var connectionsSource = graphDataDict[AssetBundleGraphSettings.ASSETBUNDLEGRAPH_DATA_CONNECTIONS] as List<object>;
			var connections = new List<ConnectionData>();
			foreach (var connectionSource in connectionsSource) {
				var connectionDict = connectionSource as Dictionary<string, object>;
				
				var connectionId = connectionDict[AssetBundleGraphSettings.CONNECTION_ID] as string;
				var connectionLabel = connectionDict[AssetBundleGraphSettings.CONNECTION_LABEL] as string;
				var fromNodeId = connectionDict[AssetBundleGraphSettings.CONNECTION_FROMNODE] as string;
				var fromNodeOutputPointId = connectionDict[AssetBundleGraphSettings.CONNECTION_FROMNODE_CONPOINT_ID] as string;
				var toNodeId = connectionDict[AssetBundleGraphSettings.CONNECTION_TONODE] as string;
				connections.Add(new ConnectionData(connectionId, connectionLabel, fromNodeId, fromNodeOutputPointId, toNodeId));
			}

			var nodeDatas = new List<NodeData>();

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
						foreach (var platform_package_key in loadPathSource.Keys) loadPath[platform_package_key] = loadPathSource[platform_package_key] as string;

						nodeDatas.Add(
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
						var exportPath = new Dictionary<string, string>();

						if (exportPathSource == null) {
							
							exportPathSource = new Dictionary<string, object>();
						}
						foreach (var platform_package_key in exportPathSource.Keys) exportPath[platform_package_key] = exportPathSource[platform_package_key] as string;

						nodeDatas.Add(
							new NodeData(
								nodeId:nodeId, 
								nodeKind:nodeKind, 
								nodeName:nodeName,
								outputPointIds:outputPointIds,
								exportPath:exportPath
							)
						);
						break;
					}

					case AssetBundleGraphSettings.NodeKind.FILTER_SCRIPT:
					// case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT:

					case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
					case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_GUI: {
						var scriptClassName = nodeDict[AssetBundleGraphSettings.NODE_SCRIPT_CLASSNAME] as string;
						nodeDatas.Add(
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
						
						nodeDatas.Add(
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
						foreach (var platform_package_key in importerPackagesSource.Keys) importerPackages[platform_package_key] = string.Empty;
						
						nodeDatas.Add(
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

					case AssetBundleGraphSettings.NodeKind.MODIFIER_GUI: {
						var modifierPackagesSource = nodeDict[AssetBundleGraphSettings.NODE_MODIFIER_PACKAGES] as Dictionary<string, object>;
						var modifierPackages = new Dictionary<string, string>();

						if (modifierPackagesSource == null) {
							
							modifierPackagesSource = new Dictionary<string, object>();
						}
						foreach (var platform_package_key in modifierPackagesSource.Keys) {
							modifierPackages[platform_package_key] = string.Empty;
						}
						
						nodeDatas.Add(
							new NodeData(
								nodeId:nodeId, 
								nodeKind:nodeKind, 
								nodeName:nodeName,
								outputPointIds:outputPointIds,
								modifierPackages:modifierPackages
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
						foreach (var platform_package_key in groupingKeywordSource.Keys) groupingKeyword[platform_package_key] = groupingKeywordSource[platform_package_key] as string;
						
						nodeDatas.Add(
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
						foreach (var platform_package_key in bundleNameTemplateSource.Keys) bundleNameTemplate[platform_package_key] = bundleNameTemplateSource[platform_package_key] as string;
						
						var bundleUseOutputSource = nodeDict[AssetBundleGraphSettings.NODE_BUNDLIZER_USE_OUTPUT] as Dictionary<string, object>;
						var bundleUseOutput = new Dictionary<string, string>();
						if (bundleUseOutputSource == null) {
							bundleUseOutputSource = new Dictionary<string, object>();
						}
						foreach (var platform_package_key in bundleUseOutputSource.Keys) bundleUseOutput[platform_package_key] = bundleUseOutputSource[platform_package_key] as string;
						
						nodeDatas.Add(
							new NodeData(
								nodeId:nodeId, 
								nodeKind:nodeKind, 
								nodeName:nodeName,
								outputPointIds:outputPointIds,
								bundleNameTemplate:bundleNameTemplate,
								bundleUseOutput:bundleUseOutput
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
						
						nodeDatas.Add(
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

			foreach (var connection in connections) {
				nodeIdListWhichHasChild.Add(connection.fromNodeId);
			}
			var noChildNodeIds = nodeIds.Except(nodeIdListWhichHasChild).ToList();

			/*
				adding parentNode id x n into childNode for run up relationship from childNode.
			*/
			foreach (var connection in connections) {
				var targetNodes = nodeDatas.Where(nodeData => nodeData.nodeId == connection.toNodeId).ToList();
				foreach (var targetNode in targetNodes) {
					targetNode.AddConnectionData(connection);
				}
			}
			
			return new EndpointNodeIdsAndNodeDatasAndConnectionDatas(noChildNodeIds, nodeDatas, connections);
		}

		/**
			setup all serialized nodes in order.
			returns orderd connectionIds
		*/
		public static List<string> SetupSerializedRoute (
			string endNodeId, 
			List<NodeData> nodeDatas, 
			List<ConnectionData> connections, 
			Dictionary<string, Dictionary<string, List<InternalAssetData>>> resultDict,
			Dictionary<string, List<string>> cacheDict
		) {
			ExecuteParent(endNodeId, nodeDatas, connections, resultDict, cacheDict, new List<string>(), false);
			return resultDict.Keys.ToList();
		}

		/**
			run all serialized nodes in order.
			returns orderd connectionIds
		*/
		public static List<string> RunSerializedRoute (
			string endNodeId, 
			List<NodeData> nodeDatas, 
			List<ConnectionData> connections, 
			Dictionary<string, Dictionary<string, List<InternalAssetData>>> resultDict,
			Dictionary<string, List<string>> cacheDict,
			Action<string, float> updateHandler=null
		) {

			ExecuteParent(endNodeId, nodeDatas, connections, resultDict, cacheDict, new List<string>(), true, updateHandler);
			return resultDict.Keys.ToList();
		}

		/**
			execute Run or Setup for each nodes in order.
		*/
		private static void ExecuteParent (
			string nodeId, 
			List<NodeData> nodeDatas, 
			List<ConnectionData> connectionDatas, 
			Dictionary<string, Dictionary<string, List<InternalAssetData>>> resultDict, 
			Dictionary<string, List<string>> cachedDict,
			List<string> usedConnectionIds,
			bool isActualRun,
			Action<string, float> updateHandler=null
		) {
			var currentNodeDatas = nodeDatas.Where(relation => relation.nodeId == nodeId).ToList();
			if (!currentNodeDatas.Any()) {
				return;
			}

			var currentNodeData = currentNodeDatas[0];

			if (currentNodeData.IsAlreadyDone()) {
				return;
			}

			var nodeName = currentNodeData.nodeName;
			var nodeKind = currentNodeData.nodeKind;
			
			/*
				run parent nodes of this node.
				search connections which are incoming to this node.
				that connection has information of parent node.
			*/
			foreach (var connectionDataOfParent in currentNodeData.connectionDataOfParents) {
				var fromNodeId = connectionDataOfParent.fromNodeId;
				var usedConnectionId = connectionDataOfParent.connectionId;

				if (usedConnectionIds.Contains(usedConnectionId)) {
					throw new NodeException("connection loop detected.", fromNodeId);
				}
				
				usedConnectionIds.Add(usedConnectionId);
				
				var parentNode = nodeDatas.Where(node => node.nodeId == fromNodeId).ToList();
				if (!parentNode.Any()) {
					return;
				}

				var parentNodeKind = parentNode[0].nodeKind;

				// check node kind order.
				AssertNodeOrder(parentNodeKind, nodeKind);

				ExecuteParent(fromNodeId, nodeDatas, connectionDatas, resultDict, cachedDict, usedConnectionIds, isActualRun, updateHandler);
			}

			/*
				childNode: this node.
				run after parent run.
			*/

			// connections Ids from this node to child nodes. non-ordered.
			// actual running order depends on order of Node's OutputPoint order.
			var nonOrderedConnectionsFromThisNodeToChildNode = connectionDatas
				.Where(con => con.fromNodeId == nodeId)
				.ToList();
			
			var orderedNodeOutputPointIds = nodeDatas.Where(node => node.nodeId == nodeId).SelectMany(node => node.outputPointIds).ToList();

			/*
				get connection ids which is orderd by node's outputPoint-order. 
			*/
			var orderedConnectionIds = new List<string>(nonOrderedConnectionsFromThisNodeToChildNode.Count);
			foreach (var orderedNodeOutputPointId in orderedNodeOutputPointIds) {
				foreach (var nonOrderedConnectionFromThisNodeToChildNode in nonOrderedConnectionsFromThisNodeToChildNode) {
					var nonOrderedConnectionOutputPointId = nonOrderedConnectionFromThisNodeToChildNode.fromNodeOutputPointId;
					if (orderedNodeOutputPointId == nonOrderedConnectionOutputPointId) {
						orderedConnectionIds.Add(nonOrderedConnectionFromThisNodeToChildNode.connectionId);
						continue;
					} 
				} 
			}
			
			/*
				FilterNode and BundlizerNode uses specific multiple output connections.
				ExportNode does not have output.
				but all other nodes has only one output connection and uses first connection.
			*/
			var firstConnectionIdFromThisNodeToChildNode = string.Empty;
			if (orderedConnectionIds.Any()) firstConnectionIdFromThisNodeToChildNode = orderedConnectionIds[0];
			
			if (updateHandler != null) updateHandler(nodeId, 0f);

			/*
				has next node, run first time.
			*/
			
			var alreadyCachedPaths = new List<string>();
			if (cachedDict.ContainsKey(nodeId)) alreadyCachedPaths.AddRange(cachedDict[nodeId]);

			/*
				load already exist cache from node.
			*/
			alreadyCachedPaths.AddRange(GetCachedDataByNodeKind(nodeKind, nodeId));

			var inputParentResults = new Dictionary<string, List<InternalAssetData>>();
			
			var receivingConnectionIds = connectionDatas
				.Where(con => con.toNodeId == nodeId)
				.Select(con => con.connectionId)
				.ToList();

			foreach (var connecionId in receivingConnectionIds) {
				if (!resultDict.ContainsKey(connecionId)) continue;

				var result = resultDict[connecionId];
				foreach (var groupKey in result.Keys) {
					if (!inputParentResults.ContainsKey(groupKey)) inputParentResults[groupKey] = new List<InternalAssetData>();
					inputParentResults[groupKey].AddRange(result[groupKey]);	
				}
			}

			/*
				the Action which is executed from Node.
				store result data records to resultDict.
			*/
			Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output = 
				(string dataSourceNodeId, string targetConnectionId, Dictionary<string, List<InternalAssetData>> result, List<string> justCached) => 
			{
				var targetConnectionIds = connectionDatas
					.Where(con => con.connectionId == targetConnectionId)
					.Select(con => con.connectionId)
					.ToList();
				
				if (!targetConnectionIds.Any()) {
					// if next connection does not exist, no results for next.
					// save results to resultDict with this endpoint node's id.
					resultDict[dataSourceNodeId] = new Dictionary<string, List<InternalAssetData>>();
					foreach (var groupKey in result.Keys) {
						if (!resultDict[dataSourceNodeId].ContainsKey(groupKey)) {
							resultDict[dataSourceNodeId][groupKey] = new List<InternalAssetData>();
						}
						resultDict[dataSourceNodeId][groupKey].AddRange(result[groupKey]);
					}
					return;
				}
				
				if (!resultDict.ContainsKey(targetConnectionId)) {
					resultDict[targetConnectionId] = new Dictionary<string, List<InternalAssetData>>();
				}
				
				/*
					merge connection result by group key.
				*/
				foreach (var groupKey in result.Keys) {
					if (!resultDict[targetConnectionId].ContainsKey(groupKey)) {
						resultDict[targetConnectionId][groupKey] = new List<InternalAssetData>();
					}
					resultDict[targetConnectionId][groupKey].AddRange(result[groupKey]);
				}

				if (isActualRun) {
					if (!cachedDict.ContainsKey(nodeId)) {
						cachedDict[nodeId] = new List<string>();
					}
					cachedDict[nodeId].AddRange(justCached);
				}
			};
			
			try {
				if (isActualRun) {
					switch (nodeKind) {
						/*
							Scripts
						*/
						case AssetBundleGraphSettings.NodeKind.FILTER_SCRIPT: {
							var scriptClassName = currentNodeData.scriptClassName;
							var executor = Executor<FilterBase>(scriptClassName, nodeId);
							executor.Run(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}
						case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_SCRIPT: {
							var scriptClassName = currentNodeData.scriptClassName;
							var executor = Executor<PrefabricatorBase>(scriptClassName, nodeId);
							executor.Run(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}
						

						/*
							GUIs
						*/
						case AssetBundleGraphSettings.NodeKind.LOADER_GUI: {
							var currentLoadFilePath = GetCurrentPlatformPackageOrDefaultFromDict(nodeKind, currentNodeData.loadFilePath);
							var executor = new IntegratedGUILoader(WithAssetsPath(currentLoadFilePath));
							executor.Run(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

						case AssetBundleGraphSettings.NodeKind.FILTER_GUI: {
							/*
								Filter requires "outputPoint ordered exist connection Id and Fake connection Id" for
								exhausting assets by keyword and type correctly.

								outputPoint which has connection can through assets by keyword and keytype,
								also outputPoint which doesn't have connection should take assets by keyword and keytype.
							*/
							var orderedConnectionIdsAndFakeConnectionIds = new string[orderedNodeOutputPointIds.Count];
							for (var i = 0; i < orderedNodeOutputPointIds.Count; i++) {
								var orderedNodeOutputPointId = orderedNodeOutputPointIds[i];
								
								foreach (var nonOrderedConnectionFromThisNodeToChildNode in nonOrderedConnectionsFromThisNodeToChildNode) {
									var connectionOutputPointId = nonOrderedConnectionFromThisNodeToChildNode.fromNodeOutputPointId;
									if (orderedNodeOutputPointId == connectionOutputPointId) {
										orderedConnectionIdsAndFakeConnectionIds[i] = nonOrderedConnectionFromThisNodeToChildNode.connectionId;
										break;
									} else {
										orderedConnectionIdsAndFakeConnectionIds[i] = AssetBundleGraphSettings.FILTER_FAKE_CONNECTION_ID;
									}
								}
							}
							var executor = new IntegratedGUIFilter(orderedConnectionIdsAndFakeConnectionIds, currentNodeData.containsKeywords, currentNodeData.containsKeytypes);
							executor.Run(nodeName, nodeId, string.Empty, inputParentResults, alreadyCachedPaths, Output);
							break;
						}
						
						case AssetBundleGraphSettings.NodeKind.IMPORTSETTING_GUI: {
							var executor = new IntegratedGUIImportSetting();
							executor.Run(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

						case AssetBundleGraphSettings.NodeKind.MODIFIER_GUI: {
							var executor = new IntegratedGUIModifier();
							executor.Run(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}
						
						case AssetBundleGraphSettings.NodeKind.GROUPING_GUI: {
							var executor = new IntegratedGUIGrouping(GetCurrentPlatformPackageOrDefaultFromDict(nodeKind, currentNodeData.groupingKeyword));
							executor.Run(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

						case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_GUI: {
							var scriptClassName = currentNodeData.scriptClassName;
							if (string.IsNullOrEmpty(scriptClassName)) {
								Debug.LogError(nodeName + ": Classname is empty. Set valid classname. Configure valid script name from editor.");
								break;
							}
							var executor = Executor<PrefabricatorBase>(scriptClassName, nodeId);
							executor.Run(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

						case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI: {
							/*
								Bundlizer requires assetOutputConnectionId and additional resourceOutputConnectionId.
								both-connected, or both-not-connected, or one of them is connected. 4 patterns exists.
								
								Bundler Node's outputPoint [0] is always the point for assetOutputConnectionId.
								Bundler Node's outputPoint [1] is always the point for resourceOutputConnectionId.
								
								if one of these outputPoint don't have connection, use Fake connection id for correct output.

								
								unorderedConnectionId \
														----> orderedConnectionIdsAndFakeConnectionIds. 
								orderedOutputPointId  / 
							*/
							var orderedConnectionIdsAndFakeConnectionIds = new string[orderedNodeOutputPointIds.Count];
							for (var i = 0; i < orderedNodeOutputPointIds.Count; i++) {
								var orderedNodeOutputPointId = orderedNodeOutputPointIds[i];
								
								foreach (var nonOrderedConnectionFromThisNodeToChildNode in nonOrderedConnectionsFromThisNodeToChildNode) {
									var connectionOutputPointId = nonOrderedConnectionFromThisNodeToChildNode.fromNodeOutputPointId;
									if (orderedNodeOutputPointId == connectionOutputPointId) {
										orderedConnectionIdsAndFakeConnectionIds[i] = nonOrderedConnectionFromThisNodeToChildNode.connectionId;
										break;
									} else {
										orderedConnectionIdsAndFakeConnectionIds[i] = AssetBundleGraphSettings.BUNDLIZER_FAKE_CONNECTION_ID;
									}
								}
							}

							var bundleNameTemplate = GetCurrentPlatformPackageOrDefaultFromDict(nodeKind, currentNodeData.bundleNameTemplate);
							var bundleUseOutputResources = GetCurrentPlatformPackageOrDefaultFromDict(nodeKind, currentNodeData.bundleUseOutput).ToLower();
							
							var useOutputResources = false;
							var resourcesOutputConnectionId = AssetBundleGraphSettings.BUNDLIZER_FAKE_CONNECTION_ID;
							switch (bundleUseOutputResources) {
								case "true" :{
									useOutputResources = true;
									resourcesOutputConnectionId = orderedConnectionIdsAndFakeConnectionIds[1];
									break;
								}
							}
							
							var executor = new IntegratedGUIBundlizer(bundleNameTemplate, orderedConnectionIdsAndFakeConnectionIds[0], useOutputResources, resourcesOutputConnectionId);
							executor.Run(nodeName, nodeId, string.Empty, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

						case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
							var bundleOptions = GetGetCurrentPlatformPackageOrDefaultFromDictList(nodeKind, currentNodeData.enabledBundleOptions);
							var executor = new IntegratedGUIBundleBuilder(bundleOptions, nodeDatas.Select(nodeData => nodeData.nodeId).ToList());
							executor.Run(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

						case AssetBundleGraphSettings.NodeKind.EXPORTER_GUI: {
							var exportPath = GetCurrentPlatformPackageOrDefaultFromDict(nodeKind, currentNodeData.exportFilePath);
							var executor = new IntegratedGUIExporter(WithProjectPath(exportPath));
							executor.Run(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

						default: {
							Debug.LogError(nodeName + " is defined as unknown kind of node. value:" + nodeKind);
							break;
						}
					}
				} else {
					switch (nodeKind) {
						/*
							Scripts
						*/
						case AssetBundleGraphSettings.NodeKind.FILTER_SCRIPT: {
							var scriptClassName = currentNodeData.scriptClassName;
							var executor = Executor<FilterBase>(scriptClassName, nodeId);
							executor.Setup(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}
						case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_SCRIPT: {
							var scriptClassName = currentNodeData.scriptClassName;
							var executor = Executor<PrefabricatorBase>(scriptClassName, nodeId);
							executor.Setup(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}
						

						/*
							GUIs
						*/
						case AssetBundleGraphSettings.NodeKind.LOADER_GUI: {
							var currentLoadFilePath = GetCurrentPlatformPackageOrDefaultFromDict(nodeKind, currentNodeData.loadFilePath);

							var executor = new IntegratedGUILoader(WithAssetsPath(currentLoadFilePath));
							executor.Setup(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

						case AssetBundleGraphSettings.NodeKind.FILTER_GUI: {
							/*
								Filter requires "outputPoint ordered exist connection Id and Fake connection Id" for
								exhausting assets by keyword and type correctly.

								outputPoint which has connection can through assets by keyword and keytype,
								also outputPoint which doesn't have connection should take assets by keyword and keytype.
							*/
							var orderedConnectionIdsAndFakeConnectionIds = new string[orderedNodeOutputPointIds.Count];
							for (var i = 0; i < orderedNodeOutputPointIds.Count; i++) {
								var orderedNodeOutputPointId = orderedNodeOutputPointIds[i];
								
								foreach (var nonOrderedConnectionFromThisNodeToChildNode in nonOrderedConnectionsFromThisNodeToChildNode) {
									var connectionOutputPointId = nonOrderedConnectionFromThisNodeToChildNode.fromNodeOutputPointId;
									if (orderedNodeOutputPointId == connectionOutputPointId) {
										orderedConnectionIdsAndFakeConnectionIds[i] = nonOrderedConnectionFromThisNodeToChildNode.connectionId;
										break;
									} else {
										orderedConnectionIdsAndFakeConnectionIds[i] = AssetBundleGraphSettings.FILTER_FAKE_CONNECTION_ID;
									}
								}
							}
							var executor = new IntegratedGUIFilter(orderedConnectionIdsAndFakeConnectionIds, currentNodeData.containsKeywords, currentNodeData.containsKeytypes);
							executor.Setup(nodeName, nodeId, string.Empty, inputParentResults, alreadyCachedPaths, Output);
							break;
						}
						
						case AssetBundleGraphSettings.NodeKind.IMPORTSETTING_GUI: {
							var executor = new IntegratedGUIImportSetting();
							executor.Setup(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

						case AssetBundleGraphSettings.NodeKind.MODIFIER_GUI: {
							var executor = new IntegratedGUIModifier();
							executor.Setup(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}
						
						case AssetBundleGraphSettings.NodeKind.GROUPING_GUI: {
							var executor = new IntegratedGUIGrouping(GetCurrentPlatformPackageOrDefaultFromDict(nodeKind, currentNodeData.groupingKeyword));
							executor.Setup(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

						case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_GUI: {
							var scriptClassName = currentNodeData.scriptClassName;
							if (string.IsNullOrEmpty(scriptClassName)) {
								AssetBundleGraph.AddNodeException(new NodeException(nodeName + ": Classname is empty. Set valid classname.", nodeId));
//								Debug.LogError("prefabriator class at node:" + nodeName + " is empty, please set valid script type.");
								break;
							}
							try {
								var executor = Executor<PrefabricatorBase>(scriptClassName, nodeId);
								executor.Setup(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							} catch (NodeException e) {
								AssetBundleGraph.AddNodeException(e);
								break;
							}
							break;
						}

						case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI: {
							/*
								Bundlizer requires assetOutputConnectionId and additional resourceOutputConnectionId.
								both-connected, or both-not-connected, or one of them is connected. 4 patterns exists.
								
								Bundler Node's outputPoint [0] is always the point for assetOutputConnectionId.
								Bundler Node's outputPoint [1] is always the point for resourceOutputConnectionId.
								
								if one of these outputPoint don't have connection, use Fake connection id for correct output.

								
								unorderedConnectionId \
														----> orderedConnectionIdsAndFakeConnectionIds. 
								orderedOutputPointId  / 
							*/
							var orderedConnectionIdsAndFakeConnectionIds = new string[orderedNodeOutputPointIds.Count];
							for (var i = 0; i < orderedNodeOutputPointIds.Count; i++) {
								var orderedNodeOutputPointId = orderedNodeOutputPointIds[i];
								
								foreach (var nonOrderedConnectionFromThisNodeToChildNode in nonOrderedConnectionsFromThisNodeToChildNode) {
									var connectionOutputPointId = nonOrderedConnectionFromThisNodeToChildNode.fromNodeOutputPointId;
									if (orderedNodeOutputPointId == connectionOutputPointId) {
										orderedConnectionIdsAndFakeConnectionIds[i] = nonOrderedConnectionFromThisNodeToChildNode.connectionId;
										break;
									} else {
										orderedConnectionIdsAndFakeConnectionIds[i] = AssetBundleGraphSettings.BUNDLIZER_FAKE_CONNECTION_ID;
									}
								}
							}

							var bundleNameTemplate = GetCurrentPlatformPackageOrDefaultFromDict(nodeKind, currentNodeData.bundleNameTemplate);
							var bundleUseOutputResources = GetCurrentPlatformPackageOrDefaultFromDict(nodeKind, currentNodeData.bundleUseOutput).ToLower();
							
							var useOutputResources = false;
							var resourcesOutputConnectionId = AssetBundleGraphSettings.BUNDLIZER_FAKE_CONNECTION_ID;
							switch (bundleUseOutputResources) {
								case "true" :{
									useOutputResources = true;
									resourcesOutputConnectionId = orderedConnectionIdsAndFakeConnectionIds[1];
									break;
								}
							}
							
							var executor = new IntegratedGUIBundlizer(bundleNameTemplate, orderedConnectionIdsAndFakeConnectionIds[0], useOutputResources, resourcesOutputConnectionId);
							executor.Setup(nodeName, nodeId, string.Empty, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

						case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
							var bundleOptions = GetGetCurrentPlatformPackageOrDefaultFromDictList(nodeKind, currentNodeData.enabledBundleOptions);
							var executor = new IntegratedGUIBundleBuilder(bundleOptions, nodeDatas.Select(nodeData => nodeData.nodeId).ToList());
							executor.Setup(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

						case AssetBundleGraphSettings.NodeKind.EXPORTER_GUI: {
							var exportPath = GetCurrentPlatformPackageOrDefaultFromDict(nodeKind, currentNodeData.exportFilePath);
							var executor = new IntegratedGUIExporter(WithProjectPath(exportPath));
							executor.Setup(nodeName, nodeId, firstConnectionIdFromThisNodeToChildNode, inputParentResults, alreadyCachedPaths, Output);
							break;
						}

						default: {
							Debug.LogError(nodeName + " is defined as unknown kind of node. value:" + nodeKind);
							break;
						}
					}
				}
			} catch (NodeException e) {
				AssetBundleGraph.AddNodeException(e);
				//Debug.LogError("error occured:\"" + e.reason + "\", please check information on node.");
				return;
				//throw new AssetBundleGraphException(nodeName + ": " + e.reason);
			}

			currentNodeData.Done();
			if (updateHandler != null) updateHandler(nodeId, 1f);
		}

		private static void AssertNodeOrder (AssetBundleGraphSettings.NodeKind fromKind, AssetBundleGraphSettings.NodeKind toKind) {
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
		

		public static string WithProjectPath (string pathUnderProjectFolder) {
			var assetPath = Application.dataPath;
			var projectPath = Directory.GetParent(assetPath).ToString();
			return FileController.PathCombine(projectPath, pathUnderProjectFolder);
		}

		public static string WithAssetsPath (string pathUnderAssetsFolder) {
			var assetPath = Application.dataPath;
			return FileController.PathCombine(assetPath, pathUnderAssetsFolder);
		}

		public static T Executor<T> (string typeStr, string nodeId) where T : INodeBase {
			var nodeScriptInstance = Assembly.LoadFile("Library/ScriptAssemblies/Assembly-CSharp-Editor.dll").CreateInstance(typeStr);
			if (nodeScriptInstance == null) {
				throw new NodeException("failed to generate class information of class:" + typeStr + " which is based on Type:" + typeof(T), nodeId);
			}
			return ((T)nodeScriptInstance);
		}

		public static List<string> GetCachedDataByNodeKind (AssetBundleGraphSettings.NodeKind nodeKind, string nodeId) {
			var platformPackageKeyCandidate = GraphStackController.GetCurrentPlatformPackageFolder();

			switch (nodeKind) {
				case AssetBundleGraphSettings.NodeKind.IMPORTSETTING_GUI: {
					// no cache file exists for importSetting.
					return new List<string>();
				}
				case AssetBundleGraphSettings.NodeKind.MODIFIER_GUI: {
					// no cache file exists for modifier.
					return new List<string>();
				}
				
				case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
				case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_GUI: {
					var cachedPathBase = FileController.PathCombine(
						AssetBundleGraphSettings.PREFABRICATOR_CACHE_PLACE, 
						nodeId,
						platformPackageKeyCandidate
					);

					// no cache folder, no cache.
					if (!Directory.Exists(cachedPathBase)) {
						// search default platform + package
						cachedPathBase = FileController.PathCombine(
							AssetBundleGraphSettings.PREFABRICATOR_CACHE_PLACE, 
							nodeId,
							GraphStackController.Default_Platform_Package_Folder()
						);

						if (!Directory.Exists(cachedPathBase)) {
							return new List<string>();
						}
					}

					return FileController.FilePathsInFolder(cachedPathBase);
				}
				 
				case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI: {
					// do nothing.
					break;
				}

				case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					var cachedPathBase = FileController.PathCombine(
						AssetBundleGraphSettings.BUNDLEBUILDER_CACHE_PLACE, 
						nodeId,
						platformPackageKeyCandidate
					);

					// no cache folder, no cache.
					if (!Directory.Exists(cachedPathBase)) {
						// search default platform + package
						cachedPathBase = FileController.PathCombine(
							AssetBundleGraphSettings.BUNDLEBUILDER_CACHE_PLACE, 
							nodeId,
							GraphStackController.Default_Platform_Package_Folder()
						);

						if (!Directory.Exists(cachedPathBase)) {
							return new List<string>();
						}
					}

					return FileController.FilePathsInFolder(cachedPathBase);
				}

				default: {
					// nothing to do.
					break;
				}
			}
			return new List<string>();
		}
		

		public static bool IsMetaFile (string filePath) {
			if (filePath.EndsWith(AssetBundleGraphSettings.UNITY_METAFILE_EXTENSION)) return true;
			return false;
		}

		public static bool ContainsHiddenFiles (string filePath) {
			var pathComponents = filePath.Split(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR);
			foreach (var path in pathComponents) {
				if (path.StartsWith(AssetBundleGraphSettings.DOTSTART_HIDDEN_FILE_HEADSTRING)) return true;
			}
			return false;
		}

		public static string ValueFromPlatformAndPackage (Dictionary<string, string> packageDict, string platform) {
			var key = Platform_Package_Key(platform);
			if (packageDict.ContainsKey(key)) {
				return packageDict[key];
			}

			if (packageDict.ContainsKey(AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME)) {
				return packageDict[AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME];
			}

			throw new AssetBundleGraphException("Default setting not found.");
		}

		public static List<string> ValueFromPlatformAndPackage (Dictionary<string, List<string>> packageDict, string platform) {
			var key = Platform_Package_Key(platform);
			if (packageDict.ContainsKey(key)) {
				return packageDict[key];
			}

			if (packageDict.ContainsKey(AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME)) {
				return packageDict[AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME];
			}

			throw new AssetBundleGraphException("Default setting not found.");
		}

		public static List<string> GetGetCurrentPlatformPackageOrDefaultFromDictList (AssetBundleGraphSettings.NodeKind kind, Dictionary<string, List<string>> packageDict) {
			var platformPackageKeyCandidate = GetCurrentPlatformPackageFolder();
			
			if (packageDict.ContainsKey(platformPackageKeyCandidate)) {
				return packageDict[platformPackageKeyCandidate];
			}
			
			if (packageDict.ContainsKey(AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME)) {
				return packageDict[AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME];
			}

			throw new AssetBundleGraphException("Default setting not found.");
		}

		public static string GetCurrentPlatformPackageOrDefaultFromDict (AssetBundleGraphSettings.NodeKind kind, Dictionary<string, string> packageDict) {
			var platformPackageKeyCandidate = GetCurrentPlatformPackageFolder();
			/*
				check best match for platform + pacakge.
			*/
			if (packageDict.ContainsKey(platformPackageKeyCandidate)) {
				return packageDict[platformPackageKeyCandidate];
			}
			
			/*
				check next match for defaultPlatform + package.
			*/
			var defaultPlatformAndCurrentPackageCandidate = Default_Platform_Package_Folder();
			if (packageDict.ContainsKey(defaultPlatformAndCurrentPackageCandidate)) {
				return packageDict[defaultPlatformAndCurrentPackageCandidate];
			}

			/*
				check default platform.
			*/
			if (packageDict.ContainsKey(AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME)) return packageDict[AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME];
			
			throw new AssetBundleGraphException("Default setting not found.");
		}

		public static string ShrinkedCurrentPlatform () {
			var currentPlatformCandidate = EditorUserBuildSettings.activeBuildTarget.ToString();
			return currentPlatformCandidate;
		}

		public static string GetCurrentPlatformPackageFolder () {
			var currentPlatformCandidate = ShrinkedCurrentPlatform();

			return Platform_Package_Key(currentPlatformCandidate);
		}

		public static string Default_Platform_Package_Folder () {
			return Platform_Package_Key(AssetBundleGraphSettings.PLATFORM_DEFAULT_NAME);
		}

		public static string Platform_Package_Key (string platformKey) {
			return platformKey.Replace(" ", "_");
		}

		public static string Platform_Dot_Package () {
			return ShrinkedCurrentPlatform();
		}

		public static string GetProjectName () {
			var assetsPath = Application.dataPath;
			var projectFolderNameArray = assetsPath.Split(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR);
			var projectFolderName = projectFolderNameArray[projectFolderNameArray.Length - 2] + AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR;
			return projectFolderName;
		}

	}

	public class NodeData {
		public readonly string nodeName;
		public readonly string nodeId;
		public readonly AssetBundleGraphSettings.NodeKind nodeKind;
		public readonly List<string> outputPointIds;

		// for All script nodes & prefabricator, bundlizer GUI.
		public readonly string scriptClassName;

		// for Loader Script
		public readonly Dictionary<string, string> loadFilePath;

		// for Exporter Script
		public readonly Dictionary<string, string> exportFilePath;

		// for Filter GUI data
		public readonly List<string> containsKeywords;
		public readonly List<string> containsKeytypes;

		// for Importer GUI data
		public readonly Dictionary<string, string> importerPackages;
		
		// for Modifier GUI data
		public readonly Dictionary<string, string> modifierPackages;

		// for Grouping GUI data
		public readonly Dictionary<string, string> groupingKeyword;

		// for Bundlizer GUI data
		public readonly Dictionary<string, string> bundleNameTemplate;
		public readonly Dictionary<string, string> bundleUseOutput;

		// for BundleBuilder GUI data
		public readonly Dictionary<string, List<string>> enabledBundleOptions;

		
		public List<ConnectionData> connectionDataOfParents = new List<ConnectionData>();

		private bool done;

		public NodeData (
			string nodeId, 
			AssetBundleGraphSettings.NodeKind nodeKind,
			string nodeName,
			List<string> outputPointIds,
			string scriptClassName = null,
			Dictionary<string, string> loadPath = null,
			Dictionary<string, string> exportPath = null,
			List<string> filterContainsKeywords = null,
			List<string> filterContainsKeytypes = null,
			Dictionary<string, string> importerPackages = null,
			Dictionary<string, string> modifierPackages = null,
			Dictionary<string, string> groupingKeyword = null,
			Dictionary<string, string> bundleNameTemplate = null,
			Dictionary<string, string> bundleUseOutput = null,
			Dictionary<string, List<string>> enabledBundleOptions = null
		) {
			this.nodeId = nodeId;
			this.nodeKind = nodeKind;
			this.nodeName = nodeName;
			this.outputPointIds = outputPointIds;

			this.scriptClassName = null;
			this.loadFilePath = null;
			this.exportFilePath = null;
			this.containsKeywords = null;
			this.importerPackages = null;
			this.modifierPackages = null;
			this.groupingKeyword = null;
			this.bundleNameTemplate = null;
			this.bundleUseOutput = null;
			this.enabledBundleOptions = null;

			switch (nodeKind) {
				case AssetBundleGraphSettings.NodeKind.LOADER_GUI: {
					this.loadFilePath = loadPath;
					break;
				}
				case AssetBundleGraphSettings.NodeKind.EXPORTER_GUI: {
					this.exportFilePath = exportPath;
					break;
				}

				case AssetBundleGraphSettings.NodeKind.FILTER_SCRIPT:
				case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
				case AssetBundleGraphSettings.NodeKind.PREFABRICATOR_GUI: {
					this.scriptClassName = scriptClassName;
					break;
				}

				case AssetBundleGraphSettings.NodeKind.FILTER_GUI: {
					this.containsKeywords = filterContainsKeywords;
					this.containsKeytypes = filterContainsKeytypes;
					break;
				}

				case AssetBundleGraphSettings.NodeKind.IMPORTSETTING_GUI: {
					this.importerPackages = importerPackages;
					break;
				}

				case AssetBundleGraphSettings.NodeKind.MODIFIER_GUI: {
					this.modifierPackages = modifierPackages;
					break;
				}
				
				case AssetBundleGraphSettings.NodeKind.GROUPING_GUI: {
					this.groupingKeyword = groupingKeyword;
					break;
				}

				case AssetBundleGraphSettings.NodeKind.BUNDLIZER_GUI: {
					this.bundleNameTemplate = bundleNameTemplate;
					this.bundleUseOutput = bundleUseOutput;
					break;
				}

				case AssetBundleGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					this.enabledBundleOptions = enabledBundleOptions;
					break;
				}

				default: {
					Debug.LogError(nodeName + " is defined as unknown kind of node. value:" + nodeKind);
					break;
				}
			}
		}

		public void AddConnectionData (ConnectionData connection) {
			connectionDataOfParents.Add(new ConnectionData(connection));
		}

		public void Done () {
			done = true;
		}

		public bool IsAlreadyDone () {
			return done;
		}
	}

	public class ConnectionData {
		public readonly string connectionId;
		public readonly string connectionLabel;
		public readonly string fromNodeId;
		public readonly string fromNodeOutputPointId;
		public readonly string toNodeId;

		public ConnectionData (string connectionId, string connectionLabel, string fromNodeId, string fromNodeOutputPointId, string toNodeId) {
			this.connectionId = connectionId;
			this.connectionLabel = connectionLabel;
			this.fromNodeId = fromNodeId;
			this.fromNodeOutputPointId = fromNodeOutputPointId;
			this.toNodeId = toNodeId;
		}

		public ConnectionData (ConnectionData connection) {
			this.connectionId = connection.connectionId;
			this.connectionLabel = connection.connectionLabel;
			this.fromNodeId = connection.fromNodeId;
			this.fromNodeOutputPointId = connection.fromNodeOutputPointId;
			this.toNodeId = connection.toNodeId;
		}
	}
}
