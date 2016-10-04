using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;


namespace AssetBundleGraph {
	[Serializable] 
	public class NodeGUI {

		public static float scaleFactor = 1.0f;// 1.0f. 0.7f, 0.4f, 0.3f
		public const float SCALE_MIN = 0.3f;
		public const float SCALE_MAX = 1.0f;
		public const int SCALE_WIDTH = 30;
		public const float SCALE_RATIO = 0.3f;

		[SerializeField] private int m_nodeWindowId;
		[SerializeField] private Rect m_baseRect;

		[SerializeField] private NodeData m_data;

		[SerializeField] private string m_nodeSyle;
		[SerializeField] private NodeGUIInspectorHelper m_nodeInsp;

		/*
			show error on node functions.
		*/
		private bool m_hasErrors = false;
		/*
					show progress on node functions(unused. due to mainthread synchronization problem.)
			can not update any visual on Editor while building AssetBundles through AssetBundleGraph.
		*/
		private float m_progress;
		private bool m_running;

		/*
		 * Properties
		 */
		public string Name {
			get {
				return m_data.Name;
			}
			set {
				m_data.Name = value;
			}
		}

		public string Id {
			get {
				return m_data.Id;
			}
		}

		public NodeKind Kind {
			get {
				return m_data.Kind;
			}
		}

		public NodeData Data {
			get {
				return m_data;
			}
		}

		public Rect Region {
			get {
				return m_baseRect;
			}
		}

		public void ResetErrorStatus () {
			m_hasErrors = false;
			this.m_nodeInsp.UpdateNode(this);
			this.m_nodeInsp.UpdateErrors(new List<string>());
		}

		public void AppendErrorSources (List<string> errors) {
			this.m_hasErrors = true;
			this.m_nodeInsp.UpdateNode(this);
			this.m_nodeInsp.UpdateErrors(errors);
		}

		public int WindowId {
			get {
				return m_nodeWindowId;
			}

			set {
				m_nodeWindowId = value;
			}
		}

		public NodeGUI (NodeData data) {
			this.m_nodeInsp = ScriptableObject.CreateInstance<NodeGUIInspectorHelper>();
			this.m_nodeInsp.hideFlags = HideFlags.DontSave;
			this.m_nodeWindowId = 0;

			this.m_data = data;

			this.m_baseRect = new Rect(m_data.X, m_data.Y, AssetBundleGraphSettings.GUI.NODE_BASE_WIDTH, AssetBundleGraphSettings.GUI.NODE_BASE_HEIGHT);

			this.m_nodeSyle = NodeGUIUtility.UnselectedStyle[m_data.Kind];

			UpdateNodeRect();
		}

		public NodeGUI Duplicate (float newX, float newY) {
			var data = m_data.Duplicate();
			data.X = newX;
			data.Y = newY;
			return new NodeGUI(data);
		}

		public void SetActive () {
			m_nodeInsp.UpdateNode(this);
			Selection.activeObject = m_nodeInsp;
			this.m_nodeSyle = NodeGUIUtility.SelectedStyle[m_data.Kind];
		}

		public void SetInactive () {
			this.m_nodeSyle = NodeGUIUtility.UnselectedStyle[m_data.Kind];
		}
			
		private void RefreshConnectionPos () {
			for (int i = 0; i < m_data.InputPoints.Count; i++) {
				var point = m_data.InputPoints[i];
				point.UpdateRegion(this, i, m_data.InputPoints.Count);
			}

			for (int i = 0; i < m_data.OutputPoints.Count; i++) {
				var point = m_data.OutputPoints[i];
				point.UpdateRegion(this, i, m_data.OutputPoints.Count);
			}
		}

		public void DrawNode () {
			var scaledBaseRect = ScaleEffect(m_baseRect);

			var movedRect = GUI.Window(m_nodeWindowId, scaledBaseRect, DrawThisNode, string.Empty, m_nodeSyle);

			m_baseRect.position = m_baseRect.position + (movedRect.position - scaledBaseRect.position);
		}

		public static Rect ScaleEffect (Rect nonScaledRect) {
			var scaledRect = new Rect(nonScaledRect);
			scaledRect.x = scaledRect.x * scaleFactor;
			scaledRect.y = scaledRect.y * scaleFactor;
			scaledRect.width = scaledRect.width * scaleFactor;
			scaledRect.height = scaledRect.height * scaleFactor;
			return scaledRect;
		}

		public static Vector2 ScaleEffect (Vector2 nonScaledVector2) {
			var scaledVector2 = new Vector2(nonScaledVector2.x, nonScaledVector2.y);
			scaledVector2.x = scaledVector2.x * scaleFactor;
			scaledVector2.y = scaledVector2.y * scaleFactor;
			return scaledVector2;
		}

		private void DrawThisNode(int id) {
			HandleNodeEvent ();
			DrawNodeContents();
			GUI.DragWindow();
		}

		/**
			retrieve mouse events for this node in this AssetGraoh window.
		*/
		private void HandleNodeEvent () {
			switch (Event.current.type) {

			/*
					handling release of mouse drag from this node to another node.
					this node doesn't know about where the other node is. the master only knows.
					only emit event.
				*/
			case EventType.Ignore: {
					NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_NODE_CONNECTION_OVERED, this, Event.current.mousePosition, null));
					break;
				}

				/*
					handling drag.
				*/
			case EventType.MouseDrag: {
					NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_NODE_MOVING, this, Event.current.mousePosition, null));
					break;
				}

				/*
					check if the mouse-down point is over one of the connectionPoint in this node.
					then emit event.
				*/
			case EventType.MouseDown: {
					ConnectionPointData result = IsOverConnectionPoint(Event.current.mousePosition);

					if (result != null) {
						if (scaleFactor == SCALE_MAX) {
							NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_NODE_CONNECT_STARTED, this, Event.current.mousePosition, result));
						}
						break;
					}
					break;
				}
			}

			/*
				retrieve mouse events for this node in|out of this AssetGraoh window.
			*/
			switch (Event.current.rawType) {
			case EventType.MouseUp: {
					bool eventRaised = false;
					// if mouse position is on the connection point, emit mouse raised event.
					Action<ConnectionPointData> raiseEventIfHit = (ConnectionPointData point) => {
						// Only one connectionPoint raise event at one mouseup event
						if(eventRaised) {
							return;
						}
//						var region = point.Region;
//						var globalConnectonPointRect = new Rect(region.x, region.y, region.width, region.height);
						if (point.Region.Contains(Event.current.mousePosition)) {
							NodeGUIUtility.NodeEventHandler(
								new NodeEvent(NodeEvent.EventType.EVENT_NODE_CONNECTION_RAISED, 
									this, Event.current.mousePosition, point));
							eventRaised = true;
							return;
						}
					};
					m_data.InputPoints.ForEach(raiseEventIfHit);
					m_data.OutputPoints.ForEach(raiseEventIfHit);
					if(!eventRaised) {
						NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_NODE_TOUCHED, 
							this, Event.current.mousePosition, null));
					}
					break;
				}
			}

			/*
				right click to open Context menu
			*/
			if (scaleFactor == SCALE_MAX) {
				if (Event.current.type == EventType.ContextClick || (Event.current.type == EventType.MouseUp && Event.current.button == 1)) 
				{
					var menu = new GenericMenu();
					menu.AddItem(
						new GUIContent("Delete"),
						false, 
						() => {
							NodeGUIUtility.NodeEventHandler(new NodeEvent(NodeEvent.EventType.EVENT_CLOSE_TAPPED, this, Vector2.zero, null));
						}
					);
					menu.ShowAsContext();
					Event.current.Use();
				}
			}
		}

		public void DrawConnectionInputPointMark (NodeEvent eventSource, bool justConnecting) {
			if (scaleFactor != SCALE_MAX) return;

			var defaultPointTex = NodeGUIUtility.inputPointMarkTex;
			bool shouldDrawEnable = 
				!( eventSource != null && eventSource.eventSourceNode != null && 
					!ConnectionData.CanConnect(eventSource.eventSourceNode.Data, m_data)
				);

			if (shouldDrawEnable && justConnecting && eventSource != null) {
				if (eventSource.eventSourceNode.Id != this.Id) {
					var connectionPoint = eventSource.point;
					if (connectionPoint.IsOutput) {
						defaultPointTex = NodeGUIUtility.enablePointMarkTex;
					}
				}
			}

			foreach (var point in m_data.InputPoints) {				
				GUI.DrawTexture(
					point.GetGlobalPointRegion(this), 
					defaultPointTex
				);
			}
		}

		public void DrawConnectionOutputPointMark (NodeEvent eventSource, bool justConnecting, Event current) {
			if (scaleFactor != SCALE_MAX) return;

			var defaultPointTex = NodeGUIUtility.outputPointMarkConnectedTex;
			bool shouldDrawEnable = 
				!( eventSource != null && eventSource.eventSourceNode != null && 
					!ConnectionData.CanConnect(m_data, eventSource.eventSourceNode.Data)
				);

			if (shouldDrawEnable && justConnecting && eventSource != null) {
				if (eventSource.eventSourceNode.Id != this.Id) {
					var connectionPoint = eventSource.point;
					if (connectionPoint.IsInput) {
						defaultPointTex = NodeGUIUtility.enablePointMarkTex;
					}
				}
			}

			var globalMousePosition = current.mousePosition;

			foreach (var point in m_data.OutputPoints) {
				var pointRegion = point.GetGlobalPointRegion(this);

				GUI.DrawTexture(
					pointRegion, 
					defaultPointTex
				);

				// eventPosition is contained by outputPointRect.
				if (pointRegion.Contains(globalMousePosition)) {
					if (current.type == EventType.MouseDown) {
						NodeGUIUtility.NodeEventHandler(
							new NodeEvent(NodeEvent.EventType.EVENT_NODE_CONNECT_STARTED, this, current.mousePosition, point));
					}
				}
			}
		}

		private void DrawNodeContents () {
			var style = new GUIStyle(EditorStyles.label);
			style.alignment = TextAnchor.MiddleCenter;

			var connectionNodeStyleOutput = new GUIStyle(EditorStyles.label);
			connectionNodeStyleOutput.alignment = TextAnchor.MiddleRight;

			var connectionNodeStyleInput = new GUIStyle(EditorStyles.label);
			connectionNodeStyleInput.alignment = TextAnchor.MiddleLeft;

			var nodeTitleRect = new Rect(0, 0, m_baseRect.width * scaleFactor, m_baseRect.height * scaleFactor);
			GUI.Label(nodeTitleRect, Name, style);

			if (m_running) {
				EditorGUI.ProgressBar(new Rect(10f, m_baseRect.height - 20f, m_baseRect.width - 20f, 10f), m_progress, string.Empty);
			}
			if (m_hasErrors) {
				GUIStyle errorStyle = new GUIStyle("CN EntryError");
				errorStyle.alignment = TextAnchor.MiddleCenter;
				var labelSize = GUI.skin.label.CalcSize(new GUIContent(Name));
				EditorGUI.LabelField(new Rect((nodeTitleRect.width - labelSize.x )/2.0f - 28f, (nodeTitleRect.height-labelSize.y)/2.0f - 7f, 20f, 20f), string.Empty, errorStyle);
			}

			// draw & update connectionPoint button interface.
			if (scaleFactor == SCALE_MAX) {
				Action<ConnectionPointData> drawConnectionPoint = (ConnectionPointData point) => 
				{
					var label = point.Label;
					if( label != AssetBundleGraphSettings.DEFAULT_INPUTPOINT_LABEL &&
						label != AssetBundleGraphSettings.DEFAULT_OUTPUTPOINT_LABEL) 
					{
						var region = point.Region;
						// if point is output node, then label position offset is minus. otherwise plus.
						var xOffset = (point.IsOutput) ? - m_baseRect.width : AssetBundleGraphSettings.GUI.INPUT_POINT_WIDTH;
						var labelStyle = (point.IsOutput) ? connectionNodeStyleOutput : connectionNodeStyleInput;
						var labelRect = new Rect(region.x + xOffset, region.y - (region.height/2), m_baseRect.width, region.height*2);

						GUI.Label(labelRect, label, labelStyle);
					}
					GUI.backgroundColor = Color.clear;
					Texture2D tex = (point.IsInput)? NodeGUIUtility.inputPointTex : NodeGUIUtility.outputPointTex;
					GUI.Button(point.Region, tex, "AnimationKeyframeBackground");
				};
				m_data.InputPoints.ForEach(drawConnectionPoint);
				m_data.OutputPoints.ForEach(drawConnectionPoint);
			}
		}

		public void UpdateNodeRect () {
			// UpdateNodeRect will be called outside OnGUI(), so it use inacurate but simple way to calcurate label width
			// instead of CalcSize()
			var contentLabelWordsLength = this.Name.Length;

			if(m_data.InputPoints.Count > 0) {
				var inputLabels = m_data.InputPoints.OrderByDescending(p => p.Label.Length).Select(p => p.Label.Length).ToList();
				if (inputLabels.Any()) {
					contentLabelWordsLength = contentLabelWordsLength + 1 + inputLabels[0];
				}
			}

			if(m_data.OutputPoints.Count > 0) {
				var outputLabels = m_data.OutputPoints.OrderByDescending(p => p.Label.Length).Select(p => p.Label.Length).ToList();
				if (outputLabels.Any()) {
					contentLabelWordsLength = contentLabelWordsLength + 1 + outputLabels[0];
				}
			}

			// update node height by number of output connectionPoint.
			var nPoints = Mathf.Max(m_data.OutputPoints.Count, m_data.InputPoints.Count);
			this.m_baseRect = new Rect(m_baseRect.x, m_baseRect.y, 
				m_baseRect.width, 
				AssetBundleGraphSettings.GUI.NODE_BASE_HEIGHT + (AssetBundleGraphSettings.GUI.FILTER_OUTPUT_SPAN * Mathf.Max(0, (nPoints - 1)))
			);

			var newWidth = Mathf.Max(AssetBundleGraphSettings.GUI.NODE_BASE_WIDTH, contentLabelWordsLength * 10.0f);
			m_baseRect = new Rect(m_baseRect.x, m_baseRect.y, newWidth, m_baseRect.height);

			RefreshConnectionPos();
		}

		private ConnectionPointData IsOverConnectionPoint (Vector2 touchedPoint) {

			foreach(var p in m_data.InputPoints) {
				var region = p.Region;
				if (region.x <= touchedPoint.x && 
					touchedPoint.x <= region.x + region.width && 
					region.y <= touchedPoint.y && 
					touchedPoint.y <= region.y + region.height
				) {
					return p;
				}
			}

			foreach(var p in m_data.OutputPoints) {
				var region = p.Region;
				if (region.x <= touchedPoint.x && 
					touchedPoint.x <= region.x + region.width && 
					region.y <= touchedPoint.y && 
					touchedPoint.y <= region.y + region.height
				) {
					return p;
				}
			}

			return null;
		}

		public Rect GetRect () {
			return m_baseRect;
		}

		public Vector2 GetPos () {
			return m_baseRect.position;
		}

		public int GetX () {
			return (int)m_baseRect.x;
		}

		public int GetY () {
			return (int)m_baseRect.y;
		}

		public int GetRightPos () {
			return (int)(m_baseRect.x + m_baseRect.width);
		}

		public int GetBottomPos () {
			return (int)(m_baseRect.y + m_baseRect.height);
		}

		public void SetPos (Vector2 position) {
			m_baseRect.position = position;
			m_data.X = position.x;
			m_data.Y = position.y;
		}

		public void SetProgress (float val) {
			m_progress = val;
		}

		public void MoveRelative (Vector2 diff) {
			m_baseRect.position = m_baseRect.position - diff;
		}

		public void ShowProgress () {
			m_running = true;
		}

		public void HideProgress () {
			m_running = false;
		}

		public bool Conitains (Vector2 globalPos) {
			if (m_baseRect.Contains(globalPos)) {
				return true;
			}
			foreach (var point in m_data.OutputPoints) {
				if (point.GetGlobalPointRegion(this).Contains(globalPos)) {
					return true;
				}
			}
			return false;
		}

		public ConnectionPointData FindConnectionPointByPosition (Vector2 globalPos) {

			foreach (var point in m_data.InputPoints) {
				if (point.GetGlobalRegion(this).Contains(globalPos) || 
					point.GetGlobalPointRegion(this).Contains(globalPos)) 
				{
					return point;
				}
			}

			foreach (var point in m_data.OutputPoints) {
				if (point.GetGlobalRegion(this).Contains(globalPos) || 
					point.GetGlobalPointRegion(this).Contains(globalPos)) 
				{
					return point;
				}
			}

			return null;
		}

		public static void ShowTypeNamesMenu (string current, List<string> contents, Action<string> ExistSelected) {
			var menu = new GenericMenu();

			for (var i = 0; i < contents.Count; i++) {
				var type = contents[i];
				var selected = false;
				if (type == current) selected = true;

				menu.AddItem(
					new GUIContent(type),
					selected,
					() => {
						ExistSelected(type);
					}
				);
			}
			menu.ShowAsContext();
		}

		public static void ShowFilterKeyTypeMenu (string current, Action<string> Selected) {
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
						Selected(type);
					}
				);
			}
			menu.ShowAsContext();
		}
	}
}
