using UnityEngine;

using System;
using System.Collections.Generic;

public class SampleFilter_2 : AssetGraph.FilterBase {

	/**
		Inに対して複数のOutを出したいな！
	*/
	public override void In (List<string> source, Action<string, List<string>> Out) {
		var the2ndList = new List<string>();
		Debug.LogError("SampleFilter_2 複数のOutを出したい");
		
		foreach (var src in source) {
			if (src.StartsWith("SampleFilter_2nd")) {
				the2ndList.Add(src);
			}
		}

		Out("SampleFilter_1_LabelOf1st", source);
		Out("SampleFilter_1_LabelOf2nd", the2ndList);
	}
}