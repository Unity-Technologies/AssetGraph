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

    internal class GraphCollectionExecuteResultTreeItem : TreeViewItem
    {
        private NodeException m_exception;
        private string m_message;
        private string m_graphGuid;

        private static int s_id = 0;

        public GraphCollectionExecuteResultTreeItem() : base(-1, -1) { }
        public GraphCollectionExecuteResultTreeItem(NodeException e) : base(s_id++, 0, string.Empty)
        {
            m_exception = e;
        }
        public GraphCollectionExecuteResultTreeItem(string msg, string graphGuid) : base(graphGuid.GetHashCode(), 0, string.Empty)
        {
            m_exception = null;
            m_message = msg;
            m_graphGuid = graphGuid;
        }
    }

    internal class GraphCollectionExecuteResultTree : TreeView
    { 
        GraphCollectionExecuteTab m_Controller;
        private bool m_ContextOnItem = false;
        List<UnityEngine.Object> m_EmptyObjectList = new List<Object>();

        public GraphCollectionExecuteResultTree(TreeViewState state, GraphCollectionExecuteTab ctrl) : base(state)
        {
            m_Controller = ctrl;
            showBorder = true;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            base.RowGUI(args);
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new GraphCollectionExecuteResultTreeItem ();

            root.AddChild (new GraphCollectionExecuteResultTreeItem("test msg", ""));

            return root;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds != null)
            {
                foreach (var id in selectedIds)
                {
                    //TODO
                }
            }
        }

        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);
            if(Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
            {
                SetSelection(new int[0], TreeViewSelectionOptions.FireSelectionChanged);
            }
        }

        void ForceReloadData(object context)
        {
            //TODO
        }

    }
}
