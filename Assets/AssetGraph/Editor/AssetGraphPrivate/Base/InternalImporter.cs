using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.Collections.Generic;

namespace AssetGraph {
	public class InternalImporter : AssetPostprocessor {
		
		static ImporterBase importer = null;
		
		public static void Attach (ImporterBase newImporter) {
			importer = newImporter;
		}
		public static void Detach () {
			importer = null;
		}

		/*
			Unity's import handlers.
			except AssetPostprocessor.OnPostprocessAllAssets(string[],string[],string[],string[]).
		*/
		public void OnPostprocessGameObjectWithUserProperties (GameObject g, string[] propNames, object[] values) {
			if (importer == null) return;
			importer.assetPostprocessor = this;
			importer.assetImporter = this.assetImporter;
			importer.assetPath = this.assetPath;
			importer.AssetGraphOnPostprocessGameObjectWithUserProperties(g, propNames, values);
		}
		public void OnPreprocessTexture () {
			if (importer == null) return;
			importer.assetPostprocessor = this;
			importer.assetImporter = this.assetImporter;
			importer.assetPath = this.assetPath;
			importer.AssetGraphOnPreprocessTexture();
		}
		public void OnPostprocessTexture (Texture2D texture) {
			if (importer == null) return;
			importer.assetPostprocessor = this;
			importer.assetImporter = this.assetImporter;
			importer.assetPath = this.assetPath;
			importer.AssetGraphOnPostprocessTexture(texture);
		}
		public void OnPreprocessAudio () {
			if (importer == null) return;
			importer.assetPostprocessor = this;
			importer.assetImporter = this.assetImporter;
			importer.assetPath = this.assetPath;
			importer.AssetGraphOnPreprocessAudio();
		}
		public void OnPostprocessAudio (AudioClip clip) {
			if (importer == null) return;
			importer.assetPostprocessor = this;
			importer.assetImporter = this.assetImporter;
			importer.assetPath = this.assetPath;
			importer.AssetGraphOnPostprocessAudio(clip);
		}
		public void OnPreprocessModel () {
			if (importer == null) return;
			importer.assetPostprocessor = this;
			importer.assetImporter = this.assetImporter;
			importer.assetPath = this.assetPath;
			importer.AssetGraphOnPreprocessModel();
		}
		public void OnPostprocessModel (GameObject g) {
			if (importer == null) return;
			importer.assetPostprocessor = this;
			importer.assetImporter = this.assetImporter;
			importer.assetPath = this.assetPath;
			importer.AssetGraphOnPostprocessModel(g);
		}
		public void OnAssignMaterialModel (Material material, Renderer renderer) {
			if (importer == null) return;
			importer.assetPostprocessor = this;
			importer.assetImporter = this.assetImporter;
			importer.assetPath = this.assetPath;
			importer.AssetGraphOnAssignMaterialModel(material, renderer);
		}
	}
}