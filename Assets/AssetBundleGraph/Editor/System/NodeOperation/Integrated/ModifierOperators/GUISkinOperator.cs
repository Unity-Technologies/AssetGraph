using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetBundleGraph.ModifierOperators {
	
	[Serializable] public class GUISkinOperator : Modifier {
		
		public GUISkinOperator () {}

		private GUISkinOperator (
			string operatorType
		) {
			this.operatorType = operatorType;
		}

		/*
			constructor for default data setting.
		*/
		public override Modifier DefaultSetting () {
			return new GUISkinOperator(
				"UnityEngine.GUISkin"
			);
		}

		public override bool IsChanged<T> (T asset) {
			//var guiSkin = asset as GUISkin;

			var changed = false;
			
			return changed; 
		}

//		GUIStyle box = GUI.skin.box;
		/*
active	Rendering settings for when the control is pressed down.
alignment	Text alignment.
border	The borders of all background images.
clipping	What to do when the contents to be rendered is too large to fit within the area given.
contentOffset	Pixel offset to apply to the content of this GUIstyle.
fixedHeight	If non-0, any GUI elements rendered with this style will have the height specified here.
fixedWidth	If non-0, any GUI elements rendered with this style will have the width specified here.
focused	Rendering settings for when the element has keyboard focus.
font	The font to use for rendering. If null, the default font for the current GUISkin is used instead.
fontSize	The font size to use (for dynamic fonts).
fontStyle	The font style to use (for dynamic fonts).
hover	Rendering settings for when the mouse is hovering over the control.
imagePosition	How image and text of the GUIContent is combined.
lineHeight	The height of one line of text with this style, measured in pixels. (Read Only)
margin	The margins between elements rendered in this style and any other GUI elements.
name	The name of this GUIStyle. Used for getting them based on name.
normal	Rendering settings for when the component is displayed normally.
onActive	Rendering settings for when the element is turned on and pressed down.
onFocused	Rendering settings for when the element has keyboard and is turned on.
onHover	Rendering settings for when the control is turned on and the mouse is hovering it.
onNormal	Rendering settings for when the control is turned on.
overflow	Extra space to be added to the background image.
padding	Space from the edge of GUIStyle to the start of the contents.
richText	Enable HTML-style tags for Text Formatting Markup.
stretchHeight	Can GUI elements of this style be stretched vertically for better layout?
stretchWidth	Can GUI elements of this style be stretched horizontally for better layouting?
wordWrap	Should the text be wordwrapped?
		*/
		
		public override void Modify<T> (T asset) {
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

		public override void DrawInspector (Action changed) {
			GUILayout.Label("GUISkinOperator inspector.");
			GUILayout.Label("要素が膨大で引いてる");
		}
	}

}