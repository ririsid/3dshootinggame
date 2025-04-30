# 3D 슈팅 게임

SAY 3D 게임 개발 실습 프로젝트입니다.

## 프로젝트 정보

- **프로젝트명**: 3D 슈팅 게임
- **엔진**: Unity 6
- **개발 기간**: 2025년 4월 ~ 2025년 5월
- **개발 목적**: SAY 3D 게임 개발 실습

## 시작하기

### 개발 사양

- Unity 6 (6000.0.40f1)

### 설치 방법

1. 이 저장소를 클론합니다:
   ```
   git clone https://github.com/ririsid/3dshootinggame.git
   ```
2. Unity Hub에서 프로젝트를 엽니다.
3. 누락된 플러그인을 설치합니다. (아래 ['에셋 및 플러그인' 섹션](#에셋-및-플러그인-사용-주의사항) 참조)

## 프로젝트 구조

- `Assets/01.Scenes/`: 게임의 씬 파일
- `Assets/02.Scripts/`: 게임 로직 스크립트
- `Assets/03.ScriptableObjects/`: ScriptableObject 데이터
- `Assets/04.Prefabs/`: 재사용 가능한 프리팹
- `Assets/05.Images/`: 이미지 파일
- `Assets/06.Models/`: 3D 모델
- `Assets/07.Materials/`: 머티리얼
- `Assets/08.Animations/`: 애니메이션 파일
- `Assets/09.VFX/`: 파티클 시스템과 시각 효과
- `Assets/10.Audio/`: 오디오 파일
- `Assets/11.Fonts/`: 게임 내 사용되는 폰트 파일
- `Assets/Plugins/`: 서드파티 플러그인 (세부 사항은 아래 참조)

### 디렉토리 구조 설계 원칙

- 자주 사용하는 디렉토리(Scenes, Scripts)를 상단에 배치
- 관련 자산을 서로 가까이 배치하여 작업 효율성 향상 (예: Scripts와 ScriptableObjects, Models와 Materials)

## 에셋 및 플러그인 사용 주의사항

본 프로젝트는 여러 서드파티 플러그인을 사용합니다. 저작권 보호를 위해 대부분의 플러그인은 저장소에 포함되어 있지 않습니다.

### 플러그인 설치

1. 저장소를 클론한 후에는 `.gitignore`에 의해 제외된 플러그인을 수동으로 설치해야 합니다.
2. 필요한 에셋을 Unity Asset Store에서 구매하고 프로젝트에 임포트하세요.
3. 각 플러그인의 세부 설치 방법은 [플러그인 설치 가이드](Assets/Plugins/README.md)를 참조하세요.

### 사용 중인 플러그인 목록

이 프로젝트에서 사용 중인 주요 플러그인은 다음과 같습니다:

- **[DOTween](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676)**: 애니메이션 트윈 시스템
- **[War FX](https://assetstore.unity.com/packages/vfx/particles/war-fx-5669)**: 전투 이펙트 시스템
- **[Material-Icons Font](https://openupm.com/packages/com.fonts.material-icons/)**: UI 아이콘 폰트

## 개발 가이드라인

- git-flow 브랜칭 전략을 따릅니다.
- 코드 컨벤션을 준수해 주세요.
- 외부 에셋을 추가할 경우 반드시 `.gitignore`에 해당 경로를 추가하세요.

## 라이선스

이 프로젝트는 교육(실습)용이며, 사용된 각 플러그인은 해당 라이선스를 따릅니다.

## 문의사항

프로젝트 관련 문의사항은 프로젝트 관리자에게 연락하세요.
