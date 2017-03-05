
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

using V1=AssetBundleGraph;
using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool
{
	[CustomNode("Group Assets/Group By Runtime Memory Size", 41)]
	public class GroupingBySize : Node {

		[SerializeField] private SerializableMultiTargetInt m_groupSizeByte;

		public override string ActiveStyle {
			get {
				return "node 2 on";
			}
		}

		public override string InactiveStyle {
			get {
				return "node 2";
			}
		}

		public override string Category {
			get {
				return "Group";
			}
		}

		public override void Initialize(Model.NodeData data) {
			m_groupSizeByte = new SerializableMultiTargetInt();

			data.AddDefaultInputPoint();
			data.AddDefaultOutputPoint();
		}

		public override Node Clone(Model.NodeData newData) {
			var newNode = new GroupingBySize();
			newNode.m_groupSizeByte = new SerializableMultiTargetInt(m_groupSizeByte);

			newData.AddDefaultInputPoint();
			newData.AddDefaultOutputPoint();
			return newNode;
		}

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged) {

			if (m_groupSizeByte == null) {
				return;
			}

			EditorGUILayout.HelpBox("Grouping by runtime memory size: Create group of assets by runtime memory size.", MessageType.Info);
			editor.UpdateNodeName(node);

			GUILayout.Space(10f);

			//Show target configuration tab
			editor.DrawPlatformSelector(node);
			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var disabledScope = editor.DrawOverrideTargetToggle(node, m_groupSizeByte.ContainsValueOf(editor.CurrentEditingGroup), (bool enabled) => {
					using(new RecordUndoScope("Remove Target Grouping Size Settings", node, true)){
						if(enabled) {
							m_groupSizeByte[editor.CurrentEditingGroup] = m_groupSizeByte.DefaultValue;
						} else {
							m_groupSizeByte.Remove(editor.CurrentEditingGroup);
						}
						onValueChanged();
					}
				});

				using (disabledScope) {
					var newSizeText = EditorGUILayout.TextField("Size(KB)",m_groupSizeByte[editor.CurrentEditingGroup].ToString());
					int newSize = 0;

					if( !Int32.TryParse(newSizeText, out newSize) ) {
						throw new NodeException("Invalid size. Size property must be in decimal format.", node.Id);
					}
					if(newSize < 0) {
						throw new NodeException("Invalid size. Size property must be a positive number.", node.Id);
					}

					if (newSize != m_groupSizeByte[editor.CurrentEditingGroup]) {
						using(new RecordUndoScope("Change Grouping Size", node, true)){
							m_groupSizeByte[editor.CurrentEditingGroup] = newSize;
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
			GroupingOutput(target, node, incoming, connectionsToOutput, Output);
		}

		private void GroupingOutput (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			ValidateGroupingKeyword(
				m_groupSizeByte[target],
				() => {
					throw new NodeException("Invalid size.", node.Id);
				}
			);

			if(connectionsToOutput == null || Output == null) {
				return;
			}
							var outputDict = new Dictionary<string, List<AssetReference>>();
			var szGroup = m_groupSizeByte[target] * 1000;

			int groupCount = 0;
			int szGroupCount = 0;
			var groupName = groupCount.ToString();

			if(incoming != null) {

				foreach(var ag in incoming) {
					foreach (var assets in ag.assetGroups.Values) {
						foreach(var a in assets) {
							szGroupCount += GetMemorySizeOfAsset(a);

							if (!outputDict.ContainsKey(groupName)) {
								outputDict[groupName] = new List<AssetReference>();
							}
							outputDict[groupName].Add(a);

							if(szGroupCount >= szGroup) {
								szGroupCount = 0;
								++groupCount;
								groupName = groupCount.ToString();
							}
						}
					}
				}
			}

			var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
				null : connectionsToOutput.First();
			Output(dst, outputDict);
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

		private int GetMemorySizeOfAsset(AssetReference a) {

			var objects = a.allData;
			int size = 0;
			foreach(var o in objects) {
				size += Profiler.GetRuntimeMemorySize(o);
			}

			a.ReleaseData();

			return size;
		}

		private void ValidateGroupingKeyword (int currentSize, 
			Action InvlaidSize
		) {
			if (currentSize < 0) {
				InvlaidSize();
			}
		}
	}
}