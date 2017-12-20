using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.AssetGraph {

    [Serializable]
    public class SerializableAnimationCurve {

        [Serializable]
        public class KeyFrameInfo {
            public float inTangent;
            public float outTangent;
            public int tangentMode;
            public float time;
            public float value;

            public KeyFrameInfo(Keyframe kf) {
                inTangent = kf.inTangent;
                outTangent = kf.outTangent;
                tangentMode = kf.tangentMode;
                time = kf.time;
                value = kf.value;
            }

            public Keyframe CreateKeyframe() {
                var kf = new Keyframe (time, value, inTangent, outTangent);
                kf.tangentMode = tangentMode;
                return kf;
            }
        }

        [SerializeField] private List<KeyFrameInfo> m_keyframes;

        public WrapMode preWrapMode;
        public WrapMode postWrapMode;

        private AnimationCurve m_curve;

        public SerializableAnimationCurve() {
            m_keyframes = new List<KeyFrameInfo> ();
		}

        public SerializableAnimationCurve(AnimationCurve curve) {
            Set (curve);
        }

        public void Set(AnimationCurve curve) {
            if (m_keyframes == null) {
                m_keyframes = new List<KeyFrameInfo> ();
            }
            m_keyframes.Clear ();
            preWrapMode = curve.preWrapMode;
            postWrapMode = curve.postWrapMode;
            for (int i = 0; i < curve.length; ++i) {
                m_keyframes.Add ( new KeyFrameInfo (curve [i]) );
            }
            m_curve = curve;
        }

        private AnimationCurve CreateCurve() {
            var curve = new AnimationCurve ();
            foreach (var k in m_keyframes) {
                curve.AddKey (k.CreateKeyframe ());
            }
            curve.preWrapMode = preWrapMode;
            curve.postWrapMode = postWrapMode;

            return curve;
        }

        public AnimationCurve Curve {
            get {
                if (m_curve == null) {
                    m_curve = CreateCurve ();
                }
                return m_curve;
            }
            set {
                this.Set (value);
            }
        }
	}
}

