using UnityEngine;
using NUnit.Framework;
using Unity.AssetGraph;

internal class ExportToDirectoryTest : AssetGraphEditorBaseTest
{
	protected override void CreateResourcesForTests()
	{
		CreateTestPrefab("Good/", "prefab01", PrimitiveType.Cube);
		CreateTestPrefab("Good/", "prefab02", PrimitiveType.Cube);
		CreateTestPrefab("Bad/", "prefab03", PrimitiveType.Cube);
		CreateTestPrefab("Bad/", "prefab04", PrimitiveType.Cube);
	}

	[Test]
	public void SampleTest()
	{
		var result = AssertGraphExecuteWithIssue();
		
		foreach (var e in result.Issues)
		{
			Assert.AreEqual(e.Node.Operation.ClassName, typeof(AssertUnwantedAssetsInBundle).AssemblyQualifiedName);
			Assert.True(e.Reason.Contains("/Bad/"));
		}
	}
	
	[Test]
	public void SampleTestNoIssue()
	{
		AssertGraphExecuteWithNoIssue();
	}
}
