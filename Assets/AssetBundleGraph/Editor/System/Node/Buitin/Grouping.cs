
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool
{
	[CustomNode("Grouping", 50)]
	public class Grouping : Node {

		[SerializeField] private SerializableMultiTargetString m_groupingKeyword;

		public override string ActiveStyle {
			get {
				return "flow node 3 on";
			}
		}

		public override string InactiveStyle {
			get {
				return "flow node 3";
			}
		}

		public override void Initialize(Model.NodeData data) {
			base.Initialize(data);
			m_groupingKeyword = new SerializableMultiTargetString(Model.Settings.GROUPING_KEYWORD_DEFAULT);

			data.AddInputPoint(Model.Settings.DEFAULT_INPUTPOINT_LABEL);
			data.AddOutputPoint(Model.Settings.DEFAULT_OUTPUTPOINT_LABEL);
		}

		public override Node Clone() {
			var newNode = new Grouping();
			newNode.m_groupingKeyword = new SerializableMultiTargetString(m_groupingKeyword);

			return newNode;
		}

		public override bool IsEqual(Node node) {
			Grouping rhs = node as Grouping;
			return rhs != null && 
				m_groupingKeyword == rhs.m_groupingKeyword;
		}

		public override string Serialize() {
			return JsonUtility.ToJson(this);
		}

		public override void OnInspectorGUI (NodeGUI node, NodeGUIEditor editor) {

			if (m_groupingKeyword == null) {
				return;
			}

			EditorGUILayout.HelpBox("Grouping: Create group of assets.", MessageType.Info);
			editor.UpdateNodeName(node);

			GUILayout.Space(10f);

			//Show target configuration tab
			editor.DrawPlatformSelector(node);
			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				var disabledScope = editor.DrawOverrideTargetToggle(node, m_groupingKeyword.ContainsValueOf(editor.CurrentEditingGroup), (bool enabled) => {
					using(new RecordUndoScope("Remove Target Grouping Keyword Settings", node, true)){
						if(enabled) {
							m_groupingKeyword[editor.CurrentEditingGroup] = m_groupingKeyword.DefaultValue;
						} else {
							m_groupingKeyword.Remove(editor.CurrentEditingGroup);
						}
					}
				});

				using (disabledScope) {
					var newGroupingKeyword = EditorGUILayout.TextField("Grouping Keyword",m_groupingKeyword[editor.CurrentEditingGroup]);
					EditorGUILayout.HelpBox(
						"Grouping Keyword requires \"*\" in itself. It assumes there is a pattern such as \"ID_0\" in incoming paths when configured as \"ID_*\" ", 
						MessageType.Info);

					if (newGroupingKeyword != m_groupingKeyword[editor.CurrentEditingGroup]) {
						using(new RecordUndoScope("Change Grouping Keywords", node, true)){
							m_groupingKeyword[editor.CurrentEditingGroup] = newGroupingKeyword;
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
				() => {
					throw new NodeException("Grouping Keyword can not be empty.", node.Id);
				},
				() => {
					throw new NodeException(String.Format("Grouping Keyword must contain {0} for numbering: currently {1}", Model.Settings.KEYWORD_WILDCARD, m_groupingKeyword[target]), node.Id);
				}
			);

			if(incoming == null || connectionsToOutput == null || Output == null) {
				return;
			}

			var outputDict = new Dictionary<string, List<AssetReference>>();
			var groupingKeyword = m_groupingKeyword[target];
			var split = groupingKeyword.Split(Model.Settings.KEYWORD_WILDCARD);
			var groupingKeywordPrefix  = split[0];
			var groupingKeywordPostfix = split[1];
			var regex = new Regex(groupingKeywordPrefix + "(.*?)" + groupingKeywordPostfix);

			foreach(var ag in incoming) {
				foreach (var assets in ag.assetGroups.Values) {
					foreach(var a in assets) {
						var targetPath = a.path;

						var match = regex.Match(targetPath);

						if (match.Success) {
							var newGroupingKey = match.Groups[1].Value;
							if (!outputDict.ContainsKey(newGroupingKey)) {
								outputDict[newGroupingKey] = new List<AssetReference>();
							}
							outputDict[newGroupingKey].Add(a);
						}
					}
				}
			}

			var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
				null : connectionsToOutput.First();
			Output(dst, outputDict);
		}

		public static void ValidateGroupingKeyword (string currentGroupingKeyword, Action NullOrEmpty, Action ShouldContainWildCardKey) {
			if (string.IsNullOrEmpty(currentGroupingKeyword)) NullOrEmpty();
			if (!currentGroupingKeyword.Contains(Model.Settings.KEYWORD_WILDCARD.ToString())) ShouldContainWildCardKey();
		}
	}
}