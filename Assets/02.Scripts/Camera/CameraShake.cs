using UnityEngine;
using System.Collections;
using DG.Tweening;

/// <summary>
/// 카메라 흔들림 효과를 제공하는 컴포넌트입니다.
/// </summary>
public class CameraShake : MonoBehaviour
{
    #region 필드
    [Header("참조")]
    /// <summary>
    /// 실제 카메라 Transform
    /// </summary>
    [SerializeField] private Transform _cameraTransform;

    /// <summary>
    /// 플레이어 발사 컴포넌트
    /// </summary>
    [SerializeField] private PlayerFire _playerFire;

    [Header("흔들림 설정")]
    /// <summary>
    /// 흔들림 활성화 여부
    /// </summary>
    [SerializeField] private bool _enableShake = true;

    /// <summary>
    /// 흔들림 강도
    /// </summary>
    [SerializeField] private float _shakeStrength = 0.05f;

    /// <summary>
    /// 흔들림 지속 시간
    /// </summary>
    [SerializeField] private float _shakeDuration = 0.1f;

    /// <summary>
    /// 흔들림 진동 횟수
    /// </summary>
    [SerializeField] private int _shakeVibrato = 20;

    /// <summary>
    /// 흔들림 무작위성 (0-90)
    /// </summary>
    [SerializeField] private float _shakeRandomness = 90f;

    /// <summary>
    /// 페이드 아웃 여부
    /// </summary>
    [SerializeField] private bool _fadeOut = true;

    [Header("카메라 모드별 강도 배율")]
    /// <summary>
    /// FPS 모드 강도 배율
    /// </summary>
    [SerializeField] private float _fpsShakeMultiplier = 1.0f;

    /// <summary>
    /// TPS 모드 강도 배율
    /// </summary>
    [SerializeField] private float _tpsShakeMultiplier = 0.7f;

    /// <summary>
    /// 쿼터뷰 모드 강도 배율
    /// </summary>
    [SerializeField] private float _quarterShakeMultiplier = 0.4f;

    /// <summary>
    /// 현재 카메라 모드
    /// </summary>
    private CameraEvents.CameraMode _currentCameraMode = CameraEvents.CameraMode.FPS;

    /// <summary>
    /// 현재 실행 중인 흔들림 효과의 Tween
    /// </summary>
    private Tween _currentShakeTween;
    #endregion

    #region Unity 이벤트 함수
    /// <summary>
    /// 초기화를 수행합니다.
    /// </summary>
    private void Start()
    {
        // 카메라 참조가 없으면 자동으로 찾기
        if (_cameraTransform == null)
        {
            // 현재 오브젝트가 카메라의 부모인 경우, 자식 카메라 찾기
            Camera childCamera = GetComponentInChildren<Camera>();
            if (childCamera != null)
                _cameraTransform = childCamera.transform;
            else
                _cameraTransform = Camera.main?.transform;
        }

        // 플레이어 발사 참조 찾기
        if (_playerFire == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                _playerFire = player.GetComponent<PlayerFire>();
        }

        // 이벤트 구독
        RegisterEvents();
    }

    /// <summary>
    /// 컴포넌트가 활성화될 때 이벤트를 구독합니다.
    /// </summary>
    private void OnEnable()
    {
        // 필요한 경우 이벤트 구독
        RegisterEvents();
    }

    /// <summary>
    /// 컴포넌트가 비활성화될 때 이벤트 구독을 해제합니다.
    /// </summary>
    private void OnDisable()
    {
        // 이벤트 구독 해제
        UnregisterEvents();

        // 진행 중인 흔들림 중지
        StopAllShakes();
    }

    /// <summary>
    /// 컴포넌트가 파괴될 때 정리 작업을 수행합니다.
    /// </summary>
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        UnregisterEvents();

        // 진행 중인 흔들림 중지
        StopAllShakes();
    }
    #endregion

    #region 이벤트 관리
    /// <summary>
    /// 이벤트 구독을 등록합니다.
    /// </summary>
    private void RegisterEvents()
    {
        if (_playerFire != null)
        {
            _playerFire.OnWeaponFired += ShakeOnFire;
        }

        // 카메라 모드 변경 이벤트 구독
        CameraEvents.OnCameraModeChanged += HandleCameraModeChanged;
    }

    /// <summary>
    /// 이벤트 구독을 해제합니다.
    /// </summary>
    private void UnregisterEvents()
    {
        if (_playerFire != null)
        {
            _playerFire.OnWeaponFired -= ShakeOnFire;
        }

        // 카메라 모드 변경 이벤트 구독 해제
        CameraEvents.OnCameraModeChanged -= HandleCameraModeChanged;
    }

    /// <summary>
    /// 카메라 모드 변경 이벤트를 처리합니다.
    /// </summary>
    /// <param name="mode">변경된 카메라 모드</param>
    private void HandleCameraModeChanged(CameraEvents.CameraMode mode)
    {
        _currentCameraMode = mode;
    }
    #endregion

    #region 흔들림 효과
    /// <summary>
    /// 무기 발사 시 카메라 흔들림을 적용합니다.
    /// </summary>
    private void ShakeOnFire()
    {
        if (!_enableShake || _cameraTransform == null) return;

        // 현재 모드에 따른 흔들림 강도 배율 적용
        float strengthMultiplier = GetShakeMultiplierByMode();

        // 이전 흔들림 중지
        StopAllShakes();

        // 모드에 따라 다른 흔들림 적용
        if (_currentCameraMode == CameraEvents.CameraMode.FPS ||
            _currentCameraMode == CameraEvents.CameraMode.TPS)
        {
            // FPS와 TPS 모드에서는 회전 기반 흔들림
            ShakeRotation(strengthMultiplier);
        }
        else
        {
            // 쿼터뷰 모드에서는 위치 기반 흔들림
            ShakePosition(strengthMultiplier);
        }
    }

    /// <summary>
    /// 위치 기반 흔들림을 적용합니다.
    /// </summary>
    /// <param name="multiplier">강도 배율</param>
    private void ShakePosition(float multiplier)
    {
        // 로컬 좌표계에서 위치 흔들림
        _currentShakeTween = _cameraTransform.DOLocalShakePosition(
            _shakeDuration,
            _shakeStrength * multiplier,
            _shakeVibrato,
            _shakeRandomness,
            _fadeOut
        );
    }

    /// <summary>
    /// 회전 기반 흔들림을 적용합니다.
    /// </summary>
    /// <param name="multiplier">강도 배율</param>
    private void ShakeRotation(float multiplier)
    {
        // 로컬 좌표계에서 회전 흔들림 적용
        _currentShakeTween = _cameraTransform.DOLocalShakeRotation(
            _shakeDuration,
            _shakeStrength * multiplier * 10f,  // 회전은 위치보다 큰 값 사용
            _shakeVibrato,
            _shakeRandomness,
            _fadeOut
        );
    }

    /// <summary>
    /// 모든 흔들림 효과를 중지합니다.
    /// </summary>
    private void StopAllShakes()
    {
        if (_currentShakeTween != null && _currentShakeTween.IsActive())
        {
            _currentShakeTween.Kill();
            _currentShakeTween = null;
        }
    }

    /// <summary>
    /// 현재 카메라 모드에 따른 흔들림 강도 배율을 반환합니다.
    /// </summary>
    /// <returns>모드에 따른 강도 배율</returns>
    private float GetShakeMultiplierByMode()
    {
        switch (_currentCameraMode)
        {
            case CameraEvents.CameraMode.FPS:
                return _fpsShakeMultiplier;
            case CameraEvents.CameraMode.TPS:
                return _tpsShakeMultiplier;
            case CameraEvents.CameraMode.Quarter:
                return _quarterShakeMultiplier;
            default:
                return 1.0f;
        }
    }
    #endregion

    #region 공개 메서드
    /// <summary>
    /// 플레이어 발사 컴포넌트를 설정합니다.
    /// </summary>
    /// <param name="playerFire">설정할 플레이어 발사 컴포넌트</param>
    public void SetPlayerFire(PlayerFire playerFire)
    {
        // 이전 이벤트 구독 해제
        if (_playerFire != null)
        {
            _playerFire.OnWeaponFired -= ShakeOnFire;
        }

        // 새 참조 설정
        _playerFire = playerFire;

        // 새 이벤트 구독
        if (_playerFire != null)
        {
            _playerFire.OnWeaponFired += ShakeOnFire;
        }
    }

    /// <summary>
    /// 직접 카메라 흔들림을 트리거합니다.
    /// </summary>
    /// <param name="strengthMultiplier">기본 강도 배율</param>
    public void TriggerShake(float strengthMultiplier = 1.0f)
    {
        if (!_enableShake) return;

        // 이전 흔들림 중지
        StopAllShakes();

        // 모드에 따라 다른 흔들림 적용
        if (_currentCameraMode == CameraEvents.CameraMode.FPS ||
            _currentCameraMode == CameraEvents.CameraMode.TPS)
        {
            ShakeRotation(strengthMultiplier);
        }
        else
        {
            ShakePosition(strengthMultiplier);
        }
    }
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 현재 카메라 흔들림이 진행 중인지 여부
    /// </summary>
    public bool IsShaking => _currentShakeTween != null && _currentShakeTween.IsActive();
    #endregion
}