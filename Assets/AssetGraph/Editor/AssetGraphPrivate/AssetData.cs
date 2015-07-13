using System;
using System.IO;

namespace AssetGraph {
	public class AssetData {
		public readonly string traceId;
		public readonly string absoluteSourcePath;
		public readonly string sourceBasePath;
		public readonly string fileNameAndExtension;
		public readonly string pathUnderSourceBase;
		public readonly string importedPath;
		public readonly string assetId;
		public readonly string makerNodeId;
		
		/**
			new assets which will be imported.
		*/
		public AssetData (string absoluteSourcePath, string sourceBasePath) {
			this.traceId = Guid.NewGuid().ToString();
			this.absoluteSourcePath = absoluteSourcePath;
			this.sourceBasePath = sourceBasePath;
			this.fileNameAndExtension = Path.GetFileName(absoluteSourcePath);
			this.pathUnderSourceBase = GetPathWithoutBasePath(absoluteSourcePath, sourceBasePath);
			this.importedPath = null;
			this.assetId = null;
			this.makerNodeId = null;
		}

		/**
			replaced assets which is imported.
		*/
		public AssetData (string traceId, string absoluteSourcePath, string sourceBasePath, string fileNameAndExtension, string pathUnderSourceBase, string importedPath, string assetId, string makerNodeId) {
			this.traceId = traceId;
			this.absoluteSourcePath = absoluteSourcePath;
			this.sourceBasePath = sourceBasePath;
			this.fileNameAndExtension = fileNameAndExtension;
			this.pathUnderSourceBase = pathUnderSourceBase;
			this.importedPath = importedPath;
			this.assetId = assetId;
			this.makerNodeId = makerNodeId;
		}

		/**
			new assets which is generated after imported or prefabricated or bundlized.
		*/
		public AssetData (string importedPath, string assetId, string makerNodeId) {
			this.traceId = Guid.NewGuid().ToString();
			this.absoluteSourcePath = null;
			this.sourceBasePath = null;
			this.fileNameAndExtension = Path.GetFileName(importedPath);
			this.pathUnderSourceBase = null;
			this.importedPath = importedPath;
			this.assetId = assetId;
			this.makerNodeId = makerNodeId;
		}

		public static string GetPathWithoutBasePath (string localPathWithBasePath, string basePath) {
			var replaced = localPathWithBasePath.Replace(basePath, string.Empty);
			if (replaced.StartsWith(AssetGraphSettings.UNITY_FOLDER_SEPARATER)) return replaced.Substring(1);
			return replaced;
		}
		
		public static string GetPathWithBasePath (string localPathWithoutBasePath, string basePath) {
			return Path.Combine(basePath, localPathWithoutBasePath);
		}
	}
}