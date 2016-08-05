//using UnityEngine;
//using UnityEditor;
//
//using System;
//using System.IO;
//using System.Collections.Generic;
//
///**
//Example code for asset bundle build postprocess.
//Finally is always called when build or dry-run is performed. 
//You can uncomment this code to see how it works.
//*/
//public class SampleFinally : AssetBundleGraph.FinallyBase {
//	/* 
//	 * Run() is called when build or dry-run is performed. 
//	 * 
//	 * @param [in] throughputs	Dictionary of group of files targeted to build
//	 * @param [in] isBuild		True if this is actual build. 
//	 */
//	public override void Run (Dictionary<string, Dictionary<string, List<string>>> throughputs, bool isBuild) {
//		Debug.Log("SampleFinally PostProcess called. isBuild:" + isBuild);
//
//		if (!isBuild) return;
//
//		foreach (var nodeName in throughputs.Keys) {
//			Debug.Log("nodeName:" + nodeName);
//
//			foreach (var groupKey in throughputs[nodeName].Keys) {
//				Debug.Log("	groupKey:" + groupKey);
//
//				foreach (var result in throughputs[nodeName][groupKey]) {
//					Debug.Log("		result:" + result);
//				}
//			}
//		}
//	}
//}
