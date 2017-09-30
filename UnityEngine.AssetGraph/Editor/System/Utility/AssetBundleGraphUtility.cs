using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {

    /// <summary>
    /// Execute graph result.
    /// </summary>
	public class ExecuteGraphResult {
		private Model.ConfigGraph  	graph;
		private List<NodeException>	issues;

		public ExecuteGraphResult(Model.ConfigGraph g, List<NodeException> issues) {
			this.graph  = g;
			this.issues = issues;
		}

        /// <summary>
        /// Gets a value indicating whether last graph execution has any issue found.
        /// </summary>
        /// <value><c>true</c> if this instance is any issue found; otherwise, <c>false</c>.</value>
		public bool IsAnyIssueFound {
			get {
				return issues.Count > 0;
			}
		}

        /// <summary>
        /// Gets the executed graph associated with this result.
        /// </summary>
        /// <value>The graph.</value>
		public Model.ConfigGraph Graph {
			get {
				return graph;
			}
		}

        /// <summary>
        /// Gets the graph asset path.
        /// </summary>
        /// <value>The graph asset path.</value>
		public string GraphAssetPath {
			get {
				return AssetDatabase.GetAssetPath(graph);
			}
		}

        /// <summary>
        /// Gets the list of issues found during last execution.
        /// </summary>
		public IEnumerable<NodeException> Issues {
			get {
				return issues.AsEnumerable();
			}
		}
	}

    /// <summary>
    /// The helper utility to execute graph and graph collection from API.
    /// </summary>
	public class AssetBundleGraphUtility {

        /// <summary>
        /// Executes the graph collection.
        /// </summary>
        /// <returns>The graph collection.</returns>
        /// <param name="collectionName">Collection name.</param>
		public static List<ExecuteGraphResult> ExecuteGraphCollection(string collectionName) {
			return ExecuteGraphCollection(EditorUserBuildSettings.activeBuildTarget, collectionName);
		}

        /// <summary>
        /// Executes the graph collection.
        /// </summary>
        /// <returns>The graph collection.</returns>
        /// <param name="t">T.</param>
        /// <param name="collectionName">Collection name.</param>
		public static List<ExecuteGraphResult> ExecuteGraphCollection(BuildTarget t, string collectionName) {
			var c = BatchBuildConfig.GetConfig().Find(collectionName);
			if(c == null) {
				throw new AssetBundleGraphException(
					string.Format("Failed to build with graph collection. Graph collection '{0}' not found. ", collectionName)
				);
			}

			return ExecuteGraphCollection(t, c);
		}

        /// <summary>
        /// Executes the graph collection.
        /// </summary>
        /// <returns>The graph collection.</returns>
        /// <param name="c">C.</param>
		public static List<ExecuteGraphResult> ExecuteGraphCollection(BatchBuildConfig.GraphCollection c) {
			return ExecuteGraphCollection(EditorUserBuildSettings.activeBuildTarget, c);
		}

        /// <summary>
        /// Executes the graph collection.
        /// </summary>
        /// <returns>The graph collection.</returns>
        /// <param name="t">T.</param>
        /// <param name="c">C.</param>
		public static List<ExecuteGraphResult> ExecuteGraphCollection(BuildTarget t, BatchBuildConfig.GraphCollection c) {

            AssetBundleBuildMap.GetBuildMap ().Clear ();

            List<ExecuteGraphResult> resultCollection = new List<ExecuteGraphResult>(c.GraphGUIDs.Count);

			foreach(var guid in c.GraphGUIDs) {
				string path = AssetDatabase.GUIDToAssetPath(guid);
				if(path != null) {
					var r = ExecuteGraph(t, path);
					resultCollection.Add(r);
				} else {
					LogUtility.Logger.LogFormat(LogType.Warning, "Failed to build graph in collection {0}: graph with guid {1} not found.",
						c.Name, guid);
				}
			}

			return  resultCollection;
		}

        /// <summary>
        /// Executes the graph.
        /// </summary>
        /// <returns>The graph.</returns>
        /// <param name="graphAssetPath">Graph asset path.</param>
		public static ExecuteGraphResult ExecuteGraph(string graphAssetPath) {
			return ExecuteGraph(EditorUserBuildSettings.activeBuildTarget, graphAssetPath);
		}

        /// <summary>
        /// Executes the graph.
        /// </summary>
        /// <returns>The graph.</returns>
        /// <param name="graph">Graph.</param>
		public static ExecuteGraphResult ExecuteGraph(Model.ConfigGraph graph) {
			return ExecuteGraph(EditorUserBuildSettings.activeBuildTarget, graph);
		}

        /// <summary>
        /// Executes the graph.
        /// </summary>
        /// <returns>The graph.</returns>
        /// <param name="target">Target.</param>
        /// <param name="graphAssetPath">Graph asset path.</param>
		public static ExecuteGraphResult ExecuteGraph(BuildTarget target, string graphAssetPath) {
			return ExecuteGraph(target, AssetDatabase.LoadAssetAtPath<Model.ConfigGraph>(graphAssetPath));
		}

        /// <summary>
        /// Executes the graph.
        /// </summary>
        /// <returns>The graph.</returns>
        /// <param name="target">Target.</param>
        /// <param name="graph">Graph.</param>
		public static ExecuteGraphResult ExecuteGraph(BuildTarget target, Model.ConfigGraph graph) {

			string assetPath = AssetDatabase.GetAssetPath(graph);

			LogUtility.Logger.LogFormat(LogType.Log, "Executing graph:{0}", assetPath);

			AssetBundleGraphController c = new AssetBundleGraphController(graph);

			// perform setup. Fails if any exception raises.
			c.Perform(target, false, true, null);

			// if there is error reported, then run
			if(c.IsAnyIssueFound) {
				return new ExecuteGraphResult(graph, c.Issues);
			}

			Model.NodeData lastNodeData = null;
			float lastProgress = 0.0f;

			Action<Model.NodeData, string, float> updateHandler = (Model.NodeData node, string message, float progress) => {
				if(node != null && lastNodeData != node) {
					lastNodeData = node;
					lastProgress = progress;

					LogUtility.Logger.LogFormat(LogType.Log, "Processing {0}", node.Name);
				}
				if(progress > lastProgress) {
					if(progress <= 1.0f) {
						LogUtility.Logger.LogFormat(LogType.Log, "{0} Complete.", node.Name);
					} else if( (progress - lastProgress) > 0.2f ) {
						LogUtility.Logger.LogFormat(LogType.Log, "{0}: {1} % : {2}", node.Name, (int)progress*100f, message);
					}
					lastProgress = progress;
				}
			};

			// run datas.
			c.Perform(target, true, true, updateHandler);

			AssetDatabase.Refresh();

			return new ExecuteGraphResult(graph, c.Issues);
		}
	}
}
