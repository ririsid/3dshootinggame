using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class MinimapCamera : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform Target;
    public float YOffset = 10f;

    [Header("Zoom Settings")]
    [SerializeField] private float _minZoom = 5f;
    [SerializeField] private float _maxZoom = 15f;
    [SerializeField] private float _zoomStep = 1f;

    [Header("UI Buttons")] // 버튼 참조
    [SerializeField] private Button _zoomInButton;
    [SerializeField] private Button _zoomOutButton;

    private Camera _minimapCamera;
    private float _currentZoom;

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

    #region Public Methods for UI Buttons
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
}
