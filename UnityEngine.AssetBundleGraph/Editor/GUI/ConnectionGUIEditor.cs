using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {
	[CustomEditor(typeof(ConnectionGUIInspectorHelper))]
	public class ConnectionGUIEditor : Editor {
		
        private GroupViewController m_groupViewController;

		public override bool RequiresConstantRepaint() {
			return true;
		}

        public void OnFocus () {
        }

		public override void OnInspectorGUI () {

			ConnectionGUIInspectorHelper helper = target as ConnectionGUIInspectorHelper;

            if(m_groupViewController == null ) {
                m_groupViewController = new GroupViewController(helper.groupViewContext);
            }

			var con = helper.connectionGUI;
			if (con == null) {
				return;
			}

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

            #if UNITY_5_6_OR_NEWER
            #else
            m_groupViewController.m_foldOuts = helper.foldouts;

            GUILayout.Label("Display", "BoldLabel");
            helper.filterPattern = EditorGUILayout.TextField("Filter assets", helper.filterPattern);
            helper.fileNameOnly = EditorGUILayout.ToggleLeft("Show only file names", helper.fileNameOnly);

            m_groupViewController.m_match = helper.filterPattern;
            m_groupViewController.isFileNameOnly = helper.fileNameOnly;
            #endif

            GUILayout.Space(4f);

            m_groupViewController.SetGroups (assetGroups);
            m_groupViewController.OnGroupViewGUI ();
		}
	}
}