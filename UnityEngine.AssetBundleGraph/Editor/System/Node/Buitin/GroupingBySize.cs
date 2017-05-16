
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif

using V1=AssetBundleGraph;
using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool
{
	[CustomNode("Group Assets/Group By Size", 41)]
	public class GroupingBySize : Node {

        enum GroupingType : int {
            ByFileSize,
            ByRuntimeMemorySize
        };

        [SerializeField] private SerializableMultiTargetInt m_groupSizeByte;
        [SerializeField] private SerializableMultiTargetInt m_groupingType;

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
            m_groupingType = new SerializableMultiTargetInt();

			data.AddDefaultInputPoint();
			data.AddDefaultOutputPoint();
		}

		public override Node Clone(Model.NodeData newData) {
			var newNode = new GroupingBySize();
            newNode.m_groupSizeByte = new SerializableMultiTargetInt(m_groupSizeByte);
            newNode.m_groupingType = new SerializableMultiTargetInt(m_groupingType);

			newData.AddDefaultInputPoint();
			newData.AddDefaultOutputPoint();
			return newNode;
		}

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged) {

			if (m_groupSizeByte == null) {
				return;
			}

			EditorGUILayout.HelpBox("Grouping by size: Create group of assets by size.", MessageType.Info);
			editor.UpdateNodeName(node);

			GUILayout.Space(10f);

			//Show target configuration tab
			editor.DrawPlatformSelector(node);
			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var disabledScope = editor.DrawOverrideTargetToggle(node, m_groupSizeByte.ContainsValueOf(editor.CurrentEditingGroup), (bool enabled) => {
					using(new RecordUndoScope("Remove Target Grouping Size Settings", node, true)){
						if(enabled) {
							m_groupSizeByte[editor.CurrentEditingGroup] = m_groupSizeByte.DefaultValue;
                            m_groupingType[editor.CurrentEditingGroup] = m_groupingType.DefaultValue;
						} else {
							m_groupSizeByte.Remove(editor.CurrentEditingGroup);
                            m_groupingType.Remove(editor.CurrentEditingGroup);
						}
						onValueChanged();
					}
				});

				using (disabledScope) {
                    var newType = (GroupingType)EditorGUILayout.EnumPopup("Grouping Type",(GroupingType)m_groupingType[editor.CurrentEditingGroup]);
                    if (newType != (GroupingType)m_groupingType[editor.CurrentEditingGroup]) {
                        using(new RecordUndoScope("Change Grouping Type", node, true)){
                            m_groupingType[editor.CurrentEditingGroup] = (int)newType;
                            onValueChanged();
                        }
                    }

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
			long szGroup = m_groupSizeByte[target] * 1000;

			int groupCount = 0;
			long szGroupCount = 0;
			var groupName = groupCount.ToString();

			if(incoming != null) {

				foreach(var ag in incoming) {
					foreach (var assets in ag.assetGroups.Values) {
						foreach(var a in assets) {
                            szGroupCount += GetSizeOfAsset(a, (GroupingType)m_groupingType[target]);

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

        private long GetSizeOfAsset(AssetReference a, GroupingType t) {

            long size = 0;

            // You can not read scene and do estimate
            if (TypeUtility.GetTypeOfAsset (a.importFrom) == typeof(UnityEditor.SceneAsset)) {
                t = GroupingType.ByFileSize;
            }

            if (t == GroupingType.ByRuntimeMemorySize) {
                var objects = a.allData;
                foreach (var o in objects) {
                    #if UNITY_5_6_OR_NEWER
                    size += Profiler.GetRuntimeMemorySizeLong (o);
                    #else
                    size += Profiler.GetRuntimeMemorySize(o);
                    #endif
                }

                a.ReleaseData ();
            } else if (t == GroupingType.ByFileSize) {
                System.IO.FileInfo fileInfo = new System.IO.FileInfo(a.absolutePath);
                if (fileInfo.Exists) {
                    size = fileInfo.Length;
                }
            }

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