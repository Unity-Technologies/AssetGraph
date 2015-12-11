using UnityEngine;
using UnityEditor;

using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using System;

using MiniJSONForAssetGraph;

/**
	sample class for finally hookPoint.

	read exported assetBundles & generate assetBundle data json as "EXPORT_PATH/bundleList.json".
*/
public class SampleFinally : AssetGraph.FinallyBase {
	public override void Run (Dictionary<string, Dictionary<string, List<string>>> throughputs, bool isBuild) {
		Debug.Log("flnally. isBuild:" + isBuild);

		foreach (var nodeName in throughputs.Keys) {
			Debug.Log("nodeName:" + nodeName);

			foreach (var groupKey in throughputs[nodeName].Keys) {
				Debug.Log("	groupKey:" + groupKey);
				
				foreach (var result in throughputs[nodeName][groupKey]) {
					Debug.Log("		result:" + result);
				}
			}
		}
	}
}