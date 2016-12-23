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

		public static Action<NodeEvent> NodeEventHandler {
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
					NodeSingleton.s.inputPointTex = AssetBundleGraphEditorWindow.LoadTextureFromFile(AssetBundleGraphSettings.GUI.RESOURCE_INPUT_BG);
				}
				return NodeSingleton.s.inputPointTex;
			}
		}

		public static Texture2D outputPointTex {
			get {
				if(NodeSingleton.s.outputPointTex == null) {
					NodeSingleton.s.outputPointTex = AssetBundleGraphEditorWindow.LoadTextureFromFile(AssetBundleGraphSettings.GUI.RESOURCE_OUTPUT_BG);
				}
				return NodeSingleton.s.outputPointTex;
			}
		}

		public static Texture2D enablePointMarkTex {
			get {
				if(NodeSingleton.s.enablePointMarkTex == null) {
					NodeSingleton.s.enablePointMarkTex = AssetBundleGraphEditorWindow.LoadTextureFromFile(AssetBundleGraphSettings.GUI.RESOURCE_CONNECTIONPOINT_ENABLE);
				}
				return NodeSingleton.s.enablePointMarkTex;
			}
		}

		public static Texture2D inputPointMarkTex {
			get {
				if(NodeSingleton.s.inputPointMarkTex == null) {
					NodeSingleton.s.inputPointMarkTex = AssetBundleGraphEditorWindow.LoadTextureFromFile(AssetBundleGraphSettings.GUI.RESOURCE_CONNECTIONPOINT_INPUT);
				}
				return NodeSingleton.s.inputPointMarkTex;
			}
		}

		public static Texture2D outputPointMarkTex {
			get {
				if(NodeSingleton.s.outputPointMarkTex == null) {
					NodeSingleton.s.outputPointMarkTex = AssetBundleGraphEditorWindow.LoadTextureFromFile(AssetBundleGraphSettings.GUI.RESOURCE_CONNECTIONPOINT_OUTPUT);
				}
				return NodeSingleton.s.outputPointMarkTex;
			}
		}

		public static Texture2D outputPointMarkConnectedTex {
			get {
				if(NodeSingleton.s.outputPointMarkConnectedTex == null) {
					NodeSingleton.s.outputPointMarkConnectedTex = AssetBundleGraphEditorWindow.LoadTextureFromFile(AssetBundleGraphSettings.GUI.RESOURCE_CONNECTIONPOINT_OUTPUT_CONNECTED);
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

		public static Dictionary<NodeKind, string> SelectedStyle {
			get {
				if(NodeSingleton.s.selectedStyle == null) {
					NodeSingleton.s.selectedStyle = new Dictionary<NodeKind, string>() {
						{NodeKind.LOADER_GUI, 			"flow node 0 on"},
						{NodeKind.EXPORTER_GUI, 		"flow node 0 on"},
						{NodeKind.FILTER_GUI, 			"flow node 1 on"},
						{NodeKind.IMPORTSETTING_GUI,	"flow node 2 on"},
						{NodeKind.GROUPING_GUI, 		"flow node 3 on"},
						{NodeKind.PREFABBUILDER_GUI, 	"flow node 4 on"},
						{NodeKind.BUNDLECONFIG_GUI, 		"flow node 5 on"},
						{NodeKind.BUNDLEBUILDER_GUI, 	"flow node 6 on"},
						{NodeKind.MODIFIER_GUI, 		"flow node 2 on"}
					};
				}
				return NodeSingleton.s.selectedStyle;
			}
		}

		public static Dictionary<NodeKind, string> UnselectedStyle {
			get {
				if(NodeSingleton.s.unselectedStyle == null) {
					NodeSingleton.s.unselectedStyle = new Dictionary<NodeKind, string>() {
						{NodeKind.LOADER_GUI, 			"flow node 0"},
						{NodeKind.EXPORTER_GUI, 		"flow node 0"},
						{NodeKind.FILTER_GUI, 			"flow node 1"},
						{NodeKind.IMPORTSETTING_GUI,	"flow node 2"},
						{NodeKind.GROUPING_GUI, 		"flow node 3"},
						{NodeKind.PREFABBUILDER_GUI, 	"flow node 4"},
						{NodeKind.BUNDLECONFIG_GUI, 	"flow node 5"},
						{NodeKind.BUNDLEBUILDER_GUI, 	"flow node 6"},
						{NodeKind.MODIFIER_GUI, 		"flow node 2"}
					};
				}
				return NodeSingleton.s.unselectedStyle;
			}
		}

		private class NodeSingleton {
			public Action<NodeEvent> emitAction;

			public Texture2D inputPointTex;
			public Texture2D outputPointTex;

			public Texture2D enablePointMarkTex;

			public Texture2D inputPointMarkTex;
			public Texture2D outputPointMarkTex;
			public Texture2D outputPointMarkConnectedTex;
			public PlatformButton[] platformButtons;

			public List<BuildTarget> supportedBuildTargets;
			public string[] supportedBuildTargetNames;
			public List<BuildTargetGroup> supportedBuildTargetGroups;

			public List<string> allNodeNames;

			public Dictionary<NodeKind, string> selectedStyle;
			public Dictionary<NodeKind, string> unselectedStyle;

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
						LogUtility.Logger.LogError(LogUtility.kTag, e.ToString());
					}
				}
			}

			private Texture2D GetPlatformIcon(string name) {
				return EditorGUIUtility.IconContent(name).image as Texture2D;
			}
		}
	}
}
