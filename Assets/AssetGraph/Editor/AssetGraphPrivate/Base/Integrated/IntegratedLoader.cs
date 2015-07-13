using UnityEngine;

using System;
using System.Collections.Generic;

namespace AssetGraph {
	public class IntegratedLoader : INodeBase {
		public void Setup (string id, string labelToNext, List<AssetData> inputSource, Action<string, string, List<AssetData>> Output) {
			var outputSource = new List<AssetData>();
			
			Debug.LogError("setup loaderの内容がまだダミー");
			Output(id, AssetGraphSettings.DEFAULT_INPUTPOINT_LABEL, outputSource);
		}
		
		public void Run (string id, string labelToNext, List<AssetData> inputSource, Action<string, string, List<AssetData>> Output) {
			var outputSource = new List<AssetData>();
			
			Debug.LogError("run loaderの内容がまだダミー");
			Output(id, AssetGraphSettings.DEFAULT_INPUTPOINT_LABEL, outputSource);
		}
	}
}