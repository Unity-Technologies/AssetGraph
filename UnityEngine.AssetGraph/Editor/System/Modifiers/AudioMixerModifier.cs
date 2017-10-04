using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

using UnityEngine.AssetGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

namespace UnityEngine.AssetGraph.Modifiers {

	/*
	 * Code template for Material modifier.
	 * You can copy and create your CustomModifier.
	 */ 

	[Serializable] 
    [CustomModifier("Default Modifier(AudioMixer)", typeof(AudioMixer))]
    public class AudioMixerModifier : IModifier {

        [SerializeField] public AudioMixerUpdateMode updateMode;

        public AudioMixerModifier () {
            updateMode = AudioMixerUpdateMode.Normal;
		}

        public bool IsModified (UnityEngine.Object[] assets, List<AssetReference> group) {
            var mixer = assets[0] as AudioMixer;

			var changed = false;

            if (mixer.updateMode != updateMode) {
				changed = true;
			}

			return changed; 
		}

        public void Modify (UnityEngine.Object[] assets, List<AssetReference> group) {
            var mixer = assets[0] as AudioMixer;

            mixer.updateMode = updateMode;
		}

		public void OnInspectorGUI (Action onValueChanged) {
			// blend mode.
            var newMode = (AudioMixerUpdateMode)EditorGUILayout.EnumPopup("Update Mode", updateMode);
            if (newMode != updateMode) {
                this.updateMode = newMode;
				onValueChanged();
			}
		}
	}

}