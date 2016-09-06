using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography;

/**
	static executor for AssetBundleGraph's data.
*/
namespace AssetBundleGraph {
	public partial class ConnectionData {

		/*
		 * Checks deserialized ConnectionData, and make some changes if necessary
		 * return false if any changes are perfomed.
		 */
		public bool Validate () {

			return true;

//			var changed = false;
//
//			/*
//				delete undetectable connection.
//					erase no start node connection.
//					erase no end node connection.
//					erase connection which label does exists in the start node.
//			*/
//
//			var connectionJson = c as Dictionary<string, object>;
//
//			var connectionLabel = connectionJson[AssetBundleGraphSettings.CONNECTION_LABEL] as string;
//			var fromNodeId 		= connectionJson[AssetBundleGraphSettings.CONNECTION_FROMNODE] as string;
//			var fromNodePointId = connectionJson[AssetBundleGraphSettings.CONNECTION_FROMNODE_CONPOINT_ID] as string;
//			var toNodeId 		= connectionJson[AssetBundleGraphSettings.CONNECTION_TONODE] as string;
//			//				var toNodePointId 	= connectionJson[AssetBundleGraphSettings.CONNECTION_TONODE_CONPOINT_ID] as string;
//
//			// detect start node.
//			var fromNodeCandidates = sanitizedAllNodesJson.Where(
//				node => {
//					var nodeId = node[AssetBundleGraphSettings.NODE_ID] as string;
//					return nodeId == fromNodeId;
//				}
//			).ToList();
//			if (!fromNodeCandidates.Any()) {
//				changed = true;
//				continue;
//			}
//
//
//			// start node should contain specific connection point.
//			var candidateNode = fromNodeCandidates[0];
//			var candidateOutputPointIdsSources = candidateNode[AssetBundleGraphSettings.NODE_OUTPUTPOINT_IDS] as List<object>;
//			var candidateOutputPointIds = new List<string>();
//			foreach (var candidateOutputPointIdsSource in candidateOutputPointIdsSources) {
//				candidateOutputPointIds.Add(candidateOutputPointIdsSource as string);
//			}
//			if (!candidateOutputPointIdsSources.Contains(fromNodePointId)) {
//				changed = true;
//				continue;
//			}
//
//			// detect end node.
//			var toNodeCandidates = sanitizedAllNodesJson.Where(
//				node => {
//					var nodeId = node[AssetBundleGraphSettings.NODE_ID] as string;
//					return nodeId == toNodeId;
//				}
//			).ToList();
//			if (!toNodeCandidates.Any()) {
//				changed = true;
//				continue;
//			}
//
//			// this connection has start node & end node.
//			// detect connectionLabel.
//			var fromNode = fromNodeCandidates[0];
//			var connectionLabelsSource = fromNode[AssetBundleGraphSettings.NODE_OUTPUTPOINT_LABELS] as List<object>;
//			var connectionLabels = new List<string>();
//			foreach (var connectionLabelSource in connectionLabelsSource) {
//				connectionLabels.Add(connectionLabelSource as string);
//			}
//
//			if (!connectionLabels.Contains(connectionLabel)) {
//				changed = true;
//				continue;
//			}
//
//			sanitizedAllConnectionsJson.Add(connectionJson);
		}
	}
}
