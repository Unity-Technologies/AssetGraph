using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.AssetGraph {

    [Serializable]
	public class SerializedComponent {
        [Serializable]
        public class ComponentInfo {
            [SerializeField] private string m_typeInfo;
            [SerializeField] private string m_componentData;

            private Type m_componentType;
            private Component m_component;
            private Editor m_componentEditor;

            public Type ComponentType {
                get {
                    return m_componentType;
                }
            }

            public Component Component {
                get {
                    return m_component;
                }
            }

            public Editor Editor {
                get {
                    return m_componentEditor;
                }
            }

            public string Data {
                get {
                    return m_componentData;
                }
            }

            public ComponentInfo(Type t, Component c) {
                m_typeInfo = t.AssemblyQualifiedName;
                m_componentType = t;
                m_component = c;
                m_componentEditor = Editor.CreateEditor (m_component);
                Save();
            }

            public ComponentInfo(ComponentInfo info) {
                m_typeInfo = info.m_typeInfo;
                m_componentType = info.m_componentType;
                m_componentData = info.m_componentData;
            }

            public void Save() {
                if (m_component != null) {
                    m_componentData = CustomScriptUtility.EncodeString(EditorJsonUtility.ToJson(m_component));
                }
            }

            public void Restore(GameObject o) {

                UnityEngine.Assertions.Assert.IsNotNull (m_typeInfo);

                if (m_componentType == null) {
                    m_componentType = Type.GetType (m_typeInfo);
                }
                UnityEngine.Assertions.Assert.IsNotNull (m_componentType);

                m_component = o.GetComponent (m_componentType);
                if (m_component == null) {
                    m_component = o.AddComponent (m_componentType);
                    if (m_componentData != null) {
                        EditorJsonUtility.FromJsonOverwrite (CustomScriptUtility.DecodeString (m_componentData), m_component);
                    }
                }

                if (m_componentEditor == null) {
                    m_componentEditor = Editor.CreateEditor (m_component);
                }
            }

            public void Invalidate(bool destroyComponent) {
                if (destroyComponent && m_component != null) {
                    Component.DestroyImmediate (m_component);
                }
                if (m_componentEditor != null) {
                    Editor.DestroyImmediate (m_componentEditor);
                    m_componentEditor = null;
                }
                m_componentType = null;
                m_component = null;
            }
        }

        [SerializeField] private List<ComponentInfo> m_attachedComponents;
		[SerializeField] private string m_instanceData;

        private GameObject m_gameObject;

        public bool IsInvalidated {
            get {
                return m_gameObject == null;
            }
        }

		public string Data {
			get {
				return m_instanceData;
			}
		}

        public List<ComponentInfo> Components {
            get {
                return m_attachedComponents;
            }
        }

        public GameObject InternalGameObject {
            get {
                if (m_gameObject == null) {
                    m_gameObject = Deserialize ();
                }
                return m_gameObject;
            }
        }

        public SerializedComponent() {
			m_instanceData = string.Empty;
            m_attachedComponents = new List<ComponentInfo> ();
		}

        public SerializedComponent(SerializedComponent c) {
			m_instanceData = c.m_instanceData;
            m_attachedComponents = new List<ComponentInfo> ();
            for (int i = 0; i < c.m_attachedComponents.Count; ++i) {
                m_attachedComponents.Add (new ComponentInfo (c.m_attachedComponents [i]));
            }
		}

        public void Invalidate() {
            if (m_gameObject != null) {
                GameObject.DestroyImmediate (m_gameObject);
                m_gameObject = null;
            }
            foreach (var info in m_attachedComponents) {
                info.Invalidate (false);
            }
        }

        public void Restore() {
            if (m_gameObject == null) {
                m_gameObject = Deserialize ();
            }
        }

        private GameObject Deserialize() {

            var obj = new GameObject ();
            obj.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            obj.name = "Attaching Component";

            if (!string.IsNullOrEmpty (m_instanceData)) {
                var decoded = CustomScriptUtility.DecodeString (m_instanceData);
                EditorJsonUtility.FromJsonOverwrite (decoded, obj);
            }

            foreach (var info in m_attachedComponents) {
                info.Restore (obj);
            }

            return obj;
		}

		public void Save() {
            if(m_gameObject != null) {
                m_instanceData = CustomScriptUtility.EncodeString(EditorJsonUtility.ToJson(m_gameObject));
			}
            foreach (var info in m_attachedComponents) {
                info.Save ();
            }
		}

        public SerializedComponent Clone() {
			Save();
            return new SerializedComponent(this);
		}

        public T GetComponent<T> () where T: UnityEngine.Component {
            return InternalGameObject.GetComponent<T> ();
        }

        public UnityEngine.Component GetComponent (Type t)  {
            return InternalGameObject.GetComponent (t);
        }

        public T AddComponent<T> () where T: UnityEngine.Component {
            var c = InternalGameObject.AddComponent<T> ();
            if (c != null) {
                m_attachedComponents.Add (new ComponentInfo (typeof(T), c));
                Save ();
            }
            return c;
        }

        public UnityEngine.Component AddComponent (Type t)  {
            var c = InternalGameObject.AddComponent (t);
            if (c != null) {
                m_attachedComponents.Add (new ComponentInfo (t, c));
                Save ();
            }
            return c;
        }

        public void RemoveComponent(ComponentInfo info) {
            info.Invalidate (true);
            m_attachedComponents.Remove (info);
        }

		public override bool Equals(object rhs)
		{
            SerializedComponent other = rhs as SerializedComponent; 
			if (other == null) {
				return false;
			} else {
				return other == this;
			}
		}

		public override int GetHashCode()
		{
			return (m_instanceData == null)? this.GetHashCode() : m_instanceData.GetHashCode();
		}

        public static bool operator == (SerializedComponent lhs, SerializedComponent rhs) {

			object lobj = lhs;
			object robj = rhs;

			if(lobj == null && robj == null) {
				return true;
			}
			if(lobj == null || robj == null) {
				return false;
			}

            if (lhs.m_instanceData != rhs.m_instanceData) {
                return false;
            }

            if (lhs.m_attachedComponents.Count != rhs.m_attachedComponents.Count) {
                return false;
            }

            for(int i = 0; i < lhs.m_attachedComponents.Count; ++i) {
                if (lhs.m_attachedComponents [i].Data != rhs.m_attachedComponents [i].Data) 
                {
                    return false;
                }
            }

            return true;
		}

        public static bool operator != (SerializedComponent lhs, SerializedComponent rhs) {
			return !(lhs == rhs);
		}
	}

    public class ComponentMenuUtility {
        private static List<Type> s_componentTypes;
        private static string[] s_componentNames;

        public static List<Type> GetComponentTypes() {

            if(s_componentTypes == null) {
                s_componentTypes = new List<Type>();

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                    var components = assembly.GetTypes ()
                        .Where (t => 
                            t.IsPublic && 
                            !t.IsAbstract &&
                                     t != typeof(UnityEngine.Component) &&
                                     t != typeof(UnityEngine.Transform) &&
                                     t != typeof(UnityEngine.MonoBehaviour) &&
                                     typeof(UnityEngine.Component).IsAssignableFrom (t)
                                     );
                    s_componentTypes.AddRange (components);
                }

                s_componentNames = s_componentTypes.Select (t => t.Name).ToArray ();
            }
            return s_componentTypes;
        }

        public static string[] GetComponentNames() {
            if (s_componentNames == null) {
                var types = GetComponentTypes ();
                s_componentNames = types.Select (t => t.Name).ToArray ();
            }
            return s_componentNames;
        }
    }
}

