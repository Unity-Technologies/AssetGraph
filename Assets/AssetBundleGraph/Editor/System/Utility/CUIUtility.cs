using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace AssetBundleGraph {
	public class CUIUtility {

		private static readonly string kCommandMethod = "AssetBundleGraph.CUIUtility.BuildFromCommandline";

		private static readonly string kCommandStr = 
			"{0} -batchmode -quit -projectPath {1} -logFile abbuild.log -executeMethod {2} {3}";

		private static readonly string kCommandName = 
			"buildassetbundle.{0}";

		[MenuItem(AssetBundleGraphSettings.GUI_TEXT_MENU_GENERATE_CUITOOL)]
		private static void CreateCUITool() {
			var appCmd = string.Format("{0}{1}", EditorApplication.applicationPath, (Application.platform == RuntimePlatform.WindowsEditor)? "" : "/Contents/MacOS/Unity");
			var argPass = (Application.platform == RuntimePlatform.WindowsEditor)? "%1 %2 %3 %4 %5 %6 %7 %8 %9" : "$*";
			var cmd = string.Format(kCommandStr, appCmd, FileUtility.ProjectPathWithSlash(), kCommandMethod, argPass);
			var ext = (Application.platform == RuntimePlatform.WindowsEditor)? "bat" : "sh";
			var cmdFile = string.Format(kCommandName, ext );

			var destinationPath = FileUtility.PathCombine(AssetBundleGraphSettings.CUISPACE_PATH, cmdFile);

			Directory.CreateDirectory(AssetBundleGraphSettings.CUISPACE_PATH);
			File.WriteAllText(destinationPath, cmd);

			AssetDatabase.Refresh();
		}

		/**
		 * Build from commandline - entrypoint.
		 */ 
		[MenuItem("Window/AssetBundleGraph/Debug/CUI Build")]
		public static void BuildFromCommandline(){
			try {
				var arguments = new List<string>(System.Environment.GetCommandLineArgs());

				Application.SetStackTraceLogType(LogType.Log, 		StackTraceLogType.None);
				Application.SetStackTraceLogType(LogType.Error, 	StackTraceLogType.None);
				Application.SetStackTraceLogType(LogType.Warning, 	StackTraceLogType.None);

				BuildTarget target = EditorUserBuildSettings.activeBuildTarget;

				int targetIndex = arguments.FindIndex(a => a == "-target");

				if(targetIndex >= 0) {
					var targetStr = arguments[targetIndex+1];
					Debug.Log("Target specified:"+ targetStr);

					var newTarget = BuildTargetUtility.BuildTargetFromString(arguments[targetIndex+1]);
					if(!BuildTargetUtility.IsBuildTargetSupported(newTarget)) {
						throw new AssetBundleGraphException(newTarget + " is not supported to build with this Unity. Please install platform support with installer(s).");
					}

					if(newTarget != target) {
						EditorUserBuildSettings.SwitchActiveBuildTarget(newTarget);
						target = newTarget;
					}
				}

				Debug.Log("Asset bundle building for:" + BuildTargetUtility.TargetToHumaneString(target));

				if (!SaveData.IsSaveDataAvailableAtDisk()) {
					Debug.Log("AssetBundleGraph save data not found. Aborting...");
					return;
				}

				// load data from file.
				SaveData saveData = SaveData.LoadFromDisk();
				List<NodeException> errors = new List<NodeException>();
				Dictionary<ConnectionData,Dictionary<string, List<Asset>>> result = null;

				Action<NodeException> errorHandler = (NodeException e) => {
					errors.Add(e);
				};

				// perform setup. Fails if any exception raises.
				AssetBundleGraphController.Perform(saveData, target, false, errorHandler, null);

				// if there is error reported, then run
				if(errors.Count > 0) {
					Debug.Log("Build terminated because following error found during Setup phase. Please fix issues by opening editor before building.");
					errors.ForEach(e => Debug.LogError(e));

					return;
				}

				NodeData lastNodeData = null;
				float lastProgress = 0.0f;
				Action<NodeData, float> updateHandler = (NodeData node, float progress) => {
					if(node != null && lastNodeData != node) {
						lastNodeData = node;
						lastProgress = progress;

						Debug.LogFormat("Processing {0}...", node.Name);
					}
					if(progress > lastProgress) {
						if(progress <= 1.0f) {
							Debug.LogFormat("{0} Complete.", node.Name);
						} else if( (progress - lastProgress) > 0.2f ) {
							Debug.LogFormat("{0}: {1} %", node.Name, (int)progress*100f);
						}
						lastProgress = progress;
					}
				};

				// run datas.
				result = AssetBundleGraphController.Perform(saveData, target, true, errorHandler, updateHandler);

				AssetDatabase.Refresh();
				AssetBundleGraphController.Postprocess(saveData, result, true);

			} catch(Exception e) {
				Debug.LogError(e);
				Debug.LogError("Building asset bundles terminated due to unexpected error.");
			} finally {
				Debug.Log("End of build.");
			}
		}
	}
}
