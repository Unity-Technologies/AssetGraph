using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
    [System.Serializable]
    public class AssetProcessEventLog {

        [SerializeField] private AssetProcessEvent m_event;
        [SerializeField] private long m_timestampUtc;
        [SerializeField] private string m_assetGuid;
        [SerializeField] private string m_graphGuid;
        [SerializeField] private string m_nodeId;
        [SerializeField] private string m_message;


        public AssetProcessEvent Event {
            get {
                return m_event;
            }
        }

        public DateTime Timestamp {
            get {
                return DateTime.FromFileTimeUtc(m_timestampUtc);
            }
        }

        public string AssetGuid {
            get {
                return m_assetGuid;
            }
        }

        public string GraphGuid {
            get {
                return m_graphGuid;
            }
        }

        public string NodeId {
            get {
                return m_nodeId;
            }
        }

        public string Message {
            get {
                return m_message;
            }
            set {
                m_message = value;
            }
        }


        public AssetProcessEventLog() {
        }

        public AssetProcessEventLog(AssetProcessEvent e, Model.ConfigGraph g) {
            Init (e, g);
        }

        public AssetProcessEventLog(AssetProcessEvent e, AssetReference a, Model.ConfigGraph g, Model.NodeData n) {
            Init (e, a, g, n);
        }

        public void Init(AssetProcessEvent e, Model.ConfigGraph g) {
            m_event = e;
            m_graphGuid = AssetDatabase.AssetPathToGUID (AssetDatabase.GetAssetPath (g));
            m_timestampUtc = DateTime.Now.ToFileTimeUtc();

            m_assetGuid = null;
            m_nodeId = null;
        }

        public void Init(AssetProcessEvent e, AssetReference a, Model.ConfigGraph g, Model.NodeData n) {
            m_event = e;
            m_assetGuid = a.assetDatabaseId;
            m_graphGuid = AssetDatabase.AssetPathToGUID (AssetDatabase.GetAssetPath (g));
            m_nodeId = n.Id;
            m_timestampUtc = DateTime.Now.ToFileTimeUtc();
        }

        public bool IsValid() {
            //TODO
            return true;
        }
    }
}

