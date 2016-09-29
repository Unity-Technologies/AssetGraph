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

		/**
			Inspector GUI for this connection.
		*/
		[CustomEditor(typeof(ConnectionGUIInspectorHelper))]
		public class ConnectionObj : Editor {

			public override bool RequiresConstantRepaint() {
				return true;
			}

			public override void OnInspectorGUI () {
				var con = ((ConnectionGUIInspectorHelper)target).con;
				if (con == null) return;
				

				var foldouts = ((ConnectionGUIInspectorHelper)target).foldouts;
				

				var count = 0;
				var throughputListDict = ((ConnectionGUIInspectorHelper)target).throughputListDict;
				if (throughputListDict == null)  return;

				foreach (var throughputList in throughputListDict.Values) {
					count += throughputList.Count;
				}

				EditorGUILayout.LabelField("Total", count.ToString());
				
				var redColor = new GUIStyle(EditorStyles.label);
				redColor.normal.textColor = Color.gray;
		 
				var index = 0;
				foreach (var groupKey in throughputListDict.Keys) {
					var throughputList = throughputListDict[groupKey];

					var foldout = foldouts[index];
					
					foldout = EditorGUILayout.Foldout(foldout, "Group Key:" + groupKey);
					if (foldout) {
						EditorGUI.indentLevel = 1;
						for (var i = 0; i < throughputList.Count; i++) {
							var sourceStr = throughputList[i].path;
							var isBundled = throughputList[i].isBundled;
							
							if (isBundled) EditorGUILayout.LabelField(sourceStr, redColor); 
							else EditorGUILayout.LabelField(sourceStr);
						}
						EditorGUI.indentLevel = 0;
					}
					foldouts[index] = foldout;

					index++;
				}
			}
		}

		public Rect GetRect () {
			return buttonRect;
		}
		
		/**
			throughputListDict contains:
				group/
					throughput assets
		*/
		public void DrawConnection (List<NodeGUI> nodes, Dictionary<string, List<DepreacatedThroughputAsset>> throughputListDict) {

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

			Handles.DrawBezier(startV3, endV3, startTan, endTan, Color.gray, null, 4f);

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

			// draw connection arrow.
			if (NodeGUI.scaleFactor == NodeGUI.SCALE_MAX) {
				GUI.DrawTexture(
					new Rect(
						endV3.x - AssetBundleGraphGUISettings.CONNECTION_ARROW_WIDTH + 4f, 
						endV3.y - (AssetBundleGraphGUISettings.CONNECTION_ARROW_HEIGHT / 2f) - 1f, 
						AssetBundleGraphGUISettings.CONNECTION_ARROW_WIDTH, 
						AssetBundleGraphGUISettings.CONNECTION_ARROW_HEIGHT
					), 
					ConnectionGUIUtility.connectionArrowTex
				);
			}

			/*
				draw throughtput badge.
			*/
			var throughputCount = 0;
			foreach (var list in throughputListDict.Values) {
				throughputCount += list.Count;
			}
			
			var offsetSize = throughputCount.ToString().Length * 20f;
			
			buttonRect = new Rect(centerPointV3.x - offsetSize/2f, centerPointV3.y - 7f, offsetSize, 20f);

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

			if (GUI.Button(buttonRect, throughputCount.ToString(), connectionButtonStyle)) {
				conInsp.UpdateCon(this, throughputListDict);
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