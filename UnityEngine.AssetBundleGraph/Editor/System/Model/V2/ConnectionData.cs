using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography;

using UnityEngine.AssetBundles.GraphTool;
using V1=AssetBundleGraph;

namespace UnityEngine.AssetBundles.GraphTool.DataModel.Version2 {

	public enum NodeOutputSemantics : uint {
		None 						= 0,
		Any  						= 0xFFFFFFFF,
		Assets						= 1,
		AssetBundleConfigurations 	= 2,
		AssetBundles 				= 4
	}

	/*
	 * connection data saved in/to Json
	 */ 
	[Serializable]
	public class ConnectionData {

		[SerializeField] private string m_id;
		[SerializeField] private string m_fromNodeId;
		[SerializeField] private string m_fromNodeConnectionPointId;
		[SerializeField] private string m_toNodeId;
		[SerializeField] private string m_toNodeConnectionPoiontId;
		[SerializeField] private string m_label;

		private ConnectionData() {
			m_id = Guid.NewGuid().ToString();
		}

		public ConnectionData(string label, ConnectionPointData output, ConnectionPointData input) {
			m_id = Guid.NewGuid().ToString();
			m_label = label;
			m_fromNodeId = output.NodeId;
			m_fromNodeConnectionPointId = output.Id;
			m_toNodeId = input.NodeId;
			m_toNodeConnectionPoiontId = input.Id;
		}

		public ConnectionData(V1.ConnectionData v1) {
			m_id = v1.Id;
			m_fromNodeId = v1.FromNodeId;
			m_fromNodeConnectionPointId = v1.FromNodeConnectionPointId;
			m_toNodeId = v1.ToNodeId;
			m_toNodeConnectionPoiontId = v1.ToNodeConnectionPointId;
			m_label = v1.Label;
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

			set {
				m_label = value;
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

		public ConnectionData Duplicate(bool keepGuid = false) {
			ConnectionData newData = new ConnectionData();
			if(keepGuid) {
				newData.m_id = m_id;
			}
			newData.m_label = m_label;
			newData.m_fromNodeId = m_fromNodeId;
			newData.m_fromNodeConnectionPointId = m_fromNodeConnectionPointId;
			newData.m_toNodeId = m_toNodeId;
			newData.m_toNodeConnectionPoiontId = m_toNodeConnectionPoiontId;

			return newData;
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

		public static bool CanConnect (NodeData from, NodeData to) {

			var inType  = (uint)to.Operation.Object.NodeInputType;
			var outType = (uint)from.Operation.Object.NodeOutputType;

			return (inType & outType) > 0;
		}
	}
}
