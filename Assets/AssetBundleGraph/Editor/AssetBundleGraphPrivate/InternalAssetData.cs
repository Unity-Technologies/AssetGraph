using UnityEngine;

using System;
using System.IO;

namespace AssetBundleGraph {
	public class InternalAssetData {
		public readonly string traceId;
		public readonly string absoluteSourcePath;
		public readonly string sourceBasePath;
		public readonly string fileNameAndExtension;
		public readonly string pathUnderSourceBase;
		public readonly string importedPath;
		public readonly string exportedPath;
		public readonly string assetId;
		public readonly Type assetType;
		public readonly bool isNew;
		
		public readonly bool isBundled;
		
		
		/**
			new assets which is Loaded by Loader.
		*/
		public static InternalAssetData InternalImportedAssetDataByLoader (string absoluteSourcePath, string sourceBasePath, string importedPath, string assetId, Type assetType) {
			return new InternalAssetData(
				traceId:Guid.NewGuid().ToString(),
				absoluteSourcePath:absoluteSourcePath,
				sourceBasePath:sourceBasePath,
				fileNameAndExtension:Path.GetFileName(absoluteSourcePath),
				pathUnderSourceBase:GetPathWithoutBasePath(absoluteSourcePath, sourceBasePath),
				importedPath:importedPath,
				assetId:assetId,
				assetType:assetType
			);
		}

		/**
			new assets which is generated through ImportSettings.
		*/
		public static InternalAssetData InternalAssetDataByImporter (string traceId, string absoluteSourcePath, string sourceBasePath, string fileNameAndExtension, string pathUnderSourceBase, string importedPath, string assetId, Type assetType) {
			return new InternalAssetData(
				traceId:traceId,
				absoluteSourcePath:absoluteSourcePath,
				sourceBasePath:sourceBasePath,
				fileNameAndExtension:fileNameAndExtension,
				pathUnderSourceBase:pathUnderSourceBase,
				importedPath:importedPath,
				assetId:assetId,
				assetType:assetType
			);
		}

		/**
			new assets which is generated on Imported or Prefabricated.
		*/
		public static InternalAssetData InternalAssetDataGeneratedByImporterOrModifierOrPrefabricator (string importedPath, string assetId, Type assetType, bool isNew, bool isBundled) {
			return new InternalAssetData(
				traceId:Guid.NewGuid().ToString(),
				fileNameAndExtension:Path.GetFileName(importedPath),
				importedPath:importedPath,
				assetId:assetId,
				assetType:assetType,
				isNew:isNew,
				isBundled:isBundled
			);
		}

		/**
			new assets which is generated on Bundlized.
			no file exists. only setting applyied.
		*/
		public static InternalAssetData InternalAssetDataGeneratedByBundlizer (string importedPath) {
			return new InternalAssetData(
				traceId:Guid.NewGuid().ToString(),
				fileNameAndExtension:Path.GetFileName(importedPath),
				importedPath:importedPath
			);
		}
		
		/**
			renew internal assetdata as "already bundled".
			isBundled is always true.
		*/
		public static InternalAssetData InternalAssetDataBundledByBundlizer (InternalAssetData bundledSourceAssetData) {
			return new InternalAssetData(
				traceId:bundledSourceAssetData.traceId,
				fileNameAndExtension:bundledSourceAssetData.fileNameAndExtension,
				importedPath:bundledSourceAssetData.importedPath,
				assetId:bundledSourceAssetData.assetId,
				assetType:bundledSourceAssetData.assetType,
				isNew:bundledSourceAssetData.isNew,
				isBundled:true
			);
		}

		public static InternalAssetData InternalAssetDataGeneratedByBundleBuilder (string importedPath) {
			return new InternalAssetData(
				traceId:Guid.NewGuid().ToString(),
				fileNameAndExtension:Path.GetFileName(importedPath),
				importedPath:importedPath
			);
		}

		public static InternalAssetData InternalAssetDataGeneratedByExporter (string exportedPath) {
			return new InternalAssetData(
				traceId:Guid.NewGuid().ToString(),
				fileNameAndExtension:Path.GetFileName(exportedPath),
				exportedPath:exportedPath
			);
		}


		private InternalAssetData (
			string traceId = null,
			string absoluteSourcePath = null,
			string sourceBasePath = null,
			string fileNameAndExtension = null,
			string pathUnderSourceBase = null,
			string importedPath = null,
			string exportedPath = null,
			string assetId = null,
			Type assetType = null,
			bool isNew = false,
			bool isBundled = false
		) {
			this.traceId = traceId;
			this.absoluteSourcePath = absoluteSourcePath;
			this.sourceBasePath = sourceBasePath;
			this.fileNameAndExtension = fileNameAndExtension;
			this.pathUnderSourceBase = pathUnderSourceBase;
			this.importedPath = importedPath;
			this.exportedPath = exportedPath;
			this.assetId = assetId;
			this.assetType = assetType;
			this.isNew = isNew;
			this.isBundled = isBundled;
		}
		
		public static string GetPathWithoutBasePath (string localPathWithBasePath, string basePath) {
			var replaced = localPathWithBasePath.Replace(basePath, string.Empty);
			if (replaced.StartsWith(AssetBundleGraphSettings.UNITY_FOLDER_SEPARATOR.ToString())) return replaced.Substring(1);
			return replaced;
		}
		
		public string GetAbsolutePathOrImportedPath () {
			if (absoluteSourcePath != null) return absoluteSourcePath;
			return importedPath;
		}
	}
}