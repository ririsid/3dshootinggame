using System;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    #region 이동 관련 변수
    [Header("이동 설정")]
    private float _walkSpeed; // 걷기 속도
    private float _runSpeed; // 달리기 속도
    private float _moveInputThreshold; // 이동 감지 임계값
    private float _diagonalMovementNormalizeThreshold; // 대각선 이동 정규화 임계값

    private float _currentMoveSpeed; // 현재 이동 속도
    private bool _isRunning = false; // 달리기 중인지 여부
    private Vector3 _moveDirection = Vector3.zero; // 이동 방향
    #endregion

    #region 점프 관련 변수
    [Header("점프 설정")]
    private float _jumpPower; // 점프 파워
    private int _maxJumpCount; // 최대 점프 횟수 (2 = 2단 점프)

    private int _jumpCount = 0; // 현재 점프 횟수
    private float _yVelocity = 0f; // Y축 속도
    private const float GRAVITY = -9.81f; // 중력 상수
    #endregion

    #region 구르기 관련 변수
    [Header("구르기 설정")]
    private float _rollSpeed; // 구르기 속도
    private float _rollDuration; // 구르기 지속 시간
    private float _rollCooldown; // 구르기 쿨다운 시간
    private float _rollRotationSpeed; // 구르기 회전 속도 (도/초)
    private float _rollYAxisClearValue; // Y축 방향 초기화 값
    private float _rollDirectionDotThreshold; // 구르기 방향 결정 임계값

    private bool _isRolling = false; // 구르기 중인지 여부
    private float _rollTimer = 0f; // 구르기 타이머
    private float _lastRollTime = -10f; // 마지막 구르기 시간 (쿨다운을 위함)
    private Vector3 _rollDirection = Vector3.zero; // 구르기 방향
    private Quaternion _originalRotation; // 구르기 전 원래 회전값
    private Vector3 _rollRotationAxis = Vector3.left; // 구르기 회전축
    #endregion

    #region 벽 오르기 관련 변수
    [Header("벽 오르기 설정")]
    private float _wallClimbSpeed; // 벽 오르기 속도
    private float _wallDescendSpeed; // 벽 내려가기 속도
    private float _wallStrafeSpeed; // 벽 좌우 이동 속도
    private float _minWallNormalY; // 벽으로 인식할 최소 수직 각도 (0.7 = 약 45도)
    [SerializeField] private LayerMask _wallLayer = 1 << 6; // 벽으로 인식할 레이어, 기본값 6번 레이어(Wall)
    private float _wallInputThreshold; // 벽 오르기 중 입력 감지 기준값
    private float _wallMaxDistance; // 벽에서 떨어질 거리 기준값

    private bool _isWallClimbing = false; // 벽 오르기 중인지 여부
    private Collider _currentWall; // 현재 접촉 중인 벽 콜라이더
    #endregion

    #region 공용 프로퍼티
    // 외부에서 읽기만 가능한 프로퍼티들
    public float WalkSpeed => _walkSpeed;
    public float RunSpeed => _runSpeed;
    public float CurrentMoveSpeed => _currentMoveSpeed;
    public float JumpPower => _jumpPower;
    public int MaxJumpCount => _maxJumpCount;
    public bool IsRunning => _isRunning;
    public bool IsWallClimbing => _isWallClimbing;
    #endregion

    #region 내부 참조
    private PlayerStat _playerStat; // 플레이어 스탯 참조
    private CharacterController _characterController; // 캐릭터 컨트롤러 컴포넌트
    #endregion

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();

        // PlayerStat 컴포넌트 자동 참조
        _playerStat = GetComponent<PlayerStat>();
        if (_playerStat == null)
        {
            Debug.LogError("PlayerStat 컴포넌트를 찾을 수 없습니다!", this);
            return;
        }

        // PlayerStat에서 모든 이동 스탯 초기화
        InitializeMovementStats();
    }

    private void Update()
    {
        // 사용자 입력 처리 및 이동 방향 계산
        CalculateMoveDirection();

        // 벽 감지 및 오르기 처리
        HandleWallClimbing();

        // 벽에 붙어있지 않을 때만 일반 이동 처리
        if (!_isWallClimbing)
        {
            // 달리기 처리
            HandleRunning();

            // 구르기 처리
            HandleRolling();

            // 점프 처리
            HandleJump();
        }

        // 중력 처리 (벽 오르기 중이 아닐 때만)
        if (!_isWallClimbing)
        {
            ApplyGravity();
        }

        // 스태미너 회복 (벽 오르기 중이 아닐 때만)
        if (!_isWallClimbing && !_isRunning)
        {
            _playerStat.RecoverStamina();
        }

        // 최종 이동 적용
        ApplyMovement();
    }

    private void InitializeMovementStats()
    {
        // PlayerStatSO의 값으로 이동 관련 변수 초기화
        _walkSpeed = _playerStat.WalkSpeed;
        _runSpeed = _playerStat.RunSpeed;
        _moveInputThreshold = _playerStat.MoveInputThreshold;
        _diagonalMovementNormalizeThreshold = _playerStat.DiagonalMovementNormalizeThreshold;

        // 점프 관련 변수 초기화
        _jumpPower = _playerStat.JumpPower;
        _maxJumpCount = _playerStat.MaxJumpCount;

        // 구르기 관련 변수 초기화
        _rollSpeed = _playerStat.RollSpeed;
        _rollDuration = _playerStat.RollDuration;
        _rollCooldown = _playerStat.RollCooldown;
        _rollRotationSpeed = _playerStat.RollRotationSpeed;
        _rollYAxisClearValue = _playerStat.RollYAxisClearValue;
        _rollDirectionDotThreshold = _playerStat.RollDirectionDotThreshold;

        // 벽 오르기 관련 변수 초기화
        _wallClimbSpeed = _playerStat.WallClimbSpeed;
        _wallDescendSpeed = _playerStat.WallDescendSpeed;
        _wallStrafeSpeed = _playerStat.WallStrafeSpeed;
        _minWallNormalY = _playerStat.MinWallNormalY;
        _wallInputThreshold = _playerStat.WallInputThreshold;
        _wallMaxDistance = _playerStat.WallMaxDistance;

        // 초기값 설정
        _currentMoveSpeed = _walkSpeed;
    }

    private void CalculateMoveDirection()
    {
        // 키보드 입력을 받는다.
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // 입력으로부터 방향을 설정한다.
        _moveDirection = new Vector3(h, 0f, v);
        _moveDirection = _moveDirection.normalized;

        // 메인 카메라를 기준으로 방향을 변환한다.
        _moveDirection = Camera.main.transform.TransformDirection(_moveDirection);
        // TransformDirection: 로컬 공간의 벡터를 월드 공간의 벡터로 바꿔주는 함수
    }

    private void HandleRunning()
    {
        // Shift 키를 누르면 달리기
        if (Input.GetKey(KeyCode.LeftShift) && _playerStat.Stamina > 0 && _moveDirection.magnitude > _moveInputThreshold)
        {
            _isRunning = true;
            _currentMoveSpeed = _runSpeed;

            // 달릴 때 스태미너 소모
            _playerStat.UseStamina(_playerStat.StaminaUseRate * Time.deltaTime);
        }
        else
        {
            _isRunning = false;
            _currentMoveSpeed = _walkSpeed;
        }
    }

    private void HandleRolling()
    {
        // 구르기 쿨다운 계산
        float timeSinceLastRoll = Time.time - _lastRollTime;

        // E 키를 눌러 구르기 시작 (쿨다운 지났고, 현재 구르기 중이 아니며, 충분한 스태미너가 있을 때)
        if (Input.GetKeyDown(KeyCode.E) && !_isRolling && timeSinceLastRoll >= _rollCooldown && _playerStat.Stamina >= _playerStat.RollStaminaCost)
        {
            // 구르기 방향 설정 (현재 이동 방향 또는 바라보는 방향)
            _rollDirection = _moveDirection.magnitude > _moveInputThreshold ? _moveDirection : transform.forward;
            _rollDirection.y = _rollYAxisClearValue; // Y축 방향 초기화
            _rollDirection = _rollDirection.normalized;

            // 구르기 상태 설정
            _isRolling = true;
            _rollTimer = 0f;
            _lastRollTime = Time.time;

            // 스태미너 소모
            _playerStat.UseStamina(_playerStat.RollStaminaCost);

            // 구르기 전 원래 회전값 저장
            _originalRotation = transform.rotation;

            // 이동 방향이 앞인지 뒤인지 확인하여 회전 방향 결정
            // 캐릭터의 정면 벡터와 이동 방향 간의 내적을 계산
            float dotProduct = Vector3.Dot(transform.forward, _rollDirection);

            // 내적이 임계값보다 작으면 앞으로 이동 중임을 의미
            if (dotProduct < _rollDirectionDotThreshold)
            {
                _rollRotationAxis = Vector3.left;
            }
            else
            {
                _rollRotationAxis = Vector3.right;
            }
        }

        // 구르기 진행 중이라면
        if (_isRolling)
        {
            _rollTimer += Time.deltaTime;

            // 구르기 회전 적용
            float rollRotationAngle = _rollRotationSpeed * _rollTimer;
            transform.rotation = _originalRotation * Quaternion.AngleAxis(rollRotationAngle, _rollRotationAxis);

            // 구르기 시간이 지났으면 종료
            if (_rollTimer >= _rollDuration)
            {
                _isRolling = false;
                transform.rotation = _originalRotation; // 구르기 종료 시 원래 회전값으로 복원
            }
        }
    }

    private void HandleJump()
    {
        // 캐릭터가 땅 위에 있다면...
        if (_characterController.isGrounded)
        {
            _jumpCount = 0; // 땅에 닿으면 점프 카운트 초기화
        }

        // 점프 적용
        if (Input.GetButtonDown("Jump") && _jumpCount < _maxJumpCount)
        {
            // 첫 번째 점프이거나 이미 점프 중이라면 2단 점프 실행
            _yVelocity = _jumpPower;
            _jumpCount++; // 점프 카운트 증가
        }
    }

    private void ApplyGravity()
    {
        // 중력 적용
        _yVelocity += GRAVITY * Time.deltaTime;
    }

    private void ApplyMovement()
    {
        // 수평 이동 계산
        Vector3 movement;
        float speed;

        if (_isWallClimbing)
        {
            // 벽 오르기 중일 때 처리
            float verticalInput = Input.GetAxis("Vertical");
            float horizontalInput = Input.GetAxis("Horizontal");

            // 벽 오르기 중 대각선 이동 지원
            Vector3 verticalMovement = Vector3.up * verticalInput;
            Vector3 horizontalMovement = -transform.right * horizontalInput;

            // 대각선 이동을 위한 벡터 합산
            movement = verticalMovement + horizontalMovement;

            // 정규화 - 대각선 이동 시에도 속도 일정하게 유지
            if (movement.magnitude > _diagonalMovementNormalizeThreshold)
            {
                movement.Normalize();
            }

            // 이동 속도 결정
            if (verticalInput > _wallInputThreshold) // 상승 이동 중심
            {
                speed = _wallClimbSpeed;
            }
            else if (verticalInput < -_wallInputThreshold) // 하강 이동 중심
            {
                speed = _wallDescendSpeed;
            }
            else // 좌우 이동 중심
            {
                speed = _wallStrafeSpeed;
            }

            // 벽 오르기 중 스태미너 소모
            if (_playerStat.Stamina > 0)
            {
                // 입력에 따른 스태미너 소모량 계산
                float staminaCost = _playerStat.GetWallClimbStaminaCost(verticalInput, horizontalInput, _wallInputThreshold);
                _playerStat.UseStamina(staminaCost);
            }
            else
            {
                // 스태미너가 0이면 벽에서 떨어짐 (자유낙하)
                _isWallClimbing = false;
                movement = Vector3.zero;
            }
        }
        else if (_isRolling)
        {
            // 구르기 중일 때 구르기 방향으로 이동
            movement = _rollDirection;
            speed = _rollSpeed;
        }
        else
        {
            // 일반 이동
            movement = _moveDirection;
            speed = _currentMoveSpeed;
        }

        // Y축 속도 적용 (벽 오르기 중이 아니면 중력/점프에 의해 결정)
        if (!_isWallClimbing)
        {
            movement.y = _yVelocity;
        }

        // 캐릭터 컨트롤러로 최종 이동 적용
        _characterController.Move(movement * speed * Time.deltaTime);
    }

    private void HandleWallClimbing()
    {
        // 벽 오르기 중이고 아직 벽이 감지되면
        if (_isWallClimbing && _currentWall != null)
        {
            // 벽에서 아직 스태미너가 있는지 확인
            if (_playerStat.Stamina > 0)
            {
                // 플레이어 위치와 벽 사이의 거리 확인 (벽에서 멀어졌는지 확인)
                Vector3 closestPoint = _currentWall.ClosestPoint(transform.position);
                float distanceToWall = Vector3.Distance(transform.position, closestPoint);

                // 벽에서 너무 멀어지면 벽 오르기 중단
                if (distanceToWall > _wallMaxDistance)
                {
                    _isWallClimbing = false;
                    _currentWall = null;
                }

                // 벽에서 내려갔는지 확인 (땅에 닿았는지)
                if (_characterController.isGrounded && Input.GetAxis("Vertical") <= 0)
                {
                    _isWallClimbing = false;
                    _currentWall = null;
                    _yVelocity = 0f; // Y축 속도 초기화
                }
            }
            else
            {
                // 스태미너가 0이면 벽에서 떨어짐
                _isWallClimbing = false;
                _currentWall = null;
            }
        }
        else
        {
            // 벽 오르기 상태가 아니면 상태 초기화
            _isWallClimbing = false;
            _currentWall = null;
        }
    }

    // CharacterController가 다른 콜라이더와 충돌할 때 호출되는 메서드
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // 충돌이 벽 레이어인지 확인
        if (((1 << hit.collider.gameObject.layer) & _wallLayer) != 0)
        {
            // 법선 벡터 확인 (수직 벽인지 확인)
            if (Mathf.Abs(hit.normal.y) < _minWallNormalY)
            {
                // 벽 정보 저장
                _currentWall = hit.collider;

                // 벽을 향해 이동 중인지 확인
                float dotProduct = Vector3.Dot(transform.forward, hit.normal);
                if (dotProduct > _rollDirectionDotThreshold && _playerStat.Stamina > 0 && Input.GetAxis("Vertical") > _wallInputThreshold)
                {
                    // 벽 오르기 시작
                    _isWallClimbing = true;
                }
            }
        }
    }
}
