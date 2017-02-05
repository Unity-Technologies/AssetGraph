using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {

	[CustomNode("Filter", 20)]
	public class Filter : INode {

		[SerializeField] private List<Model.FilterEntry> m_filter;

		public string ActiveStyle {
			get {
				return string.Empty;
			}
		}

		public string InactiveStyle {
			get {
				return string.Empty;
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

		private void ShowFilterKeyTypeMenu (string current, Action<string> ExistSelected) {
			var menu = new GenericMenu();

			menu.AddDisabledItem(new GUIContent(current));

			menu.AddSeparator(string.Empty);

			for (var i = 0; i < TypeUtility.KeyTypes.Count; i++) {
				var type = TypeUtility.KeyTypes[i];
				if (type == current) continue;

				menu.AddItem(
					new GUIContent(type),
					false,
					() => {
						ExistSelected(type);
					}
				);
			}
			menu.ShowAsContext();
		}

		public void OnInspectorGUI (NodeGUI node, NodeGUIEditor editor) {
			EditorGUILayout.HelpBox("Filter: Filter incoming assets by keywords and types. You can use regular expressions for keyword field.", MessageType.Info);
			editor.UpdateNodeName(node);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				GUILayout.Label("Filter Settings:");
				Model.FilterEntry removing = null;
				for (int i= 0; i < m_filter.Count; ++i) {
					var cond = m_filter[i];

					Action messageAction = null;

					using (new GUILayout.HorizontalScope()) {
						if (GUILayout.Button("-", GUILayout.Width(30))) {
							removing = cond;
						}
						else {
							var keyword = cond.FilterKeyword;

							GUIStyle s = new GUIStyle((GUIStyle)"TextFieldDropDownText");

							using (new EditorGUILayout.HorizontalScope()) {
								keyword = EditorGUILayout.TextField(cond.FilterKeyword, s, GUILayout.Width(120));
								if (GUILayout.Button(cond.FilterKeytype , "Popup")) {
									var ind = i;// need this because of closure locality bug in unity C#
									NodeGUI.ShowFilterKeyTypeMenu(
										cond.FilterKeytype,
										(string selectedTypeStr) => {
											using(new RecordUndoScope("Modify Filter Type", node, true)){
												m_filter[ind].FilterKeytype = selectedTypeStr;
												UpdateFilterEntry(node.Data, m_filter[ind]);
											}
											// event must raise to propagate change to connection associated with point
											NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_CONNECTIONPOINT_LABELCHANGED, node, Vector2.zero, GetConnectionPoint(node.Data, cond)));
										} 
									);
								}
							}

							if (keyword != cond.FilterKeyword) {
								using(new RecordUndoScope("Modify Filter Keyword", node, true)){
									cond.FilterKeyword = keyword;
									UpdateFilterEntry(node.Data, cond);
								}
								// event must raise to propagate change to connection associated with point
								NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_CONNECTIONPOINT_LABELCHANGED, node, Vector2.zero, GetConnectionPoint(node.Data, cond)));
							}
						}
					}

					if(messageAction != null) {
						using (new GUILayout.HorizontalScope()) {
							messageAction.Invoke();
						}
					}
				}

				// add contains keyword interface.
				if (GUILayout.Button("+")) {
					using(new RecordUndoScope("Add Filter Condition", node)){
						AddFilterCondition(node.Data,
							Model.Settings.DEFAULT_FILTER_KEYWORD, 
							Model.Settings.DEFAULT_FILTER_KEYTYPE);
					}
				}

				if(removing != null) {
					using(new RecordUndoScope("Remove Filter Condition", node, true)){
						// event must raise to remove connection associated with point
						NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_CONNECTIONPOINT_DELETED, node, Vector2.zero, GetConnectionPoint(node.Data, removing)));
						RemoveFilterCondition(node.Data, removing);
					}
				}
			}
		}

		public void OnNodeGUI(NodeGUI node) {
		}

		public void Prepare (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			ValidateOverlappingFilterCondition(node, true);
			FilterAssets(node, incoming, connectionsToOutput, Output);
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

		private void FilterAssets (Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			if(connectionsToOutput == null || incoming == null || Output == null) {
				return;
			}

			var allOutput = new Dictionary<string, Dictionary<string, List<AssetReference>>>();

			foreach(var outPoints in node.OutputPoints) {
				allOutput[outPoints.Id] = new Dictionary<string, List<AssetReference>>();
			}

			foreach(var ag in incoming) {
				foreach(var groupKey in ag.assetGroups.Keys) {

					foreach(var a in ag.assetGroups[groupKey]) {
						foreach(var filter in m_filter) {
							bool keywordMatch = Regex.IsMatch(a.importFrom, filter.FilterKeyword, 
								RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

							bool match = keywordMatch;

							if(keywordMatch && filter.FilterKeytype != Model.Settings.DEFAULT_FILTER_KEYTYPE) 
							{
								var assumedType = a.filterType;
								match = assumedType != null && filter.FilterKeytype == assumedType.ToString();
							}

							if(match) {
								var output = allOutput[filter.ConnectionPointId];
								if(!output.ContainsKey(groupKey)) {
									output[groupKey] = new List<AssetReference>();
								}
								output[groupKey].Add(a);
								// consume this asset with this output
								break;
							}
						}
					}
				}
			}

			foreach(var dst in connectionsToOutput) {
				if(allOutput.ContainsKey(dst.FromNodeConnectionPointId)) {
					Output(dst, allOutput[dst.FromNodeConnectionPointId]);
				}
			}
		}

		public void AddFilterCondition(Model.NodeData n, string keyword, string keytype) {
			var point = n.AddOutputPoint(keyword);
			var newEntry = new Model.FilterEntry(keyword, keytype, point);
			m_filter.Add(newEntry);
			UpdateFilterEntry(n, newEntry);
		}

		public void RemoveFilterCondition(Model.NodeData n, Model.FilterEntry f) {
			m_filter.Remove(f);
			n.OutputPoints.Remove(GetConnectionPoint(n, f));
		}

		public Model.ConnectionPointData GetConnectionPoint(Model.NodeData n, Model.FilterEntry f) {
			Model.ConnectionPointData p = n.OutputPoints.Find(v => v.Id == f.ConnectionPointId);
			UnityEngine.Assertions.Assert.IsNotNull(p);
			return p;
		}

		public void UpdateFilterEntry(Model.NodeData n, Model.FilterEntry f) {

			Model.ConnectionPointData p = n.OutputPoints.Find(v => v.Id == f.ConnectionPointId);
			UnityEngine.Assertions.Assert.IsNotNull(p);

			if(f.FilterKeytype == Model.Settings.DEFAULT_FILTER_KEYTYPE) {
				p.Label = f.FilterKeyword;
			} else {
				var pointIndex = f.FilterKeytype.LastIndexOf('.');
				var keytypeName = (pointIndex > 0)? f.FilterKeytype.Substring(pointIndex+1):f.FilterKeytype;
				p.Label = string.Format("{0}[{1}]", f.FilterKeyword, keytypeName);
			}
		}

		public bool ValidateOverlappingFilterCondition(Model.NodeData n, bool throwException) {

			var conditionGroup = m_filter.Select(v => v).GroupBy(v => v.Hash).ToList();
			var overlap = conditionGroup.Find(v => v.Count() > 1);

			if( overlap != null && throwException ) {
				var element = overlap.First();
				throw new NodeException(String.Format("Duplicated filter condition found for [Keyword:{0} Type:{1}]", element.FilterKeyword, element.FilterKeytype), n.Id);
			}
			return overlap != null;
		}
	}
}