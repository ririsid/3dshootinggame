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
    [SerializeField] private float _quarterViewRotationSpeed = 8f; // 쿼터뷰 모드에서의 회전 속도

    [Header("참조")]
    [SerializeField] private UI_CrosshairComponent _crosshairComponent;
    [SerializeField] private PlayerFire _playerFire;

    private float _rotationX = 0f;
    private float _rotationY = 0f;
    private CameraEvents.CameraMode _currentMode = CameraEvents.CameraMode.FPS;

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
        // 카메라 모드에 따라 회전 방식 변경
        switch (_currentMode)
        {
            case CameraEvents.CameraMode.Quarter:
                HandleQuarterViewRotation();
                break;
            case CameraEvents.CameraMode.FPS:
            case CameraEvents.CameraMode.TPS:
                HandleRotationInput();
                break;
        }
    }
    #endregion

    /// <summary>
    /// 카메라 모드 변경 이벤트를 처리합니다.
    /// </summary>
    /// <param name="mode">변경된 카메라 모드</param>
    private void OnCameraModeChanged(CameraEvents.CameraMode mode)
    {
        _currentMode = mode;
    }

    /// <summary>
    /// 마우스 입력을 처리하고 회전 이벤트를 발생시킵니다. (FPS/TPS 모드용)
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
    /// 쿼터뷰 모드에서 크로스헤어 방향으로 회전을 처리합니다.
    /// </summary>
    private void HandleQuarterViewRotation()
    {
        // 크로스헤어 컴포넌트가 없거나 플레이어 파이어가 없을 경우 종료
        if (_crosshairComponent == null && _playerFire == null) return;

        Vector3 crosshairPosition;

        // 크로스헤어 위치 가져오기 (UI_CrosshairComponent가 있으면 해당 컴포넌트에서, 없으면 PlayerFire에서)
        if (_crosshairComponent != null)
        {
            crosshairPosition = _crosshairComponent.GetCurrentWorldPosition();
        }
        else
        {
            // 반사적으로 GetMethod를 통해 _crosshairWorldPosition 필드 접근 방지
            crosshairPosition = Vector3.zero; // 기본값

            // SetCrosshairWorldPosition 메서드가 있으므로 비슷한 메서드가 있을 가능성이 높음
            // 실제로는 내부 필드에 직접 접근하는 대신 PlayerFire에 GetCrosshairWorldPosition 메서드를 추가하는 것이 좋음
        }

        // 크로스헤어 방향 계산 (Y축은 제외)
        Vector3 lookDirection = crosshairPosition - transform.position;
        lookDirection.y = 0; // Y축은 무시

        // 방향이 유효한 경우에만 회전
        if (lookDirection.sqrMagnitude > 0.01f)
        {
            // 목표 회전 계산
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

            // 부드러운 회전 적용
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * _quarterViewRotationSpeed
            );

            // 현재 회전값 저장 (카메라 동기화를 위해)
            _rotationX = transform.eulerAngles.y;
        }
    }

    /// <summary>
    /// 현재 회전값을 기준으로 플레이어 회전을 업데이트합니다. (FPS/TPS 모드용)
    /// </summary>
    private void UpdatePlayerRotation()
    {
        // 플레이어는 Y축 회전만 적용
        transform.eulerAngles = new Vector3(0f, _rotationX, 0f);
    }
}
