using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetBundleGraph.ModifierOperators {
	
	[Serializable] public class AnimationOperator : ModifierBase {
		
		public AnimationOperator () {}

		private AnimationOperator (
			string operatorType
		) {
			this.operatorType = operatorType;
		}

		/*
			constructor for default data setting.
		*/
		public override ModifierBase DefaultSetting () {
			return new AnimationOperator(
				"UnityEngine.Animation"
			);
		}

		public override bool IsChanged<T> (T asset) {
			//var animation = asset as Animation;

			var changed = false;

			/*
Variables

animatePhysics	When turned on, animations will be executed in the physics loop. This is only useful in conjunction with kinematic rigidbodies.
clip	The default animation.
cullingType	Controls culling of this Animation component.
isPlaying	Are we playing any animations?
localBounds	AABB of this Animation animation component in local space.
playAutomatically	Should the default animation clip (the Animation.clip property) automatically start playing on startup?
this[string]	Returns the animation state named name.
wrapMode	How should time beyond the playback range of the clip be treated?
			*/

			return changed; 
		}

		public override void Modify<T> (T asset) {
			//var flare = asset as Shader;
			
		}
		
		public override void DrawInspector (Action changed) {
			GUILayout.Label("AnimationOperator inspector.");
		}
	}

}