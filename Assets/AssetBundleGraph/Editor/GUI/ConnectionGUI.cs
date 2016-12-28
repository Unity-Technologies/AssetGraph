using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;

namespace AssetBundleGraph {
	[Serializable] 
	public class ConnectionGUI {
		[SerializeField] private ConnectionData m_data;

		[SerializeField] private ConnectionPointData m_outputPoint;
		[SerializeField] private ConnectionPointData m_inputPoint;
		[SerializeField] private ConnectionGUIInspectorHelper m_inspector;

		[SerializeField] private string connectionButtonStyle;

		public string Label {
			get {
				return m_data.Label;
			}
			set {
				m_data.Label = value;
			}
		}

		public string Id {
			get {
				return m_data.Id;
			}
		}

		public string OutputNodeId {
			get {
				return m_outputPoint.NodeId;
			}
		}

		public string InputNodeId {
			get {
				return m_inputPoint.NodeId;
			}
		}

		public ConnectionPointData OutputPoint {
			get {
				return m_outputPoint;
			}
		}

		public ConnectionPointData InputPoint {
			get {
				return m_inputPoint;
			}
		}

		public ConnectionData Data {
			get {
				return m_data;
			}
		}

		public ConnectionGUIInspectorHelper Inspector {
			get {
				if(m_inspector == null) {
					m_inspector = ScriptableObject.CreateInstance<ConnectionGUIInspectorHelper>();
					m_inspector.hideFlags = HideFlags.DontSave;
				}
				return m_inspector;
			}
		}

		private Rect m_buttonRect;

		public static ConnectionGUI LoadConnection (ConnectionData data, ConnectionPointData output, ConnectionPointData input) {
			return new ConnectionGUI(
				data,
				output,
				input
			);
		}

		public static ConnectionGUI CreateConnection (string label, ConnectionPointData output, ConnectionPointData input) {
			return new ConnectionGUI(
				new ConnectionData(label, output, input),
				output,
				input
			);
		}

		private ConnectionGUI (ConnectionData data, ConnectionPointData output, ConnectionPointData input) {

			UnityEngine.Assertions.Assert.IsTrue(output.IsOutput, "Given Output point is not output.");
			UnityEngine.Assertions.Assert.IsTrue(input.IsInput,   "Given Input point is not input.");

			m_inspector = ScriptableObject.CreateInstance<ConnectionGUIInspectorHelper>();
			m_inspector.hideFlags = HideFlags.DontSave;

			this.m_data = data;
			this.m_outputPoint = output;
			this.m_inputPoint = input;

			connectionButtonStyle = "sv_label_0";
		}

		public Rect GetRect () {
			return m_buttonRect;
		}
		
		public void DrawConnection (List<NodeGUI> nodes, Dictionary<string, List<AssetReference>> assetGroups) {

			var startNode = nodes.Find(node => node.Id == OutputNodeId);
			if (startNode == null) {
				return;
			}

			var endNode = nodes.Find(node => node.Id == InputNodeId);
			if (endNode == null) {
				return;
			}

			var startPoint = NodeGUI.ScaleEffect(m_outputPoint.GetGlobalPosition(startNode));
			var startV3 = new Vector3(startPoint.x, startPoint.y, 0f);

			var endPoint = NodeGUI.ScaleEffect(m_inputPoint.GetGlobalPosition(endNode));
			var endV3 = new Vector3(endPoint.x, endPoint.y + 1f, 0f);
			
			var centerPoint = startPoint + ((endPoint - startPoint) / 2);
			var centerPointV3 = new Vector3(centerPoint.x, centerPoint.y, 0f);

			var pointDistance = (endPoint.x - startPoint.x) / 3f;
			if (pointDistance < AssetBundleGraphSettings.GUI.CONNECTION_CURVE_LENGTH) pointDistance = AssetBundleGraphSettings.GUI.CONNECTION_CURVE_LENGTH;

			var startTan = new Vector3(startPoint.x + pointDistance, startPoint.y, 0f);
			var endTan = new Vector3(endPoint.x - pointDistance, endPoint.y, 0f);

			var totalAssets = 0;
			var totalGroups = 0;
			if(assetGroups != null) {
				totalAssets = assetGroups.Select(v => v.Value.Count).Sum();
				totalGroups = assetGroups.Keys.Count;
			}

			if(m_inspector != null && Selection.activeObject == m_inspector && m_inspector.connectionGUI == this) {
				Handles.DrawBezier(startV3, endV3, startTan, endTan, new Color(0.43f, 0.65f, 0.90f, 1.0f), null, 2f);
			} else {
				Handles.DrawBezier(startV3, endV3, startTan, endTan, ((totalAssets > 0) ? Color.white : Color.gray), null, 2f);
			}


			// draw connection label if connection's label is not normal.
			if (NodeGUI.scaleFactor == NodeGUI.SCALE_MAX) {

				GUIStyle labelStyle = new GUIStyle("WhiteMiniLabel");
				labelStyle.alignment = TextAnchor.MiddleLeft;

				switch (Label){
					case AssetBundleGraphSettings.DEFAULT_OUTPUTPOINT_LABEL: {
						// show nothing
						break;
					}
					
					case AssetBundleGraphSettings.BUNDLECONFIG_BUNDLE_OUTPUTPOINT_LABEL: {
						var labelWidth = labelStyle.CalcSize(new GUIContent(AssetBundleGraphSettings.BUNDLECONFIG_BUNDLE_OUTPUTPOINT_LABEL));
						var labelPointV3 = new Vector3(centerPointV3.x - (labelWidth.x / 2), centerPointV3.y - 24f, 0f) ;
						Handles.Label(labelPointV3, AssetBundleGraphSettings.BUNDLECONFIG_BUNDLE_OUTPUTPOINT_LABEL, labelStyle);
						break;
					}

					default: {
						var labelWidth = labelStyle.CalcSize(new GUIContent(Label));
						var labelPointV3 = new Vector3(centerPointV3.x - (labelWidth.x / 2), centerPointV3.y - 24f, 0f) ;
						Handles.Label(labelPointV3, Label, labelStyle);
						break;
					}
				}
			}

			string connectionLabel;
			if(totalGroups > 1) {
				connectionLabel = string.Format("{0}:{1}", totalAssets, totalGroups);
			} else {
				connectionLabel = string.Format("{0}", totalAssets);
			}

			var style = new GUIStyle(connectionButtonStyle);

			var labelSize = style.CalcSize(new GUIContent(connectionLabel));
			m_buttonRect = new Rect(centerPointV3.x - labelSize.x/2f, centerPointV3.y - labelSize.y/2f, labelSize.x, 30f);

			if (
				Event.current.type == EventType.ContextClick
				|| (Event.current.type == EventType.MouseUp && Event.current.button == 1)
			) {
				var rightClickPos = Event.current.mousePosition;
				if (m_buttonRect.Contains(rightClickPos)) {
					var menu = new GenericMenu();
					menu.AddItem(
						new GUIContent("Delete"),
						false, 
						() => {
							Delete();
						}
					);
					menu.ShowAsContext();
					Event.current.Use();
				}
			}

			if (GUI.Button(m_buttonRect, connectionLabel, style)) {
				Inspector.UpdateInspector(this, assetGroups);
				ConnectionGUIUtility.ConnectionEventHandler(new ConnectionEvent(ConnectionEvent.EventType.EVENT_CONNECTION_TAPPED, this));
			}
		}

		public bool IsEqual (ConnectionPointData from, ConnectionPointData to) {
			return (m_outputPoint == from && m_inputPoint == to);
		}
		
		public void SetActive () {
			Selection.activeObject = Inspector;
			connectionButtonStyle = "sv_label_1";
		}

		public void SetInactive () {
			connectionButtonStyle = "sv_label_0";
		}

		public void Delete () {
			ConnectionGUIUtility.ConnectionEventHandler(new ConnectionEvent(ConnectionEvent.EventType.EVENT_CONNECTION_DELETED, this));
		}
	}

	public static class NodeEditor_ConnectionListExtension {
		public static bool ContainsConnection(this List<ConnectionGUI> connections, ConnectionPointData output, ConnectionPointData input) {
			foreach (var con in connections) {
				if (con.IsEqual(output, input)) {
					return true;
				}
			}
			return false;
		}
	}
}