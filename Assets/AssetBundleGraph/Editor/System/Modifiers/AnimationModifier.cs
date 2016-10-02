using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetBundleGraph.Modifiers {
	
	[Serializable] 
	[CustomModifier("Default Modifier", typeof(Animation))]
	public class AnimationModifier : IModifier {
		
		public AnimationModifier () {}

		public bool IsModified (object asset) {
			//var animation = asset as Animation;

			//TODO: implement this
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

		public void Modify (object asset) {
			//var flare = asset as Shader;
			//TODO: implement this
		}
		
		public void OnInspectorGUI (Action onValueChanged) {
			//TODO: implement this
			GUILayout.Label("AnimationModifier inspector.");
		}

		public string Serialize() {
			//TODO: implement this
			return null;
		}
	}
}