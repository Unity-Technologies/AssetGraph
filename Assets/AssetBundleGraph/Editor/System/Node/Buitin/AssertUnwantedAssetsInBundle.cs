using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine.AssetBundles.GraphTool;
using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

/**
	ImportSetting is the class for apply specific setting to already imported files.
*/
[CustomNode("Assert/Assert Unwanted Assets In Bundle", 80)]
public class AssertUnwantedAssetsInBundle : Node {

	public enum CheckStyle : int {
		AllowOnlyAssetsInDirectory,	// Only allows asssets under given path to be included
		ProhibitAssetsInDirectory	// Prohibit assets under given path to be included
	}

	[SerializeField] private SerializableMultiTargetString m_path;
	[SerializeField] private CheckStyle m_style;

	public override string ActiveStyle {
		get {
			return "node 7 on";
		}
	}

	public override string InactiveStyle {
		get {
			return "node 7";
		}
	}

	public override string Category {
		get {
			return "Assert";
		}
	}

	public override Model.NodeOutputSemantics NodeInputType {
		get {
			return Model.NodeOutputSemantics.AssetBundleConfigurations;
		}
	}

	public override Model.NodeOutputSemantics NodeOutputType {
		get {
			return Model.NodeOutputSemantics.AssetBundleConfigurations;
		}
	}

	public override void Initialize(Model.NodeData data) {
		m_path = new SerializableMultiTargetString();
		m_style = CheckStyle.AllowOnlyAssetsInDirectory;
		data.AddDefaultInputPoint();
		data.AddDefaultOutputPoint();
	}

	public override Node Clone(Model.NodeData newData) {
		var newNode = new AssertUnwantedAssetsInBundle();
		newNode.m_path = new SerializableMultiTargetString(m_path);
		newNode.m_style = m_style;
		newData.AddDefaultInputPoint();
		newData.AddDefaultOutputPoint();
		return newNode;
	}

	public override bool OnAssetsReimported(
		Model.NodeData nodeData,
		AssetReferenceStreamManager streamManager,
		BuildTarget target, 
		string[] importedAssets, 
		string[] deletedAssets, 
		string[] movedAssets, 
		string[] movedFromAssetPaths)
	{
		return true;
	}


	public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged) {

		EditorGUILayout.HelpBox("Assert Unwanted Assets In Bundle: Checks if unwanted assets are included in bundle configurations.", MessageType.Info);
		editor.UpdateNodeName(node);

		GUILayout.Space(10f);

		var newValue = (CheckStyle)EditorGUILayout.EnumPopup("Checking Style", m_style);
		if(newValue != m_style) {
			using(new RecordUndoScope("Change Checking Style", node, true)) {
				m_style = newValue;
				onValueChanged();
			}
		}

		GUILayout.Space(4f);

		//Show target configuration tab
		editor.DrawPlatformSelector(node);
		using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
			var disabledScope = editor.DrawOverrideTargetToggle(node, m_path.ContainsValueOf(editor.CurrentEditingGroup), (bool b) => {
				using(new RecordUndoScope("Remove Target Load Path Settings", node, true)) {
					if(b) {
						m_path[editor.CurrentEditingGroup] = m_path.DefaultValue;
					} else {
						m_path.Remove(editor.CurrentEditingGroup);
					}
					onValueChanged();
				}
			});

			using (disabledScope) {
				var path = m_path[editor.CurrentEditingGroup];
				EditorGUILayout.LabelField("Path:");

				string newLoadPath = null;

				using(new EditorGUILayout.HorizontalScope()) {
					newLoadPath = EditorGUILayout.TextField(Model.Settings.ASSETS_PATH, path);

					if(GUILayout.Button("Select", GUILayout.Width(50f))) {
						var folderSelected = 
							EditorUtility.OpenFolderPanel("Select Asset Folder", 
								FileUtility.PathCombine(Model.Settings.ASSETS_PATH, path), "");
						if(!string.IsNullOrEmpty(folderSelected)) {
							var dataPath = Application.dataPath;
							if(dataPath == folderSelected) {
								folderSelected = string.Empty;
							} else {
								var index = folderSelected.IndexOf(dataPath);
								if(index >= 0 ) {
									folderSelected = folderSelected.Substring(dataPath.Length + index);
									if(folderSelected.IndexOf('/') == 0) {
										folderSelected = folderSelected.Substring(1);
									}
								}
							}
							newLoadPath = folderSelected;
						}
					}
				}

				if (newLoadPath != path) {
					using(new RecordUndoScope("Path Change", node, true)){
						m_path[editor.CurrentEditingGroup] = newLoadPath;
						onValueChanged();
					}
				}

				var dirPath = Path.Combine(Model.Settings.ASSETS_PATH,newLoadPath);
				bool dirExists = Directory.Exists(dirPath);

				GUILayout.Space(10f);

				using (new EditorGUILayout.HorizontalScope()) {
					using(new EditorGUI.DisabledScope(string.IsNullOrEmpty(newLoadPath)||!dirExists)) 
					{
						GUILayout.FlexibleSpace();
						if(GUILayout.Button("Highlight in Project Window", GUILayout.Width(180f))) {
							// trailing is "/" not good for LoadMainAssetAtPath
							if(dirPath[dirPath.Length-1] == '/') {
								dirPath = dirPath.Substring(0, dirPath.Length-1);
							}
							var obj = AssetDatabase.LoadMainAssetAtPath(dirPath);
							EditorGUIUtility.PingObject(obj);
						}
					}
				}
			}
		}	
	}

	/**
	 * Prepare is called whenever graph needs update. 
	 */ 
	public override void Prepare (BuildTarget target, 
		Model.NodeData node, 
		IEnumerable<PerformGraph.AssetGroups> incoming, 
		IEnumerable<Model.ConnectionData> connectionsToOutput, 
		PerformGraph.Output Output) 
	{
		// Pass incoming assets straight to Output
		if(Output != null) {
			var destination = (connectionsToOutput == null || !connectionsToOutput.Any())? 
				null : connectionsToOutput.First();

			if(incoming != null) {

				var checkPath = m_path[target];
				var allow = m_style == CheckStyle.AllowOnlyAssetsInDirectory;

				foreach(var ag in incoming) {
					if(!string.IsNullOrEmpty(checkPath)) {
						foreach (var assets in ag.assetGroups.Values) {
							foreach(var a in assets) {

								if(allow != a.importFrom.Contains(checkPath)) {
									throw new NodeException(node.Name + ":Unwanted asset '" + a.importFrom + "' found.", node.Id);
								}

								var dependencies = AssetDatabase.GetDependencies(new string[] { a.importFrom } );

								foreach(var d in dependencies) {
									if(allow != d.Contains(checkPath)) {
										throw new NodeException(node.Name + ":Unwanted asset found in dependency:'" + d 
											+ "' from following asset:" + a.importFrom, node.Id);
									}
								}
							}
						}
						Output(destination, ag.assetGroups);
					}
				}
			} else {
				// Overwrite output with empty Dictionary when no there is incoming asset
				Output(destination, new Dictionary<string, List<AssetReference>>());
			}
		}
	}
}
