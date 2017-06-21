using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

using V1=AssetBundleGraph;
using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {

	[CustomNode("Modify Assets/Modify Assets Directly", 61)]
	public class Modifier : Node, Model.NodeDataImporter {

		[SerializeField] private SerializableMultiTargetInstance m_instance;

		public override string ActiveStyle {
			get {
				return "node 8 on";
			}
		}

		public override string InactiveStyle {
			get {
				return "node 8";
			}
		}

		public override string Category {
			get {
				return "Modify";
			}
		}

		public override void Initialize(Model.NodeData data) {
			m_instance = new SerializableMultiTargetInstance();

			data.AddDefaultInputPoint();
			data.AddDefaultOutputPoint();
		}

		public void Import(V1.NodeData v1, Model.NodeData v2) {
			m_instance = new SerializableMultiTargetInstance(v1.ScriptClassName, v1.InstanceData);
		}

		public override Node Clone(Model.NodeData newData) {
			var newNode = new Modifier();
			newNode.m_instance = new SerializableMultiTargetInstance(m_instance);

			newData.AddDefaultInputPoint();
			newData.AddDefaultOutputPoint();
			return newNode;
		}

		public override void OnInspectorGUI(NodeGUI node, AssetReferenceStreamManager streamManager, NodeGUIEditor editor, Action onValueChanged) {

			EditorGUILayout.HelpBox("Modify Assets Directly: Modify assets.", MessageType.Info);
			editor.UpdateNodeName(node);

			GUILayout.Space(10f);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {

				Type incomingType = TypeUtility.FindFirstIncomingAssetType(streamManager, node.Data.InputPoints[0]);

				var modifier = m_instance.Get<IModifier>(editor.CurrentEditingGroup);

				if(incomingType == null) {
					// if there is no asset input to determine incomingType,
					// retrieve from assigned Modifier.
					incomingType = ModifierUtility.GetModifierTargetType(m_instance.ClassName);

					if(incomingType == null) {
						EditorGUILayout.HelpBox("Modifier needs a single type from incoming assets.", MessageType.Info);
						return;
					}
				}

				Dictionary<string, string> map = null;

				if(incomingType != null) {
					map = ModifierUtility.GetAttributeAssemblyQualifiedNameMap(incomingType);
				}

				if(map != null  && map.Count > 0) {
					using(new GUILayout.HorizontalScope()) {
						GUILayout.Label("Modifier");
						var guiName = ModifierUtility.GetModifierGUIName(m_instance.ClassName);
						if (GUILayout.Button(guiName, "Popup", GUILayout.MinWidth(150f))) {
							var builders = map.Keys.ToList();

							if(builders.Count > 0) {
								NodeGUI.ShowTypeNamesMenu(guiName, builders, (string selectedGUIName) => 
									{
										using(new RecordUndoScope("Change Modifier class", node, true)) {
											modifier = ModifierUtility.CreateModifier(selectedGUIName, incomingType);
											m_instance.Set(editor.CurrentEditingGroup,modifier);
											onValueChanged();
										}
									}  
								);
							}
						}

						MonoScript s = TypeUtility.LoadMonoScript(m_instance.ClassName);

						using(new EditorGUI.DisabledScope(s == null)) {
							if(GUILayout.Button("Edit", GUILayout.Width(50))) {
								AssetDatabase.OpenAsset(s, 0);
							}
						}
					}

				} else {

					string[] menuNames = Model.Settings.GUI_TEXT_MENU_GENERATE_MODIFIER.Split('/');

					if (incomingType == null) {
						EditorGUILayout.HelpBox(
							string.Format(
								"You need to create at least one Modifier script to select script for Modifier. " +
								"To start, select {0}>{1}>{2} menu and create a new script.",
								menuNames[1],menuNames[2], menuNames[3]
							), MessageType.Info);
					} else {
						EditorGUILayout.HelpBox(
							string.Format(
								"No CustomModifier found for {3} type. \n" +
								"You need to create at least one Modifier script to select script for Modifier. " +
								"To start, select {0}>{1}>{2} menu and create a new script.",
								menuNames[1],menuNames[2], menuNames[3], incomingType.FullName
							), MessageType.Info);
					}
				}

				GUILayout.Space(10f);

				editor.DrawPlatformSelector(node);
				using (new EditorGUILayout.VerticalScope()) {
					var disabledScope = editor.DrawOverrideTargetToggle(node, m_instance.ContainsValueOf(editor.CurrentEditingGroup), (bool enabled) => {
						if(enabled) {
							m_instance.CopyDefaultValueTo(editor.CurrentEditingGroup);
						} else {
							m_instance.Remove(editor.CurrentEditingGroup);
						}
						onValueChanged();
					});

					using (disabledScope) {
						if (modifier != null) {
							Action onChangedAction = () => {
								using(new RecordUndoScope("Change Modifier Setting", node)) {
									m_instance.Set(editor.CurrentEditingGroup, modifier);
									onValueChanged();
								}
							};

							modifier.OnInspectorGUI(onChangedAction);
						}
					}
				}
			}
		}
			
		public override void OnContextMenuGUI(GenericMenu menu) {
			MonoScript s = TypeUtility.LoadMonoScript(m_instance.ClassName);
			if(s != null) {
				menu.AddItem(
					new GUIContent("Edit Script"),
					false, 
					() => {
						AssetDatabase.OpenAsset(s, 0);
					}
				);
			}
		}

		public override void Prepare (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output) 
		{
			ValidateModifier(node, target, incoming,
				(Type expectedType, Type foundType, AssetReference foundAsset) => {
					throw new NodeException(string.Format("{3} :Modifier expect {0}, but different type of incoming asset is found({1} {2})", 
						expectedType.FullName, foundType.FullName, foundAsset.fileNameAndExtension, node.Name), node.Id);
				},
				() => {
					throw new NodeException(node.Name + " :Modifier is not configured. Please configure from Inspector.", node.Id);
				},
				() => {
					throw new NodeException(node.Name + " :Failed to create Modifier from settings. Please fix settings from Inspector.", node.Id);
				},
				(Type expectedType, Type incomingType) => {
					throw new NodeException(string.Format("{0} :Incoming asset type is does not match with this Modifier (Expected type:{1}, Incoming type:{2}).",
						node.Name, (expectedType != null)?expectedType.FullName:"null", (incomingType != null)?incomingType.FullName:"null"), node.Id);
				}
			);


			if(incoming != null && Output != null) {
				// Modifier does not add, filter or change structure of group, so just pass given group of assets
				var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
					null : connectionsToOutput.First();

				foreach(var ag in incoming) {
					Output(dst, ag.assetGroups);
				}
			}
		}

		
		public override void Build (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output,
			Action<Model.NodeData, string, float> progressFunc) 
		{
			if(incoming == null) {
				return;
			}
			var modifier = m_instance.Get<IModifier>(target);
			UnityEngine.Assertions.Assert.IsNotNull(modifier);
			bool isAnyAssetModified = false;

			foreach(var ag in incoming) {
				foreach(var assets in ag.assetGroups.Values) {
					foreach(var asset in assets) {
						if(modifier.IsModified(asset.allData)) {
							modifier.Modify(asset.allData);
							asset.SetDirty();
							isAnyAssetModified = true;
						}
					}
				}
			}

			if(isAnyAssetModified) {
				// apply asset setting changes to AssetDatabase.
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}

			foreach(var ag in incoming) {
				foreach(var assets in ag.assetGroups.Values) {
					foreach(var asset in assets) {
						asset.ReleaseData();
					}
				}
			}

			if(incoming != null && Output != null) {
				// Modifier does not add, filter or change structure of group, so just pass given group of assets
				var dst = (connectionsToOutput == null || !connectionsToOutput.Any())? 
					null : connectionsToOutput.First();

				foreach(var ag in incoming) {
					Output(dst, ag.assetGroups);
				}
			}
		}
			
		public void ValidateModifier (
			Model.NodeData node,
			BuildTarget target,
			IEnumerable<PerformGraph.AssetGroups> incoming,
			Action<Type, Type, AssetReference> multipleAssetTypeFound,
			Action noModiferData,
			Action failedToCreateModifier,
			Action<Type, Type> incomingTypeMismatch
		) {
			Type expectedType = null;
			if(incoming != null) {
				expectedType = TypeUtility.FindFirstIncomingAssetType(incoming);
				if(expectedType != null) {
					foreach(var ag in incoming) {
						foreach(var assets in ag.assetGroups.Values) {
							foreach(var a in assets) {
								Type assetType = a.filterType;
								if(assetType != expectedType) {
									multipleAssetTypeFound(expectedType, assetType, a);
								}
							}
						}
					}
				}
			}

//			var modifier = ModifierUtility.CreateModifier(node, target);
//
			if(m_instance.Get<IModifier>(target) == null) {
				failedToCreateModifier();
			}

			// if there is no incoming assets, there is no way to check if 
			// right type of asset is coming in - so we'll just skip the test
			// expectedType is not null when there is at least one incoming asset
			if(incoming != null && expectedType != null) {
				var targetType = ModifierUtility.GetModifierTargetType(m_instance.Get<IModifier>(target));
				if( targetType != expectedType ) {
					incomingTypeMismatch(targetType, expectedType);
				}
			}
		}			
	}
}
