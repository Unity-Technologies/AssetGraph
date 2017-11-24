using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
    [System.Serializable]
    public class AssetProcessEvent {

        public enum EventKind
        {
            GraphBegin,
            GraphEnd,
            Modify,
            Error
        }

        [SerializeField] private EventKind m_kind;
        [SerializeField] private long m_timestampUtc;
        [SerializeField] private string m_assetGuid;
        [SerializeField] private string m_graphGuid;
        [SerializeField] private string m_nodeId;
        [SerializeField] private string m_description;
        [SerializeField] private string m_howToFix;

        public EventKind Kind {
            get { 
                return m_kind; 
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

        public string Description {
            get {
                return m_description;
            }
        }

        public string HowToFix {
            get {
                return m_howToFix;
            }
        }

        private AssetProcessEvent() {}

        private void Init(EventKind k, string assetGuid, string graphGuid, string nodeId, string desc, string howto) {
            m_kind = k;
            m_assetGuid = assetGuid;
            m_graphGuid = graphGuid;
            m_nodeId = nodeId;
            m_timestampUtc = DateTime.Now.ToFileTimeUtc();
            m_description = desc;
            m_howToFix = howto;
        }

        public static AssetProcessEvent CreateGraphBeginEvent(string graphGuid) {
            var ev = new AssetProcessEvent();
            ev.Init (EventKind.GraphBegin, string.Empty, graphGuid, string.Empty, string.Empty, string.Empty);
            return ev;
        }

        public static AssetProcessEvent CreateGraphEndEvent(string graphGuid) {
            var ev = new AssetProcessEvent();
            ev.Init (EventKind.GraphEnd, string.Empty, graphGuid, string.Empty, string.Empty, string.Empty);
            return ev;
        }

        public static AssetProcessEvent CreateModifyEvent(string assetGuid, string graphGuid, string nodeId) {
            var ev = new AssetProcessEvent();
            ev.Init (EventKind.Modify, assetGuid, graphGuid, nodeId, string.Empty, string.Empty);
            return ev;
        }

        public static AssetProcessEvent CreateErrorEvent(NodeException e, string graphGuid) {
            var ev = new AssetProcessEvent();
            var assetId = (e.Asset == null) ? null : e.Asset.assetDatabaseId;
            ev.Init (EventKind.Error, assetId, graphGuid, e.NodeId, e.Reason, e.HowToFix);
            return ev;
        }
    }
}

