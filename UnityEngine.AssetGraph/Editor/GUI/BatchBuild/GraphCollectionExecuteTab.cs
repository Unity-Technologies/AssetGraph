using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
    [System.Serializable]
    internal class GraphCollectionExecuteTab 
    {
        private const float k_SplitterHeight = 3f;

        [SerializeField]
        private TreeViewState m_buildTargetTreeState;

        [SerializeField]
        private TreeViewState m_executeResultTreeState;

        [SerializeField]
        private float m_verticalSplitterPercent;

        [SerializeField]
        private string m_selectedCollectionGuid;

        private BatchBuildConfig.GraphCollection m_currentCollection;
        private List<ExecuteGraphResult> m_result;

        private BuildTargetTree m_buildTargetTree;
        private ExecuteResultTree m_executeResultTree;
        private string[] m_collectionNames;
        private int m_selectedCollectionIndex = -1;

        private bool m_resizingVerticalSplitter = false;
        private Rect m_verticalSplitterRect;
        private EditorWindow m_parent = null;
        private long m_lastBuildTimestamp;

        public GraphCollectionExecuteTab()
        {
            m_verticalSplitterPercent = 0.2f;
            m_verticalSplitterRect = new Rect(0,0,0, k_SplitterHeight);
        }

        public List<ExecuteGraphResult> CurrentResult {
            get {
                return m_result;
            }
        }

        public long LastBuildTimestamp {
            get {
                return m_lastBuildTimestamp;
            }
        }

        public void OnEnable(Rect pos, EditorWindow parent)
        {
            m_parent = parent;

            m_result = new List<ExecuteGraphResult> ();

            m_buildTargetTreeState = new TreeViewState ();
            m_buildTargetTree = new BuildTargetTree(m_buildTargetTreeState);

            m_executeResultTreeState = new TreeViewState ();
            m_executeResultTree = new ExecuteResultTree(m_executeResultTreeState, this);

            m_buildTargetTree.Reload ();
            m_executeResultTree.Reload ();
        }

        public void Refresh() {
            m_buildTargetTree.Reload ();
            m_executeResultTree.ReloadIfNeeded ();
            var collection = BatchBuildConfig.GetConfig ().GraphCollections;
            m_collectionNames = collection.Select (c => c.Name).ToArray ();
            m_selectedCollectionIndex = collection.FindIndex (c => c.Guid == m_selectedCollectionGuid);
            if (m_selectedCollectionIndex >= 0) {
                m_currentCollection = collection [m_selectedCollectionIndex];
            }
        }

        private void DrawBuildDropdown(Rect region) {

            Rect popupRgn  = new Rect (region.x+20f, region.y, region.width - 120f, region.height);
            Rect buttonRgn = new Rect (popupRgn.xMax+8f, popupRgn.y, 80f, popupRgn.height);

            EditorGUI.BeginDisabledGroup ( BatchBuildConfig.GetConfig ().GraphCollections.Count == 0 );

            var newIndex = EditorGUI.Popup(popupRgn, "Graph Collection", m_selectedCollectionIndex, m_collectionNames);
            if (newIndex != m_selectedCollectionIndex) {
                m_selectedCollectionIndex = newIndex;
                m_currentCollection = BatchBuildConfig.GetConfig ().GraphCollections [m_selectedCollectionIndex];
                m_selectedCollectionGuid = m_currentCollection.Guid;
            }

            EditorGUI.BeginDisabledGroup ( m_currentCollection == null || BatchBuildConfig.GetConfig ().BuildTargets.Count == 0 );
            if( GUI.Button(buttonRgn, "Build") ) {
                Build ();
            }
            EditorGUI.EndDisabledGroup ();
            EditorGUI.EndDisabledGroup ();
        }

        public void OnGUI(Rect pos)
        {
            var dropdownUIBound = new Rect (0f, 0f, pos.width, 16f);
            var labelUIBound = new Rect (8f, dropdownUIBound.yMax, 80f, 24f);
            var listviewUIBound = new Rect (0f, labelUIBound.yMax - 4f, dropdownUIBound.width, pos.height - dropdownUIBound.height);

            DrawBuildDropdown (dropdownUIBound);

            EditorGUI.BeginDisabledGroup ( m_currentCollection == null );

            var labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.alignment = TextAnchor.LowerLeft;
            GUI.Label (labelUIBound, "Build Targets", labelStyle);

            GUI.BeginGroup (listviewUIBound);
            var groupUIBound = new Rect (0f, 0f, listviewUIBound.width, listviewUIBound.height);

            HandleVerticalResize(groupUIBound);

            var boundTop = new Rect(
                8f,
                8f,
                groupUIBound.width - 16f,
                m_verticalSplitterRect.y - k_SplitterHeight - 4f);
            
            var bottomLabelUIBound = new Rect (8f, m_verticalSplitterRect.yMax, 80f, 24f);
            var boundBottom = new Rect(
                boundTop.x,
                bottomLabelUIBound.yMax,
                boundTop.width,
                groupUIBound.height - m_verticalSplitterRect.yMax - k_SplitterHeight - 8f);

            GUI.Label (bottomLabelUIBound, "Build Results", labelStyle);
            m_buildTargetTree.OnGUI (boundTop);

            if (BatchBuildConfig.GetConfig ().BuildTargets.Count == 0) {
                var style = GUI.skin.label;
                style.alignment = TextAnchor.MiddleCenter;
                style.wordWrap = true;

                GUI.Label(new Rect(boundTop.x+12f, boundTop.y, boundTop.width - 24f, boundTop.height), 
                    new GUIContent("Right click here and add targets to build."), style);
            }

            m_executeResultTree.OnGUI (boundBottom);

            if (m_resizingVerticalSplitter) {
                m_parent.Repaint ();
            }

            EditorGUI.EndDisabledGroup ();
            GUI.EndGroup ();
        }

        private void HandleVerticalResize(Rect bound)
        {
            m_verticalSplitterRect.x = bound.x;
            m_verticalSplitterRect.y = (int)(bound.height * m_verticalSplitterPercent);
            m_verticalSplitterRect.width = bound.width;

            EditorGUIUtility.AddCursorRect(m_verticalSplitterRect, MouseCursor.ResizeVertical);

            var mousePt = Event.current.mousePosition;

            if (Event.current.type == EventType.mouseDown && m_verticalSplitterRect.Contains (mousePt)) {
                m_resizingVerticalSplitter = true;
            }

            if (m_resizingVerticalSplitter)
            {
                m_verticalSplitterPercent = Mathf.Clamp(mousePt.y / bound.height, 0.1f, 0.9f);
                m_verticalSplitterRect.y = bound.y + (int)(bound.height * m_verticalSplitterPercent);
            }

            if (Event.current.type == EventType.MouseUp)
            {
                m_resizingVerticalSplitter = false;
            }
        }

        public void Build() {
            m_result.Clear ();

            foreach (var t in BatchBuildConfig.GetConfig ().BuildTargets) {

                Action<Model.NodeData, string, float> updateHandler = (node, message, progress) => {

                    string title = string.Format("{0} - Processing Graphs", BuildTargetUtility.TargetToHumaneString(t));
                    string info  = string.Format("{0}:{1}", node.Name, message);

                    EditorUtility.DisplayProgressBar(title, info, 1.0f);
                };

                var result = AssetGraphUtility.ExecuteGraphCollection(t, m_currentCollection, updateHandler);
                EditorUtility.ClearProgressBar();
                m_result.AddRange (result);

                m_lastBuildTimestamp = DateTime.UtcNow.ToFileTimeUtc ();

                m_executeResultTree.ReloadAndSelectLast ();
                m_parent.Repaint ();
            }
        }
    }
}