using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {

	class AssetGraphPostprocessor : AssetPostprocessor 
	{
        private Stack<AssetGraphController> m_controllers;
        private Stack<AssetPostprocessorContext> m_contexts;
        private Queue<AssetPostprocessorContext> m_ppQueue;

        private static AssetGraphPostprocessor s_postprocessor;
        public static AssetGraphPostprocessor Postprocessor {
            get {
                if (s_postprocessor == null) {
                    s_postprocessor = new AssetGraphPostprocessor ();
                    s_postprocessor.Init ();
                }
                return s_postprocessor;
            }
        }

        private void Init() {
            m_controllers = new Stack<AssetGraphController> ();
            m_contexts = new Stack<AssetPostprocessorContext> ();
            m_ppQueue = new Queue<AssetPostprocessorContext> ();

            EditorApplication.update += this.OnEditorUpdate;
        }

        private void OnEditorUpdate() {
            if (m_controllers.Count != 0) {
                return;
            }

            if (m_ppQueue.Count > 0) {
                var ctx = m_ppQueue.Dequeue ();
                DoPostprocessWithContext (ctx);
            }
        }

        public bool PushController(AssetGraphController c) {

            if (m_controllers.Where (x => x.TargetGraph.GetGraphGuid () == c.TargetGraph.GetGraphGuid ()).Any ()) {
                return false;
            }

            m_controllers.Push (c);

            return true;
        }

        public void PushContext(AssetPostprocessorContext c) {
            m_contexts.Push (c);
        }

        public AssetGraphController GetCurrentGraphController() {
            if (m_controllers.Count == 0) {
                return null;
            }
            return m_controllers.Peek ();
        }

        public void PopController () {
            m_controllers.Pop ();
        }

        public void PopContext () {
            m_contexts.Pop ();
            if (m_contexts.Count == 1 && m_contexts.Peek().IsAdhoc) {
                m_contexts.Pop ();
            }
        }

        public void AddModifiedAsset(AssetReference a) {

            AssetPostprocessorContext ctx = null;

            if (m_contexts.Count == 0) {
                ctx = new AssetPostprocessorContext ();
                m_contexts.Push (ctx);
            } else {
                ctx = m_contexts.Peek ();
            }

            if (!ctx.ImportedAssets.Contains (a)) {
                ctx.ImportedAssets.Add (a);
            }
        }

        static void OnPostprocessAllAssets (string[] importedAssets, 
            string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) 
        {
            Postprocessor.HandleAllAssets (importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
        }

        private void HandleAllAssets(string[] importedAssets, 
            string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) 
        {
            foreach (string movedFrom in movedFromAssetPaths) {
                if (movedFrom == AssetGraphBasePath.BasePath) {
                    AssetGraphBasePath.ResetBasePath ();
                }
            }

            foreach (string str in importedAssets) {
                AssetReferenceDatabase.GetReference (str).InvalidateTypeCache();
            }

            foreach (string str in deletedAssets) 
            {
                AssetReferenceDatabase.DeleteReference(str);
            }

            for (int i=0; i<movedAssets.Length; i++)
            {
                AssetReferenceDatabase.MoveReference(movedFromAssetPaths[i], movedAssets[i]);
            }
                
            var ctx = new AssetPostprocessorContext (importedAssets, deletedAssets, movedAssets, movedFromAssetPaths, m_contexts);

            if (!ctx.HasValidAssetToPostprocess()) {
                return;
            }

            // if modification happens inside graph, record it.
            if (m_controllers.Count > 0) {
                m_ppQueue.Enqueue (ctx);
                return;
            }

            DoPostprocessWithContext (ctx);
        }

        private void DoPostprocessWithContext(AssetPostprocessorContext ctx) {
            m_contexts.Push (ctx);
            NotifyAssetPostprocessorGraphs (ctx);
            AssetGraphEditorWindow.NotifyAssetsReimportedToAllWindows(ctx);
            AssetProcessEventLogWindow.NotifyAssetsReimportedToAllWindows(ctx);
            m_contexts.Pop ();
        }

        private void NotifyAssetPostprocessorGraphs(AssetPostprocessorContext ctx) 
		{
			var guids = AssetDatabase.FindAssets(Model.Settings.GRAPH_SEARCH_CONDITION);

            var executingGraphs = new List<Model.ConfigGraph> ();

			foreach(var guid in guids) {
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var graph = AssetDatabase.LoadAssetAtPath<Model.ConfigGraph>(path);
                if (graph != null && graph.UseAsAssetPostprocessor) {
                    bool isAnyNodeAffected = false;
					foreach(var n in graph.Nodes) {
                        isAnyNodeAffected |= n.Operation.Object.OnAssetsReimported(n, null, EditorUserBuildSettings.activeBuildTarget, ctx, true);
					}
                    if (isAnyNodeAffected) {
                        executingGraphs.Add (graph);
                    }
				}
			}

            if (executingGraphs.Count > 1) {
                executingGraphs.Sort ((l, r) => l.ExecuteOrderPriority - r.ExecuteOrderPriority);
            }
            foreach (var g in executingGraphs) {
                AssetGraphUtility.ExecuteGraph (g, false);
            }
		}
	}
}
