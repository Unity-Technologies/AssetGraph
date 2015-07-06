using UnityEditor;

[InitializeOnLoad]
public class TestIgniter {

	static TestIgniter () {
		var testContext = new Test();
		testContext.RunTests();
	}

}