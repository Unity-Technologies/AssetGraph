using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;

namespace AssetBundleGraph {
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

			EditorGUILayout.LabelField("Total", count.ToString());

			var redColor = new GUIStyle(EditorStyles.label);
			redColor.normal.textColor = Color.gray;

			var index = 0;
			foreach (var groupKey in assetGroups.Keys) {
				var assets = assetGroups[groupKey];

				var foldout = foldouts[index];

				foldout = EditorGUILayout.Foldout(foldout, "Group Key:" + groupKey);
				if (foldout) {
					EditorGUI.indentLevel = 1;
					for (var i = 0; i < assets.Count; i++) {
						var sourceStr = assets[i].path;
						var variantName = assets[i].variantName;

						if(!string.IsNullOrEmpty(variantName))
							EditorGUILayout.LabelField(string.Format("{0}[{1}]", sourceStr, variantName));
						else {
							EditorGUILayout.LabelField(sourceStr);
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