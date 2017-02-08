using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {

	[CustomNode("Exporter", 80)]
	public class Exporter : Node {

		public enum ExportOption : int {
			ErrorIfNoExportDirectoryFound,
			AutomaticallyCreateIfNoExportDirectoryFound,
			DeleteAndRecreateExportDirectory
		}

		[SerializeField] private SerializableMultiTargetString m_exportPath;
		[SerializeField] private SerializableMultiTargetInt m_exportOption;

		public override string ActiveStyle {
			get {
				return "flow node 0 on";
			}
		}

		public override string InactiveStyle {
			get {
				return "flow node 0";
			}
		}

		public override Model.NodeOutputSemantics NodeInputType {
			get {
				return 
					(Model.NodeOutputSemantics) 
					((uint)Model.NodeOutputSemantics.Assets | 
					 (uint)Model.NodeOutputSemantics.AssetBundles);
			}
		}

		public override Model.NodeOutputSemantics NodeOutputType {
			get {
				return Model.NodeOutputSemantics.None;
			}
		}

		public override void Initialize(Model.NodeData data) {
			//Take care of this with Initialize(NodeData)
			m_exportPath = new SerializableMultiTargetString();
			m_exportOption = new SerializableMultiTargetInt();

			data.AddInputPoint(Model.Settings.DEFAULT_INPUTPOINT_LABEL);
		}

		public override Node Clone() {
			var newNode = new Exporter();
			newNode.m_exportPath = new SerializableMultiTargetString(m_exportPath);
			newNode.m_exportOption = new SerializableMultiTargetInt(m_exportOption);

			return newNode;
		}

		public override bool IsEqual(Node node) {
			Exporter rhs = node as Exporter;
			return rhs != null && 
				m_exportPath == rhs.m_exportPath &&
				m_exportOption == rhs.m_exportOption;
		}

		public override string Serialize() {
			return JsonUtility.ToJson(this);
		}

		public override void OnInspectorGUI(NodeGUI node, NodeGUIEditor editor, Action onValueChanged) {
			
			if (m_exportPath == null) {
				return;
			}

			var currentEditingGroup = editor.CurrentEditingGroup;

			EditorGUILayout.HelpBox("Exporter: Export given files to output directory.", MessageType.Info);
			editor.UpdateNodeName(node);

			GUILayout.Space(10f);

			//Show target configuration tab
			editor.DrawPlatformSelector(node);
			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var disabledScope = editor.DrawOverrideTargetToggle(node, m_exportPath.ContainsValueOf(currentEditingGroup), (bool enabled) => {
					using(new RecordUndoScope("Remove Target Export Settings", node, true)){
						if(enabled) {
							m_exportPath[currentEditingGroup] = m_exportPath.DefaultValue;
						}  else {
							m_exportPath.Remove(currentEditingGroup);
						}
						onValueChanged();
					}
				} );

				using (disabledScope) {
					ExportOption opt = (ExportOption)m_exportOption[currentEditingGroup];
					var newOption = (ExportOption)EditorGUILayout.EnumPopup("Export Option", opt);
					if(newOption != opt) {
						using(new RecordUndoScope("Change Export Option", node, true)){
							m_exportOption[currentEditingGroup] = (int)newOption;
							onValueChanged();
						}
					}

					EditorGUILayout.LabelField("Export Path:");
					var newExportPath = EditorGUILayout.TextField(
						SystemDataUtility.GetProjectName(), 
						m_exportPath[currentEditingGroup]
					);

					var exporterNodePath = FileUtility.GetPathWithProjectPath(newExportPath);
					if(ValidateExportPath(
						newExportPath,
						exporterNodePath,
						() => {
						},
						() => {
							using (new EditorGUILayout.HorizontalScope()) {
								EditorGUILayout.LabelField(exporterNodePath + " does not exist.");
								if(GUILayout.Button("Create directory")) {
									Directory.CreateDirectory(exporterNodePath);
								}
								onValueChanged();
							}
							EditorGUILayout.Space();

							EditorGUILayout.LabelField("Available Directories:");
							string[] dirs = Directory.GetDirectories(Path.GetDirectoryName(exporterNodePath));
							foreach(string s in dirs) {
								EditorGUILayout.LabelField(s);
							}
						}
					)) {
						using (new EditorGUILayout.HorizontalScope()) {
							GUILayout.FlexibleSpace();
							#if UNITY_EDITOR_OSX
							string buttonName = "Reveal in Finder";
							#else
							string buttonName = "Show in Explorer";
							#endif 
							if(GUILayout.Button(buttonName)) {
								EditorUtility.RevealInFinder(exporterNodePath);
							}
						}
					}

					if (newExportPath != m_exportPath[currentEditingGroup]) {
						using(new RecordUndoScope("Change Export Path", node, true)){
							m_exportPath[currentEditingGroup] = newExportPath;
							onValueChanged();
						}
					}
				}
			}
		}

		public override void Prepare (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			ValidateExportPath(
				m_exportPath[target],
				FileUtility.GetPathWithProjectPath(m_exportPath[target]),
				() => {
					throw new NodeException(node.Name + ":Export Path is empty.", node.Id);
				},
				() => {
					if( m_exportOption[target] == (int)ExportOption.ErrorIfNoExportDirectoryFound ) {
						throw new NodeException(node.Name + ":Directory set to Export Path does not exist. Path:" + m_exportPath[target], node.Id);
					}
				}
			);
		}
		
		public override void Build (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output,
			Action<Model.NodeData, string, float> progressFunc) 
		{
			Export(target, node, incoming, connectionsToOutput, progressFunc);
		}

		private void Export (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			Action<Model.NodeData, string, float> progressFunc) 
		{
			if(incoming == null) {
				return;
			}

			var exportPath = FileUtility.GetPathWithProjectPath(m_exportPath[target]);

			if(m_exportOption[target] == (int)ExportOption.DeleteAndRecreateExportDirectory) {
				if (Directory.Exists(exportPath)) {
					Directory.Delete(exportPath, true);
				}
			}

			if(m_exportOption[target] != (int)ExportOption.ErrorIfNoExportDirectoryFound) {
				if (!Directory.Exists(exportPath)) {
					Directory.CreateDirectory(exportPath);
				}
			}

			var report = new ExportReport(node);

			foreach(var ag in incoming) {
				foreach (var groupKey in ag.assetGroups.Keys) {
					var inputSources = ag.assetGroups[groupKey];

					foreach (var source in inputSources) {					
						var destinationSourcePath = source.importFrom;

						// in bundleBulider, use platform-package folder for export destination.
						if (destinationSourcePath.StartsWith(Model.Settings.BUNDLEBUILDER_CACHE_PLACE)) {
							var depth = Model.Settings.BUNDLEBUILDER_CACHE_PLACE.Split(Model.Settings.UNITY_FOLDER_SEPARATOR).Length + 1;

							var splitted = destinationSourcePath.Split(Model.Settings.UNITY_FOLDER_SEPARATOR);
							var reducedArray = new string[splitted.Length - depth];

							Array.Copy(splitted, depth, reducedArray, 0, reducedArray.Length);
							var fromDepthToEnd = string.Join(Model.Settings.UNITY_FOLDER_SEPARATOR.ToString(), reducedArray);

							destinationSourcePath = fromDepthToEnd;
						}

						var destination = FileUtility.PathCombine(exportPath, destinationSourcePath);

						var parentDir = Directory.GetParent(destination).ToString();

						if (!Directory.Exists(parentDir)) {
							Directory.CreateDirectory(parentDir);
						}
						if (File.Exists(destination)) {
							File.Delete(destination);
						}
						if (string.IsNullOrEmpty(source.importFrom)) {
							report.AddErrorEntry(source.absolutePath, destination, "Source Asset import path is empty; given asset is not imported by Unity.");
							continue;
						}
						try {
							if(progressFunc != null) progressFunc(node, string.Format("Copying {0}", source.fileNameAndExtension), 0.5f);
							File.Copy(source.importFrom, destination);
							report.AddExportedEntry(source.importFrom, destination);
						} catch(Exception e) {
							report.AddErrorEntry(source.importFrom, destination, e.Message);
						}

						source.exportTo = destination;
					}
				}
			}

			AssetBundleBuildReport.AddExportReport(report);
		}

		public static bool ValidateExportPath (string currentExportFilePath, string combinedPath, Action NullOrEmpty, Action DoesNotExist) {
			if (string.IsNullOrEmpty(currentExportFilePath)) {
				NullOrEmpty();
				return false;
			}
			if (!Directory.Exists(combinedPath)) {
				DoesNotExist();
				return false;
			}
			return true;
		}
	}
}