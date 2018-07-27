using UnityEngine;
using NUnit.Framework;
using Unity.AssetGraph;

internal class LastImportedItemsTest : AssetGraphEditorBaseTest
{
	protected override void CreateResourcesForTests()
	{
		CreateTestPrefab("", "prefab01", PrimitiveType.Cube);
		CreateTestPrefab("", "prefab02", PrimitiveType.Cube);
		CreateTestPrefab("", "prefab03", PrimitiveType.Cube);
	}

	[Test]
	public void TestLastImportedItems()
	{
		AssertGraphExecuteWithNoIssue();
	}

}
