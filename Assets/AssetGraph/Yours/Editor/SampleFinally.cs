using UnityEngine;
using System.Collections.Generic;

public class SampleFinally : AssetGraph.FinallyBase {
	public override void Run (Dictionary<string, Dictionary<string, List<string>>> throughputs, bool isBuild) {
		Debug.LogError("finished. isBuild:" + isBuild);
		foreach (var nodeName in throughputs.Keys) {
			Debug.LogError("nodeName:" + nodeName);
			foreach (var groupKey in throughputs[nodeName].Keys) {
				Debug.LogError("	groupKey:" + groupKey);
				foreach (var result in throughputs[nodeName][groupKey]) {
					Debug.LogError("		result:" + result);
				}
			}
		}
	}
}