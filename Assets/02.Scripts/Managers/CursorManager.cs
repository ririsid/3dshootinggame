using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 게임 내 커서 상태를 관리하는 싱글톤 클래스입니다.
/// </summary>
public class CursorManager : Singleton<CursorManager>
{
    [Header("커서 설정")]
    [SerializeField] private Texture2D _defaultCursor;
    [SerializeField] private Texture2D _aimCursor;
    [SerializeField] private Texture2D _uiCursor;  // UI 위에 있을 때 사용할 커서

    [Header("UI 상호작용 설정")]
    [SerializeField] private bool _enableUIInteraction = true;
    [SerializeField] private float _uiInteractionDelay = 0.1f;  // UI와 상호작용 후 원래 상태로 돌아가는 지연 시간

    private bool _isPaused = false;
    private bool _isOverUI = false;
    private bool _wasOverUI = false;
    private float _lastUIInteractionTime = 0f;
    private CameraEvents.CameraMode _currentCameraMode;

    protected override void Awake()
    {
        base.Awake();
        // 기본 커서 설정
        if (_defaultCursor != null)
        {
            Cursor.SetCursor(_defaultCursor, Vector2.zero, CursorMode.Auto);
        }

        // UI 커서가 지정되지 않았으면 기본 커서 사용
        if (_uiCursor == null)
        {
            _uiCursor = _defaultCursor;
        }
    }

    private void Start()
    {
        // 카메라 모드 변경 이벤트 구독
        CameraEvents.OnCameraModeChanged += OnCameraModeChanged;
        _currentCameraMode = CameraManager.Instance != null ?
                            CameraManager.Instance.CurrentMode :
                            CameraEvents.CameraMode.FPS;
    }

    private void Update()
    {
        if (_enableUIInteraction)
        {
            CheckUIInteraction();
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        CameraEvents.OnCameraModeChanged -= OnCameraModeChanged;
    }

    /// <summary>
    /// UI 요소와의 상호작용을 확인하고 커서 상태를 업데이트합니다.
    /// </summary>
    private void CheckUIInteraction()
    {
        if (_isPaused) return; // 일시정지 상태에서는 항상 커서 표시됨

        // EventSystem을 사용하여 UI 요소 위에 마우스가 있는지 확인
        _isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        // UI 상호작용 상태가 변경되었을 때
        if (_isOverUI != _wasOverUI)
        {
            _wasOverUI = _isOverUI;
            _lastUIInteractionTime = Time.time;

            if (_isOverUI)
            {
                // UI 요소 위에 있을 때 커서 표시
                SetUICursor();
            }
            else
            {
                // UI 요소에서 벗어났을 때 약간의 지연 후 원래 상태로 복원
                Invoke(nameof(RestoreCursorState), _uiInteractionDelay);
            }
        }
    }

    /// <summary>
    /// UI 상호작용용 커서 설정
    /// </summary>
    private void SetUICursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // UI 커서로 변경
        if (_uiCursor != null)
        {
            Cursor.SetCursor(_uiCursor, Vector2.zero, CursorMode.Auto);
        }
    }

    /// <summary>
    /// 현재 카메라 모드에 맞게 커서 상태를 복원합니다.
    /// </summary>
    private void RestoreCursorState()
    {
        // 복원 시점에 UI 위에 있거나 일시정지 상태라면 복원하지 않음
        if (_isOverUI || _isPaused) return;

        // 현재 카메라 모드에 맞는 커서 상태로 복원
        OnCameraModeChanged(_currentCameraMode);
    }

    /// <summary>
    /// 카메라 모드에 따라 커서 상태를 설정합니다.
    /// </summary>
    private void OnCameraModeChanged(CameraEvents.CameraMode mode)
    {
        if (_isPaused) return;

        _currentCameraMode = mode;

        // UI 위에 있다면 커서 상태 변경하지 않음
        if (_isOverUI) return;

        switch (mode)
        {
            case CameraEvents.CameraMode.Quarter:
                SetQuarterViewCursor();
                break;
            default:
                SetFPSCursor();
                break;
        }
    }

    /// <summary>
    /// 쿼터뷰 모드의 커서 상태를 설정합니다.
    /// </summary>
    public void SetQuarterViewCursor()
    {
        // UI 위에 있거나 일시정지 상태라면 설정하지 않음
        if (_isOverUI || _isPaused) return;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;

        // 조준 커서로 변경
        if (_aimCursor != null)
        {
            Cursor.SetCursor(_aimCursor, new Vector2(_aimCursor.width / 2, _aimCursor.height / 2), CursorMode.Auto);
        }
    }

    /// <summary>
    /// FPS/TPS 모드의 커서 상태를 설정합니다.
    /// </summary>
    public void SetFPSCursor()
    {
        // UI 위에 있거나 일시정지 상태라면 설정하지 않음
        if (_isOverUI || _isPaused) return;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // 기본 커서로 초기화
        if (_defaultCursor != null)
        {
            Cursor.SetCursor(_defaultCursor, Vector2.zero, CursorMode.Auto);
        }
    }

    /// <summary>
    /// 게임 일시 정지 상태를 설정합니다.
    /// </summary>
    public void SetPauseState(bool isPaused)
    {
        _isPaused = isPaused;

        if (isPaused)
        {
            // 일시 정지 시 커서 표시
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            // 일시 정지 해제 시 현재 상태에 맞게 커서 복원
            RestoreCursorState();
        }
    }

    /// <summary>
    /// UI 상호작용 기능을 활성화/비활성화합니다.
    /// </summary>
    public void SetUIInteractionEnabled(bool enabled)
    {
        _enableUIInteraction = enabled;

        if (!enabled && _isOverUI)
        {
            // 비활성화 시 현재 UI 상호작용 중이라면 커서 상태 복원
            _isOverUI = false;
            _wasOverUI = false;
            RestoreCursorState();
        }
    }
}