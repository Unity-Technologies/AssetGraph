#AssetGraph Document
document version 0.8.0

##TOC
* 사용법
* AssetGraph가 편리한 이유
* 역방향 AssetGraph (how to)
* Node 정보
* HookPoint 팁
* Package 팁


#사용법
AssetGraph는 Unity의 AssetBundle을 만들기 위한 GUI 도구입니다. 자산으로 설정 및 변경 사항을 반영 흐름을 만들고 AssetBundle 생성 및 기타 자산 생성 / 조정 등 모​​든 처리를 코드 작성없이 할 수 있습니다.

###1.노드 생성
AssetGraph 창에서 마우스 오른쪽 클릭하여 노드를 추가합니다. 여러 노드를 만들 수 있습니다만, 우선 Loader를 만들어 봅니다. Loader는 AssetGraph 내로 Unity 프로젝트 내부의 에셋을 가져오는 노드입니다. 
![SS](/Doc/images/1.png)

###2.노드 연결
2 개 이상의 노드를 만들면, 그들을 이어갑니다. 노드의 좌우 양쪽의 둥근 부분을 다른 노드의 둥근 부분에 드래그 앤 드롭하면 연결됩니다.

연결하면 어떻게 될까? 왼쪽 Loader 노드에서 오른쪽 Filter 노드로 소재가 흐르게됩니다. 연결선의 중간에 숫자가 흐르는 소재의 개수입니다. 수치가 나와있는 부분을 누르면 흐르고있는 소재 목록이 Inspector에 표시됩니다.

![SS](/Doc/images/2.png)

###3.노드 설정 변경
노드를 왼쪽 클릭하면 Inspector 세세한 세팅이 표시됩니다. 노드의 종류마다 다양한 항목을 설정할 수 있습니다. 예를 들어 Filter 노드는 흐르는 소재를 이름마다 여러 흐름으로 나눌, 같은 수 있습니다. Importer 노드에서는 자산을 가져올 때의 세팅을 자유 자재로 설정할 수 있습니다.

노드의 설정을 변경하면 해당 설정은 노드를 통과하는 모든 자산에 자동으로 설정됩니다.

![SS](/Doc/images/3.png)

###4.빌드
AssetGraph 창에서 Build 버튼을 누르면 연결되어있는 노드에 소재가 흘러 처리됩니다.

Bundlizer 노드와 BundleBuild 노드가 있으면 해당 노드에 들어왔다 자산이 AssetBundle됩니다.

![SS](/Doc/images/4.png)  
![SS](/Doc/images/5.png)    
![SS](/Doc/images/6.png)

물론 캐시가 효과 때문에 2 번째 이후는 차등 소재를 처리 할 시간 밖에 걸리지 않습니다.

GUI에서 노드의 설정을 변경하는 것만으로, 어떤 양이라도 어떤 귀찮은 작업에서도 여러 번 실행할 수 있습니다.

네, 간단 하죠?




#AssetGraph가 편리한 이유
AssetGraph는 소재의 조정과 설정이 코드를 일절 쓰지 않고 제공하므로 프로그래머의 도움이 될뿐만 아니라 아티스트와 게임 디자이너도 훨씬 오는 것으로되어있다 하즈입니다. 특히 AssetBundle 주위 대해 어떠한 코드를 작성하지 않고도 만들 수있게된다는 것은 좋은 것입니다.

게임을 만들어가는 과정에서 소재가 늘어나는 것은 피할 수없는 일이지만, 그 소재를 손으로 조정하지 않아도, AssetGraph에서 소재의 조정을 자동화 해 버리면 괜찮아. 새로 추가 된 소재도 지금까지대로의 흐름을 타고 처리되므로 불필요한 수고가 필요 없습니다. 추가 된 유사한 소재 100 개를 하나씩 손으로 ,, 같은 지옥과 작별 할 수 있습니다.

게다가 AssetGraph에서 AssetBundle 이외에도 직접 만든 압축 / 암호화 Prefab 만들기 (코드 필수) 가져온 것을 에셋 채 어딘가에 낸다라고 같은 것까지 할 수 있습니다


#역방향 AssetGraph(how to)


##여러 소재를 하나의 AssetBundle하기
전혀 코드를 작성하지 않고 폴더에 들어있는 자료를 AssetGraph에로드 한 AssetBundle 할 수 있습니다.

1. Loader에서 소재가있는 폴더를 지정
1. Importer에서 소재를 가져 오기
1. Bundlizer에서 소재의 AssetBundle 화
1. BundleBuilder에서 AssetBundle 설정 생성
1. Exporter에서 AssetBundle을 생성하고 디스크에 기록

![SS](/Doc/images/howto_0.gif)


##여러 소재를 여러 AssetBundle하기
Grouping 노드를 사용하여 소재를 여러 그룹으로 나눌 수 있습니다.

Prefabricator과 Bundlizer는 Prefab과 AssetBundle을 만들 때 그룹 단위로 생성 할 수 쉽게 할 수 있도록되어 있습니다.

다음과 같은 흐름에서 그룹 단위로 AssetBundle을 만들 수 있습니다.

1. Loader에서 소재가있는 폴더를 지정
1. Importer에서 소재를 가져 오기
1. Groupimg에서 소재를 그룹화
1. Bundlizer으로 그룹화 된 소재를 AssetBundle 화
1. BundleBuilder에서 AssetBundle 설정 생성
1. Exporter에서 AssetBundle의 쏟아

![SS](/Doc/images/howto_1.gif)

포인트는 3,4에서 Grouping 여러 소재에서 그룹을 생성, Bundlizer에서 그룹마다 AssetBundle을 만들고 있습니다.


##단번에 대량의 소재를 가져올
프로젝트로 가져올 소재를 AssetGraph 프로젝트 폴더에 놓고 그 경로를 Loader로 지정 Importer으로 연결하면 단번에 소재의 수입 처리 할 수​​ 있습니다.

1. Loader에서 소재가있는 폴더를 지정
1. Importer를 연결하여 Importer의 관리자 Modify Import Setting 버튼에서 가져 오기 설정을 설정하는

![SS](/Doc/images/howto_2.gif)

이것만으로 Importer 노드를 통과 한 소재 모든 가져 오기 설정을 할 수 있습니다.

여러 종류의 소재 (eg image와 model 등)가 포함되어있는 경우 Filter를 사용하여 소재의 종류별로 Importer를 준비하면 설정이 편하고 좋을 것입니다.

1. Loader에서 소재가 놓여져있는 경로를 지정
1. Filter에서 소재 이름이나 경로에서 소재를 구분
1. Importer를 연결하여 Importer의 Modify Import Setting 버튼에서 가져 오기 설정 세트

![SS](/Doc/images/howto_3.gif)

##소재를 여러 그룹으로 나눈다
예를 들어 게임의 캐릭터가 여럿 있지만, 그들이 텍스처 + 모델로 구성되어있을 때,

Grouping 노드를 사용하면 여러 소재를 캐릭터 1 소재의 모임 (텍스쳐 + 모델), 캐릭터 2의 소재 모임 (텍스쳐 + 모델) 등과 같이 분류 할 수 있습니다.

![SS](/Doc/images/howto_4.gif)

[Grouping](https://github.com/unity3d-jp/AssetGraph/blob/master/Doc/Document.md#grouping)

##소재에서 Prefab을 만들기
AssetGraph에서 소재를로드 Prefab을 만들 수 있습니다. 그러나 Asset을 지정하거나 인스턴스화 할 필요가 있기 때문에 그 작업을 C # 스크립트로 작성할 필요가 있습니다. 스크립트는 다음과 같다.

```C#
using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Collections.Generic;

public class MyPrefabricator : AssetGraph.PrefabricatorBase {
	public override void In (string groupKey, List<AssetGraph.AssetInfo> source, string recommendedPrefabOutputDir, Func<GameObject, string, bool, string> Prefabricate) {
		// let's generate Prefab with "Prefabricate" method.
	}
}
```

AssetGraph.PrefabricatorBase 클래스를 상속하고 In 메소드를 가지는 Script입니다. 이 Script는 Window> AssetGraph> Generate Script For Node> Prefabricator Script에서 자동으로 편지지를 만들 수 있습니다.

스크립트를 만든 후 그 명을 AssetGraph의 Prefabricator 노드로 설정하여 노드에 나왔던 Asset가 자동으로 스크립트를 통과하게됩니다

![SS](/Doc/images/howto_5.gif)


Prefabricator 노드에 어떤 Asset 어떤 그룹 이름 순서로 들어 왔나은 Prefabricator 노드에 연결되어있는 Connection에서 상정 할 수 있습니다.

![SS](/Doc/images/howto_6.png)

이 경우 groupKey "0"dummy.png, kiosk001.mat, sample.fbx의 세 가지가 차례로 source에 들어간 상태에서 Prefabricator을 확장 한 In 메소드를 호출합니다.

![SS](/Doc/images/howto_7.png)

Prefab 만들기에 Prefabricate 메소드를 사용하면 캐시가 효과가 편리합니다.


[SamplePrefabricator](https://github.com/unity3d-jp/AssetGraph/blob/master/Assets/AssetGraph/UserSpace/Examples/Editor/SamplePrefabricator.cs)의 샘플 코드를 참고하세요.


##소재 그룹에서 Prefab을 만들기
Grouping 노드에서 여러 그룹을 만들어 Prefabricator 노드에 연결하면 여러 그룹화 된 소재를 Prefab 생성에 사용할 수 있습니다. 여러 그룹은 PrefabricatorBase을 확장 한 스크립트에서 groupKey 값으로 사용할 수 있습니다.


##명령줄에서 실행
AssetGraph는 명령 줄에서 실행할 수 있습니다. UnityEditor에 설정되어있는 플랫폼을 사용하는 경우 다음과 같은 shellScript / batch 실행하면 좋을 것입니다.

```shellscript
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -quit\ 
	-projectPath $(pwd)\
	-executeMethod AssetGraph.AssetGraph.Build
```

또한 다음과 같은 shellScript / batch에서 플랫폼을 사용하여 AssetGraph을 수행 할 수 있습니다.

```shellscript
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -quit\ 
	-projectPath $(pwd)\
	-executeMethod AssetGraph.AssetGraph.Build iOS
```

package를 지정하는 경우는 플랫폼의 뒤에 package 이름을 지정하여 실행할 수 있습니다.

```shellscript
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -quit\ 
	-projectPath $(pwd)\
	-executeMethod AssetGraph.AssetGraph.Build iOS newPackage
```

샘플 shellScript 보기
[build.sh](https://github.com/unity3d-jp/AssetGraph/blob/master/Assets/AssetGraph/UserSpace/build.sh)

##가져온 파일이나 작성한 Prefab, AssetBundle을 Assets / 밖으로 쏟아 싶다
Exporter 노드에서 가져온 파일이나 Prefab, AssetBundle을 출력 할 수 있습니다.


##빌드 실행 후의 처리 작업
AssetGraph는 Finally라는 훅 포인트가 빌드 완료되면 시작 Finally라는기구가 있습니다.

[Finally](https://github.com/unity3d-jp/AssetGraph/blob/master/Doc/Document.md#hookpoint-finally-tips)


##작성한 AssetBundle의 crc 및 크기 정보의 확인
Unity5에서 AssetBundle 정보는 .manifest 파일로 토출되게되었습니다. Finally기구를 이용하여 정보를 읽는 방법을 소개합니다.

[Finally](https://github.com/unity3d-jp/AssetGraph/blob/master/Doc/Document.md#assetbundleのmanifestからjsonでリストを作り出す)


##나의 플랫폼 안에, 다 언어 특정 단말 용 등의 조정을 원하는
만들고있는 게임의 대응 단말의 디스플레이 크기가 다양하기도하고, 각국 버전을 같은 흐름으로 내고 싶다,라고하는 경우가 많습니다. 그런 경우 AssetGraph에서는 package 설정을 직접 생성하여 처리 할 수​​ 있습니다.

[package](https://github.com/unity3d-jp/AssetGraph/blob/master/Doc/Document.md#package-tips)


##variants를 설정하고 싶은
variants 그 것은 취급하지 않지만, pacakge를 사용하여 유사한 것이 더 쉽게 할 수 있습니다.

[package](https://github.com/unity3d-jp/AssetGraph/blob/master/Doc/Document.md#package-tips)



##캐시 삭제
AssetGraph에서는 한 번 실행 한 가져 오기 프로세스 나 Prefab 생성 처리, AssetBundle 생성 프로세스를 캐시 같은 내용이면 캐시를 사용하면 불필요한 시간을 줄일 수 있습니다. 캐시의 실태 파일 Assets / AssetGraph / Cache 폴더에 있습니다.

Unity 메뉴에서 Clear Cache를 선택하여 AssetGraph 내에있는 모든 파일의 캐시를 지울 수 있습니다.

Unity> Window> AssetGraph> Clear Cache


##그래프 데이터의 삭제
AssetGraph 창을 열면 응답하도록되어 버리는 등 가져 오려는 파일에 의해 상태가 나빠져 버린 경우 Assets / AssetGraph / SettingFiles / AssetGraph.json 파일을 삭제하면 그래프의 데이터를 지울 수 수 있습니다.


#Node의 정보
##Loader
- OUT: 지정한 폴더에 들어있는 모든 자료

Loader path에 지정된 폴더에 들어있는 Asset을 읽는다.

![SS](/Doc/images/7_loader.png)

loadPath은 Project 폴더의 경로에서 아래를 지정할 수있다. Project 폴더에 AssetGraph 용 소재를 둔 폴더를 만들 것을 추천.

Assets / 경로를 사용하여 이미 프로젝트에서 사용하고있는 Asset을 사용할 수도있다.


##Filter
- IN: 여러 Asset
- OUT: keyword에 맞춘 여러 Asset

경로에 keyword를 포함한 소재를 여러 출력에 배분할 수 있습니다.

![SS](/Doc/images/7_filter.png)

keyword는 여러 설정 할 수 있습니다.


##Importer
- IN: 여러 Asset
- OUT: 여러 Asset

하나 ~ 둘 이상의 파일을 가져옵니다. Inspector에서 가져 오기 설정을 조정할 수 있습니다.

![SS](/Doc/images/7_importer.png)

Importer 노드에서 이미 가져온 소재를 다시 다른 Impoter 노드를 통과하면 그 소재는 다시 가져 오지 않습니다.

1 개의 Importer에서 가져 오기 설정을 할 수는 이미지 / 모델 / 음성에서 한 종류입니다. 예를 들어 이미지와 모델을 정리하고이 노드에 보내는 이미지 또는 모델 중 하나에서만 조정이 가능합니다. 하나의 Importer 노드에 대해 가능한 한 종류의 물건을 보내는 것을 권장합니다.

##Grouping
- IN: 여러 Asset
- OUT: 그룹화 된 여러 Asset

키워드를 사용하여 소재를 여러 그룹으로 나눌 수 있습니다.。

![SS](/Doc/images/7_grouping_0.png)
![SS](/Doc/images/7_grouping_1.png)
![SS](/Doc/images/7_grouping_2.png)

Inspector에서 group Key에 '그룹화에 사용하는 키워드 "를 지정하면 소재의 경로에서 여러 그룹이 만들어집니다.

group Key는 * 기호를 와일드 카드로 사용할 수 있습니다.

예를 들어 폴더 이름 / ID_mainChara / / ID_enemy / 등이 붙어있는 경우 group Key에 / ID _ * / 세트하면 "mainChara", "enemy"두 그룹이 생성됩니다.

이미 그룹화 된 Asset을 Grouping 노드에 통과 시키면 일단 그룹화가 해제되어 다시 그룹화됩니다.

##Prefabricator
- IN:  Prefab의 소재에 원하는 Asset 그룹
- OUT: 생성 된 Prefab을 포함 Asset 그룹

입력 된 Asset에서 스크립트를 통해 Prefab을 만들 수 있습니다. 출력되는 Asset 입력 된 Asset로 작성된 Prefab을 맞춘 것입니다.

![SS](/Doc/images/7_prefabricator.png)

PrefabricatorBase을 확장 한 스크립트를 쓰고 설정하여 사용합니다. 불행히도, 스크립트없이이 노드를 사용 할 수 없습니다.

Prefabricator 노드에는 두 가지 만드는 방법이 있습니다.

1. GUI에서 만든 것에 스크립트 이름을 입력
1. 스크립트를 AssetGraph 창에 드래그 앤 드롭하기

스크립트는 AssetGraph.PrefabricatorBase 클래스를 확장하고 public override void In (string groupKey, List source, string recommendedPrefabOutputDir, Func Prefabricate) 메소드를 재정의해야합니다.

샘플 스크립트 [CreateCharaPrefab.cs](https://github.com/unity3d-jp/AssetGraph/blob/master/Assets/AssetGraph/UserSpace/Examples/Editor/CreateCharaPrefab.cs)



##Bundlizer
- In: AssetBundle의 소재에 원하는 Asset 그룹
- Out: 그룹별로 생성 된 AssetBundle

입력 된 Asset에서 AssetBundle을 만들 수 있습니다. 
생성되는 AssetBundle의 이름은 BundleNameTemplate 매개 변수로 지정할 수 있습니다.

![SS](/Doc/images/7_bundlizer_0.png)
![SS](/Doc/images/7_bundlizer_1.png)
![SS](/Doc/images/7_bundlizer_2.png)

이때 BundleNameTemplate에 *가 포함되어 있으면, 거기에는 그룹 ID가 자동으로 설정됩니다.

*가 포함되지 않은 경우 AssetBundle에는 BundleNameTemplate 거리 이름이 붙습니다.

이 기능은 예를 들어 캐릭터의 AssetBundle을 그 캐릭터의 ID를 포함한 이름으로 만들고 싶어 같은 경우에 그룹 ID = 캐릭터 ID가되는 것 같은 흐름을 짜두면 자동으로 AssetBundle가 명명 될 수 되기 때문에 매우 효과적입니다.

또한이 노드로부터 출력되는 Asset은 작성된 AssetBundle 만합니다. 이 노드에서 연결 노드는 1 종류, BundleBuilder 노드뿐입니다.

Bundlizer 노드에는 두 가지 만드는 방법이 있습니다.

1. GUI에서 만든 것으로 AssetBundle 이름의 템플릿을 입력
1. BundlizerBase을 확장 한 스크립트를 AssetGraph 창에 드래그 앤 드롭하기

두 번째 방법은 직접 준비한 스크립트를 실행 할 수 있습니다. 스크립트는 AssetGraph.PrefabricatorBase 클래스를 확장하고 public override void In (string groupKey, List source, string recommendedPrefabOutputDir, Func Prefabricate) 메소드를 재정의해야합니다.

샘플 스크립트 [CreateCharaBundle.cs](https://github.com/unity3d-jp/AssetGraph/blob/master/Assets/AssetGraph/UserSpace/Examples/Editor/CreateCharaBundle.cs)

이 방법은 AssetBundle을 만드는 코드를 수 세세하게 쓰고 실행할 수 있으며 스스로 생각한 압축 및 암호화 등을 할 수 있습니다.

그러나 Bundlizer에서 BundleBuilder 노드 이외에 연결할 수 없기 때문에 스스로 AssetBundle을 만드는 코드를 쓴 경우라도 BundleBuilder로 연결해야합니다

##BundleBuilder
- In: AssetBundle 등 그룹
- Out: 실제로 생성 된 AssetBundle 등 그룹

Bundlizer에서 설정 한 AssetBundle을 실제로 생성합니다. 다양한 옵션을 설정할 수 있습니다.

![SS](/Doc/images/7_bundlebuilder.png)

Bundlizer 이외에서 연결할 수 없습니다. 코드없이 Bundlizer에서 AssetBundle 만들기를 한 경우에만이 노드에서 AssetBundle의 설정을 할 수 있습니다.

##Exporter
- In: 가져온 or AssetGraph에서 개발 된 자산 그룹

지정한 경로에 파일을 출력 할 수 있습니다.

![SS](/Doc/images/7_exporter.png)

지정할 수있는 경로는 프로젝트 폴더 이하이면 자유롭게 지정할 수 있습니다. 그러나 지정한 폴더는 실행 전에 만들어 두지 않으면 안됩니다.


#HookPoint: Finally tips
지정할 수있는 경로는 프로젝트 폴더 이하이면 자유롭게 지정할 수 있습니다. 그러나 지정한 폴더는 실행 전에 만들어 두지 않으면 안됩니다.

##FinallyBase 클래스를 extends하기
FinallyBase 클래스를 확장 한 코드는 빌드 프로세스 · 장전 처리가 끝난 시점에서 자동으로 호출됩니다.

public override void Run (Dictionary\<string, Dictionary\<string, List<string>>> throughputs, bool isBuild) 메소드에서 모든 Node 모든 그룹의 실행 결과를받을 수 있습니다.

Dictionary\<string, Dictionary\<string, List\<string>>> 메소드에서 모든 Node 모든 그룹의 실행 결과를받을 수 있습니다.

bool isBuild 빌드 처리시는 true, 그 이외는 false

##모든 노드의 생성물의 경로를 Unity 로그내는 샘플
Finally 샘플로 윈도우 내에있는 모든 노드의 생성물의 파일 경로를 로그에내는이라는 것을 만들어 봅시다.

```C#
using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Collections.Generic;

/**
	sample class for finally hookPoint.

	show results of all nodes.
*/
public class SampleFinally : AssetGraph.FinallyBase {
	public override void Run (Dictionary<string, Dictionary<string, List<string>>> throughputs, bool isBuild) {
		Debug.Log("flnally. isBuild:" + isBuild);

		if (!isBuild) return;
		
		foreach (var nodeName in throughputs.Keys) {
			Debug.Log("nodeName:" + nodeName);

			foreach (var groupKey in throughputs[nodeName].Keys) {
				Debug.Log("	groupKey:" + groupKey);

				foreach (var result in throughputs[nodeName][groupKey]) {
					Debug.Log("		result:" + result);
				}
			}
		}
	}
}
```
[SampleFinally](https://github.com/unity3d-jp/AssetGraph/blob/master/Assets/AssetGraph/UserSpace/Examples/Editor/SampleFinally.cs)

##AssetBundle의 manifest에서 json으로 목록 생성
Finally 예제 2로 AssetBundle 생성시 만들어진 .manifest 파일에서 AssetBundle 정보를 읽어 json 해 봅시다.

Bundlizer, BundleBuilder에서 AssetBundle을 만들고 Exporter에서 AssetBundle을 출력하고 그 AssetBundle의 내용을 json 형식의 목록하겠다는 전제입니다.

Finally에 써야 코드 (발췌)는 다음과 같은 것입니다.

```C#
	public override void Run (Dictionary<string, Dictionary<string, List<string>>> throughputs, bool isBuild) {
		
		// run only build time.
		if (!isBuild) return;

		var bundleInfos = new List<Dictionary<string, object>>();

		// get exported .manifest files from "Exporter" node.

		string targetNodeName = "Exporter0";
		foreach (var groupKey in throughputs[targetNodeName].Keys) {
			foreach (var result in throughputs[targetNodeName][groupKey]) {
				// ignore SOMETHING.ASSET
				if (!result.EndsWith(".manifest")) continue;

				// ignore PLATFORM.manifest file.
				if (result.EndsWith(EditorUserBuildSettings.activeBuildTarget.ToString() + ".manifest")) continue;

				// get bundle info from .manifest file.
				var bundleInfo = GetBundleInfo(result);
				bundleInfos.Add(bundleInfo);
			}
		}

		var bundleListJson = Json.Serialize(bundleInfos);

		Debug.Log(bundleListJson);
```

Exporter 노드의 이름을 지정하는 것으로, 특히 Exporter0라는 노드의 결과에만 주목하고 .manifest 파일에서 AssetBundle의 데이터를 검색하고 있습니다.

결국 그들을 List <Dictionary <string, object >> bundleInfos에 넣고 Json 형식의 string하고 있습니다.

Json함으로써 처리가 편해지 케이스 등으로 쓸만한 생각합니다.

전체 예제는 여기 [SampleFinally2](https://github.com/unity3d-jp/AssetGraph/blob/master/Assets/AssetGraph/UserSpace/Examples/Editor/SampleFinally2.cs)


#Package tips
하나의 플랫폼에서 여러 해상도가 있고, 각각에 맞는 크기의 소재를 사용하여 AssetBundle을 만들려 ,, 그렇게 생각한 것은 잖아요. 있지요.

Unity는 variants라는기구가 있고, Asset 대해 개별적으로 Inspector에서 지정하는 것으로, "동일한 GUID로 다른 내용을 가진 Asset을 일으킨다"라고 할 수 있습니다. AssetGraph는 variants과 약간 다른 접근에서 "동일한 플랫폼에 똑같은 흐름에서 조금 다른 소재를 일으킨다"라고 할 수 있습니다. pacakge를 사용하면 HD 용으로이 크기의 소재를 사용하고 그 외에는이 크기의 소재를 사용, 같은 복잡한 조정을 하나의 흐름 속에서 할 수 있습니다.

##HD 용 소재를 창출하는 예

예를 들어 일반 해상도의 단말에 대한 흐름을 이미 만들고 있기로 HD 해상도의 단말에 대해 소재를 만들기위한 "HD"라는 package를 추가하자. 
Loader의 Inspector에서 + 버튼을 눌러 "HD"라는 package를 만듭니다.

![SS](/Doc/images/8_0.gif)

Inspector에서 pacakge를 HD로 전환 Loader의 경로 설정을 전환하면 Loader에서 나오는 소재의 수가 변화하고 있습니다.

이제 package에 HD를 설정하고 일반 Loader와는 다른 경로에서 소재를 읽도록 설정했습니다.

![SS](/Doc/images/8_1.png)

Inspector에서 package를 HD로 변경하면 AssetGraph 창에 표시되어있는 package도 HD로 바뀝니다. 실행시에 사용하는 package는이 인터페이스를 지정할 수 있습니다.

package 별 설정은 각 노드에서 설정할 수 있습니다. 또한 빌드시에 지정된 package 설정이 존재하지 않는 노드에서는 기본 설정을 사용하여 작동합니다.

이 예에서와 같이 기존의 흐름에 package를 추가하여 일반 해상도의 단말을위한 흐름은 그대로, 특히 HD 버전의 소재를 만드는 경우 HD 전용 이미지를 사용하여 AssetBundle을 만들라고 이 가능하게되어 있습니다.


##variants의 차이
variants는 차이가있는 Asset을 동일한 GUID에서 생성되지만 package는 "package가 다른 것은 모두 다른 Asset"다른 폴더로 출력합니다. 출력되는 AssetBundle의 확장자는 반드시 BUNDLE_NAME.PLATFORM.PACKAGE입니다.

이름이 다른 데서도 알 수 있듯, package가 다른 AssetBundle 사이에 crc 등의 공통성은 없습니다.

##package의 혜택
* 사용자 측에서 개별적으로 "이 Asset에는 HD가 있느냐"등을 런타임에서 세세하게 체크하지 않아도
* package 단위로 다른 폴더하므로 DL 예를 들어 CDN에서 관리 할 때 처리가 편합니다.


##pacakge에서 만든 AssetBundle 사용
사용 방법은 variants과 차이가 없으며 다음과 같은 단계입니다.

1. 단말 측에서 자신이 어떤 package에 속하는지를 판정 할
1. HD를 사용하는 단말기의 경우 끝에 hd와 붙은 AssetBundle을 얻을
1. 취득한 AssetBundle를 사용하는

variants와 다른 점으로는 package가 다른 AssetBundle가 따로 생성되어 있기 때문에 crc 등도 모두 다릅니다. 따라서 HD 용 단말기는 HD 용 AssetBundle의 crc 정보 등을 별도로 지정하여 검색해야합니다.




