using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetBundleGraph.ModifierOperators {
	
	[Serializable] public class AnimatorOperator : OperatorBase {
		
		public AnimatorOperator () {}

		private AnimatorOperator (
			string operatorType
		) {
			this.operatorType = operatorType;
			
			// this.wrapMode = wrapMode;
			// this.filterMode = filterMode;
			// this.anisoLevel = anisoLevel;
		}

		/*
			constructor for default data setting.
		*/
		public override OperatorBase DefaultSetting () {
			return new AnimatorOperator(
				"UnityEngine.Animator"
			);
		}

		public override bool IsChanged<T> (T asset) {
			//var animator = asset as Animator;

			var changed = false;

			/*
				Variables

angularVelocity	Gets the avatar angular velocity for the last evaluated frame.
applyRootMotion	Should root motion be applied?
avatar	Gets/Sets the current Avatar.
bodyPosition	The position of the body center of mass.
bodyRotation	The rotation of the body center of mass.
cullingMode	Controls culling of this Animator component.
deltaPosition	Gets the avatar delta position for the last evaluated frame.
deltaRotation	Gets the avatar delta rotation for the last evaluated frame.
feetPivotActive	Blends pivot point between body center of mass and feet pivot. At 0%, the blending point is body center of mass. At 100%, the blending point is feet pivot.
gravityWeight	The current gravity weight based on current animations that are played.
hasRootMotion	Returns true if the current rig has root motion.
hasTransformHierarchy	Returns true if the object has a transform hierarchy.
humanScale	Returns the scale of the current Avatar for a humanoid rig, (1 by default if the rig is generic).
isHuman	Returns true if the current rig is humanoid, false if it is generic.
isInitialized	Returns whether the animator is initialized successfully.
isMatchingTarget	If automatic matching is active.
isOptimizable	Returns true if the current rig is optimizable with AnimatorUtility.OptimizeTransformHierarchy.
layerCount	See IAnimatorControllerPlayable.layerCount.
layersAffectMassCenter	Additional layers affects the center of mass.
leftFeetBottomHeight	Get left foot bottom height.
linearVelocityBlending	When linearVelocityBlending is set to true, the root motion velocity and angular velocity will be blended linearly.
parameterCount	See IAnimatorControllerPlayable.parameterCount.
parameters	Read only acces to the AnimatorControllerParameters used by the animator.
pivotPosition	Get the current position of the pivot.
pivotWeight	Gets the pivot weight.
playbackTime	Sets the playback position in the recording buffer.
recorderMode	Gets the mode of the Animator recorder.
recorderStartTime	Start time of the first frame of the buffer relative to the frame at which StartRecording was called.
recorderStopTime	End time of the recorded clip relative to when StartRecording was called.
rightFeetBottomHeight	Get right foot bottom height.
rootPosition	The root position, the position of the game object.
rootRotation	The root rotation, the rotation of the game object.
runtimeAnimatorController	The runtime representation of AnimatorController that controls the Animator.
speed	The playback speed of the Animator. 1 is normal playback speed.
stabilizeFeet	Automatic stabilization of feet during transition and blending.
targetPosition	Returns the position of the target specified by SetTarget(AvatarTarget targetIndex, float targetNormalizedTime)).
targetRotation	Returns the rotation of the target specified by SetTarget(AvatarTarget targetIndex, float targetNormalizedTime)).
updateMode	Specifies the update mode of the Animator.
velocity	Gets the avatar velocity for the last evaluated frame.
			*/

			return changed; 
		}

		public override void Modify<T> (T asset) {
			//var flare = asset as Shader;
			
		}
		
		public override void DrawInspector (Action changed) {
			GUILayout.Label("AnimatorOperator inspector.");
		}
	}

}