
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
	public class Grouping : INode {

		[SerializeField] private SerializableMultiTargetString m_groupingKeyword;

		public string ActiveStyle {
			get {
				return "flow node 3 on";
			}
		}

		public string InactiveStyle {
			get {
				return "flow node 3";
			}
		}

		public void Initialize(Model.NodeData data) {
			m_groupingKeyword = new SerializableMultiTargetString(Model.Settings.GROUPING_KEYWORD_DEFAULT);

			data.AddInputPoint(Model.Settings.DEFAULT_INPUTPOINT_LABEL);
			data.AddOutputPoint(Model.Settings.DEFAULT_OUTPUTPOINT_LABEL);
		}

		public INode Clone() {
			var newNode = new Grouping();
			newNode.m_groupingKeyword = new SerializableMultiTargetString(m_groupingKeyword);

			return newNode;
		}

		public bool IsEqual(INode node) {
			Grouping rhs = node as Grouping;
			return rhs != null && 
				m_groupingKeyword == rhs.m_groupingKeyword;
		}

		public string Serialize() {
			return JsonUtility.ToJson(this);
		}

		public bool IsValidInputConnectionPoint(Model.ConnectionPointData point) {
			return true;
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

		public void Prepare (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			GroupingOutput(target, node, incoming, connectionsToOutput, Output);
		}

		public void Build (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output,
			Action<Model.NodeData, string, float> progressFunc) 
		{
			//Operation is completed furing Setup() phase, so do nothing in Run.
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