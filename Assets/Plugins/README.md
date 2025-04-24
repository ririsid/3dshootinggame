# 플러그인 디렉토리 (Plugins Directory)

이 디렉토리에는 프로젝트에서 사용하는 서드파티 플러그인 및 에셋이 포함되어 있습니다. 저작권 보호를 위해 대부분의 플러그인은 `.gitignore`에 의해 저장소에서 제외되어 있습니다.

## 플러그인 디렉토리 구조

모든 서드파티 플러그인은 `Assets/Plugins` 폴더 아래에 배치해야 합니다. 각 플러그인은 자체 하위 폴더에 위치하여 프로젝트 구조를 깔끔하게 유지합니다:

```
Assets/
  Plugins/
    Demigiant/          # DOTween 플러그인
    JMO Assets/         # War FX 플러그인
    기타 플러그인 폴더/
    README.md           # 이 문서
```

이러한 구조는 플러그인 관리와 업데이트를 용이하게 하며, 특정 플러그인을 `.gitignore`에 추가하는 것도 간편하게 만듭니다.

## 사용 중인 플러그인 목록

아래는 프로젝트에서 사용하는 주요 플러그인 목록입니다. 저장소에 포함되지 않은 플러그인을 설치하려면 각 플러그인의 구매/다운로드 링크를 참조하세요. (Unity 내장 패키지는 이 목록에서 제외되었습니다.)

| 플러그인 이름       | 버전    | 라이선스   | 용도               | 설치 경로                        | 구매/다운로드 링크                                                                                 |
| ------------------- | ------- | ---------- | ------------------ | -------------------------------- | -------------------------------------------------------------------------------------------------- |
| DOTween             | 1.2.765 | 무료       | 애니메이션 트윈    | Assets/Plugins/Demigiant/        | [Asset Store 링크](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676) |
| War FX              | 1.8.04  | 무료       | 전투 이펙트 시스템 | Assets/Plugins/JMO Assets/WarFX/ | [Asset Store 링크](https://assetstore.unity.com/packages/vfx/particles/war-fx-5669)                |
| Material-Icons Font | 1.0.1   | Apache 2.0 | UI 아이콘 폰트     | Unity Package Manager (OpenUPM)  | [OpenUPM 링크](https://openupm.com/packages/com.fonts.material-icons/)                             |

## 설치 지침

1. **저장소 클론 후 초기 설정**:

   - 저장소를 클론한 후에는 `.gitignore`에 의해 제외된 플러그인을 수동으로 설치해야 합니다.
   - 필요한 에셋을 Asset Store에서 구매하고 프로젝트에 임포트하세요.
   - 모든 플러그인은 `Assets/Plugins` 폴더 내에 위치해야 합니다.

2. **DOTween**:

   - Asset Store에서 "DOTween"을 다운로드하세요.
   - 임포트 후 셋업 마법사를 실행하여 필요한 모듈을 설치하세요.
   - 설치 시 `Assets/Plugins/Demigiant/` 경로에 임포트되도록 설정하세요.

3. **War FX**:

   - Asset Store에서 "War FX"를 다운로드하세요.
   - 프로젝트에 임포트 시 샘플 씬을 포함할지 선택할 수 있습니다.
   - 임포트 후 필요한 경우 `Assets/Plugins/JMO Assets/` 폴더로 이동시키세요.

4. **Unity 내장 패키지**:

   - 이 프로젝트는 TextMeshPro, Cinemachine 등의 Unity 내장 패키지를 사용합니다.
   - 이러한 패키지는 Window > Package Manager에서 "Unity Registry"를 선택하여 설치할 수 있습니다.
   - 각 패키지를 검색하고 "Install" 버튼을 클릭하세요.

5. **OpenUPM 패키지**:
   - 이 프로젝트는 OpenUPM을 통해 패키지를 관리할 수 있습니다.
   - `Material-Icons Font` 패키지는 다음 명령어를 사용하여 설치할 수 있습니다 (프로젝트 루트 디렉토리에서 실행):
     ```bash
     openupm add com.fonts.material-icons
     ```
   - 또는 Unity Package Manager의 Git URL 기능을 사용하여 설치할 수도 있습니다.
   - OpenUPM 설정 및 사용법은 [OpenUPM 문서](https://openupm.com/)를 참조하세요.

## 라이선스 정보

이 프로젝트는 여러 서드파티 플러그인을 사용합니다. 각 플러그인은 자체 라이선스 조건을 가지고 있으며, 이를 준수해야 합니다.

- **상용 에셋**: 팀 내부에서만 사용 가능하며, 재배포가 금지됩니다.
- **무료 에셋**: 각 에셋의 라이선스 조건을 확인하세요.
- **오픈 소스 플러그인**: 소스 코드 수정 시 라이선스 조건을 준수하세요.

## 추가 및 업데이트 가이드라인

1. 새 플러그인을 추가할 때는 이 README.md 파일을 업데이트하세요.
2. 유료 에셋을 추가하는 경우 반드시 `.gitignore`에 해당 경로를 추가하세요.
3. 모든 플러그인은 `Assets/Plugins` 디렉토리 내에 설치하세요.
4. 플러그인 업데이트 후에는 호환성 문제가 없는지 확인하세요.

## 문제 해결

플러그인 관련 문제가 발생한 경우:

1. Unity 에디터를 재시작하세요.
2. 플러그인을 재임포트하세요.
3. 플러그인의 공식 문서나 지원 포럼을 확인하세요.
4. 프로젝트 이슈 트래커에 문제를 보고하세요.
