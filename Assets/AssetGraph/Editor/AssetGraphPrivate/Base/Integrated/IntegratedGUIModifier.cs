using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace AssetGraph {
	
	/**
		IntegratedGUIModifier is the class for apply specific setting to asset files.
	*/
	public class IntegratedGUIModifier : INodeBase {
		private readonly string modifierPackage;
		public IntegratedGUIModifier (string modifierPackage) {
			this.modifierPackage = modifierPackage;
		}

		public void Setup (string nodeId, string labelToNext, string unusedPackageInfo, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			
			var outputDict = new Dictionary<string, List<InternalAssetData>>();

			var first = true;
			
			if (groupedSources.Keys.Count == 0) return;
			
			// shrink group to 1 group.
			if (1 < groupedSources.Keys.Count) Debug.LogWarning("modifierSetting shrinking group to \"" + groupedSources.Keys.ToList()[0] + "\" forcely.");

			var inputSources = new List<InternalAssetData>();
			foreach (var groupKey in groupedSources.Keys) {
				inputSources.AddRange(groupedSources[groupKey]);
			}
				
			var assumedImportedAssetDatas = new List<InternalAssetData>();
			

			var samplingDirectoryPath = FileController.PathCombine(AssetGraphSettings.MODIFIER_SAMPLING_PLACE, nodeId, modifierPackage);
			ValidateModifierSample(samplingDirectoryPath,
				(string noSampleFolder) => {
					// do nothing. keep importing new asset for sampling.
				},
				(string noSampleFile) => {
					// do nothing. keep importing new asset for sampling.
				},
				(string samplePath) => {
					first = false;
				},
				(string tooManysample) => {
					first = false;
				}
			);

			var alreadyImported = new List<string>();
			var ignoredResource = new List<string>();

			foreach (var inputSource in inputSources) {
				if (string.IsNullOrEmpty(inputSource.absoluteSourcePath)) {
					if (!string.IsNullOrEmpty(inputSource.importedPath)) {
						alreadyImported.Add(inputSource.importedPath);
						continue;
					}

					ignoredResource.Add(inputSource.fileNameAndExtension);
					continue;
				}
				
				var assumedImportedPath = inputSource.importedPath;
				
				var assumedType = AssumeTypeFromExtension();

				var newData = InternalAssetData.InternalAssetDataByImporter(
					inputSource.traceId,
					inputSource.absoluteSourcePath,
					inputSource.sourceBasePath,
					inputSource.fileNameAndExtension,
					inputSource.pathUnderSourceBase,
					assumedImportedPath,
					null,
					assumedType
				);
				assumedImportedAssetDatas.Add(newData);

				if (first) {
					if (!Directory.Exists(samplingDirectoryPath)) Directory.CreateDirectory(samplingDirectoryPath);

					var absoluteFilePath = inputSource.absoluteSourcePath;
					var targetFilePath = FileController.PathCombine(samplingDirectoryPath, inputSource.fileNameAndExtension);

					EditorUtility.DisplayProgressBar("AssetGraph Modifier generating ModifierSetting...", targetFilePath, 0);
					FileController.CopyFileFromGlobalToLocal(absoluteFilePath, targetFilePath);
					first = false;
					AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
					EditorUtility.ClearProgressBar();
				}
			

				if (alreadyImported.Any()) Debug.LogError("modifierSetting:" + string.Join(", ", alreadyImported.ToArray()) + " are already imported.");
				if (ignoredResource.Any()) Debug.LogError("modifierSetting:" + string.Join(", ", ignoredResource.ToArray()) + " are ignored.");

				outputDict[groupedSources.Keys.ToList()[0]] = assumedImportedAssetDatas;
			}

			Output(nodeId, labelToNext, outputDict, new List<string>());
		}
		
		public void Run (string nodeId, string labelToNext, string package, Dictionary<string, List<InternalAssetData>> groupedSources, List<string> alreadyCached, Action<string, string, Dictionary<string, List<InternalAssetData>>, List<string>> Output) {
			var usedCache = new List<string>();
			
			var outputDict = new Dictionary<string, List<InternalAssetData>>();


			// caution if import setting file is exists already or not.
			var samplingDirectoryPath = FileController.PathCombine(AssetGraphSettings.MODIFIER_SAMPLING_PLACE, nodeId, modifierPackage);
			
			var sampleAssetPath = string.Empty;
			ValidateModifierSample(samplingDirectoryPath,
				(string noSampleFolder) => {
					Debug.LogWarning("modifierSetting:" + noSampleFolder);
				},
				(string noSampleFile) => {
					throw new Exception("modifierSetting error:" + noSampleFile);
				},
				(string samplePath) => {
					Debug.Log("using modifier setting:" + samplePath);
					sampleAssetPath = samplePath;
				},
				(string tooManysample) => {
					throw new Exception("modifierSetting error:" + tooManysample);
				}
			);
			
			if (groupedSources.Keys.Count == 0) return;
			
			var the1stGroupKey = groupedSources.Keys.ToList()[0];
			
			
			// shrink group to 1 group.
			if (1 < groupedSources.Keys.Count) Debug.LogWarning("modifierSetting shrinking group to \"" + the1stGroupKey + "\" forcely.");

			var inputSources = new List<InternalAssetData>();
			foreach (var groupKey in groupedSources.Keys) {
				inputSources.AddRange(groupedSources[groupKey]);
			}
			
			var importSetOveredAssetsAndUpdatedFlagDict = new Dictionary<InternalAssetData, bool>();
			
			/*
				check file & setting.
				if need, apply modifierSetting to file.
			*/
			{
				var samplingAssetImporter = AssetImporter.GetAtPath(sampleAssetPath);
				var effector = new InternalSamplingImportEffector(samplingAssetImporter);
				{
					foreach (var inputSource in inputSources) {
						var importer = AssetImporter.GetAtPath(inputSource.importedPath);
						
						/*
							compare type of import setting effector.
						*/
						var importerTypeStr = importer.GetType().ToString();
						
						if (importerTypeStr != samplingAssetImporter.GetType().ToString()) {
							// mismatched target will be ignored. but already imported.
							importSetOveredAssetsAndUpdatedFlagDict[inputSource] = false; 
							continue;
						}
						importSetOveredAssetsAndUpdatedFlagDict[inputSource] = false;
						
						/*
							kind of importer is matched.
							check setting then apply setting or no changed.
						*/
						switch (importerTypeStr) {
							case "UnityEditor.AssetImporter": {// materials and others... assets which are generated in Unity.
								var assetType = inputSource.assetType.ToString();
								switch (assetType) {
									case "UnityEngine.Material": {
										// 判別はできるんだけど、このあとどうしたもんか。ロードしなきゃいけない + loadしてもなんかグローバルなプロパティだけ比較とかそういうのかなあ、、
										
										// var materialInstance = AssetDatabase.LoadAssetAtPath(inputSource.importedPath, inputSource.assetType) as Material;// 型を指定してロードしないといけないので、ここのコードのようにswitchに落としたほうが良さそう。
										// var s = materialInstance.globalIlluminationFlags;// グローバルなプロパティ、リフレクションで列挙できそうではあるけど、、、
										
										break;
									}
									default: {
										Debug.LogError("unsupported type. assetType:" + assetType);
										break;
									}
								}
								
								/*
									試しにserializeして云々してみる
								*/
								var assetInstance = AssetDatabase.LoadAssetAtPath(inputSource.importedPath, inputSource.assetType) as Material;// ここの型が露出しちゃうのやばい
								var serializedObject = new UnityEditor.SerializedObject(assetInstance);
								
								var itr = serializedObject.GetIterator();
								itr.NextVisible(true);
								Debug.LogError("0 itr:" + itr.propertyPath + " displayName:" + itr.displayName + " name:" + itr.name + " type:" + itr.type);
								
								while (itr.NextVisible(true)) {
									Debug.LogError("~ itr:" + itr.propertyPath + " displayName:" + itr.displayName + " name:" + itr.name + " type:" + itr.type);
								}
								/*
									このへんのログはこんな感じになる。
									
0 itr:m_Shader displayName:Shader name:m_Shader type:PPtr<Shader>
~ itr:m_ShaderKeywords displayName:Shader Keywords name:m_ShaderKeywords type:string
~ itr:m_LightmapFlags displayName:Lightmap Flags name:m_LightmapFlags type:uint
~ itr:m_CustomRenderQueue displayName:Custom Render Queue name:m_CustomRenderQueue type:int
~ itr:stringTagMap displayName:String Tag Map name:stringTagMap type:map
~ itr:stringTagMap.Array.size displayName:Size name:size type:ArraySize
~ itr:m_SavedProperties displayName:Saved Properties name:m_SavedProperties type:UnityPropertySheet
~ itr:m_SavedProperties.m_TexEnvs displayName:Tex Envs name:m_TexEnvs type:map
~ itr:m_SavedProperties.m_TexEnvs.Array.size displayName:Size name:size type:ArraySize
~ itr:m_SavedProperties.m_TexEnvs.Array.data[0] displayName:Element 0 name:data type:pair
~ itr:m_SavedProperties.m_TexEnvs.Array.data[0].first displayName:First name:first type:FastPropertyName
~ itr:m_SavedProperties.m_TexEnvs.Array.data[0].first.name displayName:Name name:name type:string
~ itr:m_SavedProperties.m_TexEnvs.Array.data[0].second displayName:Second name:second type:UnityTexEnv
~ itr:m_SavedProperties.m_TexEnvs.Array.data[0].second.m_Texture displayName:Texture name:m_Texture type:PPtr<Texture>
~ itr:m_SavedProperties.m_TexEnvs.Array.data[0].second.m_Scale displayName:Scale name:m_Scale type:Vector2
~ itr:m_SavedProperties.m_TexEnvs.Array.data[0].second.m_Scale.x displayName:X name:x type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[0].second.m_Scale.y displayName:Y name:y type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[0].second.m_Offset displayName:Offset name:m_Offset type:Vector2
~ itr:m_SavedProperties.m_TexEnvs.Array.data[0].second.m_Offset.x displayName:X name:x type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[0].second.m_Offset.y displayName:Y name:y type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[1] displayName:Element 1 name:data type:pair
~ itr:m_SavedProperties.m_TexEnvs.Array.data[1].first displayName:First name:first type:FastPropertyName
~ itr:m_SavedProperties.m_TexEnvs.Array.data[1].first.name displayName:Name name:name type:string
~ itr:m_SavedProperties.m_TexEnvs.Array.data[1].second displayName:Second name:second type:UnityTexEnv
~ itr:m_SavedProperties.m_TexEnvs.Array.data[1].second.m_Texture displayName:Texture name:m_Texture type:PPtr<Texture>
~ itr:m_SavedProperties.m_TexEnvs.Array.data[1].second.m_Scale displayName:Scale name:m_Scale type:Vector2
~ itr:m_SavedProperties.m_TexEnvs.Array.data[1].second.m_Scale.x displayName:X name:x type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[1].second.m_Scale.y displayName:Y name:y type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[1].second.m_Offset displayName:Offset name:m_Offset type:Vector2
~ itr:m_SavedProperties.m_TexEnvs.Array.data[1].second.m_Offset.x displayName:X name:x type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[1].second.m_Offset.y displayName:Y name:y type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[2] displayName:Element 2 name:data type:pair
~ itr:m_SavedProperties.m_TexEnvs.Array.data[2].first displayName:First name:first type:FastPropertyName
~ itr:m_SavedProperties.m_TexEnvs.Array.data[2].first.name displayName:Name name:name type:string
~ itr:m_SavedProperties.m_TexEnvs.Array.data[2].second displayName:Second name:second type:UnityTexEnv
~ itr:m_SavedProperties.m_TexEnvs.Array.data[2].second.m_Texture displayName:Texture name:m_Texture type:PPtr<Texture>
~ itr:m_SavedProperties.m_TexEnvs.Array.data[2].second.m_Scale displayName:Scale name:m_Scale type:Vector2
~ itr:m_SavedProperties.m_TexEnvs.Array.data[2].second.m_Scale.x displayName:X name:x type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[2].second.m_Scale.y displayName:Y name:y type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[2].second.m_Offset displayName:Offset name:m_Offset type:Vector2
~ itr:m_SavedProperties.m_TexEnvs.Array.data[2].second.m_Offset.x displayName:X name:x type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[2].second.m_Offset.y displayName:Y name:y type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[3] displayName:Element 3 name:data type:pair
~ itr:m_SavedProperties.m_TexEnvs.Array.data[3].first displayName:First name:first type:FastPropertyName
~ itr:m_SavedProperties.m_TexEnvs.Array.data[3].first.name displayName:Name name:name type:string
~ itr:m_SavedProperties.m_TexEnvs.Array.data[3].second displayName:Second name:second type:UnityTexEnv
~ itr:m_SavedProperties.m_TexEnvs.Array.data[3].second.m_Texture displayName:Texture name:m_Texture type:PPtr<Texture>
~ itr:m_SavedProperties.m_TexEnvs.Array.data[3].second.m_Scale displayName:Scale name:m_Scale type:Vector2
~ itr:m_SavedProperties.m_TexEnvs.Array.data[3].second.m_Scale.x displayName:X name:x type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[3].second.m_Scale.y displayName:Y name:y type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[3].second.m_Offset displayName:Offset name:m_Offset type:Vector2
~ itr:m_SavedProperties.m_TexEnvs.Array.data[3].second.m_Offset.x displayName:X name:x type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[3].second.m_Offset.y displayName:Y name:y type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[4] displayName:Element 4 name:data type:pair
~ itr:m_SavedProperties.m_TexEnvs.Array.data[4].first displayName:First name:first type:FastPropertyName
~ itr:m_SavedProperties.m_TexEnvs.Array.data[4].first.name displayName:Name name:name type:string
~ itr:m_SavedProperties.m_TexEnvs.Array.data[4].second displayName:Second name:second type:UnityTexEnv
~ itr:m_SavedProperties.m_TexEnvs.Array.data[4].second.m_Texture displayName:Texture name:m_Texture type:PPtr<Texture>
~ itr:m_SavedProperties.m_TexEnvs.Array.data[4].second.m_Scale displayName:Scale name:m_Scale type:Vector2
~ itr:m_SavedProperties.m_TexEnvs.Array.data[4].second.m_Scale.x displayName:X name:x type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[4].second.m_Scale.y displayName:Y name:y type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[4].second.m_Offset displayName:Offset name:m_Offset type:Vector2
~ itr:m_SavedProperties.m_TexEnvs.Array.data[4].second.m_Offset.x displayName:X name:x type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[4].second.m_Offset.y displayName:Y name:y type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[5] displayName:Element 5 name:data type:pair
~ itr:m_SavedProperties.m_TexEnvs.Array.data[5].first displayName:First name:first type:FastPropertyName
~ itr:m_SavedProperties.m_TexEnvs.Array.data[5].first.name displayName:Name name:name type:string
~ itr:m_SavedProperties.m_TexEnvs.Array.data[5].second displayName:Second name:second type:UnityTexEnv
~ itr:m_SavedProperties.m_TexEnvs.Array.data[5].second.m_Texture displayName:Texture name:m_Texture type:PPtr<Texture>
~ itr:m_SavedProperties.m_TexEnvs.Array.data[5].second.m_Scale displayName:Scale name:m_Scale type:Vector2
~ itr:m_SavedProperties.m_TexEnvs.Array.data[5].second.m_Scale.x displayName:X name:x type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[5].second.m_Scale.y displayName:Y name:y type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[5].second.m_Offset displayName:Offset name:m_Offset type:Vector2
~ itr:m_SavedProperties.m_TexEnvs.Array.data[5].second.m_Offset.x displayName:X name:x type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[5].second.m_Offset.y displayName:Y name:y type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[6] displayName:Element 6 name:data type:pair
~ itr:m_SavedProperties.m_TexEnvs.Array.data[6].first displayName:First name:first type:FastPropertyName
~ itr:m_SavedProperties.m_TexEnvs.Array.data[6].first.name displayName:Name name:name type:string
~ itr:m_SavedProperties.m_TexEnvs.Array.data[6].second displayName:Second name:second type:UnityTexEnv
~ itr:m_SavedProperties.m_TexEnvs.Array.data[6].second.m_Texture displayName:Texture name:m_Texture type:PPtr<Texture>
~ itr:m_SavedProperties.m_TexEnvs.Array.data[6].second.m_Scale displayName:Scale name:m_Scale type:Vector2
~ itr:m_SavedProperties.m_TexEnvs.Array.data[6].second.m_Scale.x displayName:X name:x type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[6].second.m_Scale.y displayName:Y name:y type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[6].second.m_Offset displayName:Offset name:m_Offset type:Vector2
~ itr:m_SavedProperties.m_TexEnvs.Array.data[6].second.m_Offset.x displayName:X name:x type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[6].second.m_Offset.y displayName:Y name:y type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[7] displayName:Element 7 name:data type:pair
~ itr:m_SavedProperties.m_TexEnvs.Array.data[7].first displayName:First name:first type:FastPropertyName
~ itr:m_SavedProperties.m_TexEnvs.Array.data[7].first.name displayName:Name name:name type:string
~ itr:m_SavedProperties.m_TexEnvs.Array.data[7].second displayName:Second name:second type:UnityTexEnv
~ itr:m_SavedProperties.m_TexEnvs.Array.data[7].second.m_Texture displayName:Texture name:m_Texture type:PPtr<Texture>
~ itr:m_SavedProperties.m_TexEnvs.Array.data[7].second.m_Scale displayName:Scale name:m_Scale type:Vector2
~ itr:m_SavedProperties.m_TexEnvs.Array.data[7].second.m_Scale.x displayName:X name:x type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[7].second.m_Scale.y displayName:Y name:y type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[7].second.m_Offset displayName:Offset name:m_Offset type:Vector2
~ itr:m_SavedProperties.m_TexEnvs.Array.data[7].second.m_Offset.x displayName:X name:x type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[7].second.m_Offset.y displayName:Y name:y type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[8] displayName:Element 8 name:data type:pair
~ itr:m_SavedProperties.m_TexEnvs.Array.data[8].first displayName:First name:first type:FastPropertyName
~ itr:m_SavedProperties.m_TexEnvs.Array.data[8].first.name displayName:Name name:name type:string
~ itr:m_SavedProperties.m_TexEnvs.Array.data[8].second displayName:Second name:second type:UnityTexEnv
~ itr:m_SavedProperties.m_TexEnvs.Array.data[8].second.m_Texture displayName:Texture name:m_Texture type:PPtr<Texture>
~ itr:m_SavedProperties.m_TexEnvs.Array.data[8].second.m_Scale displayName:Scale name:m_Scale type:Vector2
~ itr:m_SavedProperties.m_TexEnvs.Array.data[8].second.m_Scale.x displayName:X name:x type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[8].second.m_Scale.y displayName:Y name:y type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[8].second.m_Offset displayName:Offset name:m_Offset type:Vector2
~ itr:m_SavedProperties.m_TexEnvs.Array.data[8].second.m_Offset.x displayName:X name:x type:float
~ itr:m_SavedProperties.m_TexEnvs.Array.data[8].second.m_Offset.y displayName:Y name:y type:float
~ itr:m_SavedProperties.m_Floats displayName:Floats name:m_Floats type:map
~ itr:m_SavedProperties.m_Floats.Array.size displayName:Size name:size type:ArraySize
~ itr:m_SavedProperties.m_Floats.Array.data[0] displayName:Element 0 name:data type:pair
~ itr:m_SavedProperties.m_Floats.Array.data[0].first displayName:First name:first type:FastPropertyName
~ itr:m_SavedProperties.m_Floats.Array.data[0].first.name displayName:Name name:name type:string
~ itr:m_SavedProperties.m_Floats.Array.data[0].second displayName:Second name:second type:float
~ itr:m_SavedProperties.m_Floats.Array.data[1] displayName:Element 1 name:data type:pair
~ itr:m_SavedProperties.m_Floats.Array.data[1].first displayName:First name:first type:FastPropertyName
~ itr:m_SavedProperties.m_Floats.Array.data[1].first.name displayName:Name name:name type:string
~ itr:m_SavedProperties.m_Floats.Array.data[1].second displayName:Second name:second type:float
~ itr:m_SavedProperties.m_Floats.Array.data[2] displayName:Element 2 name:data type:pair
~ itr:m_SavedProperties.m_Floats.Array.data[2].first displayName:First name:first type:FastPropertyName
~ itr:m_SavedProperties.m_Floats.Array.data[2].first.name displayName:Name name:name type:string
~ itr:m_SavedProperties.m_Floats.Array.data[2].second displayName:Second name:second type:float
~ itr:m_SavedProperties.m_Floats.Array.data[3] displayName:Element 3 name:data type:pair
~ itr:m_SavedProperties.m_Floats.Array.data[3].first displayName:First name:first type:FastPropertyName
~ itr:m_SavedProperties.m_Floats.Array.data[3].first.name displayName:Name name:name type:string
~ itr:m_SavedProperties.m_Floats.Array.data[3].second displayName:Second name:second type:float
~ itr:m_SavedProperties.m_Floats.Array.data[4] displayName:Element 4 name:data type:pair
~ itr:m_SavedProperties.m_Floats.Array.data[4].first displayName:First name:first type:FastPropertyName
~ itr:m_SavedProperties.m_Floats.Array.data[4].first.name displayName:Name name:name type:string
~ itr:m_SavedProperties.m_Floats.Array.data[4].second displayName:Second name:second type:float
~ itr:m_SavedProperties.m_Floats.Array.data[5] displayName:Element 5 name:data type:pair
~ itr:m_SavedProperties.m_Floats.Array.data[5].first displayName:First name:first type:FastPropertyName
~ itr:m_SavedProperties.m_Floats.Array.data[5].first.name displayName:Name name:name type:string
~ itr:m_SavedProperties.m_Floats.Array.data[5].second displayName:Second name:second type:float
~ itr:m_SavedProperties.m_Floats.Array.data[6] displayName:Element 6 name:data type:pair
~ itr:m_SavedProperties.m_Floats.Array.data[6].first displayName:First name:first type:FastPropertyName
~ itr:m_SavedProperties.m_Floats.Array.data[6].first.name displayName:Name name:name type:string
~ itr:m_SavedProperties.m_Floats.Array.data[6].second displayName:Second name:second type:float
~ itr:m_SavedProperties.m_Floats.Array.data[7] displayName:Element 7 name:data type:pair
~ itr:m_SavedProperties.m_Floats.Array.data[7].first displayName:First name:first type:FastPropertyName
~ itr:m_SavedProperties.m_Floats.Array.data[7].first.name displayName:Name name:name type:string
~ itr:m_SavedProperties.m_Floats.Array.data[7].second displayName:Second name:second type:float
~ itr:m_SavedProperties.m_Floats.Array.data[8] displayName:Element 8 name:data type:pair
~ itr:m_SavedProperties.m_Floats.Array.data[8].first displayName:First name:first type:FastPropertyName
~ itr:m_SavedProperties.m_Floats.Array.data[8].first.name displayName:Name name:name type:string
~ itr:m_SavedProperties.m_Floats.Array.data[8].second displayName:Second name:second type:float
~ itr:m_SavedProperties.m_Floats.Array.data[9] displayName:Element 9 name:data type:pair
~ itr:m_SavedProperties.m_Floats.Array.data[9].first displayName:First name:first type:FastPropertyName
~ itr:m_SavedProperties.m_Floats.Array.data[9].first.name displayName:Name name:name type:string
~ itr:m_SavedProperties.m_Floats.Array.data[9].second displayName:Second name:second type:float
~ itr:m_SavedProperties.m_Floats.Array.data[10] displayName:Element 10 name:data type:pair
~ itr:m_SavedProperties.m_Floats.Array.data[10].first displayName:First name:first type:FastPropertyName
~ itr:m_SavedProperties.m_Floats.Array.data[10].first.name displayName:Name name:name type:string
~ itr:m_SavedProperties.m_Floats.Array.data[10].second displayName:Second name:second type:float
~ itr:m_SavedProperties.m_Floats.Array.data[11] displayName:Element 11 name:data type:pair
~ itr:m_SavedProperties.m_Floats.Array.data[11].first displayName:First name:first type:FastPropertyName
~ itr:m_SavedProperties.m_Floats.Array.data[11].first.name displayName:Name name:name type:string
~ itr:m_SavedProperties.m_Floats.Array.data[11].second displayName:Second name:second type:float
~ itr:m_SavedProperties.m_Colors displayName:Colors name:m_Colors type:map
~ itr:m_SavedProperties.m_Colors.Array.size displayName:Size name:size type:ArraySize
~ itr:m_SavedProperties.m_Colors.Array.data[0] displayName:Element 0 name:data type:pair
~ itr:m_SavedProperties.m_Colors.Array.data[0].first displayName:First name:first type:FastPropertyName
~ itr:m_SavedProperties.m_Colors.Array.data[0].first.name displayName:Name name:name type:string
~ itr:m_SavedProperties.m_Colors.Array.data[0].second displayName:Second name:second type:Color
~ itr:m_SavedProperties.m_Colors.Array.data[1] displayName:Element 1 name:data type:pair
~ itr:m_SavedProperties.m_Colors.Array.data[1].first displayName:First name:first type:FastPropertyName
~ itr:m_SavedProperties.m_Colors.Array.data[1].first.name displayName:Name name:name type:string
~ itr:m_SavedProperties.m_Colors.Array.data[1].second displayName:Second name:second type:Color
								
								*/
								
								
								
								// もしサンプルとの差があれば、この素材には変更があったものとして、関連するPrefabの作成時にprefabのキャッシュを消すとかする。
								// if (!same) {
								// 	effector.ForceOnPreprocessTexture(texImporter);
								// 	importSetOveredAssetsAndUpdatedFlagDict[inputSource] = true;
								// }
								
								// とりあえず決め打ちで、変化があったものとしてみなす。デバッグ中。
								importSetOveredAssetsAndUpdatedFlagDict[inputSource] = true;
								break;
							}
							default: {
								throw new Exception("unhandled modifier type:" + importerTypeStr);
							}
						}
					}
				}
			}


			/*
				inputSetting sequence is over.
			*/
			
			var outputSources = new List<InternalAssetData>();
			
			
			foreach (var inputAsset in inputSources) {
				var updated = importSetOveredAssetsAndUpdatedFlagDict[inputAsset];
				if (!updated) {
					// already set completed.
					var newInternalAssetData = InternalAssetData.InternalAssetDataGeneratedByImporterOrModifierOrPrefabricator(
						inputAsset.importedPath,
						AssetDatabase.AssetPathToGUID(inputAsset.importedPath),
						AssetGraphInternalFunctions.GetAssetType(inputAsset.importedPath),
						false,// not changed.
						false
					);
					outputSources.Add(newInternalAssetData);
				} else {
					// updated asset.
					var newInternalAssetData = InternalAssetData.InternalAssetDataGeneratedByImporterOrModifierOrPrefabricator(
						inputAsset.importedPath,
						AssetDatabase.AssetPathToGUID(inputAsset.importedPath),
						AssetGraphInternalFunctions.GetAssetType(inputAsset.importedPath),
						true,// changed.
						false
					);
					outputSources.Add(newInternalAssetData);
				}
			}
			
			outputDict[the1stGroupKey] = outputSources;

			Output(nodeId, labelToNext, outputDict, usedCache);
		}

		public static void ValidateModifierSample (string samplePath, 
			Action<string> NoSampleFolderFound, 
			Action<string> NoSampleFound, 
			Action<string> ValidSampleFound,
			Action<string> TooManySampleFound
		) {
			if (Directory.Exists(samplePath)) {
				var filesInSampling = FileController.FilePathsInFolderOnly1Level(samplePath)
					.Where(path => !path.EndsWith(AssetGraphSettings.UNITY_METAFILE_EXTENSION))
					.ToList();

				switch (filesInSampling.Count) {
					case 0: {
						NoSampleFound("no importSetting file found in ImporterSetting directory:" + samplePath + ", please reload first.");
						return;
					}
					case 1: {
						ValidSampleFound(filesInSampling[0]);
						return;
					}
					default: {
						TooManySampleFound("too many samples in ImporterSetting directory:" + samplePath);
						return;
					}
				}
			}

			NoSampleFolderFound("no samples found in ImporterSetting directory:" + samplePath + ", applying default importer settings. If you want to set Importer seting, please Reload and set import setting from the inspector of Importer node.");
		}
		
		public Type AssumeTypeFromExtension () {
			// no mean. nobody can predict type of asset before import.
			return typeof(UnityEngine.Object);
		}

	}
}
