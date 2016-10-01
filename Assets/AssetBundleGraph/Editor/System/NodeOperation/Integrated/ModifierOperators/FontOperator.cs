using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetBundleGraph.ModifierOperators {
	
	[Serializable] 
	[CustomModifier("Default Editor", typeof(Font))]
	public class FontOperator : Modifier {
		
		/*
		CharacterInfo:
			以下のパラメータがある。　個別に実装することは可能っちゃ可能。

			advance	The horizontal distance from the origin of this character to the origin of the next character.
			bearing	The horizontal distance from the origin of this glyph to the begining of the glyph image.
			glyphHeight	The height of the glyph image.
			glyphWidth	The width of the glyph image.
			index	Unicode value of the character.
			maxX	The maximum extend of the glyph image in the x-axis.
			maxY	The maximum extend of the glyph image in the y-axis.
			minX	The minium extend of the glyph image in the x-axis.
			minY	The minimum extend of the glyph image in the y-axis.
			size	The size of the character or 0 if it is the default font size.
			style	The style of the character.
			uvBottomLeft	The uv coordinate matching the bottom left of the glyph image in the font texture.
			uvBottomRight	The uv coordinate matching the bottom right of the glyph image in the font texture.
			uvTopLeft	The uv coordinate matching the top left of the glyph image in the font texture.
			uvTopRight	The uv coordinate matching the top right of the glyph image in the font texture.
		*/
		[SerializeField] public CharacterInfo[] characterInfo;
		[SerializeField] public Material material;

		public FontOperator () {}

		private FontOperator (
			string operatorType
		) {
			this.operatorType = operatorType;
		}

		/*
			constructor for default data setting.
		*/
		public override Modifier DefaultSetting () {
			return new FontOperator(
				"UnityEngine.Font"
			);
		}

		public override bool IsChanged<T> (T asset) {
			var font = asset as Font;

			var changed = false;
			foreach (var s in font.characterInfo) {
				Debug.LogError("s:" + s);
			}
			// font.ascent = this.ascent;// cannot assign.
			if (font.characterInfo != this.characterInfo) changed = true;
			// font.dynamic = this.dynamic;// cannot assign.
			// font.fontSize = this.fontSize;// cannot assign.
			// font.lineHeight = this.lineHeight;// cannot assign.
			if (font.material != this.material) changed = true;
			
			return changed; 
		}

		public override void Modify<T> (T asset) {
			var font = asset as Font;
			// font.ascent = this.ascent;// cannot assign.
			font.characterInfo = this.characterInfo;
			// font.dynamic = this.dynamic;// cannot assign.
			// font.fontSize = this.fontSize;// cannot assign.
			// font.lineHeight = this.lineHeight;// cannot assign.
			font.material = this.material;
		}

		public override void DrawInspector (Action changed) {
			GUILayout.Label("FontOperator inspector. フォントのInspectorと同じ設定項目が公開API経由で設定できない要素しかなかったので、公開APIに準じた値を変更可能な値として表示している。つまりFontのInspectorとは別物。");
			GUILayout.Label("CharacterInfoに関しては公開APIでセット可能っぽいのでセット可能にすることは可能っちゃ可能。自由なパラメータになる。");
			GUILayout.Label("Materialは参照を持ってしまう。");
		}
	}

}