
using System;
using System.IO;
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
        [SerializeField] private GroupViewContext m_groupViewContext;
        [SerializeField] private bool m_freezeGroups;
        [SerializeField] private SerializableGroups m_savedGroups;

        private GroupViewController m_groupViewController;
        private Dictionary<string, List<AssetReference>> m_lastOutputGroups;

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
            m_groupViewContext = new GroupViewContext ();
            m_freezeGroups = false;

			data.AddDefaultInputPoint();
			data.AddDefaultOutputPoint();
		}

		public override Node Clone(Model.NodeData newData) {
			var newNode = new GroupingBySize();
            newNode.m_groupSizeByte = new SerializableMultiTargetInt(m_groupSizeByte);
            newNode.m_groupingType = new SerializableMultiTargetInt(m_groupingType);
            newNode.m_groupViewContext = new GroupViewContext ();
            newNode.m_freezeGroups = m_freezeGroups;

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
                    Int32.TryParse (newSizeText, out newSize);

					if (newSize != m_groupSizeByte[editor.CurrentEditingGroup]) {
						using(new RecordUndoScope("Change Grouping Size", node, true)){
							m_groupSizeByte[editor.CurrentEditingGroup] = newSize;
							onValueChanged();
						}
					}
				}
			}

            var newFreezeGroups = EditorGUILayout.ToggleLeft ("Freeze group on build", m_freezeGroups);
            if (newFreezeGroups != m_freezeGroups) {
                using(new RecordUndoScope("Change Freeze Groups", node, true)){
                    m_freezeGroups = newFreezeGroups;
                    onValueChanged();
                }
            }
            EditorGUILayout.HelpBox ("Freezing group will save group when build is performed, and any new asset from there will be put into new group.", 
                MessageType.Info);
            using (new GUILayout.HorizontalScope ()) {
                GUILayout.Label ("Group setting");
                GUILayout.FlexibleSpace ();
                if (GUILayout.Button ("Import")) {
                    if (ImportGroupsWithGUI (node)) {
                        onValueChanged ();
                    }
                }
                if(GUILayout.Button ("Export")) {
                    ExportGroupsWithGUI (node);
                }
                if(GUILayout.Button ("Reset")) {
                    if (EditorUtility.DisplayDialog ("Do you want to reset group setting?", "This will erase current saved group setting.", "OK", "Cancel")) {
                        m_savedGroups = null;
                        onValueChanged ();
                    }
                }
            }
            GUILayout.Space (8f);

            if (m_groupViewController != null) {
                m_groupViewController.OnGroupViewGUI ();
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

        public override void Build (BuildTarget target, 
            Model.NodeData nodeData, 
            IEnumerable<PerformGraph.AssetGroups> incoming, 
            IEnumerable<Model.ConnectionData> connectionsToOutput, 
            PerformGraph.Output outputFunc,
            Action<Model.NodeData, string, float> progressFunc)
        {
            if (m_freezeGroups) {
                m_savedGroups = new SerializableGroups (m_lastOutputGroups);

                // export current setting to file
                var prefabOutputDir = FileUtility.EnsureGroupingCacheDirExists(target, nodeData);
                var outputFilePath = Path.Combine (prefabOutputDir, nodeData.Name + ".json");

                string jsonString = JsonUtility.ToJson (m_savedGroups, true);
                File.WriteAllText (outputFilePath, jsonString, System.Text.Encoding.UTF8);
            }
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

            if (m_freezeGroups && m_savedGroups != null) {
                while (m_savedGroups.ContainsKey (groupCount.ToString ())) {
                    ++groupCount;
                }
            }
            var groupName = groupCount.ToString();

			if(incoming != null) {

				foreach(var ag in incoming) {
					foreach (var assets in ag.assetGroups.Values) {
						foreach(var a in assets) {

                            if (m_freezeGroups && m_savedGroups != null) {
                                var savedGroupName = m_savedGroups.FindGroupOfAsset (a.importFrom);
                                if (!string.IsNullOrEmpty (savedGroupName)) {
                                    if (!outputDict.ContainsKey(savedGroupName)) {
                                        outputDict[savedGroupName] = new List<AssetReference>();
                                    }
                                    outputDict[savedGroupName].Add(a);
                                    continue;
                                }
                            }

                            szGroupCount += GetSizeOfAsset(a, (GroupingType)m_groupingType[target]);

							if (!outputDict.ContainsKey(groupName)) {
								outputDict[groupName] = new List<AssetReference>();
							}
							outputDict[groupName].Add(a);

							if(szGroupCount >= szGroup) {
								szGroupCount = 0;
                                ++groupCount;
                                if (m_freezeGroups && m_savedGroups != null) {
                                    while (m_savedGroups.ContainsKey (groupCount.ToString ())) {
                                        ++groupCount;
                                    }
                                }
								groupName = groupCount.ToString();
							}
						}
					}
				}
			}

			var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
				null : connectionsToOutput.First();
			Output(dst, outputDict);

            if (m_groupViewController == null) {
                m_groupViewController = new GroupViewController (m_groupViewContext);
            }
            m_groupViewController.SetGroups (outputDict);
            m_lastOutputGroups = outputDict;
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
			if (a.isSceneAsset) {
                t = GroupingType.ByFileSize;
            }

            if (t == GroupingType.ByRuntimeMemorySize) {
                size = a.GetRuntimeMemorySize ();
            } else if (t == GroupingType.ByFileSize) {
                size = a.GetFileSize ();
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

        private bool ImportGroupsWithGUI(NodeGUI node) {
            string fileSelected = EditorUtility.OpenFilePanelWithFilters(
                "Select JSON files to import", 
                Application.dataPath, new string[] {"JSON files", "json", "All files", "*"});
            if(string.IsNullOrEmpty(fileSelected)) {
                return false;
            }

            var jsonContent = File.ReadAllText (fileSelected, System.Text.Encoding.UTF8);

            if (m_savedGroups != null) {
                using(new RecordUndoScope("Import Saved Group", node, true)){
                    JsonUtility.FromJsonOverwrite (jsonContent, m_savedGroups);
                }

            } else {
                using(new RecordUndoScope("Import Saved Group", node, true)){
                    m_savedGroups = new SerializableGroups ();
                    JsonUtility.FromJsonOverwrite (jsonContent, m_savedGroups);
                }
            }
            return true;
        }

        private void ExportGroupsWithGUI(NodeGUI node) {
            string path =
                EditorUtility.SaveFilePanelInProject(
                    "Export group setting to JSON file", 
                    node.Name, "json", 
                    "Export to:");
            if(string.IsNullOrEmpty(path)) {
                return;
            }

            string jsonString = JsonUtility.ToJson (m_savedGroups, true);

            File.WriteAllText (path, jsonString, System.Text.Encoding.UTF8);
        }
	}
}