using UnityEngine;

/// <summary>
/// 카메라 회전을 담당하는 클래스입니다.
/// </summary>
public class CameraRotate : MonoBehaviour
{
    [Header("회전 설정")]
    [SerializeField] private float _smoothTime = 0.1f;
    [SerializeField] private bool _useSmoothing = false;

    private Vector2 _targetRotation = Vector2.zero;

    #region Unity 이벤트 함수
    private void Start()
    {
        // 초기 회전값 설정
        Vector3 angles = transform.eulerAngles;
        _targetRotation = new Vector2(angles.y, -angles.x);
    }

    private void OnEnable()
    {
        // 플레이어 회전 입력 이벤트 구독
        CameraEvents.OnPlayerRotationInput += OnPlayerRotationInput;
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        CameraEvents.OnPlayerRotationInput -= OnPlayerRotationInput;
    }

    private void LateUpdate()
    {
        // 카메라 회전 업데이트 (플레이어 업데이트 이후)
        UpdateCameraRotation();
    }
    #endregion

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
}
