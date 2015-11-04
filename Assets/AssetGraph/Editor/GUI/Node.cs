using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;


namespace AssetGraph {
	[Serializable] public class Node {
		public static Action<OnNodeEvent> Emit;

		public static Texture2D inputPointTex;
		public static Texture2D outputPointTex;


		public static Texture2D enablePointMarkTex;

		public static Texture2D inputPointMarkTex;
		public static Texture2D outputPointMarkTex;
		public static Texture2D outputPointMarkConnectedTex;
		public static Texture2D[] platformButtonTextures;

		[SerializeField] private List<ConnectionPoint> connectionPoints = new List<ConnectionPoint>();

		[SerializeField] private int nodeWindowId;
		[SerializeField] private Rect baseRect;

		[SerializeField] public string name;
		[SerializeField] public string nodeId;
		[SerializeField] public AssetGraphSettings.NodeKind kind;

		[SerializeField] public string scriptType;
		[SerializeField] public string scriptPath;
		[SerializeField] public string loadPath;
		[SerializeField] public string exportPath;
		[SerializeField] public List<string> filterContainsKeywords;
		[SerializeField] public Dictionary<string, string> groupingKeyword;
		[SerializeField] public string bundleNameTemplate;
		[SerializeField] public Dictionary<string, List<string>> enabledBundleOptions;

		[SerializeField] private string nodeInterfaceTypeStr;
		[SerializeField] private BuildTarget currentBuildTarget;

		[SerializeField] private NodeInspector nodeInsp;




		private float progress;
		private bool running;

		public static Node LoaderNode (int index, string name, string nodeId, AssetGraphSettings.NodeKind kind, string loadPath, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				loadPath: loadPath
			);
		}

		public static Node ExporterNode (int index, string name, string nodeId, AssetGraphSettings.NodeKind kind, string exportPath, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				exportPath: exportPath
			);
		}

		public static Node ScriptNode (int index, string name, string nodeId, AssetGraphSettings.NodeKind kind, string scriptType, string scriptPath, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				scriptType: scriptType,
				scriptPath: scriptPath
			);
		}

		public static Node GUINodeForFilter (int index, string name, string nodeId, AssetGraphSettings.NodeKind kind, List<string> filterContainsKeywords, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				filterContainsKeywords: filterContainsKeywords
			);
		}

		public static Node GUINodeForImport (int index, string name, string nodeId, AssetGraphSettings.NodeKind kind, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y
			);
		}

		public static Node GUINodeForGrouping (int index, string name, string nodeId, AssetGraphSettings.NodeKind kind, Dictionary<string, string> groupingKeyword, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				groupingKeyword: groupingKeyword
			);
		}

		public static Node GUINodeForPrefabricator (int index, string name, string nodeId, AssetGraphSettings.NodeKind kind, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y
			);
		}

		public static Node GUINodeForBundlizer (int index, string name, string nodeId, AssetGraphSettings.NodeKind kind, string bundleNameTemplate, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				bundleNameTemplate: bundleNameTemplate
			);
		}

		public static Node GUINodeForBundleBuilder (int index, string name, string nodeId, AssetGraphSettings.NodeKind kind, Dictionary<string, List<string>> enabledBundleOptions, float x, float y) {
			return new Node(
				index: index,
				name: name,
				nodeId: nodeId,
				kind: kind,
				x: x,
				y: y,
				enabledBundleOptions: enabledBundleOptions
			);
		}

		/**
			Inspector GUI for this node.
		*/
		[CustomEditor(typeof(NodeInspector))]
		public class NodeObj : Editor {

			/*
				こっから調整中、必要なプラットフォームを仕切るところが難しい。
			*/
			public class BuildPlatform
			{
				public string name;
				// public GUIContent title;
				public Texture2D smallIcon;
				public BuildTargetGroup targetGroup;
				// public bool forceShowTarget;
				// public string tooltip;
				public BuildTarget DefaultTarget
				{
					get
					{
						switch (this.targetGroup)
						{
						case BuildTargetGroup.Standalone:
							return BuildTarget.StandaloneWindows;
						case BuildTargetGroup.WebPlayer:
							return BuildTarget.WebPlayer;
						case BuildTargetGroup.iOS:
							return BuildTarget.iOS;
						case BuildTargetGroup.PS3:
							return BuildTarget.PS3;
						case BuildTargetGroup.XBOX360:
							return BuildTarget.XBOX360;
						case BuildTargetGroup.Android:
							return BuildTarget.Android;
						case BuildTargetGroup.GLESEmu:
							return BuildTarget.StandaloneGLESEmu;
						case BuildTargetGroup.WebGL:
							return BuildTarget.WebGL;
						case BuildTargetGroup.Metro:
							return BuildTarget.WSAPlayer;
						case BuildTargetGroup.WP8:
							return BuildTarget.WP8Player;
						case BuildTargetGroup.BlackBerry:
							return BuildTarget.BlackBerry;
						case BuildTargetGroup.Tizen:
							return BuildTarget.Tizen;
						case BuildTargetGroup.PSP2:
							return BuildTarget.PSP2;
						case BuildTargetGroup.PS4:
							return BuildTarget.PS4;
						case BuildTargetGroup.XboxOne:
							return BuildTarget.XboxOne;
						case BuildTargetGroup.SamsungTV:
							return BuildTarget.SamsungTV;
						}
						return (BuildTarget)(-1);
					}
				}
				public BuildPlatform(string locTitle, BuildTargetGroup targetGroup, bool forceShowTarget) : this(locTitle, string.Empty, targetGroup, forceShowTarget)
				{
				}
				public BuildPlatform(string locTitle, string tooltip, BuildTargetGroup targetGroup, bool forceShowTarget)
				{
					this.targetGroup = targetGroup;
					this.name = locTitle;//((targetGroup == BuildTargetGroup.Unknown) ? string.Empty : BuildPipeline.GetBuildTargetGroupName(this.DefaultTarget));
					// this.title = new GUIContent("title def");//EditorGUIUtility.TextContent(locTitle);
					this.smallIcon = (EditorGUIUtility.IconContent(locTitle + ".Small").image as Texture2D);
					// this.tooltip = tooltip;
					// this.forceShowTarget = forceShowTarget;
				}
			}

			private class BuildPlatforms
			{
				public BuildPlatform[] buildPlatforms;
				public BuildTarget[] standaloneSubtargets;
				public GUIContent[] standaloneSubtargetStrings;
				public GUIContent[] webGLOptimizationLevels = new GUIContent[]
				{
					// EditorGUIUtility.TextContent("BuildSettings.WebGLOptimizationLevel1"),
					// EditorGUIUtility.TextContent("BuildSettings.WebGLOptimizationLevel2"),
					// EditorGUIUtility.TextContent("BuildSettings.WebGLOptimizationLevel3")
				};
				internal BuildPlatforms()
				{
					List<BuildPlatform> list = new List<BuildPlatform>();
					list.Add(new BuildPlatform("BuildSettings.Web", BuildTargetGroup.WebPlayer, true));
					list.Add(new BuildPlatform("BuildSettings.Standalone", BuildTargetGroup.Standalone, true));
					list.Add(new BuildPlatform("BuildSettings.iPhone", BuildTargetGroup.iOS, true));
					list.Add(new BuildPlatform("BuildSettings.Android", BuildTargetGroup.Android, true));
					list.Add(new BuildPlatform("BuildSettings.BlackBerry", BuildTargetGroup.BlackBerry, true));
					list.Add(new BuildPlatform("BuildSettings.Tizen", BuildTargetGroup.Tizen, false));
					list.Add(new BuildPlatform("BuildSettings.XBox360", BuildTargetGroup.XBOX360, true));
					list.Add(new BuildPlatform("BuildSettings.XboxOne", BuildTargetGroup.XboxOne, true));
					list.Add(new BuildPlatform("BuildSettings.PS3", BuildTargetGroup.PS3, true));
					list.Add(new BuildPlatform("BuildSettings.PSP2", BuildTargetGroup.PSP2, true));
					list.Add(new BuildPlatform("BuildSettings.PS4", BuildTargetGroup.PS4, true));
					list.Add(new BuildPlatform("BuildSettings.StandaloneGLESEmu", BuildTargetGroup.GLESEmu, false));
					list.Add(new BuildPlatform("BuildSettings.Metro", BuildTargetGroup.Metro, true));
					list.Add(new BuildPlatform("BuildSettings.WP8", BuildTargetGroup.WP8, true));
					list.Add(new BuildPlatform("BuildSettings.WebGL", BuildTargetGroup.WebGL, true));
					list.Add(new BuildPlatform("BuildSettings.SamsungTV", BuildTargetGroup.SamsungTV, false));
					foreach (BuildPlatform current in list)
					{
						// current.tooltip = BuildPipeline.GetBuildTargetGroupDisplayName(current.targetGroup) + " settings";
					}
					this.buildPlatforms = list.ToArray();
					this.SetupStandaloneSubtargets();
				}
				private void SetupStandaloneSubtargets()
				{
					List<BuildTarget> list = new List<BuildTarget>();
					List<GUIContent> list2 = new List<GUIContent>();
					// if (ModuleManager.IsPlatformSupportLoaded(ModuleManager.GetTargetStringFromBuildTarget(BuildTarget.StandaloneWindows)))
					// {
					// 	list.Add(BuildTarget.StandaloneWindows);
					// 	list2.Add(EditorGUIUtility.TextContent("BuildSettings.StandaloneWindows"));
					// }
					// if (ModuleManager.IsPlatformSupportLoaded(ModuleManager.GetTargetStringFromBuildTarget(BuildTarget.StandaloneOSXIntel)))
					// {
					// 	list.Add(BuildTarget.StandaloneOSXIntel);
					// 	list2.Add(EditorGUIUtility.TextContent("BuildSettings.StandaloneOSXIntel"));
					// }
					// if (ModuleManager.IsPlatformSupportLoaded(ModuleManager.GetTargetStringFromBuildTarget(BuildTarget.StandaloneLinux)))
					// {
					// 	list.Add(BuildTarget.StandaloneLinux);
					// 	list2.Add(EditorGUIUtility.TextContent("BuildSettings.StandaloneLinux"));
					// }
					this.standaloneSubtargets = list.ToArray();
					this.standaloneSubtargetStrings = list2.ToArray();
				}
				// public string GetBuildTargetDisplayName(BuildTarget target)
				// {
				// 	BuildPlatform[] array = this.buildPlatforms;
				// 	for (int i = 0; i < array.Length; i++)
				// 	{
				// 		BuildPlatform buildPlatform = array[i];
				// 		if (buildPlatform.DefaultTarget == target)
				// 		{
				// 			return buildPlatform.title.text;
				// 		}
				// 	}
				// 	if (target == BuildTarget.WebPlayerStreamed)
				// 	{
				// 		return this.BuildPlatformFromTargetGroup(BuildTargetGroup.WebPlayer).title.text;
				// 	}
				// 	for (int j = 0; j < this.standaloneSubtargets.Length; j++)
				// 	{
				// 		if (this.standaloneSubtargets[j] == BuildPlatforms.DefaultTargetForPlatform(target))
				// 		{
				// 			return this.standaloneSubtargetStrings[j].text;
				// 		}
				// 	}
				// 	return "Unsupported Target";
				// }
				public static Dictionary<GUIContent, BuildTarget> GetArchitecturesForPlatform(BuildTarget target)
				{
					switch (target)
					{
					case BuildTarget.StandaloneOSXUniversal:
					case BuildTarget.StandaloneOSXIntel:
						goto IL_B6;
					case (BuildTarget)3:
						IL_1A:
						switch (target)
						{
						case BuildTarget.StandaloneLinux64:
						case BuildTarget.StandaloneLinuxUniversal:
							goto IL_78;
						case BuildTarget.WP8Player:
							IL_33:
							switch (target)
							{
							case BuildTarget.StandaloneLinux:
								goto IL_78;
							case BuildTarget.StandaloneWindows64:
								goto IL_4D;
							}
							return null;
						case BuildTarget.StandaloneOSXIntel64:
							goto IL_B6;
						}
						goto IL_33;
						IL_78:
						return new Dictionary<GUIContent, BuildTarget>
						{

							// {
							// 	EditorGUIUtility.TextContent("x86"),
							// 	BuildTarget.StandaloneLinux
							// },

							// {
							// 	EditorGUIUtility.TextContent("x86_64"),
							// 	BuildTarget.StandaloneLinux64
							// },

							// {
							// 	EditorGUIUtility.TextContent("x86 + x86_64 (Universal)"),
							// 	BuildTarget.StandaloneLinuxUniversal
							// }
						};
					case BuildTarget.StandaloneWindows:
						goto IL_4D;
					}
					goto IL_1A;
					IL_4D:
					return new Dictionary<GUIContent, BuildTarget>
					{

						// {
						// 	EditorGUIUtility.TextContent("x86"),
						// 	BuildTarget.StandaloneWindows
						// },

						// {
						// 	EditorGUIUtility.TextContent("x86_64"),
						// 	BuildTarget.StandaloneWindows64
						// }
					};
					IL_B6:
					return new Dictionary<GUIContent, BuildTarget>
					{

						// {
						// 	EditorGUIUtility.TextContent("x86"),
						// 	BuildTarget.StandaloneOSXIntel
						// },

						// {
						// 	EditorGUIUtility.TextContent("x86_64"),
						// 	BuildTarget.StandaloneOSXIntel64
						// },

						// {
						// 	EditorGUIUtility.TextContent("Universal"),
						// 	BuildTarget.StandaloneOSXUniversal
						// }
					};
				}
				public static BuildTarget DefaultTargetForPlatform(BuildTarget target)
				{
					switch (target)
					{
					case BuildTarget.StandaloneLinux:
					case BuildTarget.StandaloneLinux64:
					case BuildTarget.StandaloneLinuxUniversal:
						return BuildTarget.StandaloneLinux;
					case (BuildTarget)18:
					case BuildTarget.WebGL:
					case (BuildTarget)22:
					case (BuildTarget)23:
						IL_37:
						switch (target)
						{
						case BuildTarget.StandaloneOSXUniversal:
						case BuildTarget.StandaloneOSXIntel:
							return BuildTarget.StandaloneOSXIntel;
						case BuildTarget.StandaloneWindows:
							return BuildTarget.StandaloneWindows;
						}
						return target;
					case BuildTarget.StandaloneWindows64:
						return BuildTarget.StandaloneWindows;
					case BuildTarget.WSAPlayer:
						return BuildTarget.WSAPlayer;
					case BuildTarget.WP8Player:
						return BuildTarget.WP8Player;
					case BuildTarget.StandaloneOSXIntel64:
						return BuildTarget.StandaloneOSXIntel;
					}
					goto IL_37;
				}
				public int BuildPlatformIndexFromTargetGroup(BuildTargetGroup group)
				{
					for (int i = 0; i < this.buildPlatforms.Length; i++)
					{
						if (group == this.buildPlatforms[i].targetGroup)
						{
							return i;
						}
					}
					return -1;
				}
				public BuildPlatform BuildPlatformFromTargetGroup(BuildTargetGroup group)
				{
					int num = this.BuildPlatformIndexFromTargetGroup(group);
					return (num == -1) ? null : this.buildPlatforms[num];
				}
			}

			static BuildPlatforms s_BuildPlatforms;

			private static void InitBuildPlatforms()
			{
				if (s_BuildPlatforms == null)
				{
					s_BuildPlatforms = new BuildPlatforms();
					RepairSelectedBuildTargetGroup();
				}
			}

			public static List<BuildPlatform> GetValidPlatforms()
			{
				InitBuildPlatforms();
				List<BuildPlatform> list = new List<BuildPlatform>();
				BuildPlatform[] buildPlatforms = s_BuildPlatforms.buildPlatforms;
				for (int i = 0; i < buildPlatforms.Length; i++)
				{
					BuildPlatform buildPlatform = buildPlatforms[i];
					// if (buildPlatform.targetGroup == BuildTargetGroup.Standalone || BuildPipeline.IsBuildTargetSupported(buildPlatform.DefaultTarget))
					{
						list.Add(buildPlatform);
					}
				}
				return list;
			}

			private static void RepairSelectedBuildTargetGroup()
			{
				BuildTargetGroup selectedBuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
				if (selectedBuildTargetGroup == BuildTargetGroup.Unknown || s_BuildPlatforms == null || s_BuildPlatforms.BuildPlatformIndexFromTargetGroup(selectedBuildTargetGroup) < 0)
				{
					EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.WebPlayer;
				}
			}

			private static int BeginPlatformGrouping(BuildPlatform[] platforms, GUIContent defaultTab)
			{
				int num = -1;
				for (int i = 0; i < platforms.Length; i++)
				{
					if (platforms[i].targetGroup == EditorUserBuildSettings.selectedBuildTargetGroup)
					{
						num = i;
					}
				}
				if (num == -1)
				{
					// EditorGUILayout.s_SelectedDefault.value = true;
					num = 0;
				}
				int num2 = num;//(defaultTab != null) ? ((!EditorGUILayout.s_SelectedDefault.value) ? num : -1) : num;
				bool enabled = GUI.enabled;
				GUI.enabled = true;
				// EditorGUI.BeginChangeCheck();
				Rect rect = EditorGUILayout.BeginVertical(GUI.skin.box, new GUILayoutOption[0]);
				rect.width -= 1f;
				int num3 = platforms.Length;
				int num4 = 18;
				
				// GUIStyle toolbarButton = EditorStyles.toolbarButton;
				// if (defaultTab != null && GUI.Toggle(new Rect(rect.x, rect.y, rect.width - (float)num3 * 30f, (float)num4), num2 == -1, defaultTab, toolbarButton))
				// {
				// 	num2 = -1;
				// }


				// for (int j = 0; j < num3; j++)
				// {
				// 	Rect position;
				// 	if (defaultTab != null)
				// 	{
				// 		position = new Rect(rect.xMax - (float)(num3 - j) * 30f, rect.y, 30f, (float)num4);
				// 	}
				// 	else
				// 	{
				// 		int num5 = Mathf.RoundToInt((float)j * rect.width / (float)num3);
				// 		int num6 = Mathf.RoundToInt((float)(j + 1) * rect.width / (float)num3);
				// 		position = new Rect(rect.x + (float)num5, rect.y, (float)(num6 - num5), (float)num4);
				// 	}
				// 	// if (GUI.Toggle(position, num2 == j, new GUIContent(platforms[j].smallIcon, platforms[j].tooltip), toolbarButton))
				// 	// {
				// 	// 	num2 = j;
				// 	// }
				// }

				// GUILayoutUtility.GetRect(10f, (float)num4);
				// GUI.enabled = enabled;

				
				// if (EditorGUI.EndChangeCheck())
				// {
				// 	if (defaultTab == null)
				// 	{
				// 		EditorUserBuildSettings.selectedBuildTargetGroup = platforms[num2].targetGroup;
				// 	}
				// 	else
				// 	{
				// 		if (num2 < 0)
				// 		{
				// 			// EditorGUILayout.s_SelectedDefault.value = true;
				// 		}
				// 		else
				// 		{
				// 			EditorUserBuildSettings.selectedBuildTargetGroup = platforms[num2].targetGroup;
				// 			// EditorGUILayout.s_SelectedDefault.value = false;
				// 		}
				// 	}
				// 	// UnityEngine.Object[] array = Resources.FindObjectsOfTypeAll(typeof(BuildPlayerWindow));
				// 	// for (int k = 0; k < array.Length; k++)
				// 	// {
				// 	// 	BuildPlayerWindow buildPlayerWindow = array[k] as BuildPlayerWindow;
				// 	// 	if (buildPlayerWindow != null)
				// 	// 	{
				// 	// 		buildPlayerWindow.Repaint();
				// 	// 	}
				// 	// }
				// }
				return num2;
			}
			/*
				ここまで。結局どんなプラットフォームを出そうか、っていうのは、
				・その人が使えるやつ　とかあんま関係なくて、文字列で取得できればいい感じなんで、なんていうか全種類必要。
			*/

			public override void OnInspectorGUI () {
				var currentTarget = (NodeInspector)target;
				var node = currentTarget.node;
				if (node == null) return;

				EditorGUILayout.LabelField("nodeId:", node.nodeId);

				switch (node.kind) {
					case AssetGraphSettings.NodeKind.LOADER_GUI: {
						EditorGUILayout.HelpBox("Loader: load files from path.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						// currentBuildTargetをアクティブなものとしてターゲットに出す。
						// んで、保存をセットしたら、この項目が特徴的なものとしてセットされるようにする。まずはdefault build target 枠を設けるか。
						// 設定だけでいいはず。ただし、importerとかにはそれ用のフォルダができちゃう。うむ。
						
						BuildPlatform[] array = GetValidPlatforms().ToArray();
						foreach (var a in array) {
							Debug.LogWarning("a:" + a.name);
						}

						GUILayout.Space(10f);
						using (new EditorGUILayout.HorizontalScope()) {
							int i = 0;

							foreach (var platformButtonData in array) {
								var platformButtonTexture = platformButtonData.smallIcon;
								// var platfornName = platformButtonData.name;

								var onOff = false;
								if (i == 8) onOff = true;

								if (GUILayout.Toggle(onOff, platformButtonTexture, "toolbarbutton")) {
									// 、、、？？毎フレームよばれてしまうっぽいな？

								}
								i++;
							}
						}

						using (new EditorGUILayout.HorizontalScope()) {
							var currentPackage = "Default";
							GUILayout.Label("Package:");

							if (GUILayout.Button(currentPackage)) {// こいつがタブ
								Action DefaultSelected = () => {
									
								};
								Action<string> ExistSelected = (string package) => {
									Debug.LogError("package:" + package);
								};

								ShowPackageMenu(DefaultSelected, ExistSelected, new List<string>{"a", "b"});
							}

							if (GUILayout.Button("+", GUILayout.Width(30))) {
								Debug.LogError("add new package windowを出す、とかかな。終わるまで放置する。");
							}
						}

						using (new EditorGUILayout.VerticalScope(GUI.skin.box, new GUILayoutOption[0])) {
							var newLoadPath = EditorGUILayout.TextField("Load Path", node.loadPath);
							if (newLoadPath != node.loadPath) {
								Debug.LogWarning("本当は打ち込み単位の更新ではなくて、Finderからパス、、とかがいいんだと思うけど、今はパス。");
								node.loadPath = newLoadPath;
								node.Save();
							}
						}

						
						break;
					}


					case AssetGraphSettings.NodeKind.FILTER_SCRIPT: {
						EditorGUILayout.HelpBox("Filter: filtering files by script.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						EditorGUILayout.LabelField("Script Path", node.scriptPath);

						var outputPointLabels = node.OutputPointLabels();
						EditorGUILayout.LabelField("connectionPoints Count", outputPointLabels.Count.ToString());
						
						foreach (var label in outputPointLabels) {
							EditorGUILayout.LabelField("label", label);
						}
						break;
					}
					case AssetGraphSettings.NodeKind.FILTER_GUI: {
						EditorGUILayout.HelpBox("Filter: filtering files by keywords.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}
						
						for (int i = 0; i < node.filterContainsKeywords.Count; i++) {
							GUILayout.BeginHorizontal();
							{
								if (GUILayout.Button("-")) {
									node.filterContainsKeywords.RemoveAt(i);
									node.UpdateOutputPoints();
									node.Save();
								} else {
									var newContainsKeyword = EditorGUILayout.TextField("Contains", node.filterContainsKeywords[i]);
									if (newContainsKeyword != node.filterContainsKeywords[i]) {
										node.filterContainsKeywords[i] = newContainsKeyword;
										node.UpdateOutputPoints();
										node.UpdateNodeRect();
										node.Save();
									}
								}
							}
							GUILayout.EndHorizontal();
						}

						// add contains keyword interface.
						if (GUILayout.Button("+")) {
							node.filterContainsKeywords.Add(AssetGraphSettings.DEFAULT_FILTER_KEYWORD);
							node.UpdateOutputPoints();
							node.UpdateNodeRect();
							node.Save();
						}

						break;
					}


					case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT: {
						EditorGUILayout.HelpBox("Importer: import files by script.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						EditorGUILayout.LabelField("Script Path", node.scriptPath);
						break;
					}
					case AssetGraphSettings.NodeKind.IMPORTER_GUI: {
						EditorGUILayout.HelpBox("Importer: import files with applying settings from SamplingAssets.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						var nodeId = node.nodeId;

						var noFilesFound = false;
						var tooManyFilesFound = false;

						var samplingPath = FileController.PathCombine(AssetGraphSettings.IMPORTER_SAMPLING_PLACE, nodeId);
						if (Directory.Exists(samplingPath)) {
							var samplingFiles = FileController.FilePathsInFolderOnly1Level(samplingPath);
							switch (samplingFiles.Count) {
								case 0: {
									noFilesFound = true;
									break;
								}
								case 1: {
									var samplingAssetPath = samplingFiles[0];
									EditorGUILayout.LabelField("Sampling Asset Path", samplingAssetPath);
									if (GUILayout.Button("Modify Import Setting")) {
										var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(samplingAssetPath);
										Selection.activeObject = obj;
									}
									if (GUILayout.Button("Reset Import Setting")) {
										var result = AssetDatabase.DeleteAsset(samplingAssetPath);
										if (!result) Debug.LogError("failed to delete samplingAsset:" + samplingAssetPath);
										node.Save();
									}
									break;
								}
								default: {
									tooManyFilesFound = true;
									break;
								}
							}
						} else {
							noFilesFound = true;
						}

						if (noFilesFound) {
							EditorGUILayout.LabelField("Sampling Asset", "no asset found. please Reload first.");
						}

						if (tooManyFilesFound) {
							EditorGUILayout.LabelField("Sampling Asset", "too many assets found. please delete file at:" + samplingPath);
						}

						break;
					}


					case AssetGraphSettings.NodeKind.GROUPING_GUI: {
						EditorGUILayout.HelpBox("Grouping: grouping files by one keyword.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}
						Debug.LogError("調整中、現在のplatform_package_keyを主体として扱う。それの変更も発生する。");
						// var groupingKeyword = EditorGUILayout.TextField("Grouping Keyword", node.groupingKeyword);
						// if (groupingKeyword != node.groupingKeyword) {
						// 	node.groupingKeyword = groupingKeyword;
						// 	node.Save();
						// }
						break;
					}
					

					case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT: {
						EditorGUILayout.HelpBox("Prefabricator: generate prefab by PrefabricatorBase extended script.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						EditorGUILayout.LabelField("Script Path", node.scriptPath);
						Debug.LogWarning("型指定をしたらScriptPathが決まる、っていうのがいいと思う。型指定の窓が欲しい。");
						break;
					}
					case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI:{
						EditorGUILayout.HelpBox("Prefabricator: generate prefab by PrefabricatorBase extended script.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						var newScriptType = EditorGUILayout.TextField("Script Type", node.scriptType);
						if (newScriptType != node.scriptType) {
							Debug.LogWarning("Scriptなんで、 ScriptをAttachできて、勝手に決まった方が良い。");
							node.scriptType = newScriptType;
							node.Save();
						}
						break;
					}


					case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT: {
						EditorGUILayout.HelpBox("Bundlizer: generate AssetBundle by script.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						EditorGUILayout.LabelField("Script Path", node.scriptPath);
						break;
					}
					case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
						EditorGUILayout.HelpBox("Bundlizer: bundle resources to AssetBundle by template.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						var bundleNameTemplate = EditorGUILayout.TextField("Bundle Name Template", node.bundleNameTemplate);
						if (bundleNameTemplate != node.bundleNameTemplate) {
							node.bundleNameTemplate = bundleNameTemplate;
							node.Save();
						}
						break;
					}

					case AssetGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
						EditorGUILayout.HelpBox("BundleBuilder: generate AssetBundle.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						var bundleOptions = node.enabledBundleOptions;
						Debug.LogError("データ形式変えたので、あとで対応");
						// for (var i = 0; i < AssetGraphSettings.DefaultBundleOptionSettings.Count; i++) {
						// 	var enablablekey = AssetGraphSettings.DefaultBundleOptionSettings[i];

						// 	var isEnable = bundleOptions.Contains(enablablekey);

						// 	var result = EditorGUILayout.ToggleLeft(enablablekey, isEnable);
						// 	if (result != isEnable) {

						// 		node.enabledBundleOptions.Add(enablablekey);

						// 		/*
						// 			Cannot use options DisableWriteTypeTree and IgnoreTypeTreeChanges at the same time.
						// 		*/
						// 		if (enablablekey == "Disable Write TypeTree" && result &&
						// 			node.enabledBundleOptions.Contains("Ignore TypeTree Changes")) {
						// 			node.enabledBundleOptions.Remove("Ignore TypeTree Changes");
						// 		}

						// 		if (enablablekey == "Ignore TypeTree Changes" && result &&
						// 			node.enabledBundleOptions.Contains("Disable Write TypeTree")) {
						// 			node.enabledBundleOptions.Remove("Disable Write TypeTree");
						// 		}

						// 		node.Save();
						// 	}
						// }
						break;
					}

					case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
						EditorGUILayout.HelpBox("Exporter: export files to path.", MessageType.Info);
						var newName = EditorGUILayout.TextField("Node Name", node.name);
						if (newName != node.name) {
							node.name = newName;
							node.UpdateNodeRect();
							node.Save();
						}

						var newExportPath = EditorGUILayout.TextField("Export Path", node.exportPath);
						if (newExportPath != node.exportPath) {
							Debug.LogWarning("本当は打ち込み単位の更新ではなくて、Finderからパス、、とかがいいんだと思うけど、今はパス。");
							node.exportPath = newExportPath;
							node.Save();
						}
						break;
					}

					default: {
						Debug.LogError("failed to match:" + node.kind);
						break;
					}
				}
			}
		}

		public void UpdateOutputPoints () {
			connectionPoints = new List<ConnectionPoint>();

			foreach (var keyword in filterContainsKeywords) {
				var newPoint = new OutputPoint(keyword);
				AddConnectionPoint(newPoint);
			}

			// add input point
			AddConnectionPoint(new InputPoint(AssetGraphSettings.DEFAULT_INPUTPOINT_LABEL));

			Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_CONNECTIONPOINT_UPDATED, this, Vector2.zero, null));
		}

		public void Save () {
			Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_SAVE, this, Vector2.zero, null));
		}

		public Node () {}

		private Node (
			int index, 
			string name, 
			string nodeId, 
			AssetGraphSettings.NodeKind kind, 
			float x, 
			float y,
			string scriptType = null, 
			string scriptPath = null, 
			string loadPath = null, 
			string exportPath = null, 
			List<string> filterContainsKeywords = null, 
			Dictionary<string, string> groupingKeyword = null,
			string bundleNameTemplate = null,
			Dictionary<string, List<string>> enabledBundleOptions = null
		) {
			nodeInsp = ScriptableObject.CreateInstance<NodeInspector>();
			nodeInsp.hideFlags = HideFlags.DontSave;

			this.nodeWindowId = index;
			this.name = name;
			this.nodeId = nodeId;
			this.kind = kind;
			this.scriptType = scriptType;
			this.scriptPath = scriptPath;
			this.loadPath = loadPath;
			this.exportPath = exportPath;
			this.filterContainsKeywords = filterContainsKeywords;
			this.groupingKeyword = groupingKeyword;
			this.bundleNameTemplate = bundleNameTemplate;
			this.enabledBundleOptions = enabledBundleOptions;
			
			this.baseRect = new Rect(x, y, AssetGraphGUISettings.NODE_BASE_WIDTH, AssetGraphGUISettings.NODE_BASE_HEIGHT);
			
			switch (this.kind) {
				case AssetGraphSettings.NodeKind.LOADER_GUI:
				case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 0";
					break;
				}

				case AssetGraphSettings.NodeKind.FILTER_SCRIPT:
				case AssetGraphSettings.NodeKind.FILTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 1";
					break;
				}
				
				case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT:
				case AssetGraphSettings.NodeKind.IMPORTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 2";
					break;
				}

				case AssetGraphSettings.NodeKind.GROUPING_GUI: {
					this.nodeInterfaceTypeStr = "flow node 3";
					break;
				}

				case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
				case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI:
				 {
					this.nodeInterfaceTypeStr = "flow node 4";
					break;
				}

				case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT:
				case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 5";
					break;
				}

				case AssetGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 6";
					break;
				}

				default: {
					Debug.LogError("failed to match:" + this.kind);
					break;
				}
			}
		}

		public void SetActive () {
			nodeInsp.UpdateNode(this);
			Selection.activeObject = nodeInsp;

			switch (this.kind) {
				case AssetGraphSettings.NodeKind.LOADER_GUI:
				case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 0 on";
					break;
				}

				case AssetGraphSettings.NodeKind.FILTER_SCRIPT:
				case AssetGraphSettings.NodeKind.FILTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 1 on";
					break;
				}
				
				case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT:
				case AssetGraphSettings.NodeKind.IMPORTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 2 on";
					break;
				}

				case AssetGraphSettings.NodeKind.GROUPING_GUI: {
					this.nodeInterfaceTypeStr = "flow node 3 on";
					break;
				}

				case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
				case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI:
				 {
					this.nodeInterfaceTypeStr = "flow node 4 on";
					break;
				}

				case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT:
				case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 5 on";
					break;
				}

				case AssetGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 6 on";
					break;
				}

				default: {
					Debug.LogError("failed to match:" + this.kind);
					break;
				}
			}
		}

		public void SetInactive () {
			switch (this.kind) {
				case AssetGraphSettings.NodeKind.LOADER_GUI:
				case AssetGraphSettings.NodeKind.EXPORTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 0";
					break;
				}

				case AssetGraphSettings.NodeKind.FILTER_SCRIPT:
				case AssetGraphSettings.NodeKind.FILTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 1";
					break;
				}
				
				case AssetGraphSettings.NodeKind.IMPORTER_SCRIPT:
				case AssetGraphSettings.NodeKind.IMPORTER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 2";
					break;
				}

				case AssetGraphSettings.NodeKind.GROUPING_GUI: {
					this.nodeInterfaceTypeStr = "flow node 3";
					break;
				}

				case AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT:
				case AssetGraphSettings.NodeKind.PREFABRICATOR_GUI:
				 {
					this.nodeInterfaceTypeStr = "flow node 4";
					break;
				}

				case AssetGraphSettings.NodeKind.BUNDLIZER_SCRIPT:
				case AssetGraphSettings.NodeKind.BUNDLIZER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 5";
					break;
				}

				case AssetGraphSettings.NodeKind.BUNDLEBUILDER_GUI: {
					this.nodeInterfaceTypeStr = "flow node 6";
					break;
				}

				default: {
					Debug.LogError("failed to match:" + this.kind);
					break;
				}
			}
		}

		public void AddConnectionPoint (ConnectionPoint adding) {
			connectionPoints.Add(adding);
			
			// update node size by number of output connectionPoint.
			var outputPointCount = connectionPoints.Where(connectionPoint => connectionPoint.isOutput).ToList().Count;
			if (1 < outputPointCount) {
				this.baseRect = new Rect(baseRect.x, baseRect.y, baseRect.width, AssetGraphGUISettings.NODE_BASE_HEIGHT + (AssetGraphGUISettings.FILTER_OUTPUT_SPAN * (outputPointCount - 1)));
			} else {
				this.baseRect = new Rect(baseRect.x, baseRect.y, baseRect.width, AssetGraphGUISettings.NODE_BASE_HEIGHT);
			}

			UpdateNodeRect();
		}

		public List<ConnectionPoint> DuplicateConnectionPoints () {
			var copiedConnectionList = new List<ConnectionPoint>();
			foreach (var connectionPoint in connectionPoints) {
				if (connectionPoint.isOutput) copiedConnectionList.Add(new OutputPoint(connectionPoint.label));
				if (connectionPoint.isInput) copiedConnectionList.Add(new InputPoint(connectionPoint.label));
			}
			return copiedConnectionList;
		}

		private void RefreshConnectionPos () {
			var inputPoints = connectionPoints.Where(p => p.isInput).ToList();
			var outputPoints = connectionPoints.Where(p => p.isOutput).ToList();

			for (int i = 0; i < inputPoints.Count; i++) {
				var point = inputPoints[i];
				point.UpdatePos(i, inputPoints.Count, baseRect.width, baseRect.height);
			}

			for (int i = 0; i < outputPoints.Count; i++) {
				var point = outputPoints[i];
				point.UpdatePos(i, outputPoints.Count, baseRect.width, baseRect.height);
			}
		}

		public List<string> OutputPointLabels () {
			return connectionPoints
						.Where(p => p.isOutput)
						.Select(p => p.label)
						.ToList();
		}

		public ConnectionPoint ConnectionPointFromLabel (string label) {
			var targetPoints = connectionPoints.Where(con => con.label == label).ToList();
			if (!targetPoints.Any()) {
				Debug.LogError("no connection label:" + label + " exists in node name:" + name);
				return null;
			}
			return targetPoints[0];
		}

		public void DrawNode () {
			baseRect = GUI.Window(nodeWindowId, baseRect, UpdateNodeEvent, string.Empty, nodeInterfaceTypeStr);
		}

		/**
			retrieve GUI events for this node.
		*/
		private void UpdateNodeEvent (int id) {
			switch (Event.current.type) {

				/*
					handling release of mouse drag from this node to another node.
					this node doesn't know about where the other node is. the master only knows.
					only emit event.
				*/
				case EventType.Ignore: {
					Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_CONNECTION_OVERED, this, Event.current.mousePosition, null));
					break;
				}

				/*
					handling release of mouse drag on this node.
				*/
				case EventType.MouseUp: {
					// if mouse position is on the connection point, emit mouse raised event over thr connection.
					foreach (var connectionPoint in connectionPoints) {
						var globalConnectonPointRect = new Rect(connectionPoint.buttonRect.x, connectionPoint.buttonRect.y, connectionPoint.buttonRect.width, connectionPoint.buttonRect.height);
						if (globalConnectonPointRect.Contains(Event.current.mousePosition)) {
							Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_CONNECTION_RAISED, this, Event.current.mousePosition, connectionPoint));
							return;
						}
					}

					Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_TATCHED, this, Event.current.mousePosition, null));
					break;
				}

				/*
					handling drag.
				*/
				case EventType.MouseDrag: {
					Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_MOVING, this, Event.current.mousePosition, null));
					break;
				}

				/*
					check if the mouse-down point is over one of the connectionPoint in this node.
					then emit event.
				*/
				case EventType.MouseDown: {
					var result = IsOverConnectionPoint(connectionPoints, Event.current.mousePosition);

					if (result != null) {
						Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_CONNECT_STARTED, this, Event.current.mousePosition, result));
						break;
					}
					break;
				}
			}

			// draw & update connectionPoint button interface.
			foreach (var point in connectionPoints) {
				switch (this.kind) {
					case AssetGraphSettings.NodeKind.FILTER_SCRIPT:
					case AssetGraphSettings.NodeKind.FILTER_GUI: {
						var label = point.label;
						var labelRect = new Rect(point.buttonRect.x - baseRect.width, point.buttonRect.y - (point.buttonRect.height/2), baseRect.width, point.buttonRect.height*2);

						var style = EditorStyles.label;
						var defaultAlignment = style.alignment;
						style.alignment = TextAnchor.MiddleRight;
						GUI.Label(labelRect, label, style);
						style.alignment = defaultAlignment;
						break;
					}
				}


				if (point.isInput) {
					// var activeFrameLabel = new GUIStyle("AnimationKeyframeBackground");// そのうちやる。
					// activeFrameLabel.backgroundColor = Color.clear;
					// Debug.LogError("contentOffset"+ activeFrameLabel.contentOffset);
					// Debug.LogError("contentOffset"+ activeFrameLabel.Button);

					GUI.backgroundColor = Color.clear;
					GUI.Button(point.buttonRect, inputPointTex, "AnimationKeyframeBackground");
				}

				if (point.isOutput) {
					GUI.backgroundColor = Color.clear;
					GUI.Button(point.buttonRect, outputPointTex, "AnimationKeyframeBackground");
				}
			}

			/*
				right click.
			*/
			if (
				Event.current.type == EventType.ContextClick
				 || (Event.current.type == EventType.MouseUp && Event.current.button == 1)
			) {
				var rightClickPos = Event.current.mousePosition;
				var menu = new GenericMenu();
				menu.AddItem(
					new GUIContent("Delete All Input Connections"),
					false, 
					() => {
						Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_DELETE_ALL_INPUT_CONNECTIONS, this, rightClickPos, null));
					}
				);
				menu.AddItem(
					new GUIContent("Delete All Output Connections"),
					false, 
					() => {
						Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_DELETE_ALL_OUTPUT_CONNECTIONS, this, rightClickPos, null));
					}
				);
				menu.AddItem(
					new GUIContent("Duplicate"),
					false, 
					() => {
						Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_DUPLICATE_TAPPED, this, rightClickPos, null));
					}
				);
				menu.AddItem(
					new GUIContent("Delete"),
					false, 
					() => {
						Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_CLOSE_TAPPED, this, Vector2.zero, null));
					}
				);
				menu.ShowAsContext();
				Event.current.Use();
			}


			DrawNodeContents();

			GUI.DragWindow();
		}

		public void DrawConnectionInputPointMark (OnNodeEvent eventSource, bool justConnecting) {
			var defaultPointTex = inputPointMarkTex;

			if (justConnecting && eventSource != null) {
				if (eventSource.eventSourceNode.nodeId != this.nodeId) {
					if (eventSource.eventSourceConnectionPoint.isOutput) {
						defaultPointTex = enablePointMarkTex;
					}
				}
			}

			foreach (var point in connectionPoints) {
				if (point.isInput) {
					GUI.DrawTexture(
						new Rect(
							baseRect.x - 2f, 
							baseRect.y + (baseRect.height - AssetGraphGUISettings.CONNECTION_POINT_MARK_SIZE)/2f, 
							AssetGraphGUISettings.CONNECTION_POINT_MARK_SIZE, 
							AssetGraphGUISettings.CONNECTION_POINT_MARK_SIZE
						), 
						defaultPointTex
					);
				}
			}
		}

		public void DrawConnectionOutputPointMark (OnNodeEvent eventSource, bool justConnecting, Event current) {
			var defaultPointTex = outputPointMarkConnectedTex;
			
			if (justConnecting && eventSource != null) {
				if (eventSource.eventSourceNode.nodeId != this.nodeId) {
					if (eventSource.eventSourceConnectionPoint.isInput) {
						defaultPointTex = enablePointMarkTex;
					}
				}
			}

			var globalMousePosition = current.mousePosition;
			
			foreach (var point in connectionPoints) {
				if (point.isOutput) {
					var outputPointRect = OutputRect(point);

					GUI.DrawTexture(
						outputPointRect, 
						defaultPointTex
					);

					// eventPosition is contained by outputPointRect.
					if (outputPointRect.Contains(globalMousePosition)) {
						if (current.type == EventType.MouseDown) {
							Emit(new OnNodeEvent(OnNodeEvent.EventType.EVENT_NODE_CONNECT_STARTED, this, current.mousePosition, point));
						}
					}
				}
			}
		}

		private Rect OutputRect (ConnectionPoint outputPoint) {
			return new Rect(
				baseRect.x + baseRect.width - 8f, 
				baseRect.y + outputPoint.buttonRect.y + 1f, 
				AssetGraphGUISettings.CONNECTION_POINT_MARK_SIZE, 
				AssetGraphGUISettings.CONNECTION_POINT_MARK_SIZE
			);
		}

		private void DrawNodeContents () {
			var style = EditorStyles.label;
			var defaultAlignment = style.alignment;
			style.alignment = TextAnchor.MiddleCenter;
			

			var nodeTitleRect = new Rect(0, 0, baseRect.width, baseRect.height);
			if (this.kind == AssetGraphSettings.NodeKind.PREFABRICATOR_GUI) GUI.contentColor = Color.black;
			if (this.kind == AssetGraphSettings.NodeKind.PREFABRICATOR_SCRIPT) GUI.contentColor = Color.black; 
			GUI.Label(nodeTitleRect, name, style);

			if (running) EditorGUI.ProgressBar(new Rect(10f, baseRect.height - 20f, baseRect.width - 20f, 10f), progress, string.Empty);

			style.alignment = defaultAlignment;
		}

		public void UpdateNodeRect () {
			var contentWidth = this.name.Length;
			if (this.kind == AssetGraphSettings.NodeKind.FILTER_GUI) {
				var longestFilterLengths = connectionPoints.OrderByDescending(con => con.label.Length).Select(con => con.label.Length).ToList();
				if (longestFilterLengths.Any()) {
					contentWidth = contentWidth + longestFilterLengths[0];
				}
			}

			var newWidth = contentWidth * 12f;
			if (newWidth < AssetGraphGUISettings.NODE_BASE_WIDTH) newWidth = AssetGraphGUISettings.NODE_BASE_WIDTH;
			baseRect = new Rect(baseRect.x, baseRect.y, newWidth, baseRect.height);

			RefreshConnectionPos();
		}

		private ConnectionPoint IsOverConnectionPoint (List<ConnectionPoint> points, Vector2 touchedPoint) {
			foreach (var p in points) {
				if (p.buttonRect.x <= touchedPoint.x && 
					touchedPoint.x <= p.buttonRect.x + p.buttonRect.width && 
					p.buttonRect.y <= touchedPoint.y && 
					touchedPoint.y <= p.buttonRect.y + p.buttonRect.height
				) {
					return p;
				}
			}
			
			return null;
		}

		public Rect GetRect () {
			return baseRect;
		}

		public Vector2 GetPos () {
			return baseRect.position;
		}

		public int GetX () {
			return (int)baseRect.x;
		}

		public int GetY () {
			return (int)baseRect.y;
		}

		public int GetRightPos () {
			return (int)(baseRect.x + baseRect.width);
		}

		public int GetBottomPos () {
			return (int)(baseRect.y + baseRect.height);
		}

		public void SetPos (Vector2 position) {
			baseRect.position = position;
		}

		public void SetProgress (float val) {
			progress = val;
		}

		public void MoveRelative (Vector2 diff) {
			baseRect.position = baseRect.position - diff;
		}

		public void ShowProgress () {
			running = true;
		}

		public void HideProgress () {
			running = false;
		}

		public bool ConitainsGlobalPos (Vector2 globalPos) {
			if (baseRect.Contains(globalPos)) {
				return true;
			}

			foreach (var connectionPoint in connectionPoints) {
				if (connectionPoint.isOutput) {
					var outputRect = OutputRect(connectionPoint);
					if (outputRect.Contains(globalPos)) {
						return true;
					}
				}
			}

			return false;
		}

		public Vector2 GlobalConnectionPointPosition(ConnectionPoint p) {
			var x = 0f;
			var y = 0f;

			if (p.isInput) {
				x = baseRect.x;
				y = baseRect.y + p.buttonRect.y + (p.buttonRect.height / 2f) - 1f;
			}

			if (p.isOutput) {
				x = baseRect.x + baseRect.width;
				y = baseRect.y + p.buttonRect.y + (p.buttonRect.height / 2f) - 1f;
			}

			return new Vector2(x, y);
		}

		public List<ConnectionPoint> ConnectionPointUnderGlobalPos (Vector2 globalPos) {
			var containedPoints = new List<ConnectionPoint>();

			foreach (var connectionPoint in connectionPoints) {
				var grobalConnectionPointRect = new Rect(
					baseRect.x + connectionPoint.buttonRect.x,
					baseRect.y + connectionPoint.buttonRect.y,
					connectionPoint.buttonRect.width,
					connectionPoint.buttonRect.height
				);

				if (grobalConnectionPointRect.Contains(globalPos)) containedPoints.Add(connectionPoint);
				if (connectionPoint.isOutput) {
					var outputRect = OutputRect(connectionPoint);
					if (outputRect.Contains(globalPos)) containedPoints.Add(connectionPoint);
				}
			}
			
			return containedPoints;
		}

		public static void ShowPackageMenu (Action NoneSelected, Action<string> ExistSelected, List<string> packages) {
			List<string> packageList = new List<string>();
			
			packageList.Add(AssetGraphSettings.PLATFORM_PACKAGE_DEFAULT_NAME);

			// delim
			packageList.Add(string.Empty);

			packageList.AddRange(packages);

			// delim
			packageList.Add(string.Empty);

			var menu = new GenericMenu();

			for (var i = 0; i < packageList.Count; i++) {
				var packageName = packageList[i];
				switch (i) {
					case 0: {
						menu.AddItem(
							new GUIContent(packageName), 
							false, // check
							() => NoneSelected()
						);
						continue;
					}
					default: {
						menu.AddItem(
							new GUIContent(packageName), 
							false, // check
							() => {
								ExistSelected(packageName);
							}
						);
						break;
					}
				}
			}
			
			menu.ShowAsContext();
		}
	}
}