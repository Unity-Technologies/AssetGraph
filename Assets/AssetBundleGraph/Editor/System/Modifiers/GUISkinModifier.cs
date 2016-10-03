using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetBundleGraph.Modifiers {
	
	[Serializable] 
	[CustomModifier("Default Modifier(GUISkin)", typeof(GUISkin))]
	public class GUISkinModifier : IModifier {
		
		public GUISkinModifier () {}

		public bool IsModified (object asset) {
			//var animation = asset as Animation;

			//TODO: implement this
			var changed = false;
			return changed; 
		}

		public void Modify (object asset) {
			var guiSkin = asset as GUISkin;
			/*
box	Style used by default for GUI.Box controls.
button	Style used by default for GUI.Button controls.
customStyles	Array of GUI styles for specific needs.
font	The default font to use for all styles.
horizontalScrollbar	Style used by default for the background part of GUI.HorizontalScrollbar controls.
horizontalScrollbarLeftButton	Style used by default for the left button on GUI.HorizontalScrollbar controls.
horizontalScrollbarRightButton	Style used by default for the right button on GUI.HorizontalScrollbar controls.
horizontalScrollbarThumb	Style used by default for the thumb that is dragged in GUI.HorizontalScrollbar controls.
horizontalSlider	Style used by default for the background part of GUI.HorizontalSlider controls.
horizontalSliderThumb	Style used by default for the thumb that is dragged in GUI.HorizontalSlider controls.
label	Style used by default for GUI.Label controls.
scrollView	Style used by default for the background of ScrollView controls (see GUI.BeginScrollView).
settings	Generic settings for how controls should behave with this skin.
textArea	Style used by default for GUI.TextArea controls.
textField	Style used by default for GUI.TextField controls.
toggle	Style used by default for GUI.Toggle controls.
verticalScrollbar	Style used by default for the background part of GUI.VerticalScrollbar controls.
verticalScrollbarDownButton	Style used by default for the down button on GUI.VerticalScrollbar controls.
verticalScrollbarThumb	Style used by default for the thumb that is dragged in GUI.VerticalScrollbar controls.
verticalScrollbarUpButton	Style used by default for the up button on GUI.VerticalScrollbar controls.
verticalSlider	Style used by default for the background part of GUI.VerticalSlider controls.
verticalSliderThumb	Style used by default for the thumb that is dragged in GUI.VerticalSlider controls.
window	Style used by default for Window controls (SA GUI.Window).
			*/
			guiSkin.box = GUI.skin.box;

		}

		public void OnInspectorGUI (Action onValueChanged) {
			//TODO: implement this
			GUILayout.Label("GUISkinModifier inspector.");
		}

		public string Serialize() {
			return JsonUtility.ToJson(this);
		}
	}
}