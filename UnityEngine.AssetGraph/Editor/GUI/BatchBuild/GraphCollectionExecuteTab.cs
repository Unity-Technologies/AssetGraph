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
        private BatchBuildConfig.GraphCollection m_currentCollection;
        private BuildTarget m_activeBuildTarget;
        private Vector2 scrollPos;
        private bool m_useGraphCollection;

        private List<ExecuteGraphResult> m_result;

        [SerializeField]
        TreeViewState m_buildTargetTreeState;

        [SerializeField]
        TreeViewState m_executeResultTreeState;

        [SerializeField]
        float m_VerticalSplitterPercent;

        Rect m_position;

        BuildTargetTree m_buildTargetTree;
        GraphCollectionExecuteResultTree m_executeResultTree;

        bool m_ResizingVerticalSplitter = false;
        Rect m_VerticalSplitterRect;

        const float k_SplitterWidth = 3f;
        private static float m_UpdateDelay = 0f;

        EditorWindow m_Parent = null;

        public GraphCollectionExecuteTab()
        {
            m_VerticalSplitterPercent = 0.7f;
            m_VerticalSplitterRect = new Rect(
                m_position.x,
                (int)(m_position.y + m_position.height * m_VerticalSplitterPercent),
                m_position.width - k_SplitterWidth, k_SplitterWidth);

//            m_graphsInProject = new List<GraphEntry>();
//
//            m_activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
//
//            m_useGraphCollection = Model.Settings.UserSettings.BatchBuildUseCollectionState;
//            string lastCollection = Model.Settings.UserSettings.BatchBuildLastSelectedCollection;
//            var newCollection = BatchBuildConfig.GetConfig().Find(lastCollection);
//
//            SelectGraphCollection(newCollection);
//
//            scrollPos = new Vector2(0f,0f);
//            UpdateGraphList();

        }

        public void OnEnable(Rect pos, EditorWindow parent)
        {
            m_Parent = parent;
            m_position = pos;
        }

        public void Refresh() {
        }

        public void Update()
        {
//            if(Time.realtimeSinceStartup - m_UpdateDelay > 0.1f)
//            {
//                m_UpdateDelay = Time.realtimeSinceStartup;
//
//                if(AssetBundleModel.Model.Update())
//                {
//                    m_Parent.Repaint();
//                }
//
//                if (m_detailTree != null) {
//                    m_detailTree.Update ();
//                }
//            }
        }

        public void ForceReloadData()
        {
//            UpdateSelectedBundles(new List<AssetBundleModel.BundleInfo>());
//            SetSelectedItems(new List<AssetBundleModel.AssetInfo>());
//            AssetBundleModel.Model.ForceReloadData(m_collectionTree);
            m_Parent.Repaint();
        }

        public void OnGUI(Rect pos)
        {
            m_position = pos;

//            if(m_collectionTree == null)
//            {
//                if (m_GraphCollectionTreeState == null) {
//                    m_GraphCollectionTreeState = new TreeViewState ();
//                }
//                m_collectionTree = new GraphCollectionTree(m_GraphCollectionTreeState, this);
//                m_collectionTree.Refresh();
//
//                if (m_GraphCollectionDetailTreeState == null) {
//                    m_GraphCollectionDetailTreeState = new TreeViewState ();
//                }
//                m_detailTree = new GraphCollectionDetailTree(m_GraphCollectionDetailTreeState, this);
//                m_detailTree.Reload();
//
//                m_Parent.Repaint();
//            }
//            
//            HandleHorizontalResize();
//
//            if (AssetBundleModel.Model.BundleListIsEmpty())
//            {
//                m_collectionTree.OnGUI(m_position);
//                var style = GUI.skin.label;
//                style.alignment = TextAnchor.MiddleCenter;
//                style.wordWrap = true;
//                GUI.Label(
//                    new Rect(m_position.x + 1f, m_position.y + 1f, m_position.width - 2f, m_position.height - 2f), 
//                    new GUIContent("Empty"),
//                    style);
//            }
//            else
//            {
//                //Left half
//                var bundleTreeRect = new Rect(
//                    m_position.x,
//                    m_position.y,
//                    m_HorizontalSplitterRect.x,
//                    m_position.height);
//                
//                m_collectionTree.OnGUI(bundleTreeRect);
//
//                //Right half.
//                float panelLeft = m_HorizontalSplitterRect.x + k_SplitterWidth;
//                float panelWidth =  m_position.width - m_HorizontalSplitterRect.x - k_SplitterWidth * 2;
//                m_detailTree.OnGUI(new Rect(
//                    panelLeft,
//                    m_position.y,
//                    panelWidth,
//                    m_position.height));
//
//                if (m_ResizingHorizontalSplitter) {
//                    m_Parent.Repaint ();
//                }
//            }
        }

        private void HandleVerticalResize()
        {
//            m_VerticalSplitterRectRight.x = m_HorizontalSplitterRect.x;
//            m_VerticalSplitterRectRight.y = (int)(m_HorizontalSplitterRect.height * m_VerticalSplitterPercentRight);
//            m_VerticalSplitterRectRight.width = m_Position.width - m_HorizontalSplitterRect.x;
//            m_VerticalSplitterRectLeft.y = (int)(m_HorizontalSplitterRect.height * m_VerticalSplitterPercentLeft);
//            m_VerticalSplitterRectLeft.width = m_VerticalSplitterRectRight.width;
//
//
//            EditorGUIUtility.AddCursorRect(m_VerticalSplitterRectRight, MouseCursor.ResizeVertical);
//            if (Event.current.type == EventType.mouseDown && m_VerticalSplitterRectRight.Contains(Event.current.mousePosition))
//                m_ResizingVerticalSplitterRight = true;
//
//            EditorGUIUtility.AddCursorRect(m_VerticalSplitterRectLeft, MouseCursor.ResizeVertical);
//            if (Event.current.type == EventType.mouseDown && m_VerticalSplitterRectLeft.Contains(Event.current.mousePosition))
//                m_ResizingVerticalSplitterLeft = true;
//
//
//            if (m_ResizingVerticalSplitterRight)
//            {
//                m_VerticalSplitterPercentRight = Mathf.Clamp(Event.current.mousePosition.y / m_HorizontalSplitterRect.height, 0.2f, 0.98f);
//                m_VerticalSplitterRectRight.y = (int)(m_HorizontalSplitterRect.height * m_VerticalSplitterPercentRight);
//            }
//            else if (m_ResizingVerticalSplitterLeft)
//            {
//                m_VerticalSplitterPercentLeft = Mathf.Clamp(Event.current.mousePosition.y / m_HorizontalSplitterRect.height, 0.25f, 0.98f);
//                m_VerticalSplitterRectLeft.y = (int)(m_HorizontalSplitterRect.height * m_VerticalSplitterPercentLeft);
//            }
//
//
//            if (Event.current.type == EventType.MouseUp)
//            {
//                m_ResizingVerticalSplitterRight = false;
//                m_ResizingVerticalSplitterLeft = false;
//            }
        }

        public void Build() {
            m_result = null;
//            var collection = m_currentCollection;
//            if(!m_useGraphCollection || collection == null) {
//                collection = CreateNewCollection(true);
//            }
//
//            m_result = AssetGraphUtility.ExecuteGraphCollection(m_activeBuildTarget, collection);
//
//            foreach(var r in m_result) {
//                if(r.IsAnyIssueFound) {
//                    foreach(var e in r.Issues) {
//
//                        LogUtility.Logger.LogError(LogUtility.kTag, r.Graph.name + ":" + e.Reason);
//                    }
//                }
//            }
        }
    }
}