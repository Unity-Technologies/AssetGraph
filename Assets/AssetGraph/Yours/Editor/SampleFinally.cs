using UnityEngine;
using System.Collections.Generic;

public class SampleFinally : AssetGraph.FinallyBase {
	public override void Run (Dictionary<string, Dictionary<string, List<string>>> throughputs, bool isBuild) {
		Debug.Log("flnally. isBuild:" + isBuild);
		// foreach (var nodeName in throughputs.Keys) {
		// 	Debug.Log("nodeName:" + nodeName);
		// 	foreach (var groupKey in throughputs[nodeName].Keys) {
		// 		Debug.Log("	groupKey:" + groupKey);
		// 		foreach (var result in throughputs[nodeName][groupKey]) {
		// 			Debug.Log("		result:" + result);
		// 		}
		// 	}
		// }
	}
}