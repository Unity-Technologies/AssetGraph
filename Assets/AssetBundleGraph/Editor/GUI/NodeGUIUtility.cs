using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;


namespace AssetBundleGraph {
	public class NodeGUIUtility {

		public static Action<OnNodeEvent> FireNodeEvent {
			get {
				return NodeSingleton.s.emitAction;
			}
			set {
				NodeSingleton.s.emitAction = value;
			}
		}

		public static Texture2D inputPointTex {
			get {
				if(NodeSingleton.s.inputPointTex == null) {
					NodeSingleton.s.inputPointTex = AssetBundleGraphEditorWindow.LoadTextureFromFile(AssetBundleGraphGUISettings.RESOURCE_INPUT_BG);
				}
				return NodeSingleton.s.inputPointTex;
			}
		}

		public static Texture2D outputPointTex {
			get {
				if(NodeSingleton.s.outputPointTex == null) {
					NodeSingleton.s.outputPointTex = AssetBundleGraphEditorWindow.LoadTextureFromFile(AssetBundleGraphGUISettings.RESOURCE_OUTPUT_BG);
				}
				return NodeSingleton.s.outputPointTex;
			}
		}

		public static Texture2D enablePointMarkTex {
			get {
				if(NodeSingleton.s.enablePointMarkTex == null) {
					NodeSingleton.s.enablePointMarkTex = AssetBundleGraphEditorWindow.LoadTextureFromFile(AssetBundleGraphGUISettings.RESOURCE_CONNECTIONPOINT_ENABLE);
				}
				return NodeSingleton.s.enablePointMarkTex;
			}
		}

		public static Texture2D inputPointMarkTex {
			get {
				if(NodeSingleton.s.inputPointMarkTex == null) {
					NodeSingleton.s.inputPointMarkTex = AssetBundleGraphEditorWindow.LoadTextureFromFile(AssetBundleGraphGUISettings.RESOURCE_CONNECTIONPOINT_INPUT);
				}
				return NodeSingleton.s.inputPointMarkTex;
			}
		}

		public static Texture2D outputPointMarkTex {
			get {
				if(NodeSingleton.s.outputPointMarkTex == null) {
					NodeSingleton.s.outputPointMarkTex = AssetBundleGraphEditorWindow.LoadTextureFromFile(AssetBundleGraphGUISettings.RESOURCE_CONNECTIONPOINT_OUTPUT);
				}
				return NodeSingleton.s.outputPointMarkTex;
			}
		}

		public static Texture2D outputPointMarkConnectedTex {
			get {
				if(NodeSingleton.s.outputPointMarkConnectedTex == null) {
					NodeSingleton.s.outputPointMarkConnectedTex = AssetBundleGraphEditorWindow.LoadTextureFromFile(AssetBundleGraphGUISettings.RESOURCE_CONNECTIONPOINT_OUTPUT_CONNECTED);
				}
				return NodeSingleton.s.outputPointMarkConnectedTex;
			}
		}

		public static Texture2D[] platformButtonTextures {
			get {
				if(NodeSingleton.s.platformButtonTextures == null) {
					NodeSingleton.s.SetupPlatformIcons();
				}
				return NodeSingleton.s.platformButtonTextures;
			}
		}

		public static string[] platformStrings {
			get {
				if(NodeSingleton.s.platformStrings == null) {
					NodeSingleton.s.SetupPlatformStrings();
				}
				return NodeSingleton.s.platformStrings;
			}
		}

		public static List<string> allNodeNames {
			get {
				return NodeSingleton.s.allNodeNames;
			}
			set {
				NodeSingleton.s.allNodeNames = value;
			}
		}

		private class NodeSingleton {
			public Action<OnNodeEvent> emitAction;

			public Texture2D inputPointTex;
			public Texture2D outputPointTex;

			public Texture2D enablePointMarkTex;

			public Texture2D inputPointMarkTex;
			public Texture2D outputPointMarkTex;
			public Texture2D outputPointMarkConnectedTex;
			public Texture2D[] platformButtonTextures;
			public string[] platformStrings;

			public List<string> allNodeNames;

			private static NodeSingleton s_singleton;

			public static NodeSingleton s {
				get {
					if( s_singleton == null ) {
						s_singleton = new NodeSingleton();
					}

					return s_singleton;
				}
			}

			public void SetupPlatformIcons () {
				var assetBundleGraphPlatformSettings = AssetBundleGraphPlatformSettings.platforms;

				var platformTexList = new List<Texture2D>();

				platformTexList.Add(GetPlatformIcon("BuildSettings.Web"));//dummy.

				if (assetBundleGraphPlatformSettings.Contains("Web")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.Web"));
				}
				if (assetBundleGraphPlatformSettings.Contains("Standalone")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.Standalone"));
				}
				if (assetBundleGraphPlatformSettings.Contains("iPhone") || assetBundleGraphPlatformSettings.Contains("iOS")) {// iPhone or iOS converted to iOS.
					platformTexList.Add(GetPlatformIcon("BuildSettings.iPhone"));
				}
				if (assetBundleGraphPlatformSettings.Contains("Android")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.Android"));
				}
				if (assetBundleGraphPlatformSettings.Contains("BlackBerry")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.BlackBerry"));
				}
				if (assetBundleGraphPlatformSettings.Contains("Tizen")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.Tizen"));
				}
				if (assetBundleGraphPlatformSettings.Contains("XBox360")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.XBox360"));
				}
				if (assetBundleGraphPlatformSettings.Contains("XboxOne")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.XboxOne"));
				}
				if (assetBundleGraphPlatformSettings.Contains("PS3")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.PS3"));
				}
				if (assetBundleGraphPlatformSettings.Contains("PSP2")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.PSP2"));
				}
				if (assetBundleGraphPlatformSettings.Contains("PS4")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.PS4"));
				}
				if (assetBundleGraphPlatformSettings.Contains("StandaloneGLESEmu")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.StandaloneGLESEmu"));
				}
				if (assetBundleGraphPlatformSettings.Contains("Metro")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.Metro"));
				}
				if (assetBundleGraphPlatformSettings.Contains("WP8")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.WP8"));
				}
				if (assetBundleGraphPlatformSettings.Contains("WebGL")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.WebGL"));
				}
				if (assetBundleGraphPlatformSettings.Contains("SamsungTV")) {
					platformTexList.Add(GetPlatformIcon("BuildSettings.SamsungTV"));
				}

				platformButtonTextures = platformTexList.ToArray();
			}


			public void SetupPlatformStrings () {
				var assetBundleGraphPlatformSettings = AssetBundleGraphPlatformSettings.platforms;

				var platformStringList = new List<string>();

				platformStringList.Add("Default");

				if (assetBundleGraphPlatformSettings.Contains("Web")) {
					platformStringList.Add("Web");
				}
				if (assetBundleGraphPlatformSettings.Contains("Standalone")) {
					platformStringList.Add("Standalone");
				}
				if (assetBundleGraphPlatformSettings.Contains("iPhone") || assetBundleGraphPlatformSettings.Contains("iOS")) {// iPhone or iOS converted to iOS.
					platformStringList.Add("iOS");
				}
				if (assetBundleGraphPlatformSettings.Contains("Android")) {
					platformStringList.Add("Android");
				}
				if (assetBundleGraphPlatformSettings.Contains("BlackBerry")) {
					platformStringList.Add("BlackBerry");
				}
				if (assetBundleGraphPlatformSettings.Contains("Tizen")) {
					platformStringList.Add("Tizen");
				}
				if (assetBundleGraphPlatformSettings.Contains("XBox360")) {
					platformStringList.Add("XBox360");
				}
				if (assetBundleGraphPlatformSettings.Contains("XboxOne")) {
					platformStringList.Add("XboxOne");
				}
				if (assetBundleGraphPlatformSettings.Contains("PS3")) {
					platformStringList.Add("PS3");
				}
				if (assetBundleGraphPlatformSettings.Contains("PSP2")) {
					platformStringList.Add("PSP2");
				}
				if (assetBundleGraphPlatformSettings.Contains("PS4")) {
					platformStringList.Add("PS4");
				}
				if (assetBundleGraphPlatformSettings.Contains("StandaloneGLESEmu")) {
					platformStringList.Add("StandaloneGLESEmu");
				}
				if (assetBundleGraphPlatformSettings.Contains("Metro")) {
					platformStringList.Add("Metro");
				}
				if (assetBundleGraphPlatformSettings.Contains("WP8")) {
					platformStringList.Add("WP8");
				}
				if (assetBundleGraphPlatformSettings.Contains("WebGL")) {
					platformStringList.Add("WebGL");
				}
				if (assetBundleGraphPlatformSettings.Contains("SamsungTV")) {
					platformStringList.Add("SamsungTV");
				}

				platformStrings = platformStringList.ToArray();
			}

			private Texture2D GetPlatformIcon(string locTitle) {
				return EditorGUIUtility.IconContent(locTitle + ".Small").image as Texture2D;
			}
		}
	}
}
