using UnityEngine;

/// <summary>
/// 카메라 시점 전환을 관리하는 싱글톤 클래스입니다.
/// </summary>
public class CameraManager : Singleton<CameraManager>
{
    [Header("카메라 참조")]
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private Camera _minimapCamera;

    [Header("카메라 위치 설정")]
    [SerializeField] private Transform _targetPlayer;
    [SerializeField] private Transform _fpsPositionTransform; // FPS 시점 위치를 지정하는 Transform
    [SerializeField] private Transform _tpsPositionTransform; // TPS 시점 위치를 지정하는 Transform
    [SerializeField] private Transform _quarterPositionTransform; // 쿼터뷰 시점 위치를 지정하는 Transform

    [Header("카메라 전환 설정")]
    [SerializeField] private float _transitionSpeed = 5f;
    [SerializeField] private bool _useSmoothing = true;
    [SerializeField] private float _smoothTime = 0.2f;

    private CameraEvents.CameraMode _currentMode = CameraEvents.CameraMode.FPS;
    private Vector3 _cameraVelocity = Vector3.zero;
    private Transform _currentPositionTransform;

    #region 프로퍼티
    /// <summary>
    /// 현재 설정된 카메라 모드를 반환합니다.
    /// </summary>
    public CameraEvents.CameraMode CurrentMode => _currentMode;
    #endregion

    #region Unity 이벤트 함수
    protected override void Awake()
    {
        base.Awake();

        if (_mainCamera == null)
            _mainCamera = Camera.main;

        // 초기 위치 트랜스폼 설정
        _currentPositionTransform = _fpsPositionTransform;
    }

    private void Start()
    {
        // 필요한 참조가 없으면 찾기
        if (_targetPlayer == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                _targetPlayer = player.transform;
            else
                Debug.LogError("[CameraManager] 플레이어를 찾을 수 없습니다.");
        }

        // Transform 참조 확인
        ValidateTransformReferences();

        // 초기 모드 설정
        SetCameraMode(CameraEvents.CameraMode.FPS);
    }

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
        _currentMode = mode;
        Debug.Log($"[CameraManager] 카메라 모드 변경: {mode}");

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
            Debug.LogWarning("[CameraManager] FPS 위치 Transform이 설정되지 않았습니다. 카메라 변환이 제대로 작동하지 않을 수 있습니다.");

        if (_tpsPositionTransform == null)
            Debug.LogWarning("[CameraManager] TPS 위치 Transform이 설정되지 않았습니다. 카메라 변환이 제대로 작동하지 않을 수 있습니다.");

        if (_quarterPositionTransform == null)
            Debug.LogWarning("[CameraManager] 쿼터뷰 위치 Transform이 설정되지 않았습니다. 카메라 변환이 제대로 작동하지 않을 수 있습니다.");
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

        // 현재 카메라 Transform의 위치를 가져옴
        Vector3 targetPosition = _currentPositionTransform.position;

        if (_useSmoothing)
        {
            // 부드러운 전환
            _mainCamera.transform.position = Vector3.SmoothDamp(
                _mainCamera.transform.position,
                targetPosition,
                ref _cameraVelocity,
                _smoothTime
            );

            // 부드러운 회전
            _mainCamera.transform.rotation = Quaternion.Slerp(
                _mainCamera.transform.rotation,
                _currentPositionTransform.rotation,
                Time.deltaTime * _transitionSpeed
            );
        }
        else
        {
            // 즉시 전환
            _mainCamera.transform.position = targetPosition;
        }
    }
    #endregion
}