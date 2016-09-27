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
	[Serializable]
	public class ConnectionData {

		// connection data
		private const string CONNECTION_LABEL = "label";
		private const string CONNECTION_ID = "id";
		private const string CONNECTION_FROMNODE = "fromNode";
		private const string CONNECTION_FROMNODE_CONPOINT_ID = "fromNodeConPointId";
		private const string CONNECTION_TONODE = "toNode";
		private const string CONNECTION_TONODE_CONPOINT_ID = "toNodeConPointId";

		[SerializeField] private string m_id;
		[SerializeField] private string m_fromNodeId;
		[SerializeField] private string m_fromNodeConnectionPointId;
		[SerializeField] private string m_toNodeId;
		[SerializeField] private string m_toNodeConnectionPoiontId;

		private string m_label;

		public ConnectionData(Dictionary<string, object> jsonData) {

			m_id = jsonData[CONNECTION_ID] as string;
			m_label = jsonData[CONNECTION_LABEL] as string;
			m_fromNodeId = jsonData[CONNECTION_FROMNODE] as string;
			m_fromNodeConnectionPointId = jsonData[CONNECTION_FROMNODE_CONPOINT_ID] as string;
			m_toNodeId = jsonData[CONNECTION_TONODE] as string;
			m_toNodeConnectionPoiontId = jsonData[CONNECTION_TONODE_CONPOINT_ID] as string;
		}

		public ConnectionData(ConnectionGUI c) {
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
			case NodeKind.GROUPING_GUI:
				{
					switch (to.Kind) {
					case NodeKind.GROUPING_GUI:
					case NodeKind.PREFABRICATOR_SCRIPT:
					case NodeKind.PREFABRICATOR_GUI:
					case NodeKind.BUNDLIZER_GUI:
						return true;
					}
					return false;
				}

			case NodeKind.FILTER_SCRIPT:
			case NodeKind.PREFABRICATOR_SCRIPT:
			case NodeKind.LOADER_GUI:
			case NodeKind.FILTER_GUI:
			case NodeKind.IMPORTSETTING_GUI:			
			case NodeKind.MODIFIER_GUI:
			case NodeKind.PREFABRICATOR_GUI:
				{
					switch (to.Kind) {
					case NodeKind.BUNDLEBUILDER_GUI:
						return false;
					}
					return true;
				}

			case NodeKind.EXPORTER_GUI:
				{
					// there is no output from exporter
					return false;
				}

			case NodeKind.BUNDLEBUILDER_GUI: 
				{
					switch (to.Kind) {
					case NodeKind.FILTER_SCRIPT:
					case NodeKind.FILTER_GUI:
					case NodeKind.GROUPING_GUI:
					case NodeKind.EXPORTER_GUI:
					case NodeKind.BUNDLIZER_GUI:
						return true;
					}
					return false;
				}
			case NodeKind.BUNDLIZER_GUI: 
				{
					switch (to.Kind) {
					case NodeKind.BUNDLEBUILDER_GUI: 
						return true;
					}
					return false;
				}
			}
			return true;
		}

		/*
		 * Checks deserialized ConnectionData, and make some changes if necessary
		 * return false if any changes are perfomed.
		 */
		public bool Validate (List<NodeData> allNodes, List<ConnectionData> allConnections) {

			var fromNode = allNodes.Find(n => n.Id == this.FromNodeId);
			var toNode   = allNodes.Find(n => n.Id == this.ToNodeId);

			if(fromNode == null) {
				return false;
			}

			if(toNode == null) {
				return false;
			}

			var outputPoint = fromNode.FindOutputPoint(this.FromNodeConnectionPointId);
			var inputPoint  = toNode.FindInputPoint(this.ToNodeConnectionPointId);

			if(null == outputPoint) {
				return false;
			}

			if(null == inputPoint) {
				return false;
			}

			// update connection label if not matching with outputPoint label
			if( outputPoint.Label != m_label ) {
				m_label = outputPoint.Label;
			}

			return true;
		}
	}
}
