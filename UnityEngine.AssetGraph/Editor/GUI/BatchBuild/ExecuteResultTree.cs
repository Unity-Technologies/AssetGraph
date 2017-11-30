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

    internal class ExecuteResultTreeItem : TreeViewItem
    {
        private ExecuteGraphResult m_result;

        private static int s_id = 0;

        public ExecuteGraphResult Result {
            get {
                return m_result;
            }
        }

        public ExecuteResultTreeItem() : base(-1, -1) { }
        public ExecuteResultTreeItem(ExecuteGraphResult r) : base(s_id++, 0, string.Empty)
        {
            m_result = r;
            displayName = string.Format ("{0}({1}):{2}", Path.GetFileNameWithoutExtension(r.GraphAssetPath), BuildTargetUtility.TargetToHumaneString(r.Target), (r.IsAnyIssueFound)?"Failed" : "Good");
        }
    }

    internal class NodeExceptionItem : TreeViewItem
    {
        private ExecuteGraphResult m_result;
        private NodeException m_exception;

        private static int s_id = 100000;

        public ExecuteGraphResult Result {
            get {
                return m_result;
            }
        }

        public NodeException Exception {
            get {
                return m_exception;
            }
        }

        public NodeExceptionItem(ExecuteGraphResult r, NodeException e) : base(s_id++, 1, string.Empty)
        {
            m_result = r;
            m_exception = e;
            displayName = string.Format ("{0}:{1}", m_exception.Node.Name, m_exception.Reason);
        }
    }

    internal class ExecuteResultTree : TreeView
    { 
        private GraphCollectionExecuteTab m_controller;
        private long m_lastTimestamp;

        public ExecuteResultTree(TreeViewState state, GraphCollectionExecuteTab ctrl) : base(state)
        {
            m_controller = ctrl;
            showBorder = true;
            showAlternatingRowBackgrounds = true;
        }

        protected override float GetCustomRowHeight(int row, TreeViewItem item) {
            return 32f;
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            base.RowGUI(args);
        }

        protected override TreeViewItem BuildRoot()
        {
            m_lastTimestamp = m_controller.LastBuildTimestamp;

            var root = new ExecuteResultTreeItem ();

            foreach (var r in m_controller.CurrentResult) {
                var resultItem = new ExecuteResultTreeItem (r);
                root.AddChild (resultItem);

                if (r.IsAnyIssueFound) {
                    foreach (var e in r.Issues) {
                        resultItem.AddChild (new NodeExceptionItem(r, e));
                    }
                }
            }

            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = base.BuildRows(root);
            return rows;
        }

        protected override void DoubleClickedItem(int id)
        {
            var resultItem = FindItem(id, rootItem) as ExecuteResultTreeItem;
            if (resultItem != null) {
                EditorGUIUtility.PingObject (resultItem.Result.Graph);
                Selection.activeObject = resultItem.Result.Graph;
            } else {
                var exeptionItem = FindItem(id, rootItem) as NodeExceptionItem;
                if (exeptionItem != null) {
                    var window = EditorWindow.GetWindow<AssetGraphEditorWindow>();
                    window.OpenGraph(exeptionItem.Result.GraphAssetPath);
                    window.SelectNode (exeptionItem.Exception.NodeId);
                }
            }
        }

//        protected override void SelectionChanged(IList<int> selectedIds)
//        {
//            if (selectedIds != null)
//            {
//                foreach (var id in selectedIds)
//                {
//                    //TODO
//                }
//            }
//        }

        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);
            if(Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
            {
                SetSelection(new int[0], TreeViewSelectionOptions.FireSelectionChanged);
            }
        }

        public void ReloadIfNeeded() {
            if (m_lastTimestamp != m_controller.LastBuildTimestamp) {
                Reload ();
            }
        }

        public void ReloadAndSelectLast()
        {
            ReloadIfNeeded ();
            if (rootItem.children != null && rootItem.children.Count > 0) {
                SetSelection (new int[] { rootItem.children.Last().id });
            }
        }
    }
}
