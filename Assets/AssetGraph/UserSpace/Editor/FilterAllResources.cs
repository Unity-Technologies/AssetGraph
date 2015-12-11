using UnityEngine;

using System;
using System.Collections.Generic;

/**
	if you want to filtering Assets manually,
	Drag & Drop this C# script to AssetGraph.
*/
public class FilterAllResources : AssetGraph.FilterBase {
	public override void In (List<string> source, Action<string, List<string>> Out)	{		
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