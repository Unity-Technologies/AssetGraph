using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;


namespace AssetGraph {
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

		public static void CleanCache () {
			var targetCachePath = AssetGraphSettings.APPLICATIONDATAPATH_CACHE_PATH;
			if (Directory.Exists(targetCachePath)) Directory.Delete(targetCachePath, true);
			AssetDatabase.Refresh();
		}

		public static void CleanSetting () {
			var targetSettingPath = AssetGraphSettings.IMPORTER_SAMPLING_PLACE;
			if (Directory.Exists(targetSettingPath)) Directory.Delete(targetSettingPath, true);
			AssetDatabase.Refresh();
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
			if (alreadyCachedPath.Contains(localAssetPath)) {
				var changed = false;
				foreach (var relatedAsset in relatedAssets) {
					if (relatedAsset.isNew) changed = true;
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

		public static List<string> GetLabelsFromSetupFilter (string scriptType) {
			var nodeScriptInstance = Assembly.GetExecutingAssembly().CreateInstance(scriptType);
			if (nodeScriptInstance == null) {
				Debug.LogError("no class found:" + scriptType);
				return new List<string>();
			}

			var labels = new List<string>();
			Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output = (string dataSourceNodeId, string connectionLabel, Dictionary<string, List<InternalAssetData>> source, List<string> usedCache) => {
				labels.Add(connectionLabel);
			};

			((FilterBase)nodeScriptInstance).Setup(
				"GetLabelsFromSetupFilter_dummy_nodeId", 
				string.Empty, 
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


			var nodesSource = graphDataDict[AssetGraphSettings.ASSETGRAPH_DATA_NODES] as List<object>;
			var newNodes = new List<Dictionary<string, object>>();

			/*
				delete undetectable node.
			*/
			foreach (var nodeSource in nodesSource) {
				var nodeDict = nodeSource as Dictionary<string, object>;
				
				var nodeId = nodeDict[AssetGraphSettings.NODE_ID] as string;

				var kindSource = nodeDict[AssetGraphSettings.NODE_KIND] as string;
				var kind = AssetGraphSettings.NodeKindFromString(kindSource);
				
				var nodeName = nodeDict[AssetGraphSettings.NODE_NAME] as string;

				// copy all key and value to new Node data dictionary.
				var newNodeDict = new Dictionary<string, object>();
				foreach (var key in nodeDict.Keys) {
					newNodeDict[key] = nodeDict[key];
				}

				switch (kind) {
					case AssetGraphSettings.NodeKind.FILTER_SCRIPT:
					case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT:
					case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
					case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT: {
						var scriptType = nodeDict[AssetGraphSettings.NODE_SCRIPT_TYPE] as string;
				
						var nodeScriptInstance = Assembly.GetExecutingAssembly().CreateInstance(scriptType);
						
						// warn if no class found.
						if (nodeScriptInstance == null) {
							changed = true;
							Debug.LogWarning("no class found:" + scriptType + " kind:" + kind + ", rebuildfing AssetGraph...");
							continue;
						}

						/*
							on validation, filter script receives all groups to only one group. group key is "0".
						*/
						if (kind == AssetGraphSettings.NodeKind.FILTER_SCRIPT) {
							var outoutLabelsSource = nodeDict[AssetGraphSettings.NODE_OUTPUT_LABELS] as List<object>;
							var outoutLabelsSet = new HashSet<string>();
							foreach (var source in outoutLabelsSource) {
								outoutLabelsSet.Add(source.ToString());
							}

							var latestLabels = new HashSet<string>();
							Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output = (string dataSourceNodeId, string connectionLabel, Dictionary<string, List<InternalAssetData>> source, List<string> usedCache) => {
								latestLabels.Add(connectionLabel);
							};

							((FilterBase)nodeScriptInstance).Setup(
								nodeId, 
								string.Empty, 
								string.Empty,
								new Dictionary<string, List<InternalAssetData>>{
									{"0", new List<InternalAssetData>()}
								},
								new List<string>(),
								Output
							);

							if (!outoutLabelsSet.SetEquals(latestLabels)) {
								changed = true;
								newNodeDict[AssetGraphSettings.NODE_OUTPUT_LABELS] = latestLabels.ToList();
							}
						}
						break;
					}

					case AssetGraphSettings.NodeKind.LOADER_GUI:
					case AssetGraphSettings.NodeKind.FILTER_GUI:
					case AssetGraphSettings.NodeKind.IMPORTER_GUI:
					case AssetGraphSettings.NodeKind.GROUPING_GUI:
					case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
						// nothing to do.
						break;
					}

					/*
						prefabricator GUI node with script.
					*/
					case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI: {
						var scriptType = nodeDict[AssetGraphSettings.NODE_SCRIPT_TYPE] as string;
						if (string.IsNullOrEmpty(scriptType)) {
							Debug.LogWarning("node:" + kind + ", script path is empty, please set prefer script to node:" + nodeName);
							break;
						}

						var nodeScriptInstance = Assembly.GetExecutingAssembly().CreateInstance(scriptType);
						
						// warn if no class found.
						if (nodeScriptInstance == null) Debug.LogWarning("no class found:" + scriptType + ", please set prefer script to node:" + nodeName);
						break;
					}

					case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
						var bundleNameTemplateSource = nodeDict[AssetGraphSettings.NODE_BUNDLIZER_BUNDLENAME_TEMPLATE] as Dictionary<string, object>;
						foreach (var platform_package_key in bundleNameTemplateSource.Keys) {
							var platform_package_bundleNameTemplate = bundleNameTemplateSource[platform_package_key] as string;
							if (string.IsNullOrEmpty(platform_package_bundleNameTemplate)) {
								Debug.LogWarning("node:" + kind + ", bundleNameTemplate is empty, please set prefer bundleNameTemplate to node:" + nodeName);
								break;
							}
						}

						break;
					}

					case AssetGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
						// nothing to do.
						break;
					}

					default: {
						Debug.LogError("not match kind:" + kind);
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
			
			var connectionsSource = graphDataDict[AssetGraphSettings.ASSETGRAPH_DATA_CONNECTIONS] as List<object>;
			var newConnections = new List<Dictionary<string, object>>();
			foreach (var connectionSource in connectionsSource) {
				var connectionDict = connectionSource as Dictionary<string, object>;

				var connectionLabel = connectionDict[AssetGraphSettings.CONNECTION_LABEL] as string;
				var fromNodeId = connectionDict[AssetGraphSettings.CONNECTION_FROMNODE] as string;
				var toNodeId = connectionDict[AssetGraphSettings.CONNECTION_TONODE] as string;
				
				// detect start node.
				var fromNodeCandidates = newNodes.Where(
					node => {
						var nodeId = node[AssetGraphSettings.NODE_ID] as string;
						return nodeId == fromNodeId;
					}
					).ToList();
				if (!fromNodeCandidates.Any()) {
					changed = true;
					continue;
				}

				// detect end node.
				var toNodeCandidates = newNodes.Where(
					node => {
						var nodeId = node[AssetGraphSettings.NODE_ID] as string;
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
				var connectionLabelsSource = fromNode[AssetGraphSettings.NODE_OUTPUT_LABELS] as List<object>;
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
					{AssetGraphSettings.ASSETGRAPH_DATA_LASTMODIFIED, DateTime.Now},
					{AssetGraphSettings.ASSETGRAPH_DATA_NODES, newNodes},
					{AssetGraphSettings.ASSETGRAPH_DATA_CONNECTIONS, newConnections}
				};
				return validatedResultDict;
			}

			return graphDataDict;
		}
		
		public static Dictionary<string, Dictionary<string, List<string>>> SetupStackedGraph (
			Dictionary<string, object> graphDataDict,
			string package
		) {
			var endpointNodeIdsAndNodeDatasAndConnectionDatas = SerializeNodeRoute(graphDataDict, package);
			
			var endpointNodeIds = endpointNodeIdsAndNodeDatasAndConnectionDatas.endpointNodeIds;
			var nodeDatas = endpointNodeIdsAndNodeDatasAndConnectionDatas.nodeDatas;
			var connectionDatas = endpointNodeIdsAndNodeDatasAndConnectionDatas.connectionDatas;

			var resultDict = new Dictionary<string, Dictionary<string, List<InternalAssetData>>>();
			var cacheDict = new Dictionary<string, List<string>>();

			foreach (var endNodeId in endpointNodeIds) {
				SetupSerializedRoute(endNodeId, nodeDatas, connectionDatas, resultDict, cacheDict, package);
			}
			
			return DictDictList(resultDict);
		}

		public static Dictionary<string, Dictionary<string, List<string>>> RunStackedGraph (
			Dictionary<string, object> graphDataDict, 
			string package,
			Action<string, float> updateHandler=null
		) {
			var EndpointNodeIdsAndNodeDatasAndConnectionDatas = SerializeNodeRoute(graphDataDict, package);
			
			var endpointNodeIds = EndpointNodeIdsAndNodeDatasAndConnectionDatas.endpointNodeIds;
			var nodeDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.nodeDatas;
			var connectionDatas = EndpointNodeIdsAndNodeDatasAndConnectionDatas.connectionDatas;

			var resultDict = new Dictionary<string, Dictionary<string, List<InternalAssetData>>>();
			var cacheDict = new Dictionary<string, List<string>>();

			foreach (var endNodeId in endpointNodeIds) {
				RunSerializedRoute(endNodeId, nodeDatas, connectionDatas, resultDict, cacheDict, package, updateHandler);
			}

			return DictDictList(resultDict);
		}

		private static Dictionary<string, Dictionary<string, List<string>>> DictDictList (Dictionary<string, Dictionary<string, List<InternalAssetData>>> sourceDictDictList) {
			var result = new Dictionary<string, Dictionary<string, List<string>>>();
			foreach (var connectionId in sourceDictDictList.Keys) {
				var connectionGroupDict = sourceDictDictList[connectionId];

				var newConnectionGroupDict = new Dictionary<string, List<string>>();
				foreach (var groupKey in connectionGroupDict.Keys) {
					var connectionThroughputList = connectionGroupDict[groupKey];

					var sourcePathList = new List<string>();
					foreach (var assetData in connectionThroughputList) {
						if (assetData.absoluteSourcePath != null) {
							var relativeAbsolutePath = assetData.absoluteSourcePath.Replace(ProjectPathWithSlash(), string.Empty);
							sourcePathList.Add(relativeAbsolutePath);
						} else {
							sourcePathList.Add(assetData.pathUnderConnectionId);
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
			return Directory.GetParent(assetPath).ToString() + AssetGraphSettings.UNITY_FOLDER_SEPARATOR;
		}
		
		public static EndpointNodeIdsAndNodeDatasAndConnectionDatas SerializeNodeRoute (Dictionary<string, object> graphDataDict, string package) {
			Debug.LogWarning("should check infinite loop.");


			var nodeIds = new List<string>();
			var nodesSource = graphDataDict[AssetGraphSettings.ASSETGRAPH_DATA_NODES] as List<object>;
			
			var connectionsSource = graphDataDict[AssetGraphSettings.ASSETGRAPH_DATA_CONNECTIONS] as List<object>;
			var connections = new List<ConnectionData>();
			foreach (var connectionSource in connectionsSource) {
				var connectionDict = connectionSource as Dictionary<string, object>;
				
				var connectionId = connectionDict[AssetGraphSettings.CONNECTION_ID] as string;
				var connectionLabel = connectionDict[AssetGraphSettings.CONNECTION_LABEL] as string;
				var fromNodeId = connectionDict[AssetGraphSettings.CONNECTION_FROMNODE] as string;
				var toNodeId = connectionDict[AssetGraphSettings.CONNECTION_TONODE] as string;
				connections.Add(new ConnectionData(connectionId, connectionLabel, fromNodeId, toNodeId));
			}

			var nodeDatas = new List<NodeData>();

			foreach (var nodeSource in nodesSource) {
				var nodeDict = nodeSource as Dictionary<string, object>;
				var nodeId = nodeDict[AssetGraphSettings.NODE_ID] as string;
				nodeIds.Add(nodeId);

				var kindSource = nodeDict[AssetGraphSettings.NODE_KIND] as string;
				var nodeKind = AssetGraphSettings.NodeKindFromString(kindSource);
				
				var nodeName = nodeDict[AssetGraphSettings.NODE_NAME] as string;
				
				switch (nodeKind) {
					case AssetGraphSettings.NodeKind.LOADER_GUI: {
						var loadPathSource = nodeDict[AssetGraphSettings.NODE_LOADER_LOAD_PATH] as Dictionary<string, object>;
						var loadPath = new Dictionary<string, string>();
						foreach (var platform_package_key in loadPathSource.Keys) loadPath[platform_package_key] = loadPathSource[platform_package_key] as string;

						nodeDatas.Add(
							new NodeData(
								nodeId:nodeId, 
								nodeKind:nodeKind, 
								nodeName:nodeName, 
								loadPath:loadPath
							)
						);
						break;
					}
					case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
						var exportPathSource = nodeDict[AssetGraphSettings.NODE_EXPORTER_EXPORT_PATH] as Dictionary<string, object>;
						var exportPath = new Dictionary<string, string>();
						foreach (var platform_package_key in exportPathSource.Keys) exportPath[platform_package_key] = exportPathSource[platform_package_key] as string;

						nodeDatas.Add(
							new NodeData(
								nodeId:nodeId, 
								nodeKind:nodeKind, 
								nodeName:nodeName,
								exportPath:exportPath
							)
						);
						break;
					}

					case AssetGraphSettings.NodeKind.FILTER_SCRIPT:
					case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT:

					case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
					case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI:

					case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT: {
						var scriptType = nodeDict[AssetGraphSettings.NODE_SCRIPT_TYPE] as string;
						nodeDatas.Add(
							new NodeData(
								nodeId:nodeId, 
								nodeKind:nodeKind, 
								nodeName:nodeName, 
								scriptType:scriptType
							)
						);
						break;
					}

					case AssetGraphSettings.NodeKind.FILTER_GUI: {
						var containsKeywordsSource = nodeDict[AssetGraphSettings.NODE_FILTER_CONTAINS_KEYWORDS] as List<object>;
						var filterContainsList = new List<string>();
						foreach (var containsKeywordSource in containsKeywordsSource) {
							filterContainsList.Add(containsKeywordSource.ToString());
						}
						nodeDatas.Add(
							new NodeData(
								nodeId:nodeId, 
								nodeKind:nodeKind, 
								nodeName:nodeName, 
								filterContainsList:filterContainsList
							)
						);
						break;
					}

					case AssetGraphSettings.NodeKind.IMPORTER_GUI: {
						nodeDatas.Add(
							new NodeData(
								nodeId:nodeId, 
								nodeKind:nodeKind, 
								nodeName:nodeName
							)
						);
						break;
					}

					case AssetGraphSettings.NodeKind.GROUPING_GUI: {
						var groupingKeywordSource = nodeDict[AssetGraphSettings.NODE_GROUPING_KEYWORD] as Dictionary<string, object>;
						var groupingKeyword = new Dictionary<string, string>();
						foreach (var platform_package_key in groupingKeywordSource.Keys) groupingKeyword[platform_package_key] = groupingKeywordSource[platform_package_key] as string;
						
						nodeDatas.Add(
							new NodeData(
								nodeId:nodeId, 
								nodeKind:nodeKind, 
								nodeName:nodeName, 
								groupingKeyword:groupingKeyword
							)
						);
						break;
					}

					case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
						var bundleNameTemplateSource = nodeDict[AssetGraphSettings.NODE_BUNDLIZER_BUNDLENAME_TEMPLATE] as Dictionary<string, object>;
						var bundleNameTemplate = new Dictionary<string, string>();
						foreach (var platform_package_key in bundleNameTemplateSource.Keys) bundleNameTemplate[platform_package_key] = bundleNameTemplateSource[platform_package_key] as string;

						nodeDatas.Add(
							new NodeData(
								nodeId:nodeId, 
								nodeKind:nodeKind, 
								nodeName:nodeName,
								bundleNameTemplate:bundleNameTemplate
							)
						);
						break;
					}

					case AssetGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
						var enabledBundleOptionsSource = nodeDict[AssetGraphSettings.NODE_BUNDLEBUILDER_ENABLEDBUNDLEOPTIONS] as Dictionary<string, object>;

						// default is empty. all settings are disabled.
						var enabledBundleOptions = new Dictionary<string, List<string>>();

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
								enabledBundleOptions:enabledBundleOptions
							)
						);
						break;
					}

					default: {
						Debug.LogError("failed to match:" + nodeKind);
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
				// collect parent Ids into child node.
				var targetNodes = nodeDatas.Where(nodeData => nodeData.nodeId == connection.toNodeId).ToList();
				foreach (var targetNode in targetNodes) targetNode.AddConnectionData(connection);
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
			Dictionary<string, List<string>> cacheDict,
			string package
		) {
			ExecuteParent(endNodeId, nodeDatas, connections, resultDict, cacheDict, package, false);

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
			string package,
			Action<string, float> updateHandler=null
		) {
			ExecuteParent(endNodeId, nodeDatas, connections, resultDict, cacheDict, package, true, updateHandler);

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
			string package,
			bool isActualRun,
			Action<string, float> updateHandler=null
		) {
			var currentNodeDatas = nodeDatas.Where(relation => relation.nodeId == nodeId).ToList();
			if (!currentNodeDatas.Any()) return;

			var currentNodeData = currentNodeDatas[0];

			if (currentNodeData.IsAlreadyDone()) return;

			/*
				run parent nodes of this node.
			*/
			var parentNodeIds = currentNodeData.connectionDataOfParents.Select(conData => conData.fromNodeId).ToList();
			foreach (var parentNodeId in parentNodeIds) {
				ExecuteParent(parentNodeId, nodeDatas, connectionDatas, resultDict, cachedDict, package, isActualRun, updateHandler);
			}

			/*
				run after parent run.
			*/
			var connectionLabelsFromThisNodeToChildNode = connectionDatas
				.Where(con => con.fromNodeId == nodeId)
				.Select(con => con.connectionLabel)
				.ToList();

			/*
				this is label of connection.

				will be ignored in Filter node,
				because the Filter node will generate new label of connection by itself.
			*/
			var labelToChild = string.Empty;
			if (connectionLabelsFromThisNodeToChildNode.Any()) {
				labelToChild = connectionLabelsFromThisNodeToChildNode[0];
			}

			if (updateHandler != null) updateHandler(nodeId, 0f);

			/*
				has next node, run first time.
			*/
			var nodeName = currentNodeData.nodeName;
			var nodeKind = currentNodeData.nodeKind;
			
			var alreadyCachedPaths = new List<string>();
			if (cachedDict.ContainsKey(nodeId)) alreadyCachedPaths.AddRange(cachedDict[nodeId]);

			/*
				load already exist cache from node.
			*/
			alreadyCachedPaths.AddRange(GetCachedData(nodeKind, nodeId));
			

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

			Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output = (string dataSourceNodeId, string connectionLabel, Dictionary<string, List<InternalAssetData>> result, List<string> justCached) => {
				var targetConnectionIds = connectionDatas
					.Where(con => con.fromNodeId == dataSourceNodeId) // from this node
					.Where(con => con.connectionLabel == connectionLabel) // from this label
					.Select(con => con.connectionId)
					.ToList();
				
				if (!targetConnectionIds.Any()) return;
				
				var targetConnectionId = targetConnectionIds[0];
				if (!resultDict.ContainsKey(targetConnectionId)) resultDict[targetConnectionId] = new Dictionary<string, List<InternalAssetData>>();
				
				var connectionResult = resultDict[targetConnectionId];

				foreach (var groupKey in result.Keys) {
					if (!connectionResult.ContainsKey(groupKey)) connectionResult[groupKey] = new List<InternalAssetData>();
					connectionResult[groupKey].AddRange(result[groupKey]);
				}

				if (isActualRun) {
					if (!cachedDict.ContainsKey(nodeId)) cachedDict[nodeId] = new List<string>();
					cachedDict[nodeId].AddRange(justCached);
				}
			};

			if (isActualRun) {
				switch (nodeKind) {
					/*
						Scripts
					*/
					case AssetGraphSettings.NodeKind.FILTER_SCRIPT: {
						var scriptType = currentNodeData.scriptType;
						var executor = Executor<FilterBase>(scriptType);
						executor.Run(nodeId, labelToChild, package, inputParentResults, alreadyCachedPaths, Output);
						break;
					}
					case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT: {
						var scriptType = currentNodeData.scriptType;
						var executor = Executor<ImporterBase>(scriptType);
						executor.Run(nodeId, labelToChild, package, inputParentResults, alreadyCachedPaths, Output);
						break;
					}
					case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT: {
						var scriptType = currentNodeData.scriptType;
						var executor = Executor<PrefabricatorBase>(scriptType);
						executor.Run(nodeId, labelToChild, package, inputParentResults, alreadyCachedPaths, Output);
						break;
					}
					case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT: {
						var scriptType = currentNodeData.scriptType;
						var executor = Executor<BundlizerBase>(scriptType);
						executor.Run(nodeId, labelToChild, package, inputParentResults, alreadyCachedPaths, Output);
						break;
					}
					

					/*
						GUIs
					*/
					case AssetGraphSettings.NodeKind.LOADER_GUI: {
						var currentLoadFilePath = Current_Platform_Package_OrDefaultFromDict(currentNodeData.loadFilePath, package);
						var executor = new IntegratedGUILoader(WithProjectPath(currentLoadFilePath));
						executor.Run(nodeId, labelToChild, package, inputParentResults, alreadyCachedPaths, Output);
						break;
					}

					case AssetGraphSettings.NodeKind.FILTER_GUI: {
						var executor = new IntegratedGUIFilter(currentNodeData.containsKeywords);
						executor.Run(nodeId, labelToChild, package, inputParentResults, alreadyCachedPaths, Output);
						break;
					}

					case AssetGraphSettings.NodeKind.IMPORTER_GUI: {
						var executor = new IntegratedGUIImporter();
						executor.Run(nodeId, labelToChild, package, inputParentResults, alreadyCachedPaths, Output);
						break;
					}

					case AssetGraphSettings.NodeKind.GROUPING_GUI: {
						var executor = new IntegratedGUIGrouping(Current_Platform_Package_OrDefaultFromDict(currentNodeData.groupingKeyword, package));
						executor.Run(nodeId, labelToChild, package, inputParentResults, alreadyCachedPaths, Output);
						break;
					}

					case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI: {
						var scriptType = currentNodeData.scriptType;
						if (string.IsNullOrEmpty(scriptType)) {
							Debug.LogError("prefabriator class at node:" + nodeName + " is empty, please set valid script type.");
							break;
						}
						var executor = Executor<PrefabricatorBase>(scriptType);
						executor.Run(nodeId, labelToChild, package, inputParentResults, alreadyCachedPaths, Output);
						break;
					}

					case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
						var bundleNameTemplate = Current_Platform_Package_OrDefaultFromDict(currentNodeData.bundleNameTemplate, package);
						var executor = new IntegratedGUIBundlizer(bundleNameTemplate);
						executor.Run(nodeId, labelToChild, package, inputParentResults, alreadyCachedPaths, Output);
						break;
					}

					case AssetGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
						var bundleOptions = Current_Platform_Package_OrDefaultFromDictList(currentNodeData.enabledBundleOptions, package);
						var executor = new IntegratedGUIBundleBuilder(bundleOptions, nodeDatas.Select(nodeData => nodeData.nodeId).ToList());
						executor.Run(nodeId, labelToChild, package, inputParentResults, alreadyCachedPaths, Output);
						break;
					}

					case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
						var exportPath = Current_Platform_Package_OrDefaultFromDict(currentNodeData.exportFilePath, package);
						var executor = new IntegratedGUIExporter(WithProjectPath(exportPath));
						executor.Run(nodeId, labelToChild, package, inputParentResults, alreadyCachedPaths, Output);
						break;
					}

					default: {
						Debug.LogError("kind not found:" + nodeKind);
						break;
					}
				}
			} else {
				switch (nodeKind) {
					/*
						Scripts
					*/
					case AssetGraphSettings.NodeKind.FILTER_SCRIPT: {
						var scriptType = currentNodeData.scriptType;
						var executor = Executor<FilterBase>(scriptType);
						executor.Setup(nodeId, labelToChild, package, inputParentResults, alreadyCachedPaths, Output);
						break;
					}
					case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT: {
						var scriptType = currentNodeData.scriptType;
						var executor = Executor<ImporterBase>(scriptType);
						executor.Setup(nodeId, labelToChild, package, inputParentResults, alreadyCachedPaths, Output);
						break;
					}
					case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT: {
						var scriptType = currentNodeData.scriptType;
						var executor = Executor<PrefabricatorBase>(scriptType);
						executor.Setup(nodeId, labelToChild, package, inputParentResults, alreadyCachedPaths, Output);
						break;
					}
					case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT: {
						var scriptType = currentNodeData.scriptType;
						var executor = Executor<BundlizerBase>(scriptType);
						executor.Setup(nodeId, labelToChild, package, inputParentResults, alreadyCachedPaths, Output);
						break;
					}
					

					/*
						GUIs
					*/
					case AssetGraphSettings.NodeKind.LOADER_GUI: {
						var currentLoadFilePath = Current_Platform_Package_OrDefaultFromDict(currentNodeData.loadFilePath, package);
						var executor = new IntegratedGUILoader(WithProjectPath(currentLoadFilePath));
						executor.Setup(nodeId, labelToChild, package, inputParentResults, alreadyCachedPaths, Output);
						break;
					}

					case AssetGraphSettings.NodeKind.FILTER_GUI: {
						var executor = new IntegratedGUIFilter(currentNodeData.containsKeywords);
						executor.Setup(nodeId, labelToChild, package, inputParentResults, alreadyCachedPaths, Output);
						break;
					}

					case AssetGraphSettings.NodeKind.IMPORTER_GUI: {
						var executor = new IntegratedGUIImporter();
						executor.Setup(nodeId, labelToChild, package, inputParentResults, alreadyCachedPaths, Output);
						break;
					}

					case AssetGraphSettings.NodeKind.GROUPING_GUI: {
						var executor = new IntegratedGUIGrouping(Current_Platform_Package_OrDefaultFromDict(currentNodeData.groupingKeyword, package));
						executor.Setup(nodeId, labelToChild, package, inputParentResults, alreadyCachedPaths, Output);
						break;
					}

					case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI: {
						var scriptType = currentNodeData.scriptType;
						if (string.IsNullOrEmpty(scriptType)) {
							Debug.LogError("prefabriator class at node:" + nodeName + " is empty, please set valid script type.");
							break;;
						}
						var executor = Executor<PrefabricatorBase>(scriptType);
						executor.Setup(nodeId, labelToChild, package, inputParentResults, alreadyCachedPaths, Output);
						break;
					}

					case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
						var bundleNameTemplate = Current_Platform_Package_OrDefaultFromDict(currentNodeData.bundleNameTemplate, package);
						var executor = new IntegratedGUIBundlizer(bundleNameTemplate);
						executor.Setup(nodeId, labelToChild, package, inputParentResults, alreadyCachedPaths, Output);
						break;
					}

					case AssetGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
						var bundleOptions = Current_Platform_Package_OrDefaultFromDictList(currentNodeData.enabledBundleOptions, package);
						var executor = new IntegratedGUIBundleBuilder(bundleOptions, nodeDatas.Select(nodeData => nodeData.nodeId).ToList());
						executor.Setup(nodeId, labelToChild, package, inputParentResults, alreadyCachedPaths, Output);
						break;
					}

					case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
						var exportPath = Current_Platform_Package_OrDefaultFromDict(currentNodeData.exportFilePath, package);
						var executor = new IntegratedGUIExporter(WithProjectPath(exportPath));
						executor.Setup(nodeId, labelToChild, package, inputParentResults, alreadyCachedPaths, Output);
						break;
					}

					default: {
						Debug.LogError("kind not found:" + nodeKind);
						break;
					}
				}
			}

			currentNodeData.Done();
			if (updateHandler != null) updateHandler(nodeId, 1f);
		}

		public static string WithProjectPath (string pathUnderProjectFolder) {
			var assetPath = Application.dataPath;
			var projectPath = Directory.GetParent(assetPath).ToString();
			var projectParentPath = Directory.GetParent(projectPath).ToString();
			return FileController.PathCombine(projectParentPath, pathUnderProjectFolder);
		}

		public static T Executor<T> (string typeStr) where T : INodeBase {
			var nodeScriptInstance = Assembly.GetExecutingAssembly().CreateInstance(typeStr);
			if (nodeScriptInstance == null) throw new Exception("failed to generate class information of class:" + typeStr + " which is based on Type:" + typeof(T));
			return ((T)nodeScriptInstance);
		}

		public static List<string> GetCachedData (AssetGraphSettings.NodeKind nodeKind, string nodeId) {
			switch (nodeKind) {
				case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT:
				case AssetGraphSettings.NodeKind.IMPORTER_GUI: {
					var cachedPathBase = FileController.PathCombine(
						AssetGraphSettings.IMPORTER_CACHE_PLACE, 
						nodeId
					);

					// get sampling file.
					var baseSettingPath = FileController.PathCombine(AssetGraphSettings.IMPORTER_SAMPLING_PLACE, nodeId);
					
					// if no setting exist, ignore all cache.
					if (!Directory.Exists(baseSettingPath)) return new List<string>();

					var baseSettingFilePaths = FileController.FilePathsInFolderOnly1Level(baseSettingPath);
					if (!baseSettingFilePaths.Any()) return new List<string>();


					// setting is exists, next, checking about cache.

					// no cache folder, no cache.
					if (!Directory.Exists(cachedPathBase)) return new List<string>();

					// search cache candidates.
					var cacheCandidates = FileController.FilePathsInFolder(cachedPathBase);

					if (0 < cacheCandidates.Count) {
						var baseSettingFilePath = baseSettingFilePaths[0];
						var baseSettingImporterOrigin = AssetImporter.GetAtPath(baseSettingFilePath);

						var cached = new List<string>();

						foreach (var candidatePath in cacheCandidates) {
							var importedCandidateImporterOrigin = AssetImporter.GetAtPath(candidatePath);
							
							// cancel if importer type does not match. maybe this is not target of this node's importer.
							// 2 potentials. 
							// a. this is sub-generated resources of the result of import.
							// b. garbage.
							// both will be treat as cached.
							if (importedCandidateImporterOrigin.GetType() != baseSettingImporterOrigin.GetType()) {
								cached.Add(candidatePath);
								continue;
							}

							if (typeof(TextureImporter).IsAssignableFrom(importedCandidateImporterOrigin.GetType())) {
								var baseSettingImporter = baseSettingImporterOrigin as TextureImporter;
								var importedCandidateImporter = importedCandidateImporterOrigin as TextureImporter;
								if (InternalSamplingImportAdopter.IsSameTextureSetting(importedCandidateImporter, baseSettingImporter)) {
									cached.Add(candidatePath);
									continue;
								}

								// delete for effect import.
								File.Delete(candidatePath);
							}

							if (typeof(ModelImporter).IsAssignableFrom(importedCandidateImporterOrigin.GetType())) {
								var baseSettingImporter = baseSettingImporterOrigin as ModelImporter;
								var importedCandidateImporter = importedCandidateImporterOrigin as ModelImporter;
								if (InternalSamplingImportAdopter.IsSameModelSetting(importedCandidateImporter, baseSettingImporter)) {
									cached.Add(candidatePath);
									continue;
								}

								// delete for effect import.
								File.Delete(candidatePath);
							}

							if (typeof(AudioImporter).IsAssignableFrom(importedCandidateImporterOrigin.GetType())) {
								var baseSettingImporter = baseSettingImporterOrigin as AudioImporter;
								var importedCandidateImporter = importedCandidateImporterOrigin as AudioImporter;
								if (InternalSamplingImportAdopter.IsSameAudioSetting(importedCandidateImporter, baseSettingImporter)) {
									cached.Add(candidatePath);
									continue;
								}

								// delete for effect import.
								File.Delete(candidatePath);
							}
						}

						return cached;
					}

					return new List<string>();
				}
				
				case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
				case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI: {
					var cachedPathBase = FileController.PathCombine(
						AssetGraphSettings.PREFABRICATOR_CACHE_PLACE, 
						nodeId
					);
					return FileController.FilePathsInFolder(cachedPathBase);
				}
				
				case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT: 
				case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
					var cachedPathBase = FileController.PathCombine(
						AssetGraphSettings.BUNDLIZER_CACHE_PLACE, 
						nodeId
					);

					return FileController.FilePathsInFolder(cachedPathBase);
				}

				case AssetGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					break;
				}

				default: {
					// nothing to do.
					break;
				}
			}
			return new List<string>();
		}

		// stringからプラットフォームのenumを得る、できるはず
		// public static NodeKind NodeKindFromString (string val) {
  // 			return (NodeKind)Enum.Parse(typeof(NodeKind), val);
  // 		}

		public static string ValueFromPlatformAndPackage (Dictionary<string, string> packageDict, string platform, string package) {
			var key = Platform_Package_Key(platform, package);
			if (packageDict.ContainsKey(key)) return packageDict[key];

			if (packageDict.ContainsKey(AssetGraphSettings.PLATFORM_DEFAULT_NAME)) return packageDict[AssetGraphSettings.PLATFORM_DEFAULT_NAME];

			throw new Exception("Failed to detect default package setting. this kind of node settings should contains at least 1 Default setting.");
		}

		public static List<string> Current_Platform_Package_OrDefaultFromDictList (Dictionary<string, List<string>> packageDict, string package) {
			var platform_package_key_candidate = Current_Platform_Package_Folder(package);
			
			if (packageDict.ContainsKey(platform_package_key_candidate)) return packageDict[platform_package_key_candidate];
			
			if (packageDict.ContainsKey(AssetGraphSettings.PLATFORM_DEFAULT_NAME)) return packageDict[AssetGraphSettings.PLATFORM_DEFAULT_NAME];
			
			throw new Exception("Failed to detect default package setting. this kind of node settings should contains at least 1 Default setting.");
		}

		public static string Current_Platform_Package_OrDefaultFromDict (Dictionary<string, string> packageDict, string package) {
			var platform_package_key_candidate = Current_Platform_Package_Folder(package);
			
			if (packageDict.ContainsKey(platform_package_key_candidate)) return packageDict[platform_package_key_candidate];
			
			if (packageDict.ContainsKey(AssetGraphSettings.PLATFORM_DEFAULT_NAME)) return packageDict[AssetGraphSettings.PLATFORM_DEFAULT_NAME];
			
			throw new Exception("Failed to detect default package setting. this kind of node settings should contains at least 1 Default setting.");
		}

		public static string Current_Platform_Package_Folder (string package) {
			Debug.LogError("プラットフォームが特定のものだったらStandaloneに変える");
			return Platform_Package_Key(EditorUserBuildSettings.activeBuildTarget.ToString(), package);
		}

		public static string Platform_Package_Key (string platformKey, string packageKey) {
			if (string.IsNullOrEmpty(packageKey)) return (platformKey + AssetGraphSettings.package_SEPARATOR + packageKey).Replace(" ", "_");
			return platformKey.Replace(" ", "_");
		}
	}

	public class NodeData {
		public readonly string nodeName;
		public readonly string nodeId;
		public readonly AssetGraphSettings.NodeKind nodeKind;
		
		public List<ConnectionData> connectionDataOfParents = new List<ConnectionData>();

		// for All script nodes & prefabricator, bundlizer GUI.
		public readonly string scriptType;

		// for Loader Script
		public readonly Dictionary<string, string> loadFilePath;

		// for Exporter Script
		public readonly Dictionary<string, string> exportFilePath;

		// for filter GUI data
		public readonly List<string> containsKeywords;

		// for importer GUI data
		public readonly Dictionary<string, string> groupingKeyword;

		// for bundlizer GUI data
		public readonly Dictionary<string, string> bundleNameTemplate;

		// for bundleBuilder GUI data
		public readonly Dictionary<string, List<string>> enabledBundleOptions;

		private bool done;

		public NodeData (
			string nodeId, 
			AssetGraphSettings.NodeKind nodeKind, 
			string nodeName = null,
			string scriptType = null,
			Dictionary<string, string> loadPath = null,
			Dictionary<string, string> exportPath = null,
			List<string> filterContainsList = null,
			Dictionary<string, string> groupingKeyword = null,
			Dictionary<string, string> bundleNameTemplate = null,
			Dictionary<string, List<string>> enabledBundleOptions = null
		) {
			this.nodeId = nodeId;
			this.nodeKind = nodeKind;
			this.nodeName = nodeName;
			
			this.scriptType = null;
			this.loadFilePath = null;
			this.exportFilePath = null;
			this.containsKeywords = null;
			this.groupingKeyword = null;
			this.bundleNameTemplate = null;
			this.enabledBundleOptions = null;

			switch (nodeKind) {
				case AssetGraphSettings.NodeKind.LOADER_GUI: {
					this.loadFilePath = loadPath;
					break;
				}
				case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
					this.exportFilePath = exportPath;
					break;
				}

				case AssetGraphSettings.NodeKind.FILTER_SCRIPT:
				case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT:

				case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
				case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI:

				case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT: {
					this.scriptType = scriptType;
					break;
				}

				case AssetGraphSettings.NodeKind.FILTER_GUI: {
					this.containsKeywords = filterContainsList;
					break;
				}

				case AssetGraphSettings.NodeKind.IMPORTER_GUI: {
					// do nothing.
					break;
				}

				case AssetGraphSettings.NodeKind.GROUPING_GUI: {
					this.groupingKeyword = groupingKeyword;
					break;
				}

				case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
					this.bundleNameTemplate = bundleNameTemplate;
					break;
				}

				case AssetGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					this.enabledBundleOptions = enabledBundleOptions;
					break;
				}

				default: {
					Debug.LogError("failed to match kind:" + nodeKind);
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
		public readonly string toNodeId;

		public ConnectionData (string connectionId, string connectionLabel, string fromNodeId, string toNodeId) {
			this.connectionId = connectionId;
			this.connectionLabel = connectionLabel;
			this.fromNodeId = fromNodeId;
			this.toNodeId = toNodeId;
		}

		public ConnectionData (ConnectionData connection) {
			this.connectionId = connection.connectionId;
			this.connectionLabel = connection.connectionLabel;
			this.fromNodeId = connection.fromNodeId;
			this.toNodeId = connection.toNodeId;
		}
	}
}