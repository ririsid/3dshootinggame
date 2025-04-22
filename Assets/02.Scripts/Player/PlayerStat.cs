using System;
using UnityEngine;

public class PlayerStat : MonoBehaviour
{
    // 이벤트 정의: 스태미너 변경 시 발생
    public event Action<float, float> OnStaminaChanged; // (현재 스태미너, 최대 스태미너)
    // 폭탄 관련 이벤트 추가
    public event Action<int, int> OnBombCountChanged; // (현재 폭탄 개수, 최대 폭탄 개수)

    [Header("스탯 데이터")]
    [SerializeField] private PlayerStatSO _playerStatData;

    private float _stamina;
    private int _currentBombCount;

    private void Awake()
    {
        if (_playerStatData == null)
        {
            Debug.LogError("PlayerStatSO가 할당되지 않았습니다!", this);
            return;
        }

        // 초기 스태미너 값 설정 및 이벤트 발생
        _stamina = _playerStatData.maxStamina;
        OnStaminaChanged?.Invoke(_stamina, MaxStamina);

        // 초기 폭탄 개수 설정 및 이벤트 발생
        _currentBombCount = _playerStatData.maxBombCount;
        OnBombCountChanged?.Invoke(_currentBombCount, MaxBombCount);
    }

    public void RecoverStamina()
    {
        if (Stamina >= MaxStamina) return; // 최대 스태미너에 도달하면 회복하지 않음
        Stamina += _playerStatData.staminaRecoveryRate * Time.deltaTime;
    }

    public bool UseStamina(float amount)
    {
        if (Stamina >= 0)
        {
            Stamina -= amount;
            return true;
        }
        return false;
    }

    public float GetWallClimbStaminaCost(float verticalInput, float horizontalInput, float wallInputThreshold)
    {
        // 입력에 따라 다른 스태미너 소모율 계산
        if (verticalInput > wallInputThreshold)
        {
            // 상승 (최대 소모율)
            return _playerStatData.wallClimbStaminaUseRate * Time.deltaTime;
        }
        else if (verticalInput < -wallInputThreshold)
        {
            // 하강 (오르기보다 적게)
            return _playerStatData.wallClimbStaminaUseRate * _playerStatData.wallDescendStaminaFactor * Time.deltaTime;
        }
        else if (Mathf.Abs(horizontalInput) > wallInputThreshold)
        {
            // 좌우 이동 (오르기보다 적게)
            return _playerStatData.wallClimbStaminaUseRate * _playerStatData.wallStrafeStaminaFactor * Time.deltaTime;
        }
        else
        {
            // 입력 없음 (가장 적게)
            return _playerStatData.wallClimbStaminaUseRate * _playerStatData.wallIdleStaminaFactor * Time.deltaTime;
        }
    }

    #region 프로퍼티
    public float Stamina
    {
        get => _stamina;
        private set
        {
            float oldValue = _stamina;
            _stamina = Mathf.Clamp(value, 0f, MaxStamina); // 스태미너를 0과 최대값 사이로 제한
            if (oldValue != _stamina)
            {
                // 값이 변경되었을 때만 이벤트 발생
                OnStaminaChanged?.Invoke(_stamina, MaxStamina);
            }
        }
    }

    // 스태미너 관련 프로퍼티
    public float MaxStamina => _playerStatData.maxStamina;
    public float StaminaUseRate => _playerStatData.staminaUseRate;
    public float RollStaminaCost => _playerStatData.rollStaminaCost;

    // 이동 관련 프로퍼티
    public float WalkSpeed => _playerStatData.walkSpeed;
    public float RunSpeed => _playerStatData.runSpeed;
    public float MoveInputThreshold => _playerStatData.moveInputThreshold;
    public float DiagonalMovementNormalizeThreshold => _playerStatData.diagonalMovementNormalizeThreshold;

    // 점프 관련 프로퍼티
    public float JumpPower => _playerStatData.jumpPower;
    public int MaxJumpCount => _playerStatData.maxJumpCount;

    // 구르기 관련 프로퍼티
    public float RollSpeed => _playerStatData.rollSpeed;
    public float RollDuration => _playerStatData.rollDuration;
    public float RollCooldown => _playerStatData.rollCooldown;
    public float RollRotationSpeed => _playerStatData.rollRotationSpeed;
    public float RollYAxisClearValue => _playerStatData.rollYAxisClearValue;
    public float RollDirectionDotThreshold => _playerStatData.rollDirectionDotThreshold;

    // 벽 오르기 관련 프로퍼티
    public float WallClimbSpeed => _playerStatData.wallClimbSpeed;
    public float WallDescendSpeed => _playerStatData.wallDescendSpeed;
    public float WallStrafeSpeed => _playerStatData.wallStrafeSpeed;
    public float MinWallNormalY => _playerStatData.minWallNormalY;
    public float WallInputThreshold => _playerStatData.wallInputThreshold;
    public float WallMaxDistance => _playerStatData.wallMaxDistance;

    // 폭탄 관련 프로퍼티
    public int CurrentBombCount
    {
        get => _currentBombCount;
        private set
        {
            if (_currentBombCount != value)
            {
                _currentBombCount = Mathf.Clamp(value, 0, MaxBombCount);
                OnBombCountChanged?.Invoke(_currentBombCount, MaxBombCount);
            }
        }
    }
    public int MaxBombCount => _playerStatData.maxBombCount;
    public float BombThrowPower => _playerStatData.bombThrowPower;

    // 필요한 경우 데이터 교체용 메서드
    public void SetPlayerStatData(PlayerStatSO newData)
    {
        if (newData != null)
        {
            _playerStatData = newData;
            // 스태미너를 새 최대값으로 재설정할 경우
            // Stamina = MaxStamina;
        }
    }
    #endregion

    #region 폭탄 관련 메서드
    public bool UseBomb()
    {
        if (CurrentBombCount > 0)
        {
            CurrentBombCount--;
            return true;
        }
        return false;
    }

    public void AddBomb(int amount = 1)
    {
        CurrentBombCount += amount;
    }
    #endregion
}