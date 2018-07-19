#if UNITY_5_6_OR_NEWER
using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.IO;
using UnityEngine.AssetGraph;

using UnityEngine.AssetGraph.DataModel.Version2;

public class PathTest {

	[Test]
	public void PathTestSimplePasses() {
		// Use the Assert class to test conditions.

        string baseDirName = Settings.Path.ToolDirName;
        Assert.IsFalse(string.IsNullOrEmpty(baseDirName));

        Assert.IsTrue(AssetGraphBasePath.BasePath.Contains(Settings.Path.ToolDirName));

		string basePath = AssetGraphBasePath.BasePath;
		string cachePath = AssetGraphBasePath.CachePath;
        this.TestPath(Path.Combine(basePath, "Editor/ScriptTemplate"), Settings.Path.ScriptTemplatePath);

        this.TestPath(Path.Combine(basePath, "Generated/Editor"), Settings.Path.UserSpacePath);
        this.TestPath(Path.Combine(basePath, "Generated/CUI"), Settings.Path.CUISpacePath);
        this.TestPath(Path.Combine(basePath, "SavedSettings"), Settings.Path.SavedSettingsPath);
		this.TestPath(Path.Combine(cachePath, "TemporalSettingFiles"), AssetGraphBasePath.TemporalSettingFilePath);

        this.TestPath(Path.Combine(basePath, "Editor/GUI/GraphicResources"), Settings.Path.GUIResourceBasePath);
		this.TestPath(Path.Combine(Path.Combine(basePath, "Editor/GUI/GraphicResources"), "ConnectionPoint.png"), Settings.GUI.ConnectionPoint);
		this.TestPath(Path.Combine(Path.Combine(basePath, "Editor/GUI/GraphicResources"), "InputBG.png"), Settings.GUI.InputBG);
		this.TestPath(Path.Combine(Path.Combine(basePath, "Editor/GUI/GraphicResources"), "NodeStyle.guiskin"), Settings.GUI.Skin);
		this.TestPath(Path.Combine(Path.Combine(basePath, "Editor/GUI/GraphicResources"), "OutputBG.png"), Settings.GUI.OutputBG);
    }

	// A UnityTest behaves like a coroutine in PlayMode
	// and allows you to yield null to skip a frame in EditMode
	[UnityTest]
	public IEnumerator PathTestWithEnumeratorPasses() {
		// Use the Assert class to test conditions.
		// yield to skip a frame
		yield return null;
	}

    private void TestPath(string expected, string path)
    {
        Assert.AreEqual(expected, path);
    }
}
#endif
