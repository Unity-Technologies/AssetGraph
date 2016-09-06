using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;


namespace AssetBundleGraph {
	public class NodeGUIUtility {

		public struct PlatformButton {
			public readonly GUIContent ui;
			public readonly BuildTargetGroup targetGroup;

			public PlatformButton(GUIContent ui, BuildTargetGroup g) {
				this.ui = ui;
				this.targetGroup = g;
			}
		}

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

		public static PlatformButton[] platformButtons {
			get {
				if(NodeSingleton.s.platformButtons == null) {
					NodeSingleton.s.SetupPlatformButtons();
				}
				return NodeSingleton.s.platformButtons;
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

		public static List<BuildTargetGroup> SupportedBuildTargets {
			get {
				if(NodeSingleton.s.supportedBuildTargets == null) {
					NodeSingleton.s.SetupSupportedBuildTargets();
				}
				return NodeSingleton.s.supportedBuildTargets;
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
			public PlatformButton[] platformButtons;
//			public string[] platformStrings;

			public List<BuildTargetGroup> supportedBuildTargets;

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

			public void SetupPlatformButtons () {
				SetupSupportedBuildTargets();
				var buttons = new List<PlatformButton>();

				Dictionary<BuildTargetGroup, string> icons = new Dictionary<BuildTargetGroup, string> {
					{BuildTargetGroup.Android, 		"BuildSettings.Android"},
					{BuildTargetGroup.iOS, 			"BuildSettings.iPhone"},
					{BuildTargetGroup.Nintendo3DS, 	"BuildSettings.Nintendo3DS"},
					{BuildTargetGroup.PS3,			"BuildSettings.PS3"},
					{BuildTargetGroup.PS4, 			"BuildSettings.PS4"},
					{BuildTargetGroup.PSM, 			"BuildSettings.PSM"},
					{BuildTargetGroup.PSP2, 		"BuildSettings.PSP2"},
					{BuildTargetGroup.SamsungTV, 	"BuildSettings.Android"},
					{BuildTargetGroup.Standalone, 	"BuildSettings.StandaloneGLESEmu"},
					{BuildTargetGroup.Tizen, 		"BuildSettings.Tizen"},
					{BuildTargetGroup.tvOS, 		"BuildSettings.tvOS"},
					{BuildTargetGroup.Unknown, 		"BuildSettings.StandaloneGLESEmu"},
					{BuildTargetGroup.WebGL, 		"BuildSettings.WebGL"},
					{BuildTargetGroup.WiiU, 		"BuildSettings.WiiU"},
					{BuildTargetGroup.WSA, 			"BuildSettings.WP8"},
					{BuildTargetGroup.XBOX360, 		"BuildSettings.Xbox360"},
					{BuildTargetGroup.XboxOne, 		"BuildSettings.XboxOne"}
				};

				buttons.Add(new PlatformButton(new GUIContent("Default", "Default settings"), BuildTargetGroup.Unknown));

				foreach(var g in supportedBuildTargets) {
					buttons.Add(new PlatformButton(new GUIContent(GetPlatformIcon(icons[g]), AssetBundleGraphPlatformSettings.BuildTargetGroupToHumaneString(g)), g));
				}

				this.platformButtons = buttons.ToArray();
			}

			public void SetupSupportedBuildTargets() {
				if( supportedBuildTargets == null ) {
					supportedBuildTargets = new List<BuildTargetGroup>();

					foreach(BuildTarget target in Enum.GetValues(typeof(BuildTarget))) {
						var objType = typeof(BuildPipeline);
						var method =  objType.GetMethod("IsBuildTargetSupported");
						
						var retval = method.Invoke(null, new object[]{System.Enum.ToObject(typeof(BuildTarget), target)});

						bool supported = Convert.ToBoolean(retval);
						if(supported) {
							BuildTargetGroup g = AssetBundleGraphPlatformSettings.BuildTargetToBuildTargetGroup(target);
							if(!supportedBuildTargets.Contains(g)) {
								supportedBuildTargets.Add(g);
							}
						}
					}
				}
			}

			private Texture2D GetPlatformIcon(string locTitle) {
				return EditorGUIUtility.IconContent(locTitle + ".Small").image as Texture2D;
			}
		}
	}
}
