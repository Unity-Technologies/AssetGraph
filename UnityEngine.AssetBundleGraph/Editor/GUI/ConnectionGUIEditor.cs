using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {
	/**
		GUI Inspector to ConnectionGUI (Through ConnectionGUIInspectorHelper)
	*/
	[CustomEditor(typeof(ConnectionGUIInspectorHelper))]
	public class ConnectionGUIEditor : Editor {
		
		public override bool RequiresConstantRepaint() {
			return true;
		}

		public override void OnInspectorGUI () {

			ConnectionGUIInspectorHelper helper = target as ConnectionGUIInspectorHelper;

			var con = helper.connectionGUI;
			if (con == null) {
				return;
			}

			var foldouts = helper.foldouts;

			var count = 0;
			var assetGroups = helper.assetGroups;
			if (assetGroups == null)  {
				return;
			}

			foreach (var assets in assetGroups.Values) {
				count += assets.Count;
			}

			var groupCount = assetGroups.Keys.Count;

			GUILayout.Label("Stats", "BoldLabel");
			EditorGUILayout.LabelField("Total groups", groupCount.ToString());
			EditorGUILayout.LabelField("Total items" , count.ToString());

			GUILayout.Space(8f);

			GUILayout.Label("Display", "BoldLabel");
			helper.filterPattern = EditorGUILayout.TextField("Filter assets", helper.filterPattern);
			helper.fileNameOnly = EditorGUILayout.ToggleLeft("Show only file names", helper.fileNameOnly);

			Regex match = null;
			if(!string.IsNullOrEmpty(helper.filterPattern)) {
				match = new Regex(helper.filterPattern);
			}

			GUILayout.Space(8f);
			GUILayout.Label("Groups", "BoldLabel");
			GUILayout.Space(4f);

			var redColor = new GUIStyle(EditorStyles.label);
			redColor.normal.textColor = Color.gray;

			var index = 0;
			foreach (var groupKey in assetGroups.Keys) {
				var assets = assetGroups[groupKey];

				var foldout = foldouts[index];

				foldout = EditorGUILayout.Foldout(foldout, string.Format("Group name: {0} ({1} items)", groupKey, assets.Count));
				if (foldout) {
					EditorGUI.indentLevel = 1;
					for (var i = 0; i < assets.Count; i++) {

						if(match != null) {
							if(!match.IsMatch(assets[i].path)) {
								continue;
							}
						}

						var sourceStr = (helper.fileNameOnly) ? assets[i].fileNameAndExtension : assets[i].path;
						var variantName = assets[i].variantName;

                        using (new EditorGUILayout.HorizontalScope ()) {
                            if (!string.IsNullOrEmpty (variantName)) {
                                EditorGUILayout.LabelField (string.Format ("{0}[{1}]", sourceStr, variantName));
                            } else {
                                EditorGUILayout.LabelField(sourceStr);
                            }
                            if (GUILayout.Button ("Select", GUILayout.Width (50f))) {
                                var obj = AssetDatabase.LoadMainAssetAtPath(assets[i].path);
                                if (obj != null) {
                                    EditorGUIUtility.PingObject(obj);
                                    Selection.activeObject = obj;
                                }
                            }
                        }
					}
					EditorGUI.indentLevel = 0;
				}
				foldouts[index] = foldout;

				index++;
			}
		}
	}
}