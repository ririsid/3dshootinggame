# 3D 슈팅 게임 개발 Copilot 지침

## 프로젝트 구조

이 프로젝트는 Unity 3D로 개발된 슈팅 게임입니다. 프로젝트는 다음 구조를 따릅니다:

- `Assets/01.Scenes`: 게임의 씬 파일
- `Assets/02.Scripts`: 게임 로직 스크립트
- `Assets/03.Prefabs`: 재사용 가능한 프리팹
- `Assets/04.Images`: 텍스처 및 이미지
- `Assets/05.Models`: 3D 모델
- `Assets/06.Sounds`: 오디오 파일
- `Assets/07.Animations`: 애니메이션 파일
- `Assets/08.Materials`: 머티리얼
- `Assets/09.ScriptableObjects`: ScriptableObject 데이터

## 코딩 스타일

### 네이밍 규칙

- `PascalCase`를 클래스, 메서드, 프로퍼티, 인터페이스, enum에 사용합니다.
- `_camelCase`를 프라이빗 변수에 사용합니다(밑줄 접두사 사용).
- `camelCase`를 로컬 변수와 메서드 매개변수에 사용합니다.
- 가능한 한 서술적이고 명확한 이름을 사용합니다.

### MonoBehaviour 생명주기

- `Awake()`는 컴포넌트 참조 및 초기화에 사용합니다.
- `Start()`는 런타임 초기화에 사용합니다.
- `Update()`는 가능한 가볍게 유지하고, 필요한 경우 `FixedUpdate()`와 `LateUpdate()`를 적절히 활용합니다.

### 코드 구성

- 클래스 변수는 상단에 그룹화합니다.
- [SerializeField] 속성으로 인스펙터에 노출할 변수를 표시합니다.
- [Header("...")] 속성으로 인스펙터 변수 그룹을 구분합니다.
- 유니티 이벤트 핸들러는 `#region Unity Event Functions`에 포함합니다.
- 퍼블릭 메서드는 프라이빗 메서드보다 먼저 선언합니다.
- 프로퍼티는 `#region Properties`에 포함합니다.

## 게임 시스템

### 플레이어 이동 시스템

- `PlayerMove` 클래스는 캐릭터 이동, 점프, 구르기, 벽 오르기를 처리합니다.
- 새 이동 시스템을 추가할 때 `PlayerStat`과 통합해야 합니다.
- 스태미너는 달리기, 구르기, 벽 오르기에 영향을 미칩니다.

### 스탯 시스템

- `PlayerStat` 클래스는 플레이어 스탯을 관리합니다.
- `PlayerStatSO` ScriptableObject는 스탯의 기본값을 포함합니다.
- 스탯 수정 시 적절한 이벤트를 발생시켜 UI와 동기화합니다.

### UI 시스템

- UI 클래스는 `UI_` 접두사로 시작합니다.
- UI 업데이트는 이벤트 기반으로 처리하여 성능을 최적화합니다.
- TextMeshPro를 모든 텍스트 표시에 사용합니다.

## 확장 지침

### 성능 고려사항

- 오브젝트 풀링을 사용하여 부하가 많은 인스턴스화/파괴를 방지합니다.
- 가능한 경우 코루틴 대신 DOTween을 사용합니다.
- C# 7.0 이상의 최신 기능을 활용하되, 불필요한 복잡성은 피합니다.
