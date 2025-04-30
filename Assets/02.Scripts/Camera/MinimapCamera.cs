using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 미니맵 카메라 기능을 관리하는 클래스입니다.
/// </summary>
[RequireComponent(typeof(Camera))]
public class MinimapCamera : MonoBehaviour
{
    #region 필드
    [Header("타겟 설정")]
    /// <summary>
    /// 미니맵 카메라가 따라갈 대상 트랜스폼
    /// </summary>
    [SerializeField] private Transform _target;

    /// <summary>
    /// 타겟으로부터의 높이 오프셋
    /// </summary>
    [SerializeField] private float _yOffset = 10f;

    [Header("줌 설정")]
    /// <summary>
    /// 최소 줌 레벨 (가장 가깝게 확대했을 때)
    /// </summary>
    [SerializeField] private float _minZoom = 5f;

    /// <summary>
    /// 최대 줌 레벨 (가장 멀리 축소했을 때)
    /// </summary>
    [SerializeField] private float _maxZoom = 15f;

    /// <summary>
    /// 줌 변경 단계 크기
    /// </summary>
    [SerializeField] private float _zoomStep = 1f;

    [Header("UI 버튼")]
    /// <summary>
    /// 미니맵 확대 버튼
    /// </summary>
    [SerializeField] private Button _zoomInButton;

    /// <summary>
    /// 미니맵 축소 버튼
    /// </summary>
    [SerializeField] private Button _zoomOutButton;

    /// <summary>
    /// 미니맵 카메라 컴포넌트
    /// </summary>
    private Camera _minimapCamera;

    /// <summary>
    /// 현재 줌 레벨
    /// </summary>
    private float _currentZoom;
    #endregion

    #region 프로퍼티
    /// <summary>
    /// 미니맵을 따라갈 대상
    /// </summary>
    public Transform Target
    {
        get => _target;
        set => _target = value;
    }

    /// <summary>
    /// 타겟으로부터의 높이 오프셋
    /// </summary>
    public float YOffset
    {
        get => _yOffset;
        set => _yOffset = value;
    }

    /// <summary>
    /// 미니맵 확대(줌인) 키 입력 감지
    /// </summary>
    private bool IsZoomInKeyPressed =>
        Input.GetKeyDown(KeyCode.KeypadPlus) ||
        (Input.GetKeyDown(KeyCode.Equals) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)));

    /// <summary>
    /// 미니맵 축소(줌아웃) 키 입력 감지
    /// </summary>
    private bool IsZoomOutKeyPressed =>
        Input.GetKeyDown(KeyCode.KeypadMinus) ||
        Input.GetKeyDown(KeyCode.Minus);
    #endregion

    #region Unity 이벤트 함수
    /// <summary>
    /// 컴포넌트 초기화 작업을 수행합니다.
    /// </summary>
    private void Awake()
    {
        _minimapCamera = GetComponent<Camera>();
        if (!_minimapCamera.orthographic)
        {
            Debug.LogWarning("미니맵 카메라가 직교 투영이 아닙니다. 줌 기능이 예상대로 작동하지 않을 수 있습니다.");
        }
        _currentZoom = _minimapCamera.orthographicSize; // 초기 줌 값 설정
        UpdateZoomButtonsState(); // 초기 버튼 상태 업데이트
    }

    /// <summary>
    /// 매 프레임 입력을 처리합니다.
    /// </summary>
    private void Update()
    {
        // 키보드 입력 처리
        ProcessKeyboardInput();
    }

    /// <summary>
    /// 모든 업데이트가 끝난 후 카메라 위치와 줌을 갱신합니다.
    /// </summary>
    private void LateUpdate()
    {
        if (Target != null)
        {
            // 위치 업데이트
            Vector3 newPosition = Target.position;
            newPosition.y += YOffset;
            transform.position = newPosition;

            // 회전 업데이트
            transform.rotation = Quaternion.Euler(90f, Target.eulerAngles.y, 0f);
        }

        // 줌 업데이트 (Orthographic 카메라일 경우)
        if (_minimapCamera.orthographic)
        {
            _minimapCamera.orthographicSize = _currentZoom;
        }
    }
    #endregion

    #region 공개 메서드
    /// <summary>
    /// 미니맵 카메라를 줌 인합니다. UI 버튼의 OnClick 이벤트에 연결하세요.
    /// </summary>
    public void ZoomIn()
    {
        _currentZoom -= _zoomStep;
        _currentZoom = Mathf.Clamp(_currentZoom, _minZoom, _maxZoom);
        UpdateZoomButtonsState();
    }

    /// <summary>
    /// 미니맵 카메라를 줌 아웃합니다. UI 버튼의 OnClick 이벤트에 연결하세요.
    /// </summary>
    public void ZoomOut()
    {
        _currentZoom += _zoomStep;
        _currentZoom = Mathf.Clamp(_currentZoom, _minZoom, _maxZoom);
        UpdateZoomButtonsState();
    }
    #endregion

    #region 비공개 메서드
    /// <summary>
    /// 키보드 입력을 처리합니다.
    /// </summary>
    private void ProcessKeyboardInput()
    {
        if (IsZoomInKeyPressed)
        {
            ZoomIn();
        }
        else if (IsZoomOutKeyPressed)
        {
            ZoomOut();
        }
    }

    /// <summary>
    /// 현재 줌 레벨에 따라 줌 인/아웃 버튼의 활성화 상태를 업데이트합니다.
    /// </summary>
    private void UpdateZoomButtonsState()
    {
        if (_zoomInButton != null)
        {
            _zoomInButton.interactable = _currentZoom > _minZoom;
        }
        if (_zoomOutButton != null)
        {
            _zoomOutButton.interactable = _currentZoom < _maxZoom;
        }
    }
    #endregion
}
