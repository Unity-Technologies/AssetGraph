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
		[SerializeField] private int m_version;
        [SerializeField] private int m_processStartIndex;

        private static AssetProcessEventRecord s_record;

        public static AssetProcessEventRecord GetRecord() {
			if(s_record == null) {
				if(!Load()) {
					// Create vanilla db
                    s_record = ScriptableObject.CreateInstance<AssetProcessEventRecord>();
                    s_record.m_events = new List<AssetProcessEvent>();
					s_record.m_version = VERSION;

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

        public void LogGraphBegin() {
            AssetGraphController gc = AssetGraphPostprocessor.Postprocessor.GetCurrentGraphController ();

            if (gc == null) {
                throw new AssetGraphException ("GraphBegin event attempt to log but no graph is in stack.");
            }

            var newEvent = AssetProcessEvent.CreateGraphBeginEvent (gc.TargetGraph.GetGraphGuid ());

            LogUtility.Logger.LogWarning (LogUtility.kTag, string.Format("[{0}]GraphBegin", gc.TargetGraph.GetGraphName()));

            m_events.Add (newEvent);
            SetRecordDirty ();
        }

        public void LogGraphEnd() {
            AssetGraphController gc = AssetGraphPostprocessor.Postprocessor.GetCurrentGraphController ();

            if (gc == null) {
                throw new AssetGraphException ("GraphEnd event attempt to log but no graph is in stack.");
            }

            var newEvent = AssetProcessEvent.CreateGraphEndEvent (gc.TargetGraph.GetGraphGuid ());

            LogUtility.Logger.LogWarning (LogUtility.kTag, string.Format("[{0}]GraphEnd", gc.TargetGraph.GetGraphName()));

            m_events.Add (newEvent);
            SetRecordDirty ();
        }

        public void LogModify(string assetGuid) {
            AssetGraphController gc = AssetGraphPostprocessor.Postprocessor.GetCurrentGraphController ();

            if (gc == null) {
                throw new AssetGraphException ("Modify event attempt to log but no graph is in stack.");
            }

            var newEvent = AssetProcessEvent.CreateModifyEvent (assetGuid, gc.TargetGraph.GetGraphGuid (), gc.CurrentNode.Id);

            LogUtility.Logger.LogWarning (LogUtility.kTag, string.Format("[{0}]Modified: {1} {2}", gc.TargetGraph.GetGraphName(), gc.CurrentNode.Name, 
                Path.GetFileName ( AssetDatabase.GUIDToAssetPath(assetGuid) )));

            m_events.Add (newEvent);
            SetRecordDirty ();
        }

        public void LogError(NodeException e) {
            AssetGraphController gc = AssetGraphPostprocessor.Postprocessor.GetCurrentGraphController ();

            if (gc == null) {
                throw new AssetGraphException ("Error event attempt to log but no graph is in stack.");
            }

            var newEvent = AssetProcessEvent.CreateErrorEvent (e, gc.TargetGraph.GetGraphGuid ());

            LogUtility.Logger.LogWarning (LogUtility.kTag, string.Format("[{0}]Error: {1} {2}", gc.TargetGraph.GetGraphName(), gc.CurrentNode.Name, 
                e.Reason, e.HowToFix));

            m_events.Add (newEvent);
            SetRecordDirty ();
        }

        public static void Clear() {
            var r = GetRecord ();
            r.m_events.Clear ();
        }
	}
}

