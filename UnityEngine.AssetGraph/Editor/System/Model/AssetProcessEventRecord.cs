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
        [SerializeField] private List<AssetProcessEventLog> m_eventLogs;
		[SerializeField] private int m_version;
        [SerializeField] private int m_processStartIndex;

        private static AssetProcessEventRecord s_record;

        private static AssetProcessEventRecord GetRecord() {
			if(s_record == null) {
				if(!Load()) {
					// Create vanilla db
                    s_record = ScriptableObject.CreateInstance<AssetProcessEventRecord>();
                    s_record.m_events = new List<AssetProcessEvent>();
                    s_record.m_eventLogs = new List<AssetProcessEventLog>();
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

		private static void SeRecordDirty() {
			EditorUtility.SetDirty(s_record);
		}

        public static void LogEvent(AssetProcessEvent e, Model.ConfigGraph g) {
            GetRecord ()._LogEvent (e, g);
        }

        public static void LogEvent(AssetProcessEvent e, AssetReference a, Model.ConfigGraph g, Model.NodeData n) {
            GetRecord ()._LogEvent (e, a, g, n);
        }

        private void _LogEvent(AssetProcessEvent e, Model.ConfigGraph g) {
            var newLog = new AssetProcessEventLog (e, g);
            m_eventLogs.Add (newLog);
            SeRecordDirty ();
        }

        private void _LogEvent(AssetProcessEvent e, AssetReference a, Model.ConfigGraph g, Model.NodeData n) {
            var newLog = new AssetProcessEventLog (e, a, g, n);
            m_eventLogs.Add (newLog);
            SeRecordDirty ();
        }

        public static AssetProcessEvent GetGraphBeginEvent() {
            return GetRecord ().GetEvent (AssetProcessEvent.EventKind.GraphBegin, string.Empty, string.Empty, string.Empty, string.Empty);
        }

        public static AssetProcessEvent GetGraphEndEvent() {
            return GetRecord ().GetEvent (AssetProcessEvent.EventKind.GraphEnd, string.Empty, string.Empty, string.Empty, string.Empty);
        }

        public static AssetProcessEvent GetProcessEvent(string nodeType, string eventName) {
            return GetRecord ().GetEvent (AssetProcessEvent.EventKind.Process, nodeType, eventName, string.Empty, string.Empty);
        }

        public static AssetProcessEvent GetErrorEvent(string nodeType, string eventName, string whatIsIt, string howToFix) {
            return GetRecord ().GetEvent (AssetProcessEvent.EventKind.Process, nodeType, eventName, whatIsIt, howToFix);
        }

        private AssetProcessEvent CreateEvent(AssetProcessEvent.EventKind k, string nodeType, string eventName, string whatIsIt, string howToFix) {
            var e = ScriptableObject.CreateInstance<AssetProcessEvent> ();
            e.Init (k, nodeType, eventName, whatIsIt, howToFix);
            m_events.Add (e);
            SeRecordDirty ();
            return e;
        }

        private AssetProcessEvent GetEvent(AssetProcessEvent.EventKind k, string nodeType, string eventName, string whatisit, string howtofix) 
        {
            var events = m_events.Where (ev => ev.Kind == k && ev.NodeType == nodeType && ev.EventName == eventName);
            if (events.Any ()) {
                return events.First ();
            } else {
                return CreateEvent(k, nodeType, eventName, whatisit, howtofix);
            }
        }
	}
}

