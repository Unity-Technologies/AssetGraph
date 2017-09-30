using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;

using V1=AssetBundleGraph;
using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {

	[CustomNode("Split Assets/Split By Filter", 20)]
	public class Filter : Node, Model.NodeDataImporter {

		[System.Serializable]
		public class FilterInstance : SerializedInstance<IFilter> {

			public FilterInstance() : base() {}
			public FilterInstance(FilterInstance instance): base(instance) {}
			public FilterInstance(IFilter obj) : base(obj) {}
		}

		[Serializable]
		public class FilterEntry {
			[SerializeField] private FilterInstance m_instance;
			[SerializeField] private string m_pointId;

			public FilterEntry(IFilter filter, Model.ConnectionPointData point) {
				m_instance = new FilterInstance(filter);
				m_pointId = point.Id;
			}

			public FilterEntry(FilterEntry e) {
				m_instance = new FilterInstance(e.m_instance);
				m_pointId = e.m_pointId;
			}

			public string ConnectionPointId {
				get {
					return m_pointId; 
				}
			}

			public FilterInstance Instance {
				get {
					return m_instance;
				}
				set {
					m_instance = value;
				}
			}

			public string Hash {
				get {
					return m_instance.Data;
				}
			}
		}

		[SerializeField] private List<FilterEntry> m_filter;

		public override string ActiveStyle {
			get {
				return "node 1 on";
			}
		}

		public override string InactiveStyle {
			get {
				return "node 1";
			}
		}

		public override string Category {
			get {
				return "Split";
			}
		}

		public override void Initialize(Model.NodeData data) {
			m_filter = new List<FilterEntry>();

			data.AddDefaultInputPoint();
		}

		public void Import(V1.NodeData v1, Model.NodeData v2) {

			foreach(var f in v1.FilterConditions) {
				m_filter.Add(new FilterEntry(new FilterByNameAndType(f.FilterKeyword, f.FilterKeytype), v2.FindOutputPoint(f.ConnectionPointId)));
			}
		}

		public override Node Clone(Model.NodeData newData) {
			var newNode = new Filter();
			newNode.m_filter = new List<FilterEntry>(m_filter.Count);

			newData.AddDefaultInputPoint();

			foreach(var f in m_filter) {
				newNode.AddFilterCondition(newData, f.Instance.Object);
			}

			return newNode;
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

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged) {

			EditorGUILayout.HelpBox("Split By Filter: Split incoming assets by filter conditions.", MessageType.Info);
			editor.UpdateNodeName(node);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				GUILayout.Label("Filter Settings:");
				FilterEntry removing = null;
				for (int i= 0; i < m_filter.Count; ++i) {
					var cond = m_filter[i];

					Action messageAction = null;

					using (new GUILayout.HorizontalScope()) {
						if (GUILayout.Button("-", GUILayout.Width(30))) {
							removing = cond;
						}
						else {
							IFilter filter = cond.Instance.Object;
							if(filter == null) {
								using (new GUILayout.VerticalScope()) {
									EditorGUILayout.HelpBox(string.Format("Failed to deserialize assigned filter({0}). Please select valid class.", cond.Instance.ClassName), MessageType.Error);
									if (GUILayout.Button(cond.Instance.ClassName, "Popup", GUILayout.MinWidth(150f))) {
                                        var map = FilterUtility.GetAttributeAssemblyQualifiedNameMap();
										NodeGUI.ShowTypeNamesMenu(cond.Instance.ClassName, map.Keys.ToList(), (string selectedGUIName) => 
											{
												using(new RecordUndoScope("Change Filter Setting", node)) {
													var newFilter = FilterUtility.CreateFilter(selectedGUIName);
													cond.Instance = new FilterInstance(newFilter);
													onValueChanged();
												}
											}  
										);
									}
								}
							} else {
								cond.Instance.Object.OnInspectorGUI(() => {
									using(new RecordUndoScope("Change Filter Setting", node)) {
										cond.Instance.Save();
										UpdateFilterEntry(node.Data, cond);
										// event must raise to propagate change to connection associated with point
										NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_CONNECTIONPOINT_LABELCHANGED, node, Vector2.zero, GetConnectionPoint(node.Data, cond)));
										onValueChanged();
									}
								});
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

					var map = FilterUtility.GetAttributeAssemblyQualifiedNameMap();
					if(map.Keys.Count > 1) {
						GenericMenu menu = new GenericMenu();
						foreach(var name in map.Keys) {
							var guiName = name;
							menu.AddItem(new GUIContent(guiName), false, () => {
								using(new RecordUndoScope("Add Filter Condition", node)){
									var filter = FilterUtility.CreateFilter(guiName);
									AddFilterCondition(node.Data, filter);
									onValueChanged();
								}
							});
						}
						menu.ShowAsContext();
					} else {
						using(new RecordUndoScope("Add Filter Condition", node)){
							AddFilterCondition(node.Data, new FilterByNameAndType());
							onValueChanged();
						}
					}
				}

				if(removing != null) {
					using(new RecordUndoScope("Remove Filter Condition", node, true)){
						// event must raise to remove connection associated with point
						NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_CONNECTIONPOINT_DELETED, node, Vector2.zero, GetConnectionPoint(node.Data, removing)));
						RemoveFilterCondition(node.Data, removing);
						onValueChanged();
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
			ValidateFilters(node);
			ValidateOverlappingFilterCondition(node, true);
			FilterAssets(node, incoming, connectionsToOutput, Output);
		}

		private void FilterAssets (Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			if(connectionsToOutput == null || Output == null) {
				return;
			}

			var allOutput = new Dictionary<string, Dictionary<string, List<AssetReference>>>();

			foreach(var outPoints in node.OutputPoints) {
				allOutput[outPoints.Id] = new Dictionary<string, List<AssetReference>>();
			}
			if(incoming != null) {
				foreach(var ag in incoming) {
					foreach(var groupKey in ag.assetGroups.Keys) {

						foreach(var a in ag.assetGroups[groupKey]) {
							foreach(var filter in m_filter) {

								if(filter.Instance.Object.FilterAsset(a)) {
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
			}

			foreach(var dst in connectionsToOutput) {
				if(allOutput.ContainsKey(dst.FromNodeConnectionPointId)) {
					Output(dst, allOutput[dst.FromNodeConnectionPointId]);
				}
			}
		}

		public void AddFilterCondition(Model.NodeData n, IFilter filter) {
			var point = n.AddOutputPoint(filter.Label);
			var newEntry = new FilterEntry(filter, point);
			m_filter.Add(newEntry);
			UpdateFilterEntry(n, newEntry);
		}

		public void RemoveFilterCondition(Model.NodeData n, FilterEntry f) {
			m_filter.Remove(f);
			n.OutputPoints.Remove(GetConnectionPoint(n, f));
		}

		public Model.ConnectionPointData GetConnectionPoint(Model.NodeData n, FilterEntry f) {
			Model.ConnectionPointData p = n.OutputPoints.Find(v => v.Id == f.ConnectionPointId);
			UnityEngine.Assertions.Assert.IsNotNull(p);
			return p;
		}

		public void UpdateFilterEntry(Model.NodeData n, FilterEntry f) {

			Model.ConnectionPointData p = n.OutputPoints.Find(v => v.Id == f.ConnectionPointId);
			UnityEngine.Assertions.Assert.IsNotNull(p);

			p.Label = f.Instance.Object.Label;
		}

		public void ValidateFilters(Model.NodeData n) {

			foreach(var f in m_filter) {
				if(f.Instance.Object == null) {
					throw new NodeException(String.Format("Could not deserialize filter with class {0}. Please open graph and fix Filter.", f.Instance.ClassName), n.Id);
				}
			}
		}

		public bool ValidateOverlappingFilterCondition(Model.NodeData n, bool throwException) {

			var conditionGroup = m_filter.Select(v => v).GroupBy(v => v.Hash).ToList();
			var overlap = conditionGroup.Find(v => v.Count() > 1);

			if( overlap != null && throwException ) {
				var element = overlap.First();
				throw new NodeException(String.Format("Duplicated filter condition found [Label:{0}]", element.Instance.Object.Label), n.Id);
			}
			return overlap != null;
		}
	}
}