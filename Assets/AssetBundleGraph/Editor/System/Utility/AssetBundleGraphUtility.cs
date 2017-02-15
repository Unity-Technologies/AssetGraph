using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {
	public class AssetBundleGraphUtility {

		public static void ExecuteGraph(string graphAssetPath) {
			ExecuteGraph(EditorUserBuildSettings.activeBuildTarget, graphAssetPath);
		}

		public static void ExecuteGraph(Model.ConfigGraph graph) {
			ExecuteGraph(EditorUserBuildSettings.activeBuildTarget, graph);
		}

		public static void ExecuteGraph(BuildTarget target, string graphAssetPath) {
			ExecuteGraph(target, AssetDatabase.LoadAssetAtPath<Model.ConfigGraph>(graphAssetPath));
		}

		public static void ExecuteGraph(BuildTarget target, Model.ConfigGraph graph) {

			string assetPath = AssetDatabase.GetAssetPath(graph);

			LogUtility.Logger.LogFormat(LogType.Log, "Executing graph:{0}", assetPath);

			AssetBundleGraphController c = new AssetBundleGraphController(graph);

			// perform setup. Fails if any exception raises.
			c.Perform(target, false, true, null);

			// if there is error reported, then run
			if(c.IsAnyIssueFound) {
				LogUtility.Logger.Log("ExecuteGraph terminated because following error found during Setup phase. Please fix issues by opening editor.");
				c.Issues.ForEach(e => LogUtility.Logger.LogError(LogUtility.kTag, e));

				return;
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
		}
	}
}
