using System;
using UnityEngine;

/// <summary>
/// 플레이어 이동 상태를 정의하는 열거형입니다.
/// </summary>
public enum PlayerMovementState
{
    Idle,       // 가만히 서 있는 상태
    Walking,    // 걷는 상태
    Running,    // 달리는 상태
    Jumping,    // 점프 중인 상태 (상승)
    Falling,    // 낙하 중인 상태 (공중)
    Rolling,    // 구르기 중인 상태
    WallClimbing // 벽 오르기 중인 상태
}

/// <summary>
/// 플레이어 이동 및 물리 동작을 처리하는 클래스입니다.
/// </summary>
public class PlayerMove : MonoBehaviour
{
    #region 필드
    [Header("컴포넌트 참조")]
    [SerializeField] private PlayerStat _playerStat;
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private PlayerInputHandler _inputHandler;

    [Header("벽 오르기 설정")]
    [SerializeField] private LayerMask _wallLayer = 1 << 7; // 벽으로 인식할 레이어, 기본값 7번 레이어(Wall)

    #region 이동 관련 변수
    private float _walkSpeed; // 걷기 속도
    private float _runSpeed; // 달리기 속도
    private float _moveInputThreshold; // 이동 감지 임계값

    private float _currentMoveSpeed; // 현재 이동 속도
    private Vector3 _moveDirection = Vector3.zero; // 입력에 따른 이동 방향
    #endregion

    #region 점프 관련 변수
    private float _jumpPower; // 점프 파워
    private int _maxJumpCount; // 최대 점프 횟수 (2 = 2단 점프)

    private int _jumpCount = 0; // 현재 점프 횟수
    private float _yVelocity = 0f; // Y축 속도
    private const float GRAVITY = -9.81f; // 중력 상수
    #endregion

    #region 구르기 관련 변수
    private float _rollSpeed; // 구르기 속도
    private float _rollDuration; // 구르기 지속 시간
    private float _rollCooldown; // 구르기 쿨다운 시간
    private float _rollRotationSpeed; // 구르기 회전 속도 (도/초)
    private float _rollYAxisClearValue; // Y축 방향 초기화 값
    private float _rollDirectionDotThreshold; // 구르기 방향 결정 임계값

    private float _rollTimer = 0f; // 구르기 타이머
    private float _lastRollTime = -10f; // 마지막 구르기 시간 (쿨다운을 위함)
    private Vector3 _rollDirection = Vector3.zero; // 실제 구르기 이동 방향
    private Quaternion _originalRotation; // 구르기 전 원래 회전값
    private Vector3 _rollRotationAxis = Vector3.left; // 구르기 회전축
    #endregion

    #region 벽 오르기 관련 변수
    private float _wallClimbSpeed; // 벽 오르기 속도
    private float _wallDescendSpeed; // 벽 내려가기 속도
    private float _wallStrafeSpeed; // 벽 좌우 이동 속도
    private float _minWallNormalY; // 벽으로 인식할 최소 수직 각도 (0.7 = 약 45도)
    private float _wallInputThreshold; // 벽 오르기 중 입력 감지 기준값
    private float _wallMaxDistance; // 벽에서 떨어질 거리 기준값

    private Collider _currentWall; // 현재 접촉 중인 벽 콜라이더
    #endregion

    #region 상태 변수
    private PlayerMovementState _currentState = PlayerMovementState.Idle;
    private bool _isRollComplete = false; // 구르기 완료 플래그
    #endregion
    #endregion

    #region 공용 프로퍼티
    /// <summary>
    /// 걷기 속도를 반환합니다.
    /// </summary>
    public float WalkSpeed => _walkSpeed;

    /// <summary>
    /// 달리기 속도를 반환합니다.
    /// </summary>
    public float RunSpeed => _runSpeed;

    /// <summary>
    /// 현재 이동 속도를 반환합니다.
    /// </summary>
    public float CurrentMoveSpeed => _currentMoveSpeed;

    /// <summary>
    /// 점프 파워를 반환합니다.
    /// </summary>
    public float JumpPower => _jumpPower;

    /// <summary>
    /// 최대 점프 횟수를 반환합니다.
    /// </summary>
    public int MaxJumpCount => _maxJumpCount;
    #endregion

    #region Unity 이벤트 함수
    private void Start()
    {
        // 참조 유효성 검사
        if (_playerStat == null)
        {
            Debug.LogError("PlayerStat 컴포넌트가 할당되지 않았습니다!", this);
        }

        if (_characterController == null)
        {
            Debug.LogError("CharacterController 컴포넌트가 할당되지 않았습니다!", this);
        }

        if (_inputHandler == null)
        {
            Debug.LogError("PlayerInputHandler 컴포넌트가 할당되지 않았습니다!", this);
        }

        InitializeMovementStats();
    }

    private void Update()
    {
        HandleInput();
        UpdateMovementState();
        ApplyPhysicsAndMove();
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        bool isWallLayer = ((1 << hit.collider.gameObject.layer) & _wallLayer) != 0;
        if (!isWallLayer) return;

        bool canAttemptWallClimb = (_currentState == PlayerMovementState.Idle ||
                                   _currentState == PlayerMovementState.Walking ||
                                   _currentState == PlayerMovementState.Running) &&
                                   _currentState != PlayerMovementState.Rolling; // 구르기 중에는 벽 오르기 시도 불가

        if (!canAttemptWallClimb) return;

        float wallNormalY = Mathf.Abs(hit.normal.y);
        bool isSteepEnough = wallNormalY < _minWallNormalY;
        if (!isSteepEnough) return;

        float dotProduct = Vector3.Dot(transform.forward, -hit.normal);
        bool isFacingWall = dotProduct > 0.5f;
        bool hasEnoughStamina = _playerStat.Stamina > 0;
        bool isPressingUp = _inputHandler.VerticalInput > _wallInputThreshold;

        if (isFacingWall && hasEnoughStamina && isPressingUp)
        {
            _currentWall = hit.collider; // 벽 정보 저장
        }
    }
    #endregion

    #region 이동 처리 메서드
    /// <summary>
    /// 입력을 처리하고 이동 방향을 계산합니다.
    /// </summary>
    private void HandleInput()
    {
        CalculateMoveDirection();
    }

    /// <summary>
    /// 입력을 기반으로 이동 방향을 계산합니다.
    /// </summary>
    private void CalculateMoveDirection()
    {
        _moveDirection = _inputHandler.MoveDirection;
    }

    /// <summary>
    /// 현재 상태에 따라 이동 상태를 업데이트합니다.
    /// </summary>
    private void UpdateMovementState()
    {
        if (HandleWallClimbingStateTransition()) return;
        if (HandleRollingStateTransition()) return;
        if (HandleAirborneStateTransition()) return;
        if (HandleRunningStateTransition()) return;
        if (HandleWalkingStateTransition()) return;

        if (_characterController.isGrounded &&
            _currentState != PlayerMovementState.Idle && // Idle 상태는 항상 유지
            _currentState != PlayerMovementState.Rolling && // Rolling 상태는 구르기 종료 시 처리
            _currentState != PlayerMovementState.WallClimbing) // WallClimbing 상태는 벽에서 떨어질 때 처리
        {
            SetState(PlayerMovementState.Idle);
        }
    }

    /// <summary>
    /// 이동 상태를 설정하고 관련 동작을 처리합니다.
    /// </summary>
    /// <param name="newState">새로운 이동 상태</param>
    private void SetState(PlayerMovementState newState)
    {
        if (_currentState == newState) return;

        // 상태 종료 로직
        switch (_currentState)
        {
            case PlayerMovementState.Rolling:
                transform.rotation = _originalRotation; // 구르기 종료 시 회전 복원
                break;
            case PlayerMovementState.WallClimbing:
                _currentWall = null; // 벽 참조 해제
                break;
        }

        _currentState = newState;

        // 상태 시작 로직
        switch (_currentState)
        {
            case PlayerMovementState.Rolling:
                _rollTimer = 0f;
                _lastRollTime = Time.time;
                _playerStat.UseStamina(_playerStat.RollStaminaCost);
                _originalRotation = transform.rotation;
                CalculateRollDirectionAndAxis();
                _isRollComplete = false; // 구르기 시작 시 완료 플래그 리셋
                break;
            case PlayerMovementState.WallClimbing:
                _yVelocity = 0f;
                _jumpCount = 0;
                break;
            case PlayerMovementState.Idle:
            case PlayerMovementState.Walking:
                if (_characterController.isGrounded) _jumpCount = 0; // 땅에 있을 때 점프 카운트 초기화
                break;
        }

        // 상태에 따른 이동 속도 설정
        switch (_currentState)
        {
            case PlayerMovementState.Running:
                _currentMoveSpeed = _runSpeed;
                break;
            case PlayerMovementState.Walking:
            case PlayerMovementState.Idle:
            case PlayerMovementState.Falling:
            case PlayerMovementState.Jumping:
                _currentMoveSpeed = _walkSpeed; // 기본 속도를 걷기 속도로 설정
                break;
        }
    }
    #endregion

    #region 벽 오르기 관련 메서드
    /// <summary>
    /// 벽 오르기 상태 전환을 처리합니다.
    /// </summary>
    /// <returns>상태 전환이 발생했는지 여부</returns>
    private bool HandleWallClimbingStateTransition()
    {
        // 아직 벽 오르기 상태가 아니고, 땅에 있으며, 최근에 유효한 벽(_currentWall)과 충돌했고, 필요한 입력/스태미너 조건 만족 시
        if (_currentState != PlayerMovementState.WallClimbing && _characterController.isGrounded && _currentWall != null)
        {
            bool hasEnoughStamina = _playerStat.Stamina > 0;
            bool isPressingUp = _inputHandler.VerticalInput > _wallInputThreshold;
            // isFacingWall, isSteepEnough 조건은 OnControllerColliderHit에서 _currentWall 설정 시 이미 검증됨

            if (isPressingUp && hasEnoughStamina)
            {
                SetState(PlayerMovementState.WallClimbing);
                return true; // 벽 오르기 시작
            }
        }

        if (_currentState == PlayerMovementState.WallClimbing)
        {
            if (CheckWallClimbContinuationAndJump())
            {
                return true; // 벽 오르기 계속
            }
            else
            {
                // 벽 오르기 종료 조건 만족 (SetState는 Check~ 내부 또는 다른 상태 핸들러에서 처리)
                return false; // 다른 상태 확인 계속
            }
        }

        // 벽 오르기 상태가 아니고 땅에 있으면 벽 참조 초기화 (중요: 벽에서 떨어진 후 다시 붙는 경우 방지)
        if (_currentState != PlayerMovementState.WallClimbing && _characterController.isGrounded)
        {
            _currentWall = null;
        }

        return false; // 벽 오르기 관련 상태 변경 없음
    }

    /// <summary>
    /// 벽 오르기 상태를 계속 유지할지 또는 벽에서 점프할지 확인합니다.
    /// </summary>
    /// <returns>벽 오르기를 계속할지 여부</returns>
    private bool CheckWallClimbContinuationAndJump()
    {
        if (_playerStat.Stamina <= 0) return false;
        if (_currentWall == null) return false; // 벽 참조가 없으면 종료 (중요)
        Vector3 closestPoint = _currentWall.ClosestPoint(transform.position);
        float distanceToWall = Vector3.Distance(transform.position, closestPoint);
        if (distanceToWall > _wallMaxDistance) return false;
        if (_characterController.isGrounded && _inputHandler.VerticalInput <= 0) return false;

        if (_inputHandler.IsJumpPressed)
        {
            Vector3 wallJumpDirection = (_currentWall.ClosestPointOnBounds(transform.position) - transform.position).normalized + Vector3.up;
            _yVelocity = _jumpPower;
            _moveDirection = wallJumpDirection;
            SetState(PlayerMovementState.Jumping);
            _jumpCount = 1;
            return false; // 벽 오르기 종료
        }
        return true; // 벽 오르기 계속
    }
    #endregion

    #region 구르기 관련 메서드
    /// <summary>
    /// 구르기 상태 전환을 처리합니다.
    /// </summary>
    /// <returns>상태 전환이 발생했는지 여부</returns>
    private bool HandleRollingStateTransition()
    {
        float timeSinceLastRoll = Time.time - _lastRollTime;

        // 구르기 시작 조건
        if (_inputHandler.IsRollPressed && _currentState != PlayerMovementState.Rolling &&
            timeSinceLastRoll >= _rollCooldown && _playerStat.Stamina >= _playerStat.RollStaminaCost &&
            _currentState != PlayerMovementState.WallClimbing && _characterController.isGrounded)
        {
            SetState(PlayerMovementState.Rolling);
            return true;
        }

        // 구르기 진행 및 종료 처리
        if (_currentState == PlayerMovementState.Rolling)
        {
            UpdateRolling();
            if (_isRollComplete) return false; // 구르기 완료 시 다른 상태 확인 허용
            return _currentState == PlayerMovementState.Rolling; // 완료되지 않았으면 구르기 상태 유지
        }

        return false;
    }

    /// <summary>
    /// 구르기 방향과 회전축을 계산합니다.
    /// </summary>
    private void CalculateRollDirectionAndAxis()
    {
        Vector3 inputDir = _moveDirection.normalized;
        _rollDirection = (inputDir.sqrMagnitude > 0.1f) ? inputDir : Vector3.zero;
        if (_rollDirection != Vector3.zero)
        {
            _rollDirection.y = 0;
            _rollDirection = _rollDirection.normalized;
        }

        if (_rollDirection == Vector3.zero)
        {
            _rollRotationAxis = Vector3.right;
        }
        else
        {
            float dotProduct = Vector3.Dot(transform.forward, _rollDirection);
            _rollRotationAxis = (dotProduct < _rollDirectionDotThreshold) ? Vector3.left : Vector3.right;
        }
    }

    /// <summary>
    /// 구르기 상태를 업데이트합니다.
    /// </summary>
    private void UpdateRolling()
    {
        _rollTimer += Time.deltaTime;

        float rollRotationAngle = _rollRotationSpeed * _rollTimer;
        transform.rotation = _originalRotation * Quaternion.AngleAxis(rollRotationAngle, _rollRotationAxis);

        // 구르기 종료 (시간 초과)
        if (_rollTimer >= _rollDuration)
        {
            _isRollComplete = true; // 완료 플래그 설정
            return; // 상태 변경은 다음 프레임 UpdateMovementState에서 처리
        }

        // 구르기 중 점프
        if (_inputHandler.IsJumpPressed)
        {
            _yVelocity = _jumpPower;
            SetState(PlayerMovementState.Jumping);
            _jumpCount = 1;
            // _rollComplete는 false인 상태로 점프로 넘어감
        }
    }
    #endregion

    #region 공중 상태 관련 메서드
    /// <summary>
    /// 점프와 낙하 상태 전환을 처리합니다.
    /// </summary>
    /// <returns>상태 전환이 발생했는지 여부</returns>
    private bool HandleAirborneStateTransition()
    {
        if (_inputHandler.IsJumpPressed && _currentState != PlayerMovementState.Rolling &&
            _currentState != PlayerMovementState.WallClimbing && _jumpCount < _maxJumpCount)
        {
            _yVelocity = _jumpPower;
            _jumpCount++;
            SetState(PlayerMovementState.Jumping);
            return true;
        }

        if (!_characterController.isGrounded)
        {
            if (_currentState != PlayerMovementState.Jumping && _currentState != PlayerMovementState.Falling)
            {
                SetState(_yVelocity > 0 ? PlayerMovementState.Jumping : PlayerMovementState.Falling);
            }
            else if (_currentState == PlayerMovementState.Jumping && _yVelocity <= 0)
            {
                SetState(PlayerMovementState.Falling); // 상승 중 속도가 0 이하가 되면 낙하 상태로 변경
            }
            return true;
        }
        return false;
    }
    #endregion

    #region 지상 이동 관련 메서드
    /// <summary>
    /// 달리기 상태 전환을 처리합니다.
    /// </summary>
    /// <returns>상태 전환이 발생했는지 여부</returns>
    private bool HandleRunningStateTransition()
    {
        if (_inputHandler.IsRunPressed && _playerStat.Stamina > 0 && _characterController.isGrounded &&
            _currentState != PlayerMovementState.Rolling && _currentState != PlayerMovementState.WallClimbing &&
            _currentState != PlayerMovementState.Jumping && _currentState != PlayerMovementState.Falling &&
            _moveDirection.sqrMagnitude > _moveInputThreshold * _moveInputThreshold)
        {
            _playerStat.UseStamina(_playerStat.StaminaUseRate * Time.deltaTime);
            SetState(PlayerMovementState.Running);
            return true;
        }
        else if (_currentState == PlayerMovementState.Running &&
                 (!_inputHandler.IsRunPressed || _playerStat.Stamina <= 0 || !_characterController.isGrounded ||
                  _moveDirection.sqrMagnitude <= _moveInputThreshold * _moveInputThreshold))
        {
            return false; // 달리기 종료 조건, 다른 상태 확인 계속
        }
        return _currentState == PlayerMovementState.Running;
    }

    /// <summary>
    /// 걷기 상태 전환을 처리합니다.
    /// </summary>
    /// <returns>상태 전환이 발생했는지 여부</returns>
    private bool HandleWalkingStateTransition()
    {
        if (_moveDirection.sqrMagnitude > _moveInputThreshold * _moveInputThreshold && _characterController.isGrounded &&
             _currentState != PlayerMovementState.Rolling && _currentState != PlayerMovementState.WallClimbing &&
             _currentState != PlayerMovementState.Jumping && _currentState != PlayerMovementState.Falling &&
             _currentState != PlayerMovementState.Running)
        {
            SetState(PlayerMovementState.Walking);
            return true;
        }
        return _currentState == PlayerMovementState.Walking;
    }
    #endregion

    #region 물리 및 이동 적용 메서드
    /// <summary>
    /// 중력과 이동을 적용합니다.
    /// </summary>
    private void ApplyPhysicsAndMove()
    {
        ApplyGravity();

        if (_currentState == PlayerMovementState.Idle || _currentState == PlayerMovementState.Walking)
        {
            _playerStat.RecoverStamina();
        }

        CalculateAndApplyMovement();
    }

    /// <summary>
    /// 중력을 적용합니다.
    /// </summary>
    private void ApplyGravity()
    {
        if (_currentState == PlayerMovementState.WallClimbing)
        {
            _yVelocity = 0f;
            return;
        }

        if (_characterController.isGrounded)
        {
            if (!_inputHandler.IsJumpPressed && _currentState != PlayerMovementState.Rolling)
            {
                _yVelocity = -1f; // 땅에 붙도록 함
            }
        }
        else
        {
            _yVelocity += GRAVITY * Time.deltaTime;
        }
    }

    /// <summary>
    /// 현재 상태에 따라 이동을 계산하고 적용합니다.
    /// </summary>
    private void CalculateAndApplyMovement()
    {
        Vector3 finalMovement = Vector3.zero;
        float currentSpeed = _currentMoveSpeed;

        switch (_currentState)
        {
            case PlayerMovementState.WallClimbing:
                finalMovement = CalculateWallClimbMovement(out currentSpeed);
                break;
            case PlayerMovementState.Rolling:
                finalMovement = CalculateRollMovement(out currentSpeed);
                finalMovement.y = _yVelocity; // 구르기 중 중력 적용
                break;
            case PlayerMovementState.Jumping:
            case PlayerMovementState.Falling:
                finalMovement = CalculateAirborneMovement(out currentSpeed);
                finalMovement.y = _yVelocity;
                break;
            case PlayerMovementState.Running:
            case PlayerMovementState.Walking:
                finalMovement = CalculateGroundMovement(out currentSpeed);
                finalMovement.y = _yVelocity;
                break;
            case PlayerMovementState.Idle:
                finalMovement = Vector3.zero;
                finalMovement.y = _yVelocity;
                currentSpeed = 1f;
                break;
        }

        _characterController.Move(finalMovement * currentSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 지상 이동을 계산합니다.
    /// </summary>
    /// <param name="speed">적용할 이동 속도</param>
    /// <returns>이동 벡터</returns>
    private Vector3 CalculateGroundMovement(out float speed)
    {
        speed = _currentMoveSpeed;
        return _moveDirection;
    }

    /// <summary>
    /// 공중 이동을 계산합니다.
    /// </summary>
    /// <param name="speed">적용할 이동 속도</param>
    /// <returns>이동 벡터</returns>
    private Vector3 CalculateAirborneMovement(out float speed)
    {
        speed = _currentMoveSpeed;
        return _moveDirection;
    }

    /// <summary>
    /// 구르기 이동을 계산합니다.
    /// </summary>
    /// <param name="speed">적용할 이동 속도</param>
    /// <returns>이동 벡터</returns>
    private Vector3 CalculateRollMovement(out float speed)
    {
        speed = _rollSpeed;
        return _rollDirection;
    }

    /// <summary>
    /// 벽 오르기 이동을 계산합니다.
    /// </summary>
    /// <param name="speed">적용할 이동 속도</param>
    /// <returns>이동 벡터</returns>
    private Vector3 CalculateWallClimbMovement(out float speed)
    {
        Vector3 wallMovement = _inputHandler.GetWallClimbDirection(transform.right);
        WallClimbSpeedType speedType = _inputHandler.GetWallClimbSpeedType();

        switch (speedType)
        {
            case WallClimbSpeedType.Climb:
                speed = _wallClimbSpeed;
                break;
            case WallClimbSpeedType.Descend:
                speed = _wallDescendSpeed;
                break;
            default: // Strafe
                speed = _wallStrafeSpeed;
                break;
        }

        if (_playerStat.Stamina > 0)
        {
            float staminaCost = _playerStat.GetWallClimbStaminaCost(
                _inputHandler.VerticalInput,
                _inputHandler.HorizontalInput,
                _inputHandler.WallInputThreshold);
            _playerStat.UseStamina(staminaCost);
        }
        else
        {
            // 스태미너 없으면 상태 변경은 UpdateMovementState에서 처리
            speed = 0f;
            return Vector3.zero;
        }
        return wallMovement;
    }
    #endregion

    #region 초기화 메서드
    /// <summary>
    /// 이동 관련 스탯을 초기화합니다.
    /// </summary>
    private void InitializeMovementStats()
    {
        _walkSpeed = _playerStat.WalkSpeed;
        _runSpeed = _playerStat.RunSpeed;
        _moveInputThreshold = _playerStat.MoveInputThreshold;

        _jumpPower = _playerStat.JumpPower;
        _maxJumpCount = _playerStat.MaxJumpCount;

        _rollSpeed = _playerStat.RollSpeed;
        _rollDuration = _playerStat.RollDuration;
        _rollCooldown = _playerStat.RollCooldown;
        _rollRotationSpeed = _playerStat.RollRotationSpeed;
        _rollYAxisClearValue = _playerStat.RollYAxisClearValue;
        _rollDirectionDotThreshold = _playerStat.RollDirectionDotThreshold;

        _wallClimbSpeed = _playerStat.WallClimbSpeed;
        _wallDescendSpeed = _playerStat.WallDescendSpeed;
        _wallStrafeSpeed = _playerStat.WallStrafeSpeed;
        _minWallNormalY = _playerStat.MinWallNormalY;
        _wallInputThreshold = _playerStat.WallInputThreshold;
        _wallMaxDistance = _playerStat.WallMaxDistance;

        _currentMoveSpeed = _walkSpeed;
        SetState(PlayerMovementState.Idle); // 초기 상태 설정
    }
    #endregion
}
