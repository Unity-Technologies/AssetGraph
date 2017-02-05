using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

using Model=UnityEngine.AssetBundles.GraphTool.DataModel.Version2;

namespace UnityEngine.AssetBundles.GraphTool {

	[CustomNode("Modifier", 40)]
	public class Modifier : INode {

		[SerializeField] private MultiTargetSerializedInstance<IModifier> m_instance;

		public string ActiveStyle {
			get {
				return string.Empty;
			}
		}

		public string InactiveStyle {
			get {
				return string.Empty;
			}
		}

		public void Initialize(Model.NodeData data) {
		}

		public INode Clone() {
			return null;
		}

		public bool Validate(List<Model.NodeData> allNodes, List<Model.ConnectionData> allConnections) {
			return false;
		}

		public bool IsEqual(INode node) {
			return false;
		}

		public string Serialize() {
			return string.Empty;
		}

		public bool IsValidInputConnectionPoint(Model.ConnectionPointData point) {
			return false;
		}

		public bool CanConnectFrom(INode fromNode) {
			return false;
		}

		public bool OnAssetsReimported(BuildTarget target, 
			string[] importedAssets, 
			string[] deletedAssets, 
			string[] movedAssets, 
			string[] movedFromAssetPaths)
		{
			return false;
		}

		public void OnNodeGUI(NodeGUI node) {
		}

		public void OnInspectorGUI (NodeGUI node, NodeGUIEditor editor) {
			
			EditorGUILayout.HelpBox("Modifier: Modify asset settings.", MessageType.Info);
			editor.UpdateNodeName(node);

			GUILayout.Space(10f);

			using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {

				Type incomingType = TypeUtility.FindFirstIncomingAssetType(node.Data.InputPoints[0]);

				var modifier = m_instance[editor.CurrentEditingGroup];

				if(incomingType == null) {
					// if there is no asset input to determine incomingType,
					// retrieve from assigned Modifier.
					incomingType = ModifierUtility.GetModifierTargetType(modifier.ClassName);

					if(incomingType == null) {
						EditorGUILayout.HelpBox("Modifier needs a single type of incoming assets.", MessageType.Info);
						return;
					}
				}

				var map = ModifierUtility.GetAttributeClassNameMap(incomingType);
				if(map.Count > 0) {
					using(new GUILayout.HorizontalScope()) {
						GUILayout.Label("Modifier");
						var guiName = ModifierUtility.GetModifierGUIName(modifier.ClassName);
						if (GUILayout.Button(guiName, "Popup", GUILayout.MinWidth(150f))) {
							var builders = map.Keys.ToList();

							if(builders.Count > 0) {
								NodeGUI.ShowTypeNamesMenu(guiName, builders, (string selectedGUIName) => 
									{
										using(new RecordUndoScope("Change Modifier class", node, true)) {
											var newModifier = ModifierUtility.CreateModifier(selectedGUIName, incomingType);
											if(newModifier != null) {
												modifier = new SerializedInstance<IModifier>(newModifier);
												m_instance[editor.CurrentEditingGroup] = modifier;
											}
										}
									}  
								);
							}
						}

						MonoScript s = TypeUtility.LoadMonoScript(modifier.ClassName);

						using(new EditorGUI.DisabledScope(s == null)) {
							if(GUILayout.Button("Edit", GUILayout.Width(50))) {
								AssetDatabase.OpenAsset(s, 0);
							}
						}
					}

				} else {
					string[] menuNames = Model.Settings.GUI_TEXT_MENU_GENERATE_MODIFIER.Split('/');
					EditorGUILayout.HelpBox(
						string.Format(
							"No CustomModifier found for {3} type. \n" +
							"You need to create at least one Modifier script to select script for Modifier. " +
							"To start, select {0}>{1}>{2} menu and create a new script.",
							menuNames[1],menuNames[2], menuNames[3], incomingType.FullName
						), MessageType.Info);
				}

				GUILayout.Space(10f);

				if(editor.DrawPlatformSelector(node)) {
					// if platform tab is changed, renew modifierModifierInstance for that tab.
					// m_modifier = null;
				}
				using (new EditorGUILayout.VerticalScope()) {
					var disabledScope = editor.DrawOverrideTargetToggle(node, m_instance.ContainsValueOf(editor.CurrentEditingGroup), (bool enabled) => {
						if(enabled) {
							m_instance[editor.CurrentEditingGroup] = m_instance.DefaultValue;
						} else {
							m_instance.Remove(editor.CurrentEditingGroup);
						}
//						m_modifier = null;						
					});

					using (disabledScope) {
						//reload modifierModifier instance from saved modifierModifier data.
//						if (modifier.Object == null) {
//							m_modifier = ModifierUtility.CreateModifier(node.Data, editor.CurrentEditingGroup);
//							if(m_modifier != null) {
//								m_className = m_modifier.GetType().FullName;
//								if(m_instanceData.ContainsValueOf(editor.CurrentEditingGroup)) {
//									m_instanceData[editor.CurrentEditingGroup] = m_modifier.Serialize();
//								}
//							}
//						}

						if (modifier.Object != null) {
							Action onChangedAction = () => {
								using(new RecordUndoScope("Change Modifier Setting", node)) {
									modifier.Save();
//									m_className = m_modifier.GetType().FullName;
//									if(m_instanceData.ContainsValueOf(editor.CurrentEditingGroup)) {
//										m_instanceData[editor.CurrentEditingGroup] = m_modifier.Serialize();
//									}
								}
							};

							modifier.Object.OnInspectorGUI(onChangedAction);
						}
					}
				}
			}
		}

		public void Prepare (BuildTarget target, 
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

		
		public void Build (BuildTarget target, 
			Model.NodeData node, 
			IEnumerable<PerformGraph.AssetGroups> incoming, 
			IEnumerable<Model.ConnectionData> connectionsToOutput, 
			PerformGraph.Output Output,
			Action<Model.NodeData, string, float> progressFunc) 
		{
			if(incoming == null) {
				return;
			}
			var modifier = m_instance[target].Object;
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

			if(!m_instance.ContainsValueOf(BuildTargetUtility.TargetToGroup(target))) {
				noModiferData();
			}

//			var modifier = ModifierUtility.CreateModifier(node, target);
//
			if(m_instance[target].Object == null) {
				failedToCreateModifier();
			}

			// if there is no incoming assets, there is no way to check if 
			// right type of asset is coming in - so we'll just skip the test
			// expectedType is not null when there is at least one incoming asset
			if(incoming != null && expectedType != null) {
				var targetType = ModifierUtility.GetModifierTargetType(m_instance[target].Object);
				if( targetType != expectedType ) {
					incomingTypeMismatch(targetType, expectedType);
				}
			}
		}			
	}
}
