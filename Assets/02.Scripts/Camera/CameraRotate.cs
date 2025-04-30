using UnityEngine;

/// <summary>
/// 카메라 회전을 담당하는 클래스입니다.
/// </summary>
public class CameraRotate : MonoBehaviour
{
    [Header("회전 설정")]
    /// <summary>
    /// 회전 보간에 사용되는 시간 값입니다. 값이 작을수록 더 빠르게 회전합니다.
    /// </summary>
    [Tooltip("회전 보간에 사용되는 시간 값입니다. 값이 작을수록 더 빠르게 회전합니다.")]
    [SerializeField] private float _smoothTime = 0.1f;

    /// <summary>
    /// 부드러운 회전 효과 사용 여부입니다.
    /// </summary>
    [Tooltip("부드러운 회전 효과를 사용할지 여부를 설정합니다.")]
    [SerializeField] private bool _useSmoothing = false;

    /// <summary>
    /// 카메라 흔들림 컴포넌트 참조입니다.
    /// </summary>
    [Tooltip("카메라 흔들림을 제어하는 컴포넌트입니다.")]
    [SerializeField] private CameraShake _cameraShake;

    /// <summary>
    /// 목표 회전값입니다. (x: 좌우, y: 상하)
    /// </summary>
    private Vector2 _targetRotation = Vector2.zero;

    #region Unity 이벤트 함수
    /// <summary>
    /// 초기화를 수행합니다.
    /// </summary>
    private void Start()
    {
        // 초기 회전값 설정
        Vector3 angles = transform.eulerAngles;
        _targetRotation = new Vector2(angles.y, -angles.x);
    }

    /// <summary>
    /// 컴포넌트가 활성화될 때 이벤트를 구독합니다.
    /// </summary>
    private void OnEnable()
    {
        // 플레이어 회전 입력 이벤트 구독
        CameraEvents.OnPlayerRotationInput += OnPlayerRotationInput;
    }

    /// <summary>
    /// 컴포넌트가 비활성화될 때 이벤트 구독을 해제합니다.
    /// </summary>
    private void OnDisable()
    {
        // 이벤트 구독 해제
        CameraEvents.OnPlayerRotationInput -= OnPlayerRotationInput;
    }

    /// <summary>
    /// 모든 업데이트가 끝난 후 카메라 회전을 적용합니다.
    /// </summary>
    private void LateUpdate()
    {
        // 카메라 회전 업데이트 (플레이어 업데이트 이후)
        UpdateCameraRotation();
    }
    #endregion

    #region 비공개 메서드
    /// <summary>
    /// 플레이어 회전 입력 이벤트를 처리합니다.
    /// </summary>
    /// <param name="rotationInput">회전 입력값(X: 좌우, Y: 상하)</param>
    private void OnPlayerRotationInput(Vector2 rotationInput)
    {
        _targetRotation = rotationInput;
    }

    /// <summary>
    /// 현재 회전값을 기준으로 카메라 회전을 업데이트합니다.
    /// </summary>
    private void UpdateCameraRotation()
    {
        // 카메라 흔들림이 진행 중이면 회전 적용하지 않음
        if (_cameraShake != null && _cameraShake.IsShaking)
            return;

        if (_useSmoothing)
        {
            // 부드러운 회전 적용
            Vector2 currentRotation = new Vector2(transform.eulerAngles.y, -transform.eulerAngles.x);
            currentRotation.x = Mathf.LerpAngle(currentRotation.x, _targetRotation.x, Time.deltaTime * 1 / _smoothTime);
            currentRotation.y = Mathf.LerpAngle(currentRotation.y, _targetRotation.y, Time.deltaTime * 1 / _smoothTime);

            transform.eulerAngles = new Vector3(-currentRotation.y, currentRotation.x, 0f);
        }
        else
        {
            // 즉시 회전 적용
            transform.eulerAngles = new Vector3(-_targetRotation.y, _targetRotation.x, 0f);
        }
    }
    #endregion
}
