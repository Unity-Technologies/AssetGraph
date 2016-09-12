using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetBundleGraph.ModifierOperators {
	
	[Serializable] public class FontOperator : ModifierBase {
		
		public FontOperator () {}

		private FontOperator (
			string operatorType
		) {
			this.operatorType = operatorType;
		}

		/*
			constructor for default data setting.
		*/
		public override ModifierBase DefaultSetting () {
			return new FontOperator(
				"UnityEngine.Font"
			);
		}

		public override bool IsChanged<T> (T asset) {
			var font = asset as Font;

			var changed = false;

			/*
Variables

ascent	The ascent of the font.
characterInfo	Access an array of all characters contained in the font texture.
dynamic	Is the font a dynamic font.
fontSize	The default size of the font.
lineHeight	The line height of the font.
material	The material used for the font display.
			*/
			
			return changed; 
		}

		public override void Modify<T> (T asset) {
			var font = asset as Font;
			
		}

		public override void DrawInspector (Action changed) {
			GUILayout.Label("FontOperator inspector.");
		}
	}

}