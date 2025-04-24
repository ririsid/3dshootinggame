using UnityEngine;

public class PlayerInputHandler : MonoBehaviour
{
    // 이동 방향 벡터
    private Vector3 _moveDirection = Vector3.zero;

    // 상태 플래그들
    private bool _isJumpPressed = false;
    private bool _isRunPressed = false;
    private bool _isRollPressed = false;

    // 벽 오르기 관련 입력
    private float _verticalInput = 0f;
    private float _horizontalInput = 0f;
    private bool _isWallClimbInputActive = false;

    // 입력 임계값
    [SerializeField] private float _moveInputThreshold = 0.1f;
    [SerializeField] private float _wallInputThreshold = 0.3f;

    #region 공용 프로퍼티
    // 다른 컴포넌트가 접근할 프로퍼티들
    public Vector3 MoveDirection => _moveDirection;
    public bool IsJumpPressed => _isJumpPressed;
    public bool IsRunPressed => _isRunPressed;
    public bool IsRollPressed => _isRollPressed;
    public float VerticalInput => _verticalInput;
    public float HorizontalInput => _horizontalInput;
    public float MoveInputThreshold => _moveInputThreshold;
    public float WallInputThreshold => _wallInputThreshold;
    public bool IsWallClimbInputActive => _isWallClimbInputActive;
    #endregion

    void Update()
    {
        // 이동 입력 처리
        ProcessMovementInput();

        // 액션 입력 처리
        ProcessActionInput();

        // 벽 오르기 입력 처리
        ProcessWallClimbingInput();
    }

    private void ProcessMovementInput()
    {
        // 키보드 입력을 받는다.
        _horizontalInput = Input.GetAxis("Horizontal");
        _verticalInput = Input.GetAxis("Vertical");

        // 입력으로부터 방향을 설정한다.
        _moveDirection = new Vector3(_horizontalInput, 0f, _verticalInput);

        // 이동 벡터의 크기가 설정된 임계값보다 크면 정규화
        if (_moveDirection.magnitude > _moveInputThreshold)
        {
            _moveDirection = _moveDirection.normalized;
        }

        // 메인 카메라를 기준으로 방향을 변환한다.
        _moveDirection = Camera.main.transform.TransformDirection(_moveDirection);
        _moveDirection.y = 0f; // Y축은 0으로 설정하여 수평 이동만 적용
    }

    private void ProcessActionInput()
    {
        // 점프 입력 확인
        _isJumpPressed = Input.GetButtonDown("Jump");

        // 달리기 입력 확인
        _isRunPressed = Input.GetKey(KeyCode.LeftShift);

        // 구르기 입력 확인
        _isRollPressed = Input.GetKeyDown(KeyCode.E);
    }

    private void ProcessWallClimbingInput()
    {
        // 벽 오르기 입력 활성화 여부 확인
        // 수직 입력이 임계값보다 크면 벽 오르기 입력이 활성화된 것으로 간주
        _isWallClimbInputActive = _verticalInput > _wallInputThreshold;
    }

    /// <summary>
    /// 벽 오르기 방향을 계산합니다. 
    /// 플레이어의 로컬 좌표계 기준으로 벽 오르기 입력을 처리합니다.
    /// </summary>
    /// <param name="rightVector">플레이어의 오른쪽 방향 벡터</param>
    /// <returns>정규화된 벽 오르기 방향 벡터</returns>
    public Vector3 GetWallClimbDirection(Vector3 rightVector)
    {
        // 벽 오르기 중 대각선 이동 지원
        Vector3 verticalMovement = Vector3.up * _verticalInput;
        Vector3 horizontalMovement = transform.TransformDirection(rightVector * _horizontalInput); // 우측 벡터를 기준으로 수평 이동 계산

        // 대각선 이동을 위한 벡터 합산
        Vector3 direction = verticalMovement + horizontalMovement;

        // 벡터의 크기가 0보다 클 때 정규화
        if (direction.magnitude > 0)
        {
            return direction.normalized;
        }
        return direction;
    }

    /// <summary>
    /// 벽 오르기 입력에 따른 속도 타입을 반환합니다.
    /// </summary>
    /// <returns>벽 오르기 속도 타입 (오름, 내림, 좌우 이동)</returns>
    public WallClimbSpeedType GetWallClimbSpeedType()
    {
        if (_verticalInput > _wallInputThreshold)
        {
            return WallClimbSpeedType.Climb; // 상승 이동
        }
        else if (_verticalInput < -_wallInputThreshold)
        {
            return WallClimbSpeedType.Descend; // 하강 이동
        }
        else
        {
            return WallClimbSpeedType.Strafe; // 좌우 이동
        }
    }
}

// 벽 오르기 속도 타입을 정의하는 열거형
public enum WallClimbSpeedType
{
    Climb,   // 벽 오르기 (상승)
    Descend, // 벽 내려가기 (하강)
    Strafe   // 벽에서 좌우 이동
}