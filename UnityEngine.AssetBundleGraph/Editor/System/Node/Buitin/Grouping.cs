
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
	[CustomNode("Group Assets/Group By File Path", 40)]
	public class Grouping : Node, Model.NodeDataImporter {

		enum GroupingPatternType : int {
			WildCard,
			RegularExpression
		};

		[SerializeField] private SerializableMultiTargetString m_groupingKeyword;
		[SerializeField] private SerializableMultiTargetInt m_patternType;
		[SerializeField] private bool m_allowSlash;

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
			m_groupingKeyword = new SerializableMultiTargetString(Model.Settings.GROUPING_KEYWORD_DEFAULT);
			m_patternType = new SerializableMultiTargetInt((int)GroupingPatternType.WildCard);
			m_allowSlash = false;

			data.AddDefaultInputPoint();
			data.AddDefaultOutputPoint();
		}

		public void Import(V1.NodeData v1, Model.NodeData v2) {
			m_groupingKeyword = new SerializableMultiTargetString(v1.GroupingKeywords);
			m_patternType = new SerializableMultiTargetInt((int)GroupingPatternType.WildCard);
			m_allowSlash = true;
		}

		public override Node Clone(Model.NodeData newData) {
			var newNode = new Grouping();
			newNode.m_groupingKeyword = new SerializableMultiTargetString(m_groupingKeyword);
			newNode.m_patternType = new SerializableMultiTargetInt(m_patternType);
			newNode.m_allowSlash = m_allowSlash;

			newData.AddDefaultInputPoint();
			newData.AddDefaultOutputPoint();
			return newNode;
		}

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged) {

			if (m_groupingKeyword == null) {
				return;
			}

			EditorGUILayout.HelpBox("Group By File Path: Create group of assets from asset's file path.", MessageType.Info);
			editor.UpdateNodeName(node);

			GUILayout.Space(10f);
			var newSlash = EditorGUILayout.ToggleLeft("Allow directory separator ('/') in group name", m_allowSlash);
			if(newSlash != m_allowSlash) {
				using(new RecordUndoScope("Change Allow Slash Setting", node, true)){
					m_allowSlash = newSlash;
					onValueChanged();
				}
			}
			if(m_allowSlash) {
				EditorGUILayout.HelpBox("Allowing directory separator for group name may create incompatible group name with other nodes. Please use this option carefully.", MessageType.Info);
			}
			GUILayout.Space(4f);

			//Show target configuration tab
			editor.DrawPlatformSelector(node);
			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var disabledScope = editor.DrawOverrideTargetToggle(node, m_groupingKeyword.ContainsValueOf(editor.CurrentEditingGroup), (bool enabled) => {
					using(new RecordUndoScope("Remove Target Grouping Keyword Settings", node, true)){
						if(enabled) {
							m_groupingKeyword[editor.CurrentEditingGroup] = m_groupingKeyword.DefaultValue;
							m_patternType[editor.CurrentEditingGroup] = m_patternType.DefaultValue;
						} else {
							m_groupingKeyword.Remove(editor.CurrentEditingGroup);
							m_patternType.Remove(editor.CurrentEditingGroup);
						}
						onValueChanged();
					}
				});

				using (disabledScope) {
					var newType = (GroupingPatternType)EditorGUILayout.EnumPopup("Pattern Type",(GroupingPatternType)m_patternType[editor.CurrentEditingGroup]);
					if (newType != (GroupingPatternType)m_patternType[editor.CurrentEditingGroup]) {
						using(new RecordUndoScope("Change Grouping Pattern Type", node, true)){
							m_patternType[editor.CurrentEditingGroup] = (int)newType;
							onValueChanged();
						}
					}

					var newGroupingKeyword = EditorGUILayout.TextField("Grouping Keyword",m_groupingKeyword[editor.CurrentEditingGroup]);
					string helpText = null;
					switch((GroupingPatternType)m_patternType[editor.CurrentEditingGroup]) {
					case GroupingPatternType.WildCard:
						helpText = "Grouping Keyword requires \"*\" in itself. It assumes there is a pattern such as \"ID_0\" in incoming paths when configured as \"ID_*\" ";
						break;
					case GroupingPatternType.RegularExpression:
						helpText = "Grouping Keyword requires pattern definition with \"()\" in Regular Expression manner.";
						break;
					}
					EditorGUILayout.HelpBox(helpText, MessageType.Info);

					if (newGroupingKeyword != m_groupingKeyword[editor.CurrentEditingGroup]) {
						using(new RecordUndoScope("Change Grouping Keywords", node, true)){
							m_groupingKeyword[editor.CurrentEditingGroup] = newGroupingKeyword;
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
				m_groupingKeyword[target],
				(GroupingPatternType)m_patternType[target],
				() => {
					throw new NodeException("Grouping Keyword can not be empty.", node.Id);
				},
				() => {
					throw new NodeException(String.Format("Grouping Keyword must contain {0} for numbering: currently {1}", Model.Settings.KEYWORD_WILDCARD, m_groupingKeyword[target]), node.Id);
				}
			);

			if(connectionsToOutput == null || Output == null) {
				return;
			}

			var outputDict = new Dictionary<string, List<AssetReference>>();

			if(incoming != null) {
				Regex regex = null;
				switch((GroupingPatternType)m_patternType[target]) {
				case GroupingPatternType.WildCard: 
					{
						var groupingKeyword = m_groupingKeyword[target];
						var split = groupingKeyword.Split(Model.Settings.KEYWORD_WILDCARD);
						var groupingKeywordPrefix  = split[0];
						var groupingKeywordPostfix = split[1];
						regex = new Regex(groupingKeywordPrefix + "(.*?)" + groupingKeywordPostfix);
					}
					break;
				case GroupingPatternType.RegularExpression:
					{
						regex = new Regex(m_groupingKeyword[target]);
					}
					break;
				}

				foreach(var ag in incoming) {
					foreach (var assets in ag.assetGroups.Values) {
						foreach(var a in assets) {
							var targetPath = a.path;

							var match = regex.Match(targetPath);

							if (match.Success) {
								var newGroupingKey = match.Groups[1].Value;

								if(!m_allowSlash && newGroupingKey.Contains("/")) {
									throw new NodeException(String.Format("Grouping Keyword with directory separator('/') found: \"{0}\" from asset: {1}", 
										newGroupingKey, targetPath), node.Id);
								}

								if (!outputDict.ContainsKey(newGroupingKey)) {
									outputDict[newGroupingKey] = new List<AssetReference>();
								}
								outputDict[newGroupingKey].Add(a);
							}
						}
					}
				}
			}

			var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
				null : connectionsToOutput.First();
			Output(dst, outputDict);
		}

		private void ValidateGroupingKeyword (string currentGroupingKeyword, 
			GroupingPatternType currentType,
			Action NullOrEmpty, 
			Action ShouldContainWildCardKey
		) {
			if (string.IsNullOrEmpty(currentGroupingKeyword)) NullOrEmpty();
			if (currentType == GroupingPatternType.WildCard && 
				!currentGroupingKeyword.Contains(Model.Settings.KEYWORD_WILDCARD.ToString())) 
			{
				ShouldContainWildCardKey();
			}
		}
	}
}