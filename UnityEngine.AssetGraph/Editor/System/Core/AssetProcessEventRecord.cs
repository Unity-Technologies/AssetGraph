using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
	public class AssetProcessEventRecord : ScriptableObject {

        private const int VERSION = 1;

        private delegate AssetProcessEvent EventCreator ();

        [SerializeField] private List<AssetProcessEvent> m_events;
        [SerializeField] private int m_errorEventCount;
        [SerializeField] private int m_infoEventCount;
        [SerializeField] private int m_version;
        [SerializeField] private int m_processStartIndex;

        public delegate void AssetProcessEventCallback(AssetProcessEvent e);
        public event AssetProcessEventCallback onAssetProcessEvent;

        private static AssetProcessEventRecord s_record;

        private List<AssetProcessEvent> m_filteredEvents;
        private bool m_includeError;
        private bool m_includeInfo;

        public List<AssetProcessEvent> Events {
            get {
                return m_filteredEvents;
            }
        }

        public int InfoEventCount {
            get {
                return m_infoEventCount;
            }
        }

        public int ErrorEventCount {
            get {
                return m_errorEventCount;
            }
        }

        public static AssetProcessEventRecord GetRecord() {
			if(s_record == null) {
				if(!Load()) {
					// Create vanilla db
                    s_record = ScriptableObject.CreateInstance<AssetProcessEventRecord>();
                    s_record.Init ();

                    var DBDir = AssetGraphBasePath.TemporalSettingFilePath;

					if (!Directory.Exists(DBDir)) {
						Directory.CreateDirectory(DBDir);
					}

                    AssetDatabase.CreateAsset(s_record, Model.Settings.Path.EventRecordPath);
				}
			}

			return s_record;
		}

		private static bool Load() {

			bool loaded = false;

			try {
                var path = Model.Settings.Path.EventRecordPath;
				
				if(File.Exists(path)) 
				{
                    AssetProcessEventRecord record = AssetDatabase.LoadAssetAtPath<AssetProcessEventRecord>(path);

					if(record != null && record.m_version == VERSION) {
						s_record = record;
						loaded = true;
                    } else {
                        if(record != null) {
                            Resources.UnloadAsset(record);
                        }
                    }
				}
			} catch(Exception e) {
				LogUtility.Logger.LogWarning(LogUtility.kTag, e);
			}

			return loaded;
		}

		private static void SetRecordDirty() {
			EditorUtility.SetDirty(s_record);
		}

        private void Init() {
            m_events = new List<AssetProcessEvent>();
            m_filteredEvents = new List<AssetProcessEvent>();
            m_errorEventCount = 0;
            m_infoEventCount = 0;
            m_includeError = true;
            m_includeInfo = true;
            m_version = VERSION;

        }

        public void SetFilterCondition(bool includeInfo, bool includeError) {

            if (includeInfo != m_includeInfo || includeError != m_includeError) {
                m_includeInfo = includeInfo;
                m_includeError = includeError;

                RebuildFilteredEvents ();
            }
        }

        private void RebuildFilteredEvents() {
            m_filteredEvents.Clear ();
            m_filteredEvents.Capacity = m_events.Count;

            foreach (var e in m_events) {
                if (m_includeError && e.Kind == AssetProcessEvent.EventKind.Error) {
                    m_filteredEvents.Add (e);
                }
                if (m_includeInfo && e.Kind != AssetProcessEvent.EventKind.Error) {
                    m_filteredEvents.Add (e);
                }
            }
        }

        public void LogGraphBegin() {
            AssetGraphController gc = AssetGraphPostprocessor.Postprocessor.GetCurrentGraphController ();

            if (gc == null) {
                throw new AssetGraphException ("GraphBegin event attempt to log but no graph is in stack.");
            }

            var newEvent = AssetProcessEvent.CreateGraphBeginEvent (gc.TargetGraph.GetGraphGuid ());

            AddEvent (newEvent);
        }

        public void LogGraphEnd() {
            AssetGraphController gc = AssetGraphPostprocessor.Postprocessor.GetCurrentGraphController ();

            if (gc == null) {
                throw new AssetGraphException ("GraphEnd event attempt to log but no graph is in stack.");
            }

            var newEvent = AssetProcessEvent.CreateGraphEndEvent (gc.TargetGraph.GetGraphGuid ());

            AddEvent (newEvent);
        }

        public void LogModify(string assetGuid) {
            AssetGraphController gc = AssetGraphPostprocessor.Postprocessor.GetCurrentGraphController ();

            if (gc == null) {
                throw new AssetGraphException ("Modify event attempt to log but no graph is in stack.");
            }

            var newEvent = AssetProcessEvent.CreateModifyEvent (assetGuid, gc.TargetGraph.GetGraphGuid (), gc.CurrentNode);

            AddEvent (newEvent);
        }

        public void LogError(NodeException e) {
            AssetGraphController gc = AssetGraphPostprocessor.Postprocessor.GetCurrentGraphController ();

            if (gc == null) {
                throw new AssetGraphException ("Error event attempt to log but no graph is in stack.");
            }

            var newEvent = AssetProcessEvent.CreateErrorEvent (e, gc.TargetGraph.GetGraphGuid ());

            AddEvent (newEvent);
        }

        private void AddEvent(AssetProcessEvent e) {
            m_events.Add (e);

            if (e.Kind == AssetProcessEvent.EventKind.Error) {
                ++m_errorEventCount;

                if(m_includeError) {
                    m_filteredEvents.Add (e);
                }
            }
            if (e.Kind != AssetProcessEvent.EventKind.Error) {
                ++m_infoEventCount;

                if(m_includeInfo) {
                    m_filteredEvents.Add (e);
                }
            }

            if (onAssetProcessEvent != null) {
                onAssetProcessEvent (e);
            }
            SetRecordDirty ();
        }

        public void Clear(bool executeGraphsWithError) {

            List<string> graphGuids = null;

            if (executeGraphsWithError) {
                graphGuids = m_events.Where (e => e.Kind == AssetProcessEvent.EventKind.Error).Select (e => e.GraphGuid).Distinct().ToList ();
            }

            m_events.Clear ();
            m_filteredEvents.Clear ();
            m_errorEventCount = 0;
            m_infoEventCount = 0;

            if (executeGraphsWithError) {
                AssetGraphUtility.ExecuteAllGraphs (graphGuids, true);
            }
        }
	}
}

