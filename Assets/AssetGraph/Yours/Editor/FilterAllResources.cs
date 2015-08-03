using UnityEngine;

using System;
using System.Collections.Generic;

public class FilterAllResources : AssetGraph.FilterBase {
	public override void In (List<string> source, Action<string, List<string>> Out)	{
		// source:/Users/runnershigh/Desktop/AssetGraph/TestResourcesFor_0_14_RunStackedGraph_Sample/bgms/crank_ringtone.mp3
		// source:/Users/runnershigh/Desktop/AssetGraph/TestResourcesFor_0_14_RunStackedGraph_Sample/images/0/dummy.png
		// source:/Users/runnershigh/Desktop/AssetGraph/TestResourcesFor_0_14_RunStackedGraph_Sample/images/1/dummy.png
		// source:/Users/runnershigh/Desktop/AssetGraph/TestResourcesFor_0_14_RunStackedGraph_Sample/models/0/sample.fbx
		// source:/Users/runnershigh/Desktop/AssetGraph/TestResourcesFor_0_14_RunStackedGraph_Sample/models/1/sample.fbx
		// source:/Users/runnershigh/Desktop/AssetGraph/TestResourcesFor_0_14_RunStackedGraph_Sample/ses/crank_ringtone.mp3
		
		var images = new List<string>();
		var models = new List<string>();
		var bgms = new List<string>();
		var ses = new List<string>();

		foreach (var path in source) {
			if (path.Contains("/images/")) images.Add(path);
			if (path.Contains("/models/")) models.Add(path);
			if (path.Contains("/bgms/")) bgms.Add(path);
			if (path.Contains("/ses/")) ses.Add(path);
		}

		Out("images", images);
		Out("models", models);
		Out("bgms", bgms);
		Out("ses", ses);
	}
}