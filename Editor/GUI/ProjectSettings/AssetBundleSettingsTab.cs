using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using Model=Unity.AssetGraph.DataModel.Version2;

namespace Unity.AssetGraph {
	public class AssetBundlesSettingsTab {

        private string[] m_graphGuids;
        private string[] m_graphNames;

	    private string m_buildmapPath;
	    private string m_buildmapMoveErrorMsg;

        public AssetBundlesSettingsTab() {
            Refresh();
        }

        public void Refresh()
        {
            m_buildmapPath = AssetBundleBuildMap.UserSettings.AssetBundleBuildMapPath;
            m_buildmapMoveErrorMsg = string.Empty;
            m_graphGuids = AssetDatabase.FindAssets(Model.Settings.GRAPH_SEARCH_CONDITION);
            m_graphNames = new string[m_graphGuids.Length];
            for (int i = 0; i < m_graphGuids.Length; ++i) {
                m_graphNames[i] = Path.GetFileNameWithoutExtension (AssetDatabase.GUIDToAssetPath (m_graphGuids[i]));
            }
        }

        private void DrawConfigBaseDirGUI() {
            using (new EditorGUILayout.VerticalScope()) {

                string baseDir = Model.Settings.UserSettings.ConfigBaseDir;

                using (new EditorGUILayout.HorizontalScope ()) {                    
                    var newBaseDir = GUIHelper.DrawFolderSelector ("Config Directory", "Select Config Folder", 
                        baseDir,
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
                    if (newBaseDir != baseDir) {
                        Model.Settings.UserSettings.ConfigBaseDir = newBaseDir;
                    }
                }

                using (new EditorGUI.DisabledScope (!Directory.Exists (baseDir))) 
                {
                    using (new EditorGUILayout.HorizontalScope ()) {
                        GUILayout.FlexibleSpace ();
                        if (GUILayout.Button (GUIHelper.RevealInFinderLabel)) {
                            EditorUtility.RevealInFinder (baseDir);
                        }
                    }
                }
            }

            EditorGUILayout.HelpBox (
                "Bundle Cache Directory is the default place to save AssetBundles when 'Build Asset Bundles' node performs build. " +
                "This can be set outside of the project with relative path.", 
                MessageType.Info);
        }        
        
        private void DrawCacheDirGUI() {
            using (new EditorGUILayout.VerticalScope()) {

                string cacheDir = Model.Settings.UserSettings.AssetBundleBuildCacheDir;

                using (new EditorGUILayout.HorizontalScope ()) {                    
                    var newCacheDir = GUIHelper.DrawFolderSelector ("Bundle Cache Directory", "Select Cache Folder", 
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
                        if (GUILayout.Button (GUIHelper.RevealInFinderLabel)) {
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

	    private void DrawABBuildMapPath()
	    {
	        using (new EditorGUILayout.HorizontalScope())
	        {
	            m_buildmapPath = EditorGUILayout.TextField("AssetBundle Build Map File", m_buildmapPath);

	            using (new EditorGUI.DisabledScope(m_buildmapPath == AssetBundleBuildMap.UserSettings.AssetBundleBuildMapPath))
	            {
	                if (GUILayout.Button("Set", GUILayout.Width(50)))
	                {
	                    var oldPath = AssetBundleBuildMap.UserSettings.AssetBundleBuildMapPath;
	                    m_buildmapMoveErrorMsg = string.Empty;
	                    if (File.Exists(oldPath))
	                    {
	                        m_buildmapMoveErrorMsg = AssetDatabase.MoveAsset(oldPath, m_buildmapPath);
	                    }

	                    if (string.IsNullOrEmpty(m_buildmapMoveErrorMsg))
	                    {
	                        AssetBundleBuildMap.UserSettings.AssetBundleBuildMapPath = m_buildmapPath;
	                    }
	                }
	            }
	        }

	        if (!string.IsNullOrEmpty(m_buildmapMoveErrorMsg))
	        {
	            EditorGUILayout.HelpBox (
	                m_buildmapMoveErrorMsg, 
	                MessageType.Error);
	        }
	        
	        EditorGUILayout.HelpBox (
	            "AssetBundle build map file is an asset used to store assets to assetbundles relationship. ", 
	            MessageType.Info);
	    }

		public void OnGUI () {
            DrawCacheDirGUI ();

		    GUILayout.Space (20f);

		    DrawABBuildMapPath();
		    
            GUILayout.Space (20f);

            DrawABGraphList ();
		}
	}
}
