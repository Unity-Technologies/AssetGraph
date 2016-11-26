using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace AssetBundleGraph {
	/*
	 * AssetBundleGraphController executes operations based on graph 
	 */
	public class AssetBundleGraphController {

		private SaveData m_saveData;
		private List<NodeException> m_nodeExceptions;
		private AssetReferenceStreamManager m_streamManager;
		private PerformGraph[] m_performGraph;
		private int gIndex;

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
			SaveData saveData, 
			BuildTarget target,
			bool isRun,
			bool callPostprocess,
			Action<NodeData, float> updateHandler) 
		{
			m_nodeExceptions.Clear();
			m_saveData = saveData;

			try {
				PerformGraph oldGraph = m_performGraph[gIndex];
				gIndex = (gIndex+1) %2;
				PerformGraph newGraph = m_performGraph[gIndex];
				newGraph.BuildGraphFromSaveData(saveData, target, oldGraph);

				PerformGraph.Perform performFunc =
					(NodeData data, 
						ConnectionData src, 
						ConnectionData dst, 
						Dictionary<string, List<AssetReference>> inputGroups, 
						PerformGraph.Output outputFunc) =>
				{
					DoNodeOperation(target, data, src, dst, inputGroups, outputFunc, isRun, updateHandler);
				};

				newGraph.VisitAll(performFunc, isRun);

				if(callPostprocess) {
					Postprocess(isRun);
				}

			} catch (NodeException e) {
				m_nodeExceptions.Add(e);
			}
			Profiler.EndSample();
		}

		public void Validate (
			NodeGUI node, 
			BuildTarget target) 
		{
			m_nodeExceptions.RemoveAll(e => e.Id == node.Data.Id);

			try {
				Debug.LogFormat("[validate] {0} validate", node.Name);
				DoNodeOperation(target, node.Data, null, null, new Dictionary<string, List<AssetReference>>(), 
					(Dictionary<string, List<AssetReference>> outputGroupAsset) => {}, 
					false, null);
				
			} catch (NodeException e) {
				m_nodeExceptions.Add(e);
			}
			Profiler.EndSample();
		}

		/**
			Perform Run or Setup from parent of given terminal node recursively.
		*/
		private void DoNodeOperation (
			BuildTarget target,
			NodeData currentNodeData,
			ConnectionData sourceConnection,
			ConnectionData destinationConnection,
			Dictionary<string, List<AssetReference>> inputGroupAssets,
			PerformGraph.Output outputFunc,
			bool isActualRun,
			Action<NodeData, float> updateHandler) 
		{
			if (updateHandler != null) {
				updateHandler(currentNodeData, 0f);
			}

			INodeOperation executor = CreateOperation(currentNodeData);
			if(executor != null) {
				if(isActualRun) {
					executor.Run(target, currentNodeData, sourceConnection, destinationConnection, inputGroupAssets, outputFunc);
				}
				else {
					executor.Setup(target, currentNodeData, sourceConnection, destinationConnection, inputGroupAssets, outputFunc);
				}
			}

			if (updateHandler != null) {
				updateHandler(currentNodeData, 1f);
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
						Debug.LogError(currentNodeData.Name + " is defined as unknown kind of node. value:" + currentNodeData.Kind);
						break;
					}
				} 
			} catch (NodeException e) {
				m_nodeExceptions.Add(e);
			}

			return executor;
		}

		private void Postprocess (bool isRun) 
		{
			var postprocessType = typeof(IPostprocess);
			var ppTypes = Assembly.GetExecutingAssembly().GetTypes().Select(v => v).Where(v => v != postprocessType && postprocessType.IsAssignableFrom(v)).ToList();
			foreach (var t in ppTypes) {
				var postprocessScriptInstance = Assembly.GetExecutingAssembly().CreateInstance(t.Name);
				if (postprocessScriptInstance == null) {
					throw new AssetBundleGraphException("Postprocess " + t.Name + " failed to run (failed to create instance from assembly).");
				}

				//TODO: call postprocess with proper output
//				var postprocessInstance = (IPostprocess)postprocessScriptInstance;
//				postprocessInstance.Run(nodeResult, isRun);
			}
		}

		public void OnAssetsReimported(string[] assetPaths, BuildTarget target) {

			if(m_saveData == null || m_saveData.Nodes == null) {
				return;
			}

			foreach(var node in m_saveData.Nodes) {
				if(node.Kind == NodeKind.LOADER_GUI) {
					var loadPath = node.LoaderLoadPath[target];
					if(string.IsNullOrEmpty(loadPath)) {
						Debug.LogFormat("{0} is marked to revisit", node.Name);
						node.NeedsRevisit = true;
					}
					foreach(var path in assetPaths) {
						if(path.StartsWith(node.LoaderLoadPath[target])) {
							Debug.LogFormat("{0} is marked to revisit", node.Name);
							node.NeedsRevisit = true;
							break;
						}
					}
				}
			}
		}
	}
}
