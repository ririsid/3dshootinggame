using UnityEngine;
using DG.Tweening;

public class UI_CrosshairComponent : UI_Component, IUIPlayerComponent
{
    [Header("크로스헤어 설정")]
    [SerializeField] private RectTransform _crosshairTransform;
    [SerializeField] private float _recoilScale = 1.2f;         // 반동 시 확대 배율
    [SerializeField] private float _recoilDuration = 0.05f;     // 반동 지속 시간
    [SerializeField] private float _returnDuration = 0.1f;      // 원래 크기로 돌아오는 시간
    [SerializeField] private Ease _recoilEase = Ease.OutQuad;   // 반동 이징
    [SerializeField] private Ease _returnEase = Ease.InOutQuad; // 복귀 이징

    [Header("쿼터뷰 모드 설정")]
    [SerializeField] private float _quarterViewRadius = 5f;     // 크로스헤어 이동 반경
    [SerializeField] private float _smoothSpeed = 10f;          // 이동 부드러움 정도
    [SerializeField] private float _edgeMargin = 50f;           // 화면 가장자리 마진
    [SerializeField] private LayerMask _groundLayer;            // 지면 레이어

    [Header("참조")]
    [SerializeField] private PlayerFire _playerFire;
    [SerializeField] private Transform _playerTransform;        // 플레이어 트랜스폼

    private Vector3 _originalScale;
    private Tweener _currentTween;
    private CameraEvents.CameraMode _currentCameraMode = CameraEvents.CameraMode.FPS;
    private Camera _mainCamera;
    private Vector3 _targetPosition;                           // 목표 월드 위치
    private Vector3 _currentWorldPosition;                     // 현재 월드 위치
    private bool _initialized = false;

    #region Unity 이벤트 함수
    private void Awake()
    {
        // 크로스헤어 없으면 현재 게임오브젝트의 RectTransform 사용
        if (_crosshairTransform == null)
            _crosshairTransform = GetComponent<RectTransform>();

        _originalScale = _crosshairTransform.localScale;
        _mainCamera = Camera.main;
    }

    private void Start()
    {
        _initialized = true;
        if (_playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                _playerTransform = player.transform;
            else
                Debug.LogError("[UI_CrosshairComponent] 플레이어를 찾을 수 없습니다.");
        }

        // 기본 위치 설정 (화면 중앙)
        _currentWorldPosition = GetWorldPositionFromScreenCenter();

        // 현재 모드에 맞게 커서 상태 초기화
        OnCameraModeChanged(_currentCameraMode);
    }

    private void Update()
    {
        if (!_initialized) return;

        // 쿼터뷰 모드에서만 크로스헤어 위치 업데이트
        if (_currentCameraMode == CameraEvents.CameraMode.Quarter)
        {
            UpdateCrosshairPositionInQuarterView();
        }
        else
        {
            // FPS/TPS 모드에서는 화면 중앙에 고정
            ResetCrosshairToCenter();
        }
    }
    #endregion

    #region 이벤트 등록
    protected override void RegisterEvents()
    {
        if (_playerFire != null)
        {
            _playerFire.OnWeaponFired += ApplyRecoilEffect;
        }

        // 카메라 모드 변경 이벤트 구독
        CameraEvents.OnCameraModeChanged += OnCameraModeChanged;
    }

    protected override void UnregisterEvents()
    {
        if (_playerFire != null)
        {
            _playerFire.OnWeaponFired -= ApplyRecoilEffect;
        }

        // 이벤트 구독 해제
        CameraEvents.OnCameraModeChanged -= OnCameraModeChanged;
    }
    #endregion

    #region 크로스헤어 위치 업데이트
    /// <summary>
    /// 쿼터뷰 모드에서 크로스헤어 위치를 업데이트합니다.
    /// </summary>
    private void UpdateCrosshairPositionInQuarterView()
    {
        if (_playerTransform == null || _mainCamera == null) return;

        // 현재 마우스 위치 가져오기
        Vector3 mousePosition = Input.mousePosition;

        // 화면 가장자리 제한 적용
        mousePosition.x = Mathf.Clamp(mousePosition.x, _edgeMargin, Screen.width - _edgeMargin);
        mousePosition.y = Mathf.Clamp(mousePosition.y, _edgeMargin, Screen.height - _edgeMargin);

        // 마우스 위치를 월드 공간의 레이로 변환
        Ray ray = _mainCamera.ScreenPointToRay(mousePosition);

        // 지면과의 충돌 확인
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, _groundLayer))
        {
            // 지면에 닿은 지점 사용
            _targetPosition = hit.point;
        }
        else
        {
            // 레이캐스트 실패 시 평면과의 교차점 계산
            Plane groundPlane = new Plane(Vector3.up, _playerTransform.position.y);
            if (groundPlane.Raycast(ray, out float enter))
            {
                _targetPosition = ray.GetPoint(enter);
            }
        }

        // 플레이어 중심으로부터의 방향 및 거리 계산
        Vector3 directionFromPlayer = _targetPosition - _playerTransform.position;
        directionFromPlayer.y = 0; // Y축 무시

        // 고정된 거리로 설정
        directionFromPlayer = directionFromPlayer.normalized * _quarterViewRadius;
        _targetPosition = _playerTransform.position + directionFromPlayer;

        // 현재 위치를 목표 위치로 부드럽게 이동
        _currentWorldPosition = Vector3.Lerp(_currentWorldPosition, _targetPosition, Time.deltaTime * _smoothSpeed);

        // 월드 위치를 화면 위치로 변환
        Vector3 screenPos = _mainCamera.WorldToScreenPoint(_currentWorldPosition);

        // RectTransform 위치 업데이트
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _crosshairTransform.parent as RectTransform,
            screenPos,
            null,
            out localPos);

        _crosshairTransform.localPosition = localPos;

        // 전역 위치 정보 업데이트 이벤트 발생
        if (_playerFire != null)
        {
            _playerFire.SetCrosshairWorldPosition(_currentWorldPosition);
        }
    }

    /// <summary>
    /// 크로스헤어를 화면 중앙으로 재설정합니다.
    /// </summary>
    private void ResetCrosshairToCenter()
    {
        _crosshairTransform.localPosition = Vector3.zero;

        // FPS/TPS 모드에서는 크로스헤어 위치 초기화
        if (_playerFire != null)
        {
            _playerFire.SetCrosshairWorldPosition(Vector3.zero); // 기본값, 실제로는 사용되지 않음
        }
    }

    /// <summary>
    /// 화면 중앙에 해당하는 월드 좌표를 반환합니다.
    /// </summary>
    private Vector3 GetWorldPositionFromScreenCenter()
    {
        if (_mainCamera == null) return Vector3.zero;

        // 화면 중앙에서 레이 발사
        Ray ray = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        // 플레이어 높이의 평면과의 교차점 계산
        Plane groundPlane = new Plane(Vector3.up, _playerTransform != null ? _playerTransform.position.y : 0);
        if (groundPlane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }

        return Vector3.zero;
    }
    #endregion

    #region 이벤트 핸들러
    /// <summary>
    /// 카메라 모드 변경 이벤트를 처리합니다.
    /// </summary>
    private void OnCameraModeChanged(CameraEvents.CameraMode mode)
    {
        _currentCameraMode = mode;

        // 모드에 따라 커서 상태 변경
        switch (mode)
        {
            case CameraEvents.CameraMode.Quarter:
                // 쿼터뷰 모드: 커서 표시 및 화면 내 제한
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.Confined;

                // 위치 초기화
                _currentWorldPosition = GetWorldPositionFromScreenCenter();
                break;

            case CameraEvents.CameraMode.FPS:
            case CameraEvents.CameraMode.TPS:
                // FPS/TPS 모드: 커서 숨김 및 잠금
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;

                // 크로스헤어를 화면 중앙으로 재설정
                ResetCrosshairToCenter();
                break;
        }
    }
    #endregion

    #region 효과 메서드
    /// <summary>
    /// 총 발사 시 크로스헤어에 반동 효과를 적용합니다.
    /// </summary>
    private void ApplyRecoilEffect()
    {
        // 기존 트윈 중지
        _currentTween?.Kill();

        // 크로스헤어 확대 (반동 효과)
        _currentTween = _crosshairTransform.DOScale(_originalScale * _recoilScale, _recoilDuration)
            .SetEase(_recoilEase)
            .OnComplete(() =>
            {
                // 원래 크기로 돌아오기
                _currentTween = _crosshairTransform.DOScale(_originalScale, _returnDuration)
                    .SetEase(_returnEase);
            });
    }
    #endregion

    #region 공개 메서드
    /// <summary>
    /// PlayerFire 참조를 설정합니다. (IUIPlayerComponent 구현)
    /// </summary>
    public void SetPlayerFire(PlayerFire playerFire)
    {
        // 기존 이벤트 연결 해제
        UnregisterEvents();

        // 새 참조 설정
        _playerFire = playerFire;

        // 새 이벤트 연결
        RegisterEvents();
    }

    /// <summary>
    /// 이전 버전과의 호환성을 위한 메서드
    /// </summary>
    public void SetupPlayerFire(PlayerFire playerFire)
    {
        SetPlayerFire(playerFire);
    }

    /// <summary>
    /// PlayerStat 참조를 설정합니다. (IUIPlayerComponent 구현)
    /// 이 컴포넌트는 PlayerStat을 사용하지 않으므로 빈 구현입니다.
    /// </summary>
    public void SetPlayerStat(PlayerStat playerStat)
    {
        // 이 컴포넌트는 PlayerStat을 사용하지 않으므로 아무 작업도 수행하지 않음
    }

    /// <summary>
    /// 플레이어 Transform을 설정합니다.
    /// </summary>
    public void SetPlayerTransform(Transform playerTransform)
    {
        _playerTransform = playerTransform;
    }

    /// <summary>
    /// 현재 크로스헤어의 월드 위치를 반환합니다.
    /// </summary>
    public Vector3 GetCurrentWorldPosition()
    {
        return _currentWorldPosition;
    }
    #endregion
}