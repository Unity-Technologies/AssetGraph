using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AssetBundleGraph {
	public static class CustomScriptUtility {
		public static string DecodeString(string data) {
			if(data.StartsWith(AssetBundleGraphSettings.BASE64_IDENTIFIER)) {
				var bytes = Convert.FromBase64String(data.Substring(AssetBundleGraphSettings.BASE64_IDENTIFIER.Length));
				data = System.Text.Encoding.UTF8.GetString(bytes);
			}
			return data;
		}
		public static string EncodeString(string data) {
			return AssetBundleGraphSettings.BASE64_IDENTIFIER + 
				Convert.ToBase64String( System.Text.Encoding.UTF8.GetBytes(data));
		}
	}
}
