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

    internal class BuildTargetTreeItem : TreeViewItem
    {
        private BuildTargetGroup    m_group;
        private BuildTarget         m_target;

        public BuildTargetGroup Group {
            get {
                return m_group;
            }
            set {
                m_group = value;
            }
        }

        public BuildTarget Target {
            get {
                return m_target;
            }
            set {
                m_target = value;
            }
        }

        public BuildTargetTreeItem() : base(-1, -1) { }
        public BuildTargetTreeItem(BuildTarget t) : base((int)t, 0, string.Empty)
        {
            m_group = BuildTargetUtility.TargetToGroup(t);
            m_target = t;
        }
    }

    internal class BuildTargetTree : TreeView
    { 
        GraphCollectionExecuteTab m_controller;
        private bool m_ContextOnItem = false;
        List<UnityEngine.Object> m_EmptyObjectList = new List<Object>();

        private List<BuildTarget> m_targets;

        public BuildTargetTree(TreeViewState state, GraphCollectionExecuteTab ctrl) : base(state)
        {
            m_controller = ctrl;
            showBorder = true;
            m_targets = new List<BuildTarget> ();
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            base.RowGUI(args);
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new BuildTargetTreeItem ();

            foreach (var t in m_targets) {
                root.AddChild (new BuildTargetTreeItem (t));
            }

            return root;
        }

        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);
        }

        protected override void KeyEvent()
        {
            if (Event.current.keyCode == KeyCode.Delete && GetSelection().Count > 0)
            {
//                List<AssetBundleModel.BundleTreeItem> selectedNodes = new List<AssetBundleModel.BundleTreeItem>();
//                foreach (var nodeID in GetSelection())
//                {
//                    selectedNodes.Add(FindItem(nodeID, rootItem) as AssetBundleModel.BundleTreeItem);
//                }
//                DeleteBundles(selectedNodes);
            }
        }

        class DragAndDropData
        {
            public DragAndDropArgs args;

            public DragAndDropData(DragAndDropArgs a)
            {
                args = a;
//                draggedNodes = DragAndDrop.GetGenericData("AssetBundleModel.BundleInfo") as List<AssetBundleModel.BundleInfo>;
//                targetNode = args.parentItem as AssetBundleModel.BundleTreeItem;
//                paths = DragAndDrop.paths;
//
//                if (draggedNodes != null)
//                {
//                    foreach (var bundle in draggedNodes)
//                    {
//                        if ((bundle as AssetBundleModel.BundleFolderInfo) != null)
//                        {
//                            hasBundleFolder = true;
//                        }
//                        else
//                        {
//                            var dataBundle = bundle as AssetBundleModel.BundleDataInfo;
//                            if (dataBundle != null)
//                            {
//                                if (dataBundle.isSceneBundle)
//                                    hasScene = true;
//                                else
//                                    hasNonScene = true;
//
//                                if ( (dataBundle as AssetBundleModel.BundleVariantDataInfo) != null)
//                                    hasVariantChild = true;
//                            }
//                        }
//                    }
//                }
//                else if (DragAndDrop.paths != null)
//                {
//                    foreach (var assetPath in DragAndDrop.paths)
//                    {
//                        if (AssetDatabase.GetMainAssetTypeAtPath(assetPath) == typeof(SceneAsset))
//                            hasScene = true;
//                        else
//                            hasNonScene = true;
//                    }
//                }
            }

        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            DragAndDropVisualMode visualMode = DragAndDropVisualMode.None;
//            DragAndDropData data = new DragAndDropData(args);
//            
//            if (AssetBundleModel.Model.DataSource.IsReadOnly ()) {
//                return DragAndDropVisualMode.Rejected;
//            }
//
//            if ( (data.hasScene && data.hasNonScene) ||
//                (data.hasVariantChild) )
//                return DragAndDropVisualMode.Rejected;
//            
//            switch (args.dragAndDropPosition)
//            {
//                case DragAndDropPosition.UponItem:
//                    visualMode = HandleDragDropUpon(data);
//                    break;
//                case DragAndDropPosition.BetweenItems:
//                    visualMode = HandleDragDropBetween(data);
//                    break;
//                case DragAndDropPosition.OutsideItems:
//                    if (data.draggedNodes != null)
//                    {
//                        visualMode = DragAndDropVisualMode.Copy;
//                        if (data.args.performDrop)
//                        {
//                            AssetBundleModel.Model.HandleBundleReparent(data.draggedNodes, null);
//                            Reload();
//                        }
//                    }
//                    else if(data.paths != null)
//                    {
//                        visualMode = DragAndDropVisualMode.Copy;
//                        if (data.args.performDrop)
//                        {
//                            DragPathsToNewSpace(data.paths, null);
//                        }
//                    }
//                    break;
//            }
            return visualMode;
        }

        private DragAndDropVisualMode HandleDragDropUpon(DragAndDropData data)
        {
            DragAndDropVisualMode visualMode = DragAndDropVisualMode.Copy;//Move;
//            var targetDataBundle = data.targetNode.bundle as AssetBundleModel.BundleDataInfo;
//            if (targetDataBundle != null)
//            {
//                if (targetDataBundle.isSceneBundle)
//                {
//                    if(data.hasNonScene)
//                        return DragAndDropVisualMode.Rejected;
//                }
//                else
//                {
//                    if (data.hasBundleFolder)
//                    {
//                        return DragAndDropVisualMode.Rejected;
//                    }
//                    else if (data.hasScene && !targetDataBundle.IsEmpty())
//                    {
//                        return DragAndDropVisualMode.Rejected;
//                    }
//
//                }
//
//               
//                if (data.args.performDrop)
//                {
//                    if (data.draggedNodes != null)
//                    {
//                        AssetBundleModel.Model.HandleBundleMerge(data.draggedNodes, targetDataBundle);
//                        ReloadAndSelect(targetDataBundle.nameHashCode, false);
//                    }
//                    else if (data.paths != null)
//                    {
//                        AssetBundleModel.Model.MoveAssetToBundle(data.paths, targetDataBundle.m_Name.bundleName, targetDataBundle.m_Name.variant);
//                        AssetBundleModel.Model.ExecuteAssetMove();
//                        ReloadAndSelect(targetDataBundle.nameHashCode, false);
//                    }
//                }
//
//            }
//            else
//            {
//                var folder = data.targetNode.bundle as AssetBundleModel.BundleFolderInfo;
//                if (folder != null)
//                {
//                    if (data.args.performDrop)
//                    {
//                        if (data.draggedNodes != null)
//                        {
//                            AssetBundleModel.Model.HandleBundleReparent(data.draggedNodes, folder);
//                            Reload();
//                        }
//                        else if (data.paths != null)
//                        {
//                            DragPathsToNewSpace(data.paths, folder);
//                        }
//                    }
//                }
//                else
//                    visualMode = DragAndDropVisualMode.Rejected; //must be a variantfolder
//                
//            }
            return visualMode;
        }

        private DragAndDropVisualMode HandleDragDropBetween(DragAndDropData data)
        {
            DragAndDropVisualMode visualMode = DragAndDropVisualMode.Copy;//Move;

//            var parent = (data.args.parentItem as AssetBundleModel.BundleTreeItem);
//
//            if (parent != null)
//            {
//                var variantFolder = parent.bundle as AssetBundleModel.BundleVariantFolderInfo;
//                if (variantFolder != null)
//                    return DragAndDropVisualMode.Rejected;
//
//                if (data.args.performDrop)
//                {
//                    var folder = parent.bundle as AssetBundleModel.BundleFolderConcreteInfo;
//                    if (folder != null)
//                    {
//                        if (data.draggedNodes != null)
//                        {
//                            AssetBundleModel.Model.HandleBundleReparent(data.draggedNodes, folder);
//                            Reload();
//                        }
//                        else if (data.paths != null)
//                        {
//                            DragPathsToNewSpace(data.paths, folder);
//                        }
//                    }
//                }
//            }

            return visualMode;
        }

//        private string[] dragToNewSpacePaths = null;
//        private AssetBundleModel.BundleFolderInfo dragToNewSpaceRoot = null;
//        private void DragPathsAsOneBundle()
//        {
//            var newBundle = AssetBundleModel.Model.CreateEmptyBundle(dragToNewSpaceRoot);
//            AssetBundleModel.Model.MoveAssetToBundle(dragToNewSpacePaths, newBundle.m_Name.bundleName, newBundle.m_Name.variant);
//            AssetBundleModel.Model.ExecuteAssetMove();
//            ReloadAndSelect(newBundle.nameHashCode, true);
//        }
//        private void DragPathsAsManyBundles()
//        {
//            List<int> hashCodes = new List<int>();
//            foreach (var assetPath in dragToNewSpacePaths)
//            {
//                var newBundle = AssetBundleModel.Model.CreateEmptyBundle(dragToNewSpaceRoot, System.IO.Path.GetFileNameWithoutExtension(assetPath).ToLower());
//                AssetBundleModel.Model.MoveAssetToBundle(assetPath, newBundle.m_Name.bundleName, newBundle.m_Name.variant);
//                hashCodes.Add(newBundle.nameHashCode);
//            }
//            AssetBundleModel.Model.ExecuteAssetMove();
//            ReloadAndSelect(hashCodes);
//        }
//
//        private void DragPathsToNewSpace(string[] paths, AssetBundleModel.BundleFolderInfo root)
//        {
//            dragToNewSpacePaths = paths;
//            dragToNewSpaceRoot = root;
//            if (paths.Length > 1)
//            {
//                GenericMenu menu = new GenericMenu();
//                menu.AddItem(new GUIContent("Create 1 Bundle"), false, DragPathsAsOneBundle);
//                var message = "Create ";
//                message += paths.Length;
//                message += " Bundles";
//                menu.AddItem(new GUIContent(message), false, DragPathsAsManyBundles);
//                menu.ShowAsContext();
//            }
//            else
//                DragPathsAsManyBundles();
//        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
//            DragAndDrop.PrepareStartDrag();
//
//            var selectedBundles = new List<AssetBundleModel.BundleInfo>();
//            foreach (var id in args.draggedItemIDs)
//            {
//                var item = FindItem(id, rootItem) as AssetBundleModel.BundleTreeItem;
//                selectedBundles.Add(item.bundle);
//            }
//            DragAndDrop.paths = null;
//            DragAndDrop.objectReferences = m_EmptyObjectList.ToArray();
//            DragAndDrop.SetGenericData("AssetBundleModel.BundleInfo", selectedBundles);
//            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;//Move;
//            DragAndDrop.StartDrag("AssetBundleTree");
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            return true;
        }

        internal void Refresh()
        {
            var selection = GetSelection();
            Reload();
            SelectionChanged(selection);
        }

        private void ReloadAndSelect(int hashCode, bool rename)
        {
            var selection = new List<int>();
            selection.Add(hashCode);
            ReloadAndSelect(selection);
            if(rename)
            {
                BeginRename(FindItem(hashCode, rootItem), 0.25f);
            }
        }
        private void ReloadAndSelect(IList<int> hashCodes)
        {
            Reload();
            SetSelection(hashCodes, TreeViewSelectionOptions.RevealAndFrame);
            SelectionChanged(hashCodes);
        }
    }
}
