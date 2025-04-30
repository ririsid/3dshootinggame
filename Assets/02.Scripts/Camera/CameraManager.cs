using UnityEngine;

/// <summary>
/// 카메라 시점 전환을 관리하는 싱글톤 클래스입니다.
/// </summary>
public class CameraManager : Singleton<CameraManager>
{
    #region 필드
    [Header("카메라 참조")]
    /// <summary>
    /// 메인 카메라 참조
    /// </summary>
    [SerializeField] private Camera _mainCamera;

    /// <summary>
    /// 미니맵 카메라 참조
    /// </summary>
    [SerializeField] private Camera _minimapCamera;

    [Header("카메라 위치 설정")]
    /// <summary>
    /// 카메라가 따라갈 플레이어 트랜스폼
    /// </summary>
    [SerializeField] private Transform _targetPlayer;

    /// <summary>
    /// 1인칭 시점 위치를 지정하는 Transform
    /// </summary>
    [SerializeField] private Transform _fpsPositionTransform;

    /// <summary>
    /// 3인칭 시점 위치를 지정하는 Transform
    /// </summary>
    [SerializeField] private Transform _tpsPositionTransform;

    /// <summary>
    /// 쿼터뷰 시점 위치를 지정하는 Transform
    /// </summary>
    [SerializeField] private Transform _quarterPositionTransform;

    [Header("카메라 전환 설정")]
    /// <summary>
    /// 카메라 전환 시 부드러운 이동 효과 사용 여부
    /// </summary>
    [SerializeField] private bool _useSmoothing = false;

    /// <summary>
    /// 부드러운 이동 시 전환 시간(초)
    /// </summary>
    [SerializeField] private float _smoothTime = 0.2f;

    /// <summary>
    /// 현재 카메라 모드
    /// </summary>
    private CameraEvents.CameraMode _currentMode = CameraEvents.CameraMode.FPS;

    /// <summary>
    /// 부드러운 이동 계산을 위한 속도 벡터
    /// </summary>
    private Vector3 _cameraVelocity = Vector3.zero;

    /// <summary>
    /// 현재 카메라 위치 트랜스폼
    /// </summary>
    private Transform _currentPositionTransform;

    /// <summary>
    /// 카메라 전환 중인지 추적하는 플래그
    /// </summary>
    private bool _isTransitioning = false;

    /// <summary>
    /// 카메라 이동 목표 위치
    /// </summary>
    private Vector3 _targetPosition;
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 현재 설정된 카메라 모드를 반환합니다.
    /// </summary>
    public CameraEvents.CameraMode CurrentMode => _currentMode;
    #endregion

    #region Unity 이벤트 함수
    /// <summary>
    /// 컴포넌트 초기화를 수행합니다.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();

        // 초기 위치 트랜스폼 설정
        _currentPositionTransform = _fpsPositionTransform;
    }

    /// <summary>
    /// 시작 시 참조를 확인하고 초기 설정을 수행합니다.
    /// </summary>
    private void Start()
    {
        // 카메라 참조가 없으면 자동으로 찾기
        if (_mainCamera == null)
        {
            _mainCamera = GetComponent<Camera>();
            if (_mainCamera == null)
                _mainCamera = Camera.main;
        }

        // 플레이어 참조가 없으면 자동으로 찾기
        if (_targetPlayer == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                _targetPlayer = player.transform;
            else
                Debug.LogError("플레이어를 찾을 수 없습니다.");
        }

        // Transform 참조 확인
        ValidateTransformReferences();

        // 초기 모드 설정 (전환 효과 없이 즉시 적용)
        SetCameraMode(CameraEvents.CameraMode.FPS);
        _isTransitioning = false;
    }

    /// <summary>
    /// 매 프레임 마지막에 카메라 위치를 업데이트합니다.
    /// </summary>
    private void LateUpdate()
    {
        // 키 입력 체크
        CheckKeyInput();

        // 카메라 위치 업데이트 (플레이어 업데이트 이후에 실행되도록 LateUpdate 사용)
        UpdateCameraPosition();
    }
    #endregion

    #region 입력 처리
    /// <summary>
    /// 키보드 입력을 체크하여 카메라 모드를 전환합니다.
    /// </summary>
    private void CheckKeyInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            SetCameraMode(CameraEvents.CameraMode.FPS);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            SetCameraMode(CameraEvents.CameraMode.TPS);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            SetCameraMode(CameraEvents.CameraMode.Quarter);
        }
    }
    #endregion

    #region 카메라 모드 설정
    /// <summary>
    /// 카메라 모드를 변경합니다.
    /// </summary>
    /// <param name="mode">변경할 카메라 모드</param>
    public void SetCameraMode(CameraEvents.CameraMode mode)
    {
        // 같은 모드로 변경할 경우 무시
        if (_currentMode == mode)
            return;

        _currentMode = mode;

        // 개발 빌드에서만 로그 출력
        if (Debug.isDebugBuild)
        {
            Debug.Log($"카메라 모드 변경: {mode}");
        }

        // 모드에 따른 Transform 설정
        switch (mode)
        {
            case CameraEvents.CameraMode.FPS:
                _currentPositionTransform = _fpsPositionTransform;
                break;
            case CameraEvents.CameraMode.TPS:
                _currentPositionTransform = _tpsPositionTransform;
                break;
            case CameraEvents.CameraMode.Quarter:
                _currentPositionTransform = _quarterPositionTransform;
                break;
        }

        // 전환 시작 플래그 설정
        _isTransitioning = true;

        // 미니맵 카메라 설정 업데이트
        UpdateMinimapCamera();

        // 카메라 모드 변경 이벤트 발생
        CameraEvents.RaiseCameraModeChanged(mode);
    }

    /// <summary>
    /// Transform 참조가 유효한지 검사하고 누락된 경우 경고 로그를 출력합니다.
    /// </summary>
    private void ValidateTransformReferences()
    {
        if (_fpsPositionTransform == null)
            Debug.LogWarning("FPS 위치 Transform이 설정되지 않았습니다. 카메라 변환이 제대로 작동하지 않을 수 있습니다.");

        if (_tpsPositionTransform == null)
            Debug.LogWarning("TPS 위치 Transform이 설정되지 않았습니다. 카메라 변환이 제대로 작동하지 않을 수 있습니다.");

        if (_quarterPositionTransform == null)
            Debug.LogWarning("쿼터뷰 위치 Transform이 설정되지 않았습니다. 카메라 변환이 제대로 작동하지 않을 수 있습니다.");
    }

    /// <summary>
    /// 미니맵 카메라 설정을 현재 카메라 모드에 맞게 업데이트합니다.
    /// </summary>
    private void UpdateMinimapCamera()
    {
        if (_minimapCamera != null)
        {
            _minimapCamera.gameObject.SetActive(true);
        }
    }
    #endregion

    #region 카메라 업데이트
    /// <summary>
    /// 카메라 위치와 회전을 현재 모드에 맞게 업데이트합니다.
    /// </summary>
    private void UpdateCameraPosition()
    {
        if (_targetPlayer == null || _mainCamera == null || _currentPositionTransform == null)
            return;

        // 목표 위치 계산
        if (_currentMode == CameraEvents.CameraMode.Quarter)
        {
            _targetPosition = new Vector3(
                _targetPlayer.position.x + _currentPositionTransform.position.x,
                _currentPositionTransform.position.y,
                _targetPlayer.position.z
            );
        }
        else
        {
            _targetPosition = _currentPositionTransform.position;
        }

        // 전환 중일 때는 부드러운 이동 적용
        if (_isTransitioning && _useSmoothing)
        {
            // SmoothDamp를 사용한 부드러운 전환
            _mainCamera.transform.position = Vector3.SmoothDamp(
                _mainCamera.transform.position,
                _targetPosition,
                ref _cameraVelocity,
                _smoothTime
            );

            // 쿼터뷰일 경우 부드러운 회전 적용
            if (_currentMode == CameraEvents.CameraMode.Quarter)
            {
                // 플레이어를 향하는 방향 벡터 계산
                Vector3 lookDirection = _targetPlayer.position - _mainCamera.transform.position;
                if (lookDirection != Vector3.zero)  // 방향 벡터가 0이 아닌지 확인
                {
                    // 현재 회전과 목표 회전 사이의 부드러운 보간
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                    _mainCamera.transform.rotation = targetRotation;
                }
            }

            // 전환이 거의 완료되었는지 확인 (위치 기준)
            if (Vector3.Distance(_mainCamera.transform.position, _targetPosition) < 0.01f)
            {
                _isTransitioning = false;

                // 정확한 위치로 설정
                _mainCamera.transform.position = _targetPosition;

                // 쿼터뷰인 경우 카메라가 플레이어를 정확히 바라보도록 설정
                if (_currentMode == CameraEvents.CameraMode.Quarter)
                {
                    _mainCamera.transform.LookAt(_targetPlayer.position);
                }
            }
        }
        // 전환 중이 아니거나 스무딩을 사용하지 않을 때는 즉시 위치 적용
        else
        {
            _mainCamera.transform.position = _targetPosition;

            // 쿼터뷰일 경우 플레이어를 바라보도록 설정
            if (_currentMode == CameraEvents.CameraMode.Quarter)
            {
                _mainCamera.transform.LookAt(_targetPlayer.position);
            }
        }
    }
    #endregion
}