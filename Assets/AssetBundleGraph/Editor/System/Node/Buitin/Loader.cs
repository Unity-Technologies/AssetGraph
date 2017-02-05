using UnityEngine;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {

	[CustomNode("Loader", 10)]
	public class Loader : INode {

		[SerializeField] private SerializableMultiTargetString m_loadPath;

		public string ActiveStyle {
			get {
				return "flow node 0 on";
			}
		}

		public string InactiveStyle {
			get {
				return "flow node 0";
			}
		}
			
		public void Initialize(Model.NodeData data) {
		}

		public INode Clone() {
			return null;
		}

		public bool Validate(List<Model.NodeData> allNodes, List<Model.ConnectionData> allConnections) {
			return false;
		}

		public bool IsEqual(INode node) {
			return false;
		}

		public string Serialize() {
			return string.Empty;
		}

		public bool IsValidInputConnectionPoint(Model.ConnectionPointData point) {
			return false;
		}

		public bool CanConnectFrom(INode fromNode) {
			return false;
		}

		public bool OnAssetsReimported(BuildTarget target, 
			string[] importedAssets, 
			string[] deletedAssets, 
			string[] movedAssets, 
			string[] movedFromAssetPaths)
		{
			return false;
		}

		public void OnNodeGUI(NodeGUI node) {
		}

		public void OnInspectorGUI (NodeGUI node, NodeGUIEditor editor) {
			
			if (m_loadPath == null) {
				return;
			}

			EditorGUILayout.HelpBox("Loader: Load assets in given directory path.", MessageType.Info);
			editor.UpdateNodeName(node);

			GUILayout.Space(10f);

			//Show target configuration tab
			editor.DrawPlatformSelector(node);
			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var disabledScope = editor.DrawOverrideTargetToggle(node, m_loadPath.ContainsValueOf(editor.CurrentEditingGroup), (bool b) => {
					using(new RecordUndoScope("Remove Target Load Path Settings", node, true)) {
						if(b) {
							m_loadPath[editor.CurrentEditingGroup] = m_loadPath.DefaultValue;
						} else {
							m_loadPath.Remove(editor.CurrentEditingGroup);
						}
					}
				});

				using (disabledScope) {
					var path = m_loadPath[editor.CurrentEditingGroup];
					EditorGUILayout.LabelField("Load Path:");

					var newLoadPath = EditorGUILayout.TextField(Model.Settings.ASSETS_PATH, path);
					if (newLoadPath != path) {
						using(new RecordUndoScope("Load Path Changed", node, true)){
							m_loadPath[editor.CurrentEditingGroup] = newLoadPath;
						}
					}

					var dirPath = Path.Combine(Model.Settings.ASSETS_PATH,newLoadPath);
					bool dirExists = Directory.Exists(dirPath);

					using (new EditorGUILayout.HorizontalScope()) {
						using(new EditorGUI.DisabledScope(string.IsNullOrEmpty(newLoadPath)||!dirExists)) 
						{
							GUILayout.FlexibleSpace();
							if(GUILayout.Button("Select in Project Window", GUILayout.Width(150))) {
								var obj = AssetDatabase.LoadMainAssetAtPath(dirPath);
								EditorGUIUtility.PingObject(obj);
							}
						}
					}

					if(!dirExists) {
						EditorGUILayout.LabelField("Available Directories:");
						string[] dirs = Directory.GetDirectories(Path.GetDirectoryName(dirPath));
						foreach(string s in dirs) {
							EditorGUILayout.LabelField(s);
						}
					}
				}
			}
		}


		public void Prepare (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			ValidateLoadPath(
				m_loadPath[target],
				GetLoaderFullLoadPath(target),
				() => {
					//can be empty
					//throw new NodeException(node.Name + ": Load Path is empty.", node.Id);
				}, 
				() => {
					throw new NodeException(node.Name + ": Directory not found: " + GetLoaderFullLoadPath(target), node.Id);
				}
			);

			Load(target, node, connectionsToOutput, Output);
		}
		
		public void Build (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output,
			Action<Model.NodeData, string, float> progressFunc) 
		{
			//Load operation is completed furing Setup() phase, so do nothing in Run.
		}

		void Load (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{

			if(connectionsToOutput == null || Output == null) {
				return;
			}

			// SOMEWHERE_FULLPATH/PROJECT_FOLDER/Assets/
			var assetsFolderPath = Application.dataPath + Model.Settings.UNITY_FOLDER_SEPARATOR;

			var outputSource = new List<AssetReference>();
			var targetFilePaths = FileUtility.GetAllFilePathsInFolder(GetLoaderFullLoadPath(target));

			foreach (var targetFilePath in targetFilePaths) {

				if(targetFilePath.Contains(Model.Settings.ASSETBUNDLEGRAPH_PATH)) {
					continue;
				}

				// already contained into Assets/ folder.
				// imported path is Assets/SOMEWHERE_FILE_EXISTS.
				if (targetFilePath.StartsWith(assetsFolderPath)) {
					var relativePath = targetFilePath.Replace(assetsFolderPath, Model.Settings.ASSETS_PATH);

					var r = AssetReferenceDatabase.GetReference(relativePath);

					if(!TypeUtility.IsLoadingAsset(r)) {
						continue;
					}

					if(r != null) {
						outputSource.Add(AssetReferenceDatabase.GetReference(relativePath));
					}
					continue;
				}

				throw new NodeException(node.Name + ": Invalid Load Path. Path must start with Assets/", node.Name);
			}

			var output = new Dictionary<string, List<AssetReference>> {
				{"0", outputSource}
			};

			var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
				null : connectionsToOutput.First();
			Output(dst, output);
		}

		public static void ValidateLoadPath (string currentLoadPath, string combinedPath, Action NullOrEmpty, Action NotExist) {
			if (string.IsNullOrEmpty(currentLoadPath)) NullOrEmpty();
			if (!Directory.Exists(combinedPath)) NotExist();
		}

		private string GetLoaderFullLoadPath(BuildTarget g) {
			return FileUtility.PathCombine(Application.dataPath, m_loadPath[g]);
		}
	}
}