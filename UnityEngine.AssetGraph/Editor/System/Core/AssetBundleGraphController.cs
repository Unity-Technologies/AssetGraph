using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {
	public class AssetBundleGraphController {

		private List<NodeException> m_nodeExceptions;
		private AssetReferenceStreamManager m_streamManager;
		private Model.ConfigGraph m_targetGraph;
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

		public Model.ConfigGraph TargetGraph {
			get {
				return m_targetGraph;
			}
		}

		public BuildTarget ActiveTarget {
			get {
				return m_lastTarget;
			}
			set {
				Perform(value, false, false, null);
			}
		}

		public AssetBundleGraphController(Model.ConfigGraph graph) {
			m_targetGraph = graph;
			m_nodeExceptions = new List<NodeException>();
			m_streamManager = new AssetReferenceStreamManager(m_targetGraph);
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
			bool isBuild,
			bool forceVisitAll,
			Action<Model.NodeData, string, float> updateHandler) 
		{
			LogUtility.Logger.Log(LogType.Log, (isBuild) ? "---Build BEGIN---" : "---Setup BEGIN---");
			m_isBuilding = true;

			if(isBuild) {
				AssetBundleBuildReport.ClearReports();
			}

			foreach(var e in m_nodeExceptions) {
				var errorNode = m_targetGraph.Nodes.Find(n => n.Id == e.Id);
				// errorNode may not be found if user delete it on graph
				if(errorNode != null) {
					LogUtility.Logger.LogFormat(LogType.Log, "[Perform] {0} is marked to revisit due to last error", errorNode.Name);
					errorNode.NeedsRevisit = true;
				}
			}

			m_nodeExceptions.Clear();
			m_lastTarget = target;

			try {
				PerformGraph oldGraph = m_performGraph[gIndex];
				gIndex = (gIndex+1) %2;
				PerformGraph newGraph = m_performGraph[gIndex];
				newGraph.BuildGraphFromSaveData(m_targetGraph, target, oldGraph);

				PerformGraph.Perform performFunc =
					(Model.NodeData data, 
						IEnumerable<PerformGraph.AssetGroups> incoming, 
						IEnumerable<Model.ConnectionData> connectionsToOutput, 
						PerformGraph.Output outputFunc) =>
				{
					DoNodeOperation(target, data, incoming, connectionsToOutput, outputFunc, isBuild, updateHandler);
				};

				newGraph.VisitAll(performFunc, forceVisitAll);

				if(isBuild && m_nodeExceptions.Count == 0) {
					Postprocess();
				}
			}
			catch (NodeException e) {
				m_nodeExceptions.Add(e);
			}
			// Minimize impact of errors happened during node operation
			catch (Exception e) {
				LogUtility.Logger.LogException(e);
			}

			m_isBuilding = false;
			LogUtility.Logger.Log(LogType.Log, (isBuild) ? "---Build END---" : "---Setup END---");
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
					(Model.ConnectionData dst, Dictionary<string, List<AssetReference>> outputGroupAsset) => {}, 
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
			Model.NodeData currentNodeData,
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output outputFunc,
			bool isActualRun,
			Action<Model.NodeData, string, float> updateHandler) 
		{
			try {
				if (updateHandler != null) {
					updateHandler(currentNodeData, "Starting...", 0f);
				}

				if(isActualRun) {
					currentNodeData.Operation.Object.Build(target, currentNodeData, incoming, connectionsToOutput, outputFunc, updateHandler);
				}
				else {
					currentNodeData.Operation.Object.Prepare(target, currentNodeData, incoming, connectionsToOutput, outputFunc);
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

		private void Postprocess () 
		{
			var postprocessType = typeof(IPostprocess);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                var ppTypes = assembly.GetTypes().Select(v => v).Where(v => v != postprocessType && postprocessType.IsAssignableFrom(v)).ToList();
                foreach (var t in ppTypes) {
                    var postprocessScriptInstance = assembly.CreateInstance(t.FullName);
                    if (postprocessScriptInstance == null) {
                        throw new AssetBundleGraphException("Postprocess " + t.Name + " failed to run (failed to create instance from assembly).");
                    }

                    var postprocessInstance = (IPostprocess)postprocessScriptInstance;
                    postprocessInstance.DoPostprocess(AssetBundleBuildReport.BuildReports, AssetBundleBuildReport.ExportReports);
                }
            }
		}

		public void OnAssetsReimported(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {

			// ignore asset reimport event during build
			if(m_isBuilding) {
				return;
			}

			if(m_targetGraph.Nodes == null) {
				return;
			}

			bool isAnyNodeAffected = false;

			foreach(var n in m_targetGraph.Nodes) {
				bool affected = n.Operation.Object.OnAssetsReimported(n, m_streamManager, m_lastTarget, importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
				if(affected) {
					n.NeedsRevisit = true;
				}
				isAnyNodeAffected |= affected;
			}

			if(isAnyNodeAffected) {
				Perform(m_lastTarget, false, false, null);
			}
		}
	}
}
