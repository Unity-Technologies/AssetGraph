// using UnityEngine;
// using UnityEditor;

// using System;
// using System.IO;
// using System.Collections.Generic;

// /**
// 	sample class for finally hookPoint.

// 	show results of all nodes.
// */
// public class SampleFinally : AssetGraph.FinallyBase {
// 	public override void Run (Dictionary<string, Dictionary<string, List<string>>> throughputs, bool isBuild) {
// 		Debug.Log("flnally. isBuild:" + isBuild);

// 		if (!isBuild) return;
		
// 		foreach (var nodeName in throughputs.Keys) {
// 			Debug.Log("nodeName:" + nodeName);

// 			foreach (var groupKey in throughputs[nodeName].Keys) {
// 				Debug.Log("	groupKey:" + groupKey);

// 				foreach (var result in throughputs[nodeName][groupKey]) {
// 					Debug.Log("		result:" + result);
// 				}
// 			}
// 		}
// 	}
// }