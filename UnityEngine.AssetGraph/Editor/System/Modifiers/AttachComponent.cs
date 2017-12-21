using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine.AssetGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

[CustomModifier("Attach Component", typeof(GameObject))]
public class AttachComponent : IModifier {

    enum AttachPolicy {
        RootObject = 1,
        MiddleObject = 2,
        LeafObject = 4
    }

    [SerializeField] private SerializedComponent m_component;
    [SerializeField] private AttachPolicy m_attachPolicy = AttachPolicy.RootObject | AttachPolicy.MiddleObject | AttachPolicy.LeafObject;
    [SerializeField] private string m_nameFormat;

    private int m_selectedIndex = -1;
    private Texture2D m_popupIcon;
    private Texture2D m_helpIcon;

    public void OnValidate () {
    }

	// Test if asset is different from intended configuration 
	public bool IsModified (UnityEngine.Object[] assets, List<AssetReference> group) {

        if (m_component == null) {
            return false;
        }

        if (m_component.IsInvalidated) {
            m_component.Restore ();
        }

        return assets.Where (a => a is GameObject).Any ();
	}

	// Actually change asset configurations. 
	public void Modify (UnityEngine.Object[] assets, List<AssetReference> group) {

        Regex r = new Regex(m_nameFormat);
        bool isRootObjTargeting = (m_attachPolicy & AttachPolicy.RootObject) > 0;
        bool isLeafObjTargeting = (m_attachPolicy & AttachPolicy.LeafObject) > 0;
        bool isMiddleObjTargeting = (m_attachPolicy & AttachPolicy.MiddleObject) > 0;

        foreach (var o in assets) {
            GameObject go = o as GameObject;
            if (go == null) {
                continue;
            }

            if (!r.IsMatch (go.name)) {
                continue;
            }

            bool isRootObj = go.transform.parent == null;
            bool isLeafObj = go.transform.childCount == 0;
            bool isMiddleObj = !isRootObj && !isLeafObj;

            bool isTargeting = 
                (isRootObj && isRootObjTargeting) ||
                (isLeafObj && isLeafObjTargeting) ||
                (isMiddleObj && isMiddleObjTargeting);

            if (!isTargeting) {
                continue;
            }

            foreach (var info in m_component.Components) {
                var dst = go.GetComponent (info.ComponentType);
                if (dst == null) {
                    dst = go.AddComponent (info.ComponentType);
                }

                if (dst != null) {
                    EditorUtility.CopySerialized (info.Component, dst);
                }
            }
        }
	}

    private void DrawComponentHeader(SerializedComponent.ComponentInfo info) {

        if (m_popupIcon == null) {
            m_popupIcon = EditorGUIUtility.Load ("icons/_Popup.png") as Texture2D;
        }

        if (m_helpIcon == null) {
            m_helpIcon = EditorGUIUtility.Load ("icons/_Help.png") as Texture2D;
        }

        GUILayout.Box("", new GUILayoutOption[]{GUILayout.ExpandWidth(true), GUILayout.Height(1)});
        using (new EditorGUILayout.HorizontalScope ()) {
            var thumbnail = AssetPreview.GetMiniTypeThumbnail (info.ComponentType);
            if (thumbnail == null) {
                if (typeof(MonoBehaviour).IsAssignableFrom (info.ComponentType)) {
                    thumbnail = AssetPreview.GetMiniTypeThumbnail (typeof(MonoScript));
                } else {
                    thumbnail = AssetPreview.GetMiniTypeThumbnail (typeof(UnityEngine.Object));
                }
            }

            GUILayout.Label(thumbnail, GUILayout.Width(32f), GUILayout.Height(32f));
            GUILayout.Label (info.ComponentType.Name, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace ();

            if (Help.HasHelpForObject (info.Component)) {
                var tooltip = string.Format ("Open Reference for {0}.", info.ComponentType.Name);
                if(GUILayout.Button(new GUIContent(m_helpIcon, tooltip), EditorStyles.miniLabel, GUILayout.Width(18f), GUILayout.Height(24f))) {
                    Help.ShowHelpForObject (info.Component);
                }
            }

            if(GUILayout.Button(m_popupIcon, EditorStyles.miniLabel, GUILayout.Width(18f), GUILayout.Height(24f))) {
                GenericMenu m = new GenericMenu ();
                m.AddItem (new GUIContent ("Copy Component"), false, () => {
                    UnityEditorInternal.ComponentUtility.CopyComponent(info.Component);
                });

                var pasteLabel = new GUIContent ("Paste Component Values");
                m.AddItem (pasteLabel, false, () => {
                    UnityEditorInternal.ComponentUtility.PasteComponentValues(info.Component);
                });

                MonoScript s = TypeUtility.LoadMonoScript(info.ComponentType.AssemblyQualifiedName);
                if(s != null) {
                    m.AddSeparator ("");
                    m.AddItem(
                        new GUIContent("Edit Script"),
                        false, 
                        () => {
                            AssetDatabase.OpenAsset(s, 0);
                        }
                    );
                }

                m.ShowAsContext ();
            }
        }
        GUILayout.Space (4f);
    }

	// Draw inspector gui 
	public void OnInspectorGUI (Action onValueChanged) {

        if (m_component == null) {
            m_component = new SerializedComponent ();
        }

        if (m_component.IsInvalidated) {
            m_component.Restore ();
        }

        #if UNITY_2017_3_OR_NEWER
        var newAttachPolicy = (AttachPolicy)EditorGUILayout.EnumFlagsField ("Attach Policy", m_attachPolicy);
        #else
        var newAttachPolicy = (AttachPolicy)EditorGUILayout.EnumMaskField ("Attach Policy", m_attachPolicy);
        #endif
        if(newAttachPolicy != m_attachPolicy) {
            m_attachPolicy = newAttachPolicy;
            onValueChanged();
        }

        var newNameFormat = EditorGUILayout.TextField ("Name Pattern", m_nameFormat);
        if(newNameFormat != m_nameFormat) {
            m_nameFormat = newNameFormat;
            onValueChanged();
        }

        using (new EditorGUILayout.HorizontalScope ()) {
            m_selectedIndex = EditorGUILayout.Popup ("Component", m_selectedIndex, ComponentMenuUtility.GetComponentNames ());

            using (new EditorGUI.DisabledScope (m_selectedIndex < 0)) {
                if (GUILayout.Button ("Add", GUILayout.Width(40))) {
                    var type = ComponentMenuUtility.GetComponentTypes()[m_selectedIndex];

                    var c = m_component.AddComponent (type);
                    if (c != null) {
                        m_component.Save ();
                        onValueChanged ();
                    }
                }
            }
        }

        EditorGUI.BeginChangeCheck ();

        SerializedComponent.ComponentInfo removingItem = null;

        foreach (var info in m_component.Components) {
            if (info.Editor != null) {
                DrawComponentHeader (info);
                GUILayout.BeginHorizontal();
                GUILayout.Space(16f);
                GUILayout.BeginVertical();
                info.Editor.DrawDefaultInspector ();
                GUILayout.EndVertical ();
                GUILayout.EndHorizontal ();
                using (new EditorGUILayout.HorizontalScope ()) {
                    GUILayout.FlexibleSpace ();
                    if (GUILayout.Button ("Remove", GUILayout.Width(60))) {
                        removingItem = info;
                    }
                }
            }
        }

        if (removingItem != null) {
            m_component.RemoveComponent (removingItem);
        }

        if (EditorGUI.EndChangeCheck ()) {
            m_component.Save ();
            onValueChanged ();
        }
	}
}
