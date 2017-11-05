using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph {
    [System.Serializable]
    public class AssetProcessEvent : ScriptableObject {

        public enum EventKind
        {
            GraphBegin,
            GraphEnd,
            Process,
            Error
        }

        [SerializeField] private EventKind m_kind;
        [SerializeField] private string m_nodeType;
        [SerializeField] private string m_eventName;
        [SerializeField] private string m_whatIsThisDesc;
        [SerializeField] private string m_howToFixErrorDesc;

        public void Init(EventKind k, string nodeType, string eventName, string whatIsIt, string howToFix) {
            m_kind = k;
            m_nodeType = nodeType;
            m_eventName = eventName;
            m_whatIsThisDesc = whatIsIt;
            m_howToFixErrorDesc = howToFix;
        }

        public EventKind Kind {get { return m_kind; }}
        public string NodeType {get { return m_nodeType; }}
        public string EventName {get { return m_eventName; }}
        public string WhatIsIt {get { return m_whatIsThisDesc; }}
        public string HowToFix {get { return m_howToFixErrorDesc; }}
    }
}
