using UnityEngine;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;

using V1=AssetBundleGraph;
using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {

	[CustomNode("Load Assets/Load By Search Filter", 11)]
	public class LoaderBySearch : Node {

		[SerializeField] private SerializableMultiTargetString m_searchFilter;

		public override string ActiveStyle {
			get {
				return "node 0 on";
			}
		}

		public override string InactiveStyle {
			get {
				return "node 0";
			}
		}
			
		public override string Category {
			get {
				return "Load";
			}
		}

		public override Model.NodeOutputSemantics NodeInputType {
			get {
				return Model.NodeOutputSemantics.None;
			}
		}

		public override void Initialize(Model.NodeData data) {
			m_searchFilter = new SerializableMultiTargetString();

			data.AddDefaultOutputPoint();
		}

		public override Node Clone(Model.NodeData newData) {
			var newNode = new LoaderBySearch();
			newNode.m_searchFilter = new SerializableMultiTargetString(m_searchFilter);

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

			if (m_searchFilter == null) {
				return;
			}

			EditorGUILayout.HelpBox("Load By Search Filter: Load assets match given search filter condition.", MessageType.Info);
			editor.UpdateNodeName(node);

			GUILayout.Space(10f);

			//Show target configuration tab
			editor.DrawPlatformSelector(node);
			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var disabledScope = editor.DrawOverrideTargetToggle(node, m_searchFilter.ContainsValueOf(editor.CurrentEditingGroup), (bool b) => {
					using(new RecordUndoScope("Remove Target Search Filter Settings", node, true)) {
						if(b) {
							m_searchFilter[editor.CurrentEditingGroup] = m_searchFilter.DefaultValue;
						} else {
							m_searchFilter.Remove(editor.CurrentEditingGroup);
						}
						onValueChanged();
					}
				});

				using (disabledScope) {
					var condition = m_searchFilter[editor.CurrentEditingGroup];
					EditorGUILayout.LabelField("Search Filter");

					string newCondition = null;

					using(new EditorGUILayout.HorizontalScope()) {
						newCondition = EditorGUILayout.TextField(condition);
					}

					if (newCondition != condition) {
						using(new RecordUndoScope("Modify Search Filter", node, true)){
							m_searchFilter[editor.CurrentEditingGroup] = newCondition;
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
			ValidateSearchCondition(
				m_searchFilter[target],
				() => {
					throw new NodeException(node.Name + ": Serach filter is empty", node.Id);
				}
			);

			Load(target, node, connectionsToOutput, Output);
		}
		
		void Load (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{

			if(connectionsToOutput == null || Output == null) {
				return;
			}

			var cond = m_searchFilter[target];
			var assetsFolderPath = Application.dataPath + Model.Settings.UNITY_FOLDER_SEPARATOR;
			var outputSource = new List<AssetReference>();

			var guids = AssetDatabase.FindAssets(cond);

			foreach (var guid in guids) {

				var targetFilePath = AssetDatabase.GUIDToAssetPath(guid);

                if (TypeUtility.IsGraphToolSystemAsset (targetFilePath)) {
                    continue;
                }

                var relativePath = targetFilePath.Replace(assetsFolderPath, Model.Settings.Path.ASSETS_PATH);

				var r = AssetReferenceDatabase.GetReference(relativePath);

				if(!TypeUtility.IsLoadingAsset(r)) {
					continue;
				}

				if(r != null) {
					outputSource.Add(AssetReferenceDatabase.GetReference(relativePath));
				}
			}

			var output = new Dictionary<string, List<AssetReference>> {
				{"0", outputSource}
			};

			var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
				null : connectionsToOutput.First();
			Output(dst, output);
		}

		public static void ValidateSearchCondition (string currentCondition, Action NullOrEmpty) {
			if (string.IsNullOrEmpty(currentCondition)) NullOrEmpty();
		}

		private string GetLoaderFullLoadPath(BuildTarget g) {
			return FileUtility.PathCombine(Application.dataPath, m_searchFilter[g]);
		}
	}
}