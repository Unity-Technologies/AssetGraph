using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;

namespace AssetBundleGraph {
	[Serializable] 
	public class ConnectionGUI {
		[SerializeField] private string label;
		[SerializeField] private string id;

		[SerializeField] private ConnectionPointData outputPoint;
		[SerializeField] private ConnectionPointData inputPoint;
		[SerializeField] private ConnectionGUIInspectorHelper conInsp;

		[SerializeField] private string connectionButtonStyle;

		public string Label {
			get {
				return label;
			}
			set {
				label = value;
			}
		}

		public string Id {
			get {
				return id;
			}
		}

		public string OutputNodeId {
			get {
				return outputPoint.NodeId;
			}
		}

		public string InputNodeId {
			get {
				return inputPoint.NodeId;
			}
		}

		public ConnectionPointData OutputPoint {
			get {
				return outputPoint;
			}
		}

		public ConnectionPointData InputPoint {
			get {
				return inputPoint;
			}
		}

		private Rect buttonRect;

		public static ConnectionGUI LoadConnection (string label, string id, ConnectionPointData output, ConnectionPointData input) {
			return new ConnectionGUI(
				label,
				id,
				output,
				input
			);
		}

		public static ConnectionGUI CreateConnection (string label, ConnectionPointData output, ConnectionPointData input) {
			return new ConnectionGUI(
				label,
				Guid.NewGuid().ToString(),
				output,
				input
			);
		}

		private ConnectionGUI (string label, string id, ConnectionPointData output, ConnectionPointData input) {

			UnityEngine.Assertions.Assert.IsTrue(output.IsOutput, "Given Output point is not output.");
			UnityEngine.Assertions.Assert.IsTrue(input.IsInput,   "Given Input point is not input.");

			conInsp = ScriptableObject.CreateInstance<ConnectionGUIInspectorHelper>();
			conInsp.hideFlags = HideFlags.DontSave;

			this.label = label;
			this.id = id;

			this.outputPoint = output;
			this.inputPoint = input;

			connectionButtonStyle = "sv_label_0";
		}

		public Rect GetRect () {
			return buttonRect;
		}
		
		public void DrawConnection (List<NodeGUI> nodes, Dictionary<string, List<Asset>> assetGroups) {

			var startNode = nodes.Find(node => node.Id == OutputNodeId);
			if (startNode == null) {
				return;
			}

			var endNode = nodes.Find(node => node.Id == InputNodeId);
			if (endNode == null) {
				return;
			}

			var startPoint = NodeGUI.ScaleEffect(outputPoint.GetGlobalPosition(startNode));
			var startV3 = new Vector3(startPoint.x, startPoint.y, 0f);

			var endPoint = NodeGUI.ScaleEffect(inputPoint.GetGlobalPosition(endNode));
			var endV3 = new Vector3(endPoint.x, endPoint.y + 1f, 0f);
			
			var centerPoint = startPoint + ((endPoint - startPoint) / 2);
			var centerPointV3 = new Vector3(centerPoint.x, centerPoint.y, 0f);

			var pointDistance = (endPoint.x - startPoint.x) / 3f;
			if (pointDistance < AssetBundleGraphGUISettings.CONNECTION_CURVE_LENGTH) pointDistance = AssetBundleGraphGUISettings.CONNECTION_CURVE_LENGTH;

			var startTan = new Vector3(startPoint.x + pointDistance, startPoint.y, 0f);
			var endTan = new Vector3(endPoint.x - pointDistance, endPoint.y, 0f);

			var totalAssets = 0;
			var totalGroups = 0;
			if(assetGroups != null) {
				totalAssets = assetGroups.Select(v => v.Value.Count).Sum();
				totalGroups = assetGroups.Keys.Count;
			}

			if(conInsp != null && Selection.activeObject == conInsp && conInsp.connectionGUI == this) {
				Handles.DrawBezier(startV3, endV3, startTan, endTan, new Color(0.43f, 0.65f, 0.90f, 1.0f), null, 2f);
			} else {
				Handles.DrawBezier(startV3, endV3, startTan, endTan, ((totalAssets > 0) ? Color.white : Color.gray), null, 2f);
			}



			// draw connection label if connection's label is not normal.
			if (NodeGUI.scaleFactor == NodeGUI.SCALE_MAX) {
				switch (label){
					case AssetBundleGraphSettings.DEFAULT_OUTPUTPOINT_LABEL: {
						// show nothing
						break;
					}
					
					case AssetBundleGraphSettings.BUNDLECONFIG_BUNDLE_OUTPUTPOINT_LABEL: {
						var labelPointV3 = new Vector3(centerPointV3.x - ((AssetBundleGraphSettings.BUNDLECONFIG_BUNDLE_OUTPUTPOINT_LABEL.Length * 6f) / 2), centerPointV3.y - 24f, 0f) ;
						Handles.Label(labelPointV3, AssetBundleGraphSettings.BUNDLECONFIG_BUNDLE_OUTPUTPOINT_LABEL);
						break;
					}

					default: {
						var labelPointV3 = new Vector3(centerPointV3.x - ((label.Length * 7f) / 2), centerPointV3.y - 24f, 0f) ;
						Handles.Label(labelPointV3, label);
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
			buttonRect = new Rect(centerPointV3.x - labelSize.x/2f, centerPointV3.y - labelSize.y/2f, labelSize.x, 30f);

			if (
				Event.current.type == EventType.ContextClick
				|| (Event.current.type == EventType.MouseUp && Event.current.button == 1)
			) {
				var rightClickPos = Event.current.mousePosition;
				if (buttonRect.Contains(rightClickPos)) {
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

			if (GUI.Button(buttonRect, connectionLabel, style)) {
				conInsp.UpdateInspector(this, assetGroups);
				ConnectionGUIUtility.ConnectionEventHandler(new ConnectionEvent(ConnectionEvent.EventType.EVENT_CONNECTION_TAPPED, this));
			}
		}

		public bool IsEqual (ConnectionPointData from, ConnectionPointData to) {
			return (outputPoint == from && inputPoint == to);
		}
		
		public void SetActive () {
			Selection.activeObject = conInsp;
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