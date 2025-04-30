using System;
using UnityEngine;

public class PlayerStat : MonoBehaviour
{
    // 스태미너 관련 이벤트
    public event Action<float, float> OnStaminaChanged; // (현재 스태미너, 최대 스태미너)

    // 무기 관련 이벤트
    public event Action<int, int> OnBombCountChanged; // (현재 폭탄 개수, 최대 폭탄 개수)
    public event Action<int, int> OnAmmoChanged; // (현재 총알 개수, 최대 총알 개수)
    public event Action<bool> OnReloadingChanged; // 재장전 중 여부

    // 체력 관련 이벤트
    public event Action<int, int> OnHealthChanged; // (현재 체력, 최대 체력)
    public event Action OnPlayerDied; // 플레이어 사망 시 발생
    public event Action<Damage> OnPlayerHit; // 플레이어 피격 시 발생

    [Header("스탯 데이터")]
    [SerializeField] private PlayerStatSO _playerStatData;

    // 스태미너 관련 변수
    private float _stamina;

    // 무기 관련 변수
    private int _currentBombCount;
    private int _currentAmmo;
    private bool _isReloading;

    // 체력 관련 변수
    private int _currentHealth;
    private bool _isDead;
    private bool _isInvincible;
    private float _lastDamageTime = -999f;

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

        // 초기 체력 설정 및 이벤트 발생
        _currentHealth = _playerStatData.maxHealth;
        OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
    }

    private void Update()
    {
        // 자동 체력 회복 처리
        HandleHealthRecovery();
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

    // 발사 관련 프로퍼티
    public float FireRate => _playerStatData.fireRate;

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
    public float BombThrowMaxPower => _playerStatData.bombThrowMaxPower;
    public float BombChargingSpeed => _playerStatData.bombChargingSpeed;
    public float BombMaxChargeTime => _playerStatData.bombMaxChargeTime;

    // 총알 관련 프로퍼티
    public int CurrentAmmo
    {
        get => _currentAmmo;
        private set
        {
            if (_currentAmmo != value)
            {
                _currentAmmo = Mathf.Clamp(value, 0, MaxAmmo);
                OnAmmoChanged?.Invoke(_currentAmmo, MaxAmmo);
            }
        }
    }
    public int MaxAmmo => _playerStatData.maxAmmo;
    public float ReloadTime => _playerStatData.reloadTime;
    public bool IsReloading
    {
        get => _isReloading;
        private set
        {
            if (_isReloading != value)
            {
                _isReloading = value;
                OnReloadingChanged?.Invoke(_isReloading);
            }
        }
    }

    // 체력 관련 프로퍼티
    public int CurrentHealth
    {
        get => _currentHealth;
        private set
        {
            int oldHealth = _currentHealth;
            _currentHealth = Mathf.Clamp(value, 0, MaxHealth);

            if (oldHealth != _currentHealth)
            {
                OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
            }
        }
    }

    public int MaxHealth => _playerStatData.maxHealth;

    public bool IsDead => _isDead;

    public bool IsInvincible => _isInvincible;

    public float InvincibilityDuration => _playerStatData.invincibilityDuration;

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

    #region 총알 관련 메서드
    public void InitializeAmmo()
    {
        CurrentAmmo = MaxAmmo;
    }

    public bool UseAmmo()
    {
        if (IsReloading) return false;

        if (CurrentAmmo > 0)
        {
            CurrentAmmo--;
            return true;
        }

        return false;
    }

    public void StartReloading()
    {
        if (IsReloading || CurrentAmmo >= MaxAmmo) return;

        IsReloading = true;
    }

    public void CompleteReloading()
    {
        if (!IsReloading) return;

        CurrentAmmo = MaxAmmo;
        IsReloading = false;
    }

    public void CancelReloading()
    {
        if (!IsReloading) return;

        IsReloading = false;
    }
    #endregion

    #region 체력 관련 메서드
    /// <summary>
    /// 데미지를 처리합니다.
    /// </summary>
    /// <param name="damage">적용할 피해의 정보</param>
    public void TakeDamage(Damage damage)
    {
        // 사망 상태거나 무적 상태면 데미지를 받지 않음
        if (IsDead || IsInvincible) return;

        // 피격 시간 기록
        _lastDamageTime = Time.time;

        // 데미지 적용
        CurrentHealth -= damage.Amount;

        // 피격 이벤트 발생
        OnPlayerHit?.Invoke(damage);

        // 체력이 0 이하면 사망 처리
        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 체력을 회복합니다.
    /// </summary>
    /// <param name="amount">회복할 체력량</param>
    public void HealHealth(int amount)
    {
        if (IsDead) return;
        CurrentHealth += amount;
    }

    /// <summary>
    /// 플레이어가 사망할 때 호출되는 메서드
    /// </summary>
    private void Die()
    {
        if (_isDead) return;
        _isDead = true;
        OnPlayerDied?.Invoke();
    }

    /// <summary>
    /// 자동 체력 회복을 처리합니다
    /// </summary>
    private void HandleHealthRecovery()
    {
        // 최대 체력이면 회복하지 않음
        if (_currentHealth >= MaxHealth || IsDead)
        {
            // 최대 체력에 도달했을 때 회복 상태 리셋
            // 이렇게 하면 다음에 피격 시 제대로 타이머가 작동함
            if (_currentHealth >= MaxHealth)
            {
                _lastDamageTime = Time.time;
            }
            return;
        }

        // 마지막 피격 후 일정 시간이 지났을 때만 체력 회복
        if (Time.time - _lastDamageTime < _playerStatData.healthRecoveryDelay)
            return;

        _lastDamageTime = Time.time; // 회복 시작 시 타이머 리셋

        // 체력 회복
        int healAmount = Mathf.CeilToInt(_playerStatData.healthRecoveryRate);
        CurrentHealth += healAmount;
    }

    /// <summary>
    /// 무적 상태를 설정합니다.
    /// </summary>
    /// <param name="isInvincible">무적 여부</param>
    public void SetInvincible(bool isInvincible)
    {
        _isInvincible = isInvincible;
    }
    #endregion
}