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
    [SerializeField] private Vector3 _fpsPositionOffset = new Vector3(0f, 0.5f, 0.5f); // 기본 FPS 오프셋 (눈 높이)
    [SerializeField] private Vector3 _tpsPositionOffset = new Vector3(1.5f, 1f, -3.5f); // 기본 TPS 오프셋 (뒤쪽 어깨 위)

    [Header("카메라 전환 설정")]
    [SerializeField] private float _transitionSpeed = 5f;
    [SerializeField] private bool _useSmoothing = true;
    [SerializeField] private float _smoothTime = 0.2f;

    private CameraEvents.CameraMode _currentMode = CameraEvents.CameraMode.FPS;
    private Vector3 _currentPositionOffset;
    private Vector3 _cameraVelocity = Vector3.zero;

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

        // 초기 위치 오프셋 설정
        _currentPositionOffset = _fpsPositionOffset;
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

        switch (mode)
        {
            case CameraEvents.CameraMode.FPS:
                _currentPositionOffset = _fpsPositionOffset;
                break;
            case CameraEvents.CameraMode.TPS:
                _currentPositionOffset = _tpsPositionOffset;
                break;
        }

        // 미니맵 카메라 설정 업데이트
        UpdateMinimapCamera();

        // 카메라 모드 변경 이벤트 발생
        CameraEvents.RaiseCameraModeChanged(mode);
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
        if (_targetPlayer == null || _mainCamera == null)
            return;

        // 플레이어의 회전에 따라 오프셋 위치를 계산
        Vector3 targetPosition = CalculateTargetPosition();

        if (_useSmoothing)
        {
            // 부드러운 전환
            _mainCamera.transform.position = Vector3.SmoothDamp(
                _mainCamera.transform.position,
                targetPosition,
                ref _cameraVelocity,
                _smoothTime
            );
        }
        else
        {
            // 즉시 전환
            _mainCamera.transform.position = targetPosition;
        }
    }

    /// <summary>
    /// 플레이어 위치와 로컬 오프셋을 기반으로 카메라의 목표 위치를 계산합니다.
    /// </summary>
    /// <returns>계산된 목표 위치</returns>
    private Vector3 CalculateTargetPosition()
    {
        // 플레이어의 로컬 좌표계를 기준으로 카메라 위치 계산
        return _targetPlayer.position + _targetPlayer.TransformDirection(_currentPositionOffset);
    }
    #endregion
}