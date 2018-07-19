using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
	public class AssetBundlesSettingsTab {

        private string[] m_graphGuids;
        private string[] m_graphNames;

        public AssetBundlesSettingsTab() {
            Refresh();
        }

        public void Refresh() {
            m_graphGuids = AssetDatabase.FindAssets(Model.Settings.GRAPH_SEARCH_CONDITION);
            m_graphNames = new string[m_graphGuids.Length];
            for (int i = 0; i < m_graphGuids.Length; ++i) {
                m_graphNames[i] = Path.GetFileNameWithoutExtension (AssetDatabase.GUIDToAssetPath (m_graphGuids[i]));
            }
        }

        private string DrawFolderSelector(string label, 
            string dialogTitle, 
            string currentDirPath, 
            string directoryOpenPath, 
            Func<string, string> onValidFolderSelected = null) 
        {
            string newDirPath;
            using(new EditorGUILayout.HorizontalScope()) {
                if (string.IsNullOrEmpty (label)) {
                    newDirPath = EditorGUILayout.TextField(currentDirPath);
                } else {
                    newDirPath = EditorGUILayout.TextField(label, currentDirPath);
                }

                if(GUILayout.Button("Select", GUILayout.Width(50f))) {
                    var folderSelected = 
                        EditorUtility.OpenFolderPanel(dialogTitle, directoryOpenPath, "");
                    if(!string.IsNullOrEmpty(folderSelected)) {
                        if (onValidFolderSelected != null) {
                            newDirPath = onValidFolderSelected (folderSelected);
                        } else {
                            newDirPath = folderSelected;
                        }
                    }
                }
            }
            return newDirPath;
        }

        private void DrawCacheDirGUI() {
            using (new EditorGUILayout.VerticalScope()) {

                string cacheDir = Model.Settings.UserSettings.AssetBundleBuildCacheDir;

                using (new EditorGUILayout.HorizontalScope ()) {                    
                    var newCacheDir = DrawFolderSelector ("Bundle Cache Directory", "Select Cache Folder", 
                        cacheDir,
                        Application.dataPath + "/../",
                        (string folderSelected) => {
                            var projectPath = Directory.GetParent(Application.dataPath).ToString();

                            if(projectPath == folderSelected) {
                                folderSelected = string.Empty;
                            } else {
                                var index = folderSelected.IndexOf(projectPath);
                                if(index >= 0 ) {
                                    folderSelected = folderSelected.Substring(projectPath.Length + index);
                                    if(folderSelected.IndexOf('/') == 0) {
                                        folderSelected = folderSelected.Substring(1);
                                    }
                                }
                            }
                            return folderSelected;
                        }
                    );
                    if (newCacheDir != cacheDir) {
                        Model.Settings.UserSettings.AssetBundleBuildCacheDir = newCacheDir;
                    }
                }

                using (new EditorGUI.DisabledScope (!Directory.Exists (cacheDir))) 
                {
                    using (new EditorGUILayout.HorizontalScope ()) {
                        GUILayout.FlexibleSpace ();
                        #if UNITY_EDITOR_OSX
                        string buttonName = "Reveal in Finder";
                        #else
                        string buttonName = "Show in Explorer";
                        #endif
                        if (GUILayout.Button (buttonName)) {
                            EditorUtility.RevealInFinder (cacheDir);
                        }
                    }
                }
            }

            EditorGUILayout.HelpBox (
                "Bundle Cache Directory is the default place to save AssetBundles when 'Build Asset Bundles' node performs build. " +
                "This can be set outside of the project with relative path.", 
                MessageType.Info);
        }

        private void DrawABGraphList() {
            string abGraphGuid = Model.Settings.UserSettings.DefaultAssetBundleBuildGraphGuid;

            int index = ArrayUtility.IndexOf(m_graphGuids, abGraphGuid);
            var selected = EditorGUILayout.Popup ("Default AssetBundle Graph", index, m_graphNames);

            if (index != selected) {
                Model.Settings.UserSettings.DefaultAssetBundleBuildGraphGuid = m_graphGuids [selected];
            }

            EditorGUILayout.HelpBox (
                "Default AssetBundle Graph is the default graph to build AssetBundles for this project. " +
                "This graph will be automatically called in AssetBundle Browser integration.", 
                MessageType.Info);
        }

		public void OnGUI () {
            DrawCacheDirGUI ();

            GUILayout.Space (20f);

            DrawABGraphList ();
		}
	}
}
