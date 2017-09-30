using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {

	[CustomEditor(typeof(Model.ConfigGraph))]
	public class ConfigGraphEditor : Editor {

		private class Styles {
			public static readonly string kEDITBUTTON_LABEL		= "Open in Graph Editor";
			public static readonly string kEDITBUTTON_DESCRIPTION	= "Opens in the AssetBundle Graph Editor, which will allow you to configure the graph";
			public static readonly GUIContent kEDITBUTTON = new GUIContent(kEDITBUTTON_LABEL, kEDITBUTTON_DESCRIPTION);
		}

		public override void OnInspectorGUI()
		{
			Model.ConfigGraph graph = target as Model.ConfigGraph;

			using(new EditorGUILayout.HorizontalScope()) {
				GUILayout.Label(graph.name, "BoldLabel");
				if (GUILayout.Button(Styles.kEDITBUTTON, GUILayout.Width(150f), GUILayout.ExpandWidth(false)))
				{
					// Get the target we are inspecting and open the graph
					var window = EditorWindow.GetWindow<AssetBundleGraphEditorWindow>();
					window.OpenGraph(graph);
				}
			}

			using(new EditorGUILayout.VerticalScope(GUI.skin.box)) {
				EditorGUILayout.LabelField("Version", graph.Version.ToString());
				EditorGUILayout.LabelField("Last Modified", graph.LastModified.ToString());
				using(new EditorGUILayout.HorizontalScope()) {
					GUILayout.Label("Description", GUILayout.Width(100f));
					string newdesc = EditorGUILayout.TextArea(graph.Descrption, GUILayout.MaxHeight(100f));
					if(newdesc != graph.Descrption) {
						graph.Descrption = newdesc;
					}
				}
				GUILayout.Space(2f);
			}
		}
	}
}
	