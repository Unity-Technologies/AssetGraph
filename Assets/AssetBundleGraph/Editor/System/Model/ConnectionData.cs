using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace AssetBundleGraph {

	/*
	 * connection data saved in/to Json
	 */ 
	public partial class ConnectionData {

		// connection data
		private const string CONNECTION_LABEL = "label";
		private const string CONNECTION_ID = "id";
		private const string CONNECTION_FROMNODE = "fromNode";
		private const string CONNECTION_FROMNODE_CONPOINT_ID = "fromNodeConPointId";
		private const string CONNECTION_TONODE = "toNode";
		private const string CONNECTION_TONODE_CONPOINT_ID = "toNodeConPointId";

		private Dictionary<string, object> m_jsonData;

		private string m_id;
		private string m_fromNodeId;
		private string m_fromNodeConnectionPointId;
		private string m_toNodeId;
		private string m_toNodeConnectionPoiontId;

		private string m_label;

		public ConnectionData(Dictionary<string, object> jsonData) {
			m_jsonData = jsonData;

			m_id = m_jsonData[CONNECTION_ID] as string;
			m_label = m_jsonData[CONNECTION_LABEL] as string;
			m_fromNodeId = m_jsonData[CONNECTION_FROMNODE] as string;
			m_fromNodeConnectionPointId = m_jsonData[CONNECTION_FROMNODE_CONPOINT_ID] as string;
			m_toNodeId = m_jsonData[CONNECTION_TONODE] as string;
			m_toNodeConnectionPoiontId = m_jsonData[CONNECTION_TONODE_CONPOINT_ID] as string;
		}

		public ConnectionData(ConnectionGUI c) {
			m_jsonData = null;

			m_id = c.Id;
			m_label = c.Label;
			m_fromNodeId = c.OutputNodeId;
			m_fromNodeConnectionPointId = c.OutputPoint.Id;
			m_toNodeId = c.InputNodeId;
			m_toNodeConnectionPoiontId = c.InputPoint.Id;
		}

		public string Id {
			get {
				return m_id;
			}
		}

		public string Label {
			get {
				return m_label;
			}
		}

		public string FromNodeId {
			get {
				return m_fromNodeId;
			}
		}

		public string FromNodeConnectionPointId {
			get {
				return m_fromNodeConnectionPointId;
			}
		}

		public string ToNodeId {
			get {
				return m_toNodeId;
			}
		}

		public string ToNodeConnectionPointId {
			get {
				return m_toNodeConnectionPoiontId;
			}
		}

		public Dictionary<string, object> ToJsonDictionary() {
			Dictionary<string, object> json = new Dictionary<string, object>();

			json[CONNECTION_ID] = m_id;
			json[CONNECTION_LABEL] = m_label;
			json[CONNECTION_FROMNODE] = m_fromNodeId;
			json[CONNECTION_FROMNODE_CONPOINT_ID] = m_fromNodeConnectionPointId;
			json[CONNECTION_TONODE] = m_toNodeId;
			json[CONNECTION_TONODE_CONPOINT_ID] = m_toNodeConnectionPoiontId;

			return json;
		}

		public static void ValidateConnection (NodeData from, NodeData to) {
			if(!CanConnect(from, to)) {
				throw new AssetBundleGraphException(to.Kind + " does not accept connection from " + from.Kind);
			}
		}

		public static bool CanConnect (NodeData from, NodeData to) {
			switch (from.Kind) {
			case NodeKind.BUNDLEBUILDER_GUI: 
				{
					switch (from.Kind) {
					case NodeKind.BUNDLIZER_GUI:
						return true;
					}
					return false;
				}
			}

			switch (from.Kind) {
			case NodeKind.BUNDLIZER_GUI: 
				{
					switch (to.Kind) {
					case NodeKind.BUNDLEBUILDER_GUI: 
						return true;
					}
					return false;
				}
			case NodeKind.BUNDLEBUILDER_GUI: 
				{
					switch (to.Kind) {
					case NodeKind.FILTER_SCRIPT:
					case NodeKind.FILTER_GUI:
					case NodeKind.GROUPING_GUI:
					case NodeKind.EXPORTER_GUI:
						return true;
					}
					return false;
				}
			}
			return true;
		}	
	}
}
