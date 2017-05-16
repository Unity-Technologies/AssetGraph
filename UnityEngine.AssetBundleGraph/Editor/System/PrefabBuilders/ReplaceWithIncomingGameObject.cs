using UnityEngine;
using UnityEditor;

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine.AssetBundles.GraphTool;

[CustomPrefabBuilder("[Experimental]Replace With Incoming GameObject", "v1.0", 15)]
public class ReplaceWithIncomingGameObject : IPrefabBuilder {

    [SerializeField] GameObject m_replacingObject;

    private string GetPrefabName(string srcGameObjectName, string groupKeyName) {
        return string.Format("{0}_{1}",srcGameObjectName, groupKeyName);
    }

	/**
		 * Test if prefab can be created with incoming assets.
		 * @result Name of prefab file if prefab can be created. null if not.
		 */
	public string CanCreatePrefab (string groupKey, List<UnityEngine.Object> objects) {

        var go = objects.FindAll(o => o.GetType() == typeof(UnityEngine.GameObject) &&
            ((GameObject)o).transform.parent == null );

        if(go.Any()) {
            return GetPrefabName (m_replacingObject.name, groupKey);
		}

		return null;
	}

	/**
	 * Create Prefab.
	 */ 
	public UnityEngine.GameObject CreatePrefab (string groupKey, List<UnityEngine.Object> objects) {

        List<UnityEngine.Object> srcs = objects.FindAll(o => o.GetType() == typeof(UnityEngine.GameObject) &&
            ((GameObject)o).transform.parent == null );

        GameObject go = GameObject.Instantiate (m_replacingObject);

        go.name = GetPrefabName (m_replacingObject.name, groupKey);

        if (m_replacingObject != null) {
            ReplaceChildRecursively(go, srcs);
        }

		return go;
	}

    private void ReplaceChildRecursively(GameObject parent, List<UnityEngine.Object> srcs) {
        for (int i = 0; i < parent.transform.childCount; ++i) {
            var childTransform = parent.transform.GetChild (i);
            foreach(var obj in srcs) {
                if (childTransform.gameObject.name == obj.name) {
                    var newObj = (GameObject)GameObject.Instantiate (obj, 
                        childTransform.position, 
                        childTransform.rotation, 
                        parent.transform);
                    newObj.SetActive (childTransform.gameObject.activeSelf);
                    newObj.name = childTransform.gameObject.name; // suppress "(Clone)"
                    UnityEngine.Object.DestroyImmediate (childTransform.gameObject);
                }
            }
            if (childTransform != null) {
                if (childTransform.childCount > 0) {
                    ReplaceChildRecursively (childTransform.gameObject, srcs);
                }
            }
        }
    }

	/**
	 * Draw Inspector GUI for this PrefabBuilder.
	 */ 
	public void OnInspectorGUI (Action onValueChanged) {

        using (new EditorGUILayout.VerticalScope (GUI.skin.box)) {

            var newObj  = (UnityEngine.GameObject)EditorGUILayout.ObjectField(
                m_replacingObject, 
                typeof(UnityEngine.GameObject), 
                false);

            if (newObj != m_replacingObject) {
                m_replacingObject = newObj;
                onValueChanged ();
            }
        }
	}
}
