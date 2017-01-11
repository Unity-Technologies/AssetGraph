using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace AssetBundleGraph {
	/*
	 * AssetBundleGraphController executes operations based on graph 
	 */
	public class AssetBundleGraphController {

		private List<NodeException> m_nodeExceptions;
		private AssetReferenceStreamManager m_streamManager;
		private PerformGraph[] m_performGraph;
		private int gIndex;

		private BuildTarget m_lastTarget;

		private bool m_isBuilding;

		public bool IsAnyIssueFound {
			get {
				return m_nodeExceptions.Count > 0;
			}
		}

		public List<NodeException> Issues {
			get {
				return m_nodeExceptions;
			}
		}

		public AssetReferenceStreamManager StreamManager {
			get {
				return m_streamManager;
			}
		}

		public AssetBundleGraphController() {
			m_nodeExceptions = new List<NodeException>();
			m_streamManager = new AssetReferenceStreamManager();
			m_performGraph  = new PerformGraph[] { 
				new PerformGraph(m_streamManager), 
				new PerformGraph(m_streamManager)
			};
			gIndex = 0;
		}

		/**
		 * Execute Run operations using current graph
		 */
		public void Perform (
			BuildTarget target,
			bool isRun,
			bool forceVisitAll,
			Action<NodeData, string, float> updateHandler) 
		{
			LogUtility.Logger.Log(LogType.Log, (isRun) ? "---Build BEGIN---" : "---Setup BEGIN---");
			m_isBuilding = true;

			if(isRun) {
				AssetBundleBuildReport.ClearReports();
			}

			var saveData = SaveData.Data;
			foreach(var e in m_nodeExceptions) {
				var errorNode = saveData.Nodes.Find(n => n.Id == e.Id);
				// errorNode may not be found if user delete it on graph
				if(errorNode != null) {
					LogUtility.Logger.LogFormat(LogType.Log, "[Perform] {0} is marked to revisit due to last error", errorNode.Name);
					errorNode.NeedsRevisit = true;
				}
			}

			m_nodeExceptions.Clear();
			m_lastTarget = target;

			PerformGraph oldGraph = m_performGraph[gIndex];
			gIndex = (gIndex+1) %2;
			PerformGraph newGraph = m_performGraph[gIndex];
			newGraph.BuildGraphFromSaveData(target, oldGraph);

			PerformGraph.Perform performFunc =
				(NodeData data, 
					IEnumerable<PerformGraph.AssetGroups> incoming, 
					IEnumerable<ConnectionData> connectionsToOutput, 
					PerformGraph.Output outputFunc) =>
			{
				DoNodeOperation(target, data, incoming, connectionsToOutput, outputFunc, isRun, updateHandler);
			};

			newGraph.VisitAll(performFunc, forceVisitAll);

			if(isRun && m_nodeExceptions.Count == 0) {
				Postprocess();
			}

			m_isBuilding = false;
			LogUtility.Logger.Log(LogType.Log, (isRun) ? "---Build END---" : "---Setup END---");
		}

		public void Validate (
			NodeGUI node, 
			BuildTarget target) 
		{
			m_nodeExceptions.RemoveAll(e => e.Id == node.Data.Id);

			try {
				LogUtility.Logger.LogFormat(LogType.Log, "[validate] {0} validate", node.Name);
				m_isBuilding = true;
				DoNodeOperation(target, node.Data, null, null, 
					(ConnectionData dst, Dictionary<string, List<AssetReference>> outputGroupAsset) => {}, 
					false, null);

				LogUtility.Logger.LogFormat(LogType.Log, "[Perform] {0} ", node.Name);

				Perform(target, false, false, null);
				m_isBuilding = false;
			} catch (NodeException e) {
				m_nodeExceptions.Add(e);
			}
		}

		/**
			Perform Run or Setup from parent of given terminal node recursively.
		*/
		private void DoNodeOperation (
			BuildTarget target,
			NodeData currentNodeData,
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<ConnectionData> connectionsToOutput, 
			PerformGraph.Output outputFunc,
			bool isActualRun,
			Action<NodeData, string, float> updateHandler) 
		{
			try {
				if (updateHandler != null) {
					updateHandler(currentNodeData, "Starting...", 0f);
				}

				INodeOperation executor = CreateOperation(currentNodeData);
				if(executor != null) {
					if(isActualRun) {
						executor.Run(target, currentNodeData, incoming, connectionsToOutput, outputFunc, updateHandler);
					}
					else {
						executor.Setup(target, currentNodeData, incoming, connectionsToOutput, outputFunc);
					}
				}

				if (updateHandler != null) {
					updateHandler(currentNodeData, "Completed.", 1f);
				}
			} catch (NodeException e) {
				m_nodeExceptions.Add(e);
			} 
			// Minimize impact of errors happened during node operation
			catch (Exception e) {
				m_nodeExceptions.Add(new NodeException(string.Format("{0}:{1} (See Console for detail)", e.GetType().ToString(), e.Message), currentNodeData.Id));
				LogUtility.Logger.LogException(e);
			}
		}

		private INodeOperation CreateOperation(NodeData currentNodeData) {
			INodeOperation executor = null;

			try {
				switch (currentNodeData.Kind) {
				case NodeKind.LOADER_GUI: {
						executor = new IntegratedGUILoader();
						break;
					}
				case NodeKind.FILTER_GUI: {
						executor = new IntegratedGUIFilter();
						break;
					}

				case NodeKind.IMPORTSETTING_GUI: {
						executor = new IntegratedGUIImportSetting();
						break;
					}
				case NodeKind.MODIFIER_GUI: {
						executor = new IntegratedGUIModifier();
						break;
					}
				case NodeKind.GROUPING_GUI: {
						executor = new IntegratedGUIGrouping();
						break;
					}
				case NodeKind.PREFABBUILDER_GUI: {
						executor = new IntegratedPrefabBuilder();
						break;
					}

				case NodeKind.BUNDLECONFIG_GUI: {
						executor = new IntegratedGUIBundleConfigurator();
						break;
					}

				case NodeKind.BUNDLEBUILDER_GUI: {
						executor = new IntegratedGUIBundleBuilder();
						break;
					}

				case NodeKind.EXPORTER_GUI: {
						executor = new IntegratedGUIExporter();
						break;
					}

				default: {
						LogUtility.Logger.LogError(LogUtility.kTag, currentNodeData.Name + " is defined as unknown kind of node. value:" + currentNodeData.Kind);
						break;
					}
				} 
			} catch (NodeException e) {
				m_nodeExceptions.Add(e);
			}

			return executor;
		}

		private void Postprocess () 
		{
			var postprocessType = typeof(IPostprocess);
			var ppTypes = Assembly.GetExecutingAssembly().GetTypes().Select(v => v).Where(v => v != postprocessType && postprocessType.IsAssignableFrom(v)).ToList();
			foreach (var t in ppTypes) {
				var postprocessScriptInstance = Assembly.GetExecutingAssembly().CreateInstance(t.Name);
				if (postprocessScriptInstance == null) {
					throw new AssetBundleGraphException("Postprocess " + t.Name + " failed to run (failed to create instance from assembly).");
				}

				var postprocessInstance = (IPostprocess)postprocessScriptInstance;
				// TODO: implement this properly
				postprocessInstance.DoPostprocess(AssetBundleBuildReport.BuildReports, AssetBundleBuildReport.ExportReports);
			}
		}

		public void OnAssetsReimported(BuildTarget target, string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {

			// ignore asset reimport event during build
			if(m_isBuilding) {
				return;
			}

			var saveData = SaveData.Data;

			if(saveData.Nodes == null) {
				return;
			}

			bool isAnyNodeAffected = false;

			Regex importSettingsIdMatch = new Regex("ImportSettings\\/([0-9a-z\\-]+)\\/");

			foreach(var imported in importedAssets) {
				Match m = importSettingsIdMatch.Match(imported);
				if(m.Success) {
					Group id = m.Groups[1];
					NodeData n = saveData.Nodes.Find(v => v.Id == id.ToString());
					UnityEngine.Assertions.Assert.IsNotNull(n);
					UnityEngine.Assertions.Assert.IsTrue(n.Kind == NodeKind.IMPORTSETTING_GUI);

					n.NeedsRevisit = true;
					isAnyNodeAffected = true;
				}
			}

			foreach(var node in saveData.Nodes) {
				if(node.Kind == NodeKind.LOADER_GUI) {
					var loadPath = node.LoaderLoadPath[target];
					if(string.IsNullOrEmpty(loadPath)) {
						// ignore config file path update
						var notConfigFilePath = importedAssets.Where( path => !path.Contains(AssetBundleGraphSettings.ASSETBUNDLEGRAPH_PATH)).FirstOrDefault();
						if(!string.IsNullOrEmpty(notConfigFilePath)) {
							LogUtility.Logger.LogFormat(LogType.Log, "{0} is marked to revisit", node.Name);
							node.NeedsRevisit = true;
							isAnyNodeAffected = true;
						}
					}

					var connOut = saveData.Connections.Find(c => c.FromNodeId == node.Id);

					if( connOut != null ) {

						var assetGroup = m_streamManager.FindAssetGroup(connOut);
						var importPath = "Assets/" + node.LoaderLoadPath[target];

						foreach(var path in importedAssets) {
							if(path.StartsWith(importPath)) {
								// if this is reimport, we don't need to redo Loader
								if ( assetGroup["0"].Find(x => x.importFrom == path) == null ) {
									LogUtility.Logger.LogFormat(LogType.Log, "{0} is marked to revisit", node.Name);
									node.NeedsRevisit = true;
									isAnyNodeAffected = true;
									break;
								}
							}
						}
						foreach(var path in deletedAssets) {
							if(path.StartsWith(importPath)) {
								LogUtility.Logger.LogFormat(LogType.Log, "{0} is marked to revisit", node.Name);
								node.NeedsRevisit = true;
								isAnyNodeAffected = true;
								break;
							}
						}
						foreach(var path in movedAssets) {
							if(path.StartsWith(importPath)) {
								LogUtility.Logger.LogFormat(LogType.Log, "{0} is marked to revisit", node.Name);
								node.NeedsRevisit = true;
								isAnyNodeAffected = true;
								break;
							}
						}
						foreach(var path in movedFromAssetPaths) {
							if(path.StartsWith(importPath)) {
								LogUtility.Logger.LogFormat(LogType.Log, "{0} is marked to revisit", node.Name);
								node.NeedsRevisit = true;
								isAnyNodeAffected = true;
								break;
							}
						}
					}
				}
			}

			if(isAnyNodeAffected) {
				Perform(m_lastTarget, false, false, null);
			}
		}
	}
}
