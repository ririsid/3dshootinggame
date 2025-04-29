using UnityEngine;

/// <summary>
/// 게임 내 커서 상태를 관리하는 싱글톤 클래스입니다.
/// </summary>
public class CursorManager : Singleton<CursorManager>
{
    [SerializeField] private Texture2D _defaultCursor;
    [SerializeField] private Texture2D _aimCursor;

    private bool _isPaused = false;

    protected override void Awake()
    {
        base.Awake();
        // 기본 커서 설정
        if (_defaultCursor != null)
        {
            Cursor.SetCursor(_defaultCursor, Vector2.zero, CursorMode.Auto);
        }
    }

    private void Start()
    {
        // 카메라 모드 변경 이벤트 구독
        CameraEvents.OnCameraModeChanged += OnCameraModeChanged;
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        CameraEvents.OnCameraModeChanged -= OnCameraModeChanged;
    }

    /// <summary>
    /// 카메라 모드에 따라 커서 상태를 설정합니다.
    /// </summary>
    private void OnCameraModeChanged(CameraEvents.CameraMode mode)
    {
        if (_isPaused) return;

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
            // 일시 정지 해제 시 현재 카메라 모드에 맞게 커서 상태 복원
            OnCameraModeChanged(CameraManager.Instance.CurrentMode);
        }
    }
}