using UnityEngine;
using System.Collections.Generic;

public class SampleFinally : AssetGraph.FinallyBase {
	public override void Run (Dictionary<string, Dictionary<string, List<string>>> throughputs, bool isBuild) {
		Debug.LogError("finished. isBuild:" + isBuild);
	}
}