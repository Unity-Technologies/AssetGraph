using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AssetGraph;
using Model=UnityEngine.AssetGraph.DataModel.Version2;

[System.Serializable]
[CustomAssetGenerator("Create Material From Texture", "v1.0")]
public class CreateMaterialFromTexture : IAssetGenerator {

    [System.Serializable]
    public class PropertyField {
        [SerializeField] public string propertyName;
        [SerializeField] public TextureReference texture;

        public PropertyField() {
            texture = new TextureReference();
        }
    }

    [SerializeField] public MaterialReference m_referenceMat;
    [SerializeField] public string m_propertyName;
    [SerializeField] public List<PropertyField> m_properties;

    public void OnValidate () {
        if (m_referenceMat == null || m_referenceMat.Empty) {
            throw new NodeException ("Reference Material is empty", "Set Reference Material from inspector.");
        }

        if (string.IsNullOrEmpty(m_propertyName)) {
            throw new NodeException ("Property name is empty", "Set property name for incoming texture from inspector.");
        }
    }

    public string GetAssetExtension (AssetReference asset) {
        return ".mat";
    }

    public Type GetAssetType(AssetReference asset) {
        return typeof(Material);
    }

	public bool CanGenerateAsset (AssetReference asset) {

        if (asset.importerType != typeof(TextureImporter)) {
            throw new NodeException ("CreateMaterialFromTexture needs texture for source asset.", string.Format("Remove {0} from input.", asset.fileNameAndExtension));
		}

		return true;
	}

	/**
	 * Generate asset.
	 */ 
	public bool GenerateAsset (AssetReference asset, string generateAssetPath) {

		var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(asset.importFrom);
		if (tex == null) {
			return false;
		}

        var newMat = new Material (m_referenceMat.Object);

        newMat.SetTexture (m_propertyName, tex);

        if (m_properties != null) {
            foreach (var p in m_properties) {
                newMat.SetTexture (p.propertyName, p.texture.Object);
            }
        }

        AssetDatabase.CreateAsset (newMat, generateAssetPath);

		return true;
	}

	/**
	 * Draw Inspector GUI for this AssetGenerator.
	 */ 
	public void OnInspectorGUI (Action onValueChanged) {

        if (m_referenceMat == null) {
            m_referenceMat = new MaterialReference ();
            onValueChanged ();
        }

        if (m_properties == null) {
            m_properties = new List<PropertyField> ();
            onValueChanged ();
        }

        var refMat = (Material)EditorGUILayout.ObjectField ("Reference Material", m_referenceMat.Object, typeof(Material), false);
        if (refMat != m_referenceMat.Object) {
            m_referenceMat.Object = refMat;
            onValueChanged ();
        }

        var newFieldName = EditorGUILayout.TextField ("Property Name", m_propertyName);
        if (newFieldName != m_propertyName) {
            m_propertyName = newFieldName;
            onValueChanged ();
        }

        GUILayout.Space (8f);

        PropertyField removing = null;

        foreach (var p in m_properties) {
            var t = (Texture)EditorGUILayout.ObjectField ("Texture", p.texture.Object, typeof(Texture2D), false);
            if (t != p.texture.Object) {
                p.texture.Object = t;
                onValueChanged ();
            }

            using(new GUILayout.HorizontalScope()) {
                var n = EditorGUILayout.TextField ("Property Name", p.propertyName);
                if (n != p.propertyName) {
                    p.propertyName = n;
                    onValueChanged ();
                }

                if (GUILayout.Button ("-", GUILayout.Width(20f))) {
                    removing = p;
                }
            }
        }

        if (GUILayout.Button ("Add Property")) {
            m_properties.Add (new PropertyField ());
            onValueChanged ();
        }

        if (removing != null) {
            m_properties.Remove (removing);
            onValueChanged ();
        }
	}
}