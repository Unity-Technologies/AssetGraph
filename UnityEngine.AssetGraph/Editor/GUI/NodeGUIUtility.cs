using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {
	public class NodeGUIUtility {

		public static Texture2D LoadTextureFromFile(string path) {
			Texture2D texture = new Texture2D(1, 1);
			texture.LoadImage(File.ReadAllBytes(path));
			return texture;
		}


		public struct PlatformButton {
			public readonly GUIContent ui;
			public readonly BuildTargetGroup targetGroup;

			public PlatformButton(GUIContent ui, BuildTargetGroup g) {
				this.ui = ui;
				this.targetGroup = g;
			}
		}

		public static Action<NodeEvent> NodeEventHandler {
			get {
				return NodeSingleton.s.emitAction;
			}
			set {
				NodeSingleton.s.emitAction = value;
			}
		}

		public static GUISkin nodeSkin {
			get {
				if(NodeSingleton.s.nodeSkin == null) {
                    NodeSingleton.s.nodeSkin = AssetDatabase.LoadAssetAtPath<GUISkin>(Model.Settings.GUI.Skin);
				}
				return NodeSingleton.s.nodeSkin;
			}
		}

		public static Texture2D inputPointBG {
			get {
				if(NodeSingleton.s.inputPointBG == null) {
                    NodeSingleton.s.inputPointBG = LoadTextureFromFile(Model.Settings.GUI.InputBG);
				}
				return NodeSingleton.s.inputPointBG;
			}
		}

		public static Texture2D outputPointBG {
			get {
				if(NodeSingleton.s.outputPointBG == null) {
                    NodeSingleton.s.outputPointBG = LoadTextureFromFile(Model.Settings.GUI.OutputBG);
				}
				return NodeSingleton.s.outputPointBG;
			}
		}

		public static Texture2D pointMark {
			get {
				if(NodeSingleton.s.pointMark == null) {
                    NodeSingleton.s.pointMark = LoadTextureFromFile(Model.Settings.GUI.ConnectionPoint);
				}
				return NodeSingleton.s.pointMark;
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

		public static PlatformButton GetPlatformButtonFor(BuildTargetGroup g) {
			foreach(var button in platformButtons) {
				if(button.targetGroup == g) {
					return button;
				}
			}

			throw new AssetBundleGraphException("Fatal: unknown target group requsted(can't happen)" + g);
		}

		public static List<string> allNodeNames {
			get {
				return NodeSingleton.s.allNodeNames;
			}
			set {
				NodeSingleton.s.allNodeNames = value;
			}
		}

		public static List<BuildTarget> SupportedBuildTargets {
			get {
				if(NodeSingleton.s.supportedBuildTargets == null) {
					NodeSingleton.s.SetupSupportedBuildTargets();
				}
				return NodeSingleton.s.supportedBuildTargets;
			}
		}
		public static string[] supportedBuildTargetNames {
			get {
				if(NodeSingleton.s.supportedBuildTargetNames == null) {
					NodeSingleton.s.SetupSupportedBuildTargets();
				}
				return NodeSingleton.s.supportedBuildTargetNames;
			}
		}


		public static List<BuildTargetGroup> SupportedBuildTargetGroups {
			get {
				if(NodeSingleton.s.supportedBuildTargetGroups == null) {
					NodeSingleton.s.SetupSupportedBuildTargets();
				}
				return NodeSingleton.s.supportedBuildTargetGroups;
			}
		}


		private class NodeSingleton {
			public Action<NodeEvent> emitAction;

			public Texture2D inputPointBG;
			public Texture2D outputPointBG;
			public GUISkin nodeSkin;

			public Texture2D pointMark;
			public PlatformButton[] platformButtons;

			public List<BuildTarget> supportedBuildTargets;
			public string[] supportedBuildTargetNames;
			public List<BuildTargetGroup> supportedBuildTargetGroups;

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
					{BuildTargetGroup.Android, 		"BuildSettings.Android.Small"},
					{BuildTargetGroup.iOS, 			"BuildSettings.iPhone.Small"},
					{BuildTargetGroup.PS4, 			"BuildSettings.PS4.Small"},
					{BuildTargetGroup.PSM, 			"BuildSettings.PSM.Small"},
					{BuildTargetGroup.PSP2, 		"BuildSettings.PSP2.Small"},
					{BuildTargetGroup.SamsungTV, 	"BuildSettings.Android.Small"},
					{BuildTargetGroup.Standalone, 	"BuildSettings.Standalone.Small"},
					{BuildTargetGroup.Tizen, 		"BuildSettings.Tizen.Small"},
					{BuildTargetGroup.tvOS, 		"BuildSettings.tvOS.Small"},
					{BuildTargetGroup.Unknown, 		"BuildSettings.Standalone.Small"},
					{BuildTargetGroup.WebGL, 		"BuildSettings.WebGL.Small"},
					{BuildTargetGroup.WiiU, 		"BuildSettings.WiiU.Small"},
					{BuildTargetGroup.WSA, 			"BuildSettings.WP8.Small"},
					{BuildTargetGroup.XboxOne, 		"BuildSettings.XboxOne.Small"}
					#if !UNITY_5_5_OR_NEWER
					,
					{BuildTargetGroup.XBOX360, 		"BuildSettings.Xbox360.Small"},
					{BuildTargetGroup.Nintendo3DS, 	"BuildSettings.N3DS.Small"},
					{BuildTargetGroup.PS3,			"BuildSettings.PS3.Small"}
					#endif
					#if UNITY_5_5_OR_NEWER
					,
					{BuildTargetGroup.N3DS, 		"BuildSettings.N3DS.Small"}
					#endif
					#if UNITY_5_6 || UNITY_5_6_OR_NEWER
					,
					{BuildTargetGroup.Facebook, 	"BuildSettings.Facebook.Small"},
					{BuildTargetGroup.Switch, 		"BuildSettings.Switch.Small"}
					#endif
				};

				buttons.Add(new PlatformButton(new GUIContent("Default", "Default settings"), BuildTargetGroup.Unknown));

				foreach(var g in supportedBuildTargetGroups) {
					buttons.Add(new PlatformButton(new GUIContent(GetPlatformIcon(icons[g]), BuildTargetUtility.GroupToHumaneString(g)), g));
				}

				this.platformButtons = buttons.ToArray();
			}

			public void SetupSupportedBuildTargets() {
				
				if( supportedBuildTargets == null ) {
					supportedBuildTargets = new List<BuildTarget>();
					supportedBuildTargetGroups = new List<BuildTargetGroup>();

					try {
						foreach(BuildTarget target in Enum.GetValues(typeof(BuildTarget))) {
							if(BuildTargetUtility.IsBuildTargetSupported(target)) {
								if(!supportedBuildTargets.Contains(target)) {
									supportedBuildTargets.Add(target);
								}
								BuildTargetGroup g = BuildTargetUtility.TargetToGroup(target);
								if(g == BuildTargetGroup.Unknown) {
									// skip unknown platform
									continue;
								}
								if(!supportedBuildTargetGroups.Contains(g)) {
									supportedBuildTargetGroups.Add(g);
								}
							}
						}

						supportedBuildTargetNames = new string[supportedBuildTargets.Count];
						for(int i =0; i < supportedBuildTargets.Count; ++i) {
							supportedBuildTargetNames[i] = BuildTargetUtility.TargetToHumaneString(supportedBuildTargets[i]);
						}

					} catch(Exception e) {
						LogUtility.Logger.LogError(LogUtility.kTag, e);
					}
				}
			}

			private Texture2D GetPlatformIcon(string name) {
				return EditorGUIUtility.IconContent(name).image as Texture2D;
			}
		}
	}
}
