using UnityEngine;

/// <summary>
/// 플레이어 캐릭터의 회전을 담당하는 클래스입니다.
/// </summary>
public class PlayerRotate : MonoBehaviour
{
    [Header("회전 설정")]
    [SerializeField] private float _rotationSpeed = 100f;
    [SerializeField] private float _minVerticalAngle = -90f;
    [SerializeField] private float _maxVerticalAngle = 90f;

    private float _rotationX = 0f;
    private float _rotationY = 0f;
    private bool _isEnabled = true;

    #region Unity 이벤트 함수
    private void Start()
    {
        // 초기 회전값 설정
        _rotationY = transform.eulerAngles.y;
    }

    private void OnEnable()
    {
        // 이벤트 구독
        CameraEvents.OnCameraModeChanged += OnCameraModeChanged;
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        CameraEvents.OnCameraModeChanged -= OnCameraModeChanged;
    }

    private void Update()
    {
        if (!_isEnabled)
            return;

        // 마우스 입력 처리
        HandleRotationInput();
    }
    #endregion

    /// <summary>
    /// 카메라 모드 변경 이벤트를 처리합니다.
    /// </summary>
    /// <param name="mode">변경된 카메라 모드</param>
    private void OnCameraModeChanged(CameraEvents.CameraMode mode)
    {
        _isEnabled = true; // 모든 모드에서 회전 활성화
    }

    /// <summary>
    /// 마우스 입력을 처리하고 회전 이벤트를 발생시킵니다.
    /// </summary>
    private void HandleRotationInput()
    {
        // 마우스 입력을 받습니다
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // 회전 값을 누적합니다
        _rotationX += mouseX * _rotationSpeed * Time.deltaTime;
        _rotationY += mouseY * _rotationSpeed * Time.deltaTime;
        _rotationY = Mathf.Clamp(_rotationY, _minVerticalAngle, _maxVerticalAngle); // Y축 회전 제한

        // 플레이어 오브젝트를 Y축으로만 회전시킵니다
        UpdatePlayerRotation();

        // 회전 입력 이벤트를 발생시킵니다 (카메라가 이 정보를 사용하여 회전)
        CameraEvents.RaisePlayerRotationInput(new Vector2(_rotationX, _rotationY));
    }

    /// <summary>
    /// 현재 회전값을 기준으로 플레이어 회전을 업데이트합니다.
    /// </summary>
    private void UpdatePlayerRotation()
    {
        // 플레이어는 Y축 회전만 적용
        transform.eulerAngles = new Vector3(0f, _rotationX, 0f);
    }
}
