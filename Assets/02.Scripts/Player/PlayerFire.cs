using UnityEngine;
using System;
using System.Collections;

public class PlayerFire : MonoBehaviour
{
    #region Fields
    [Header("발사 설정")]
    [SerializeField] private GameObject _firePosition;
    [SerializeField] private ParticleSystem _bulletEffectPrefab;
    [SerializeField] private float _bulletEffectDuration = 1.5f; // 총알 이펙트 지속 시간
    [SerializeField] private int _bulletEffectPoolSize = 10;     // 총알 이펙트 풀 초기 크기

    [Header("폭탄 설정")]
    [SerializeField] private GameObject _bombPrefab;
    [SerializeField] private int _initialPoolSize = 5;       // 폭탄 풀 초기 크기

    // 폭탄 충전 이벤트 추가
    public event Action<float, float> OnBombChargeChanged; // (현재 충전량, 최대 충전량)
    public event Action<bool> OnBombChargeStateChanged; // (충전 중 여부)

    private PlayerStat _playerStat;
    private bool _isChargingBomb = false;
    private float _currentBombCharge = 0f;
    private bool _isBombPoolInitialized = false;
    private bool _isBulletEffectPoolInitialized = false;

    // 연사 관련 변수 추가
    private float _nextFireTime = 0f;
    private bool _isFiring = false;
    #endregion

    #region Properties
    public int CurrentBombCount => _playerStat.CurrentBombCount;
    public int MaxBombCount => _playerStat.MaxBombCount;
    public float BombChargePercentage => _currentBombCharge / _playerStat.BombMaxChargeTime;
    #endregion

    #region Unity Event Functions
    private void Awake()
    {
        _playerStat = GetComponent<PlayerStat>();
        if (_playerStat == null)
        {
            Debug.LogError("PlayerStat 컴포넌트를 찾을 수 없습니다!", this);
        }
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        // 오브젝트 풀 초기화
        InitializeBombPool();
        InitializeBulletEffectPool();
    }

    private void Update()
    {
        HandleBombThrow();
        HandleGunFire();
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// 폭탄 오브젝트 풀 초기화
    /// </summary>
    private void InitializeBombPool()
    {
        if (_bombPrefab != null && !_isBombPoolInitialized)
        {
            ObjectPoolManager.Instance.InitializePool(_bombPrefab, _initialPoolSize);
            _isBombPoolInitialized = true;
            Debug.Log($"폭탄 오브젝트 풀 초기화 완료 (크기: {_initialPoolSize})");
        }
    }

    /// <summary>
    /// 총알 이펙트 오브젝트 풀 초기화
    /// </summary>
    private void InitializeBulletEffectPool()
    {
        if (_bulletEffectPrefab != null && !_isBulletEffectPoolInitialized)
        {
            // ParticleSystem을 GameObject로 변환하여 풀 초기화
            GameObject bulletEffectObj = _bulletEffectPrefab.gameObject;
            ObjectPoolManager.Instance.InitializePool(bulletEffectObj, _bulletEffectPoolSize);
            _isBulletEffectPoolInitialized = true;
            Debug.Log($"총알 이펙트 오브젝트 풀 초기화 완료 (크기: {_bulletEffectPoolSize})");
        }
    }

    private void HandleBombThrow()
    {
        // 오른쪽 버튼을 누르고 있는지 체크
        if (Input.GetMouseButtonDown(1) && _playerStat.CurrentBombCount > 0)
        {
            _isChargingBomb = true;
            _currentBombCharge = 0f;
            // 충전 시작 이벤트 발생
            OnBombChargeStateChanged?.Invoke(true);
            OnBombChargeChanged?.Invoke(_currentBombCharge, _playerStat.BombMaxChargeTime);
        }

        // 충전 중 파워 증가
        if (_isChargingBomb)
        {
            float prevCharge = _currentBombCharge;
            _currentBombCharge += Time.deltaTime;
            _currentBombCharge = Mathf.Min(_currentBombCharge, _playerStat.BombMaxChargeTime);

            // 충전량이 변경되었을 때만 이벤트 발생
            if (prevCharge != _currentBombCharge)
            {
                OnBombChargeChanged?.Invoke(_currentBombCharge, _playerStat.BombMaxChargeTime);
            }
        }

        // 오른쪽 버튼을 떼었을 때 폭탄 던지기
        if (Input.GetMouseButtonUp(1) && _isChargingBomb)
        {
            _isChargingBomb = false;

            // 충전 종료 이벤트 발생
            OnBombChargeStateChanged?.Invoke(false);

            // PlayerStat을 통해 폭탄 사용 시도
            if (_playerStat.UseBomb())
            {
                // 충전 정도에 따른 폭탄 던지기 파워 계산
                float chargePercentage = _currentBombCharge / _playerStat.BombMaxChargeTime;
                float throwPower = Mathf.Lerp(_playerStat.BombThrowPower, _playerStat.BombThrowMaxPower, chargePercentage);

                // 오브젝트 풀에서 폭탄 가져오기
                GameObject bomb = ObjectPoolManager.Instance.GetFromPool(_bombPrefab.name);
                if (bomb != null)
                {
                    bomb.transform.position = _firePosition.transform.position;

                    // 생성된 수류탄을 카메라 방향으로 물리적인 힘 가하기
                    Rigidbody bombRigidbody = bomb.GetComponent<Rigidbody>();
                    bombRigidbody.AddForce(Camera.main.transform.forward * throwPower, ForceMode.Impulse);
                    bombRigidbody.AddTorque(Vector3.one);

                    Debug.Log($"폭탄 사용: 파워 {throwPower:F1}, 남은 개수 {_playerStat.CurrentBombCount}/{_playerStat.MaxBombCount}");
                }
            }

            // 충전 초기화
            _currentBombCharge = 0f;
            OnBombChargeChanged?.Invoke(_currentBombCharge, _playerStat.BombMaxChargeTime);
        }
    }

    private void HandleGunFire()
    {
        // 왼쪽 버튼 입력 받기
        if (Input.GetMouseButtonDown(0))
        {
            _isFiring = true;
            Fire();
        }

        // 버튼을 계속 누르고 있는 경우 연사 처리
        if (Input.GetMouseButton(0) && _isFiring)
        {
            // 현재 시간이 다음 발사 가능 시간을 넘었는지 확인
            if (Time.time >= _nextFireTime)
            {
                Fire();
            }
        }

        // 버튼을 떼면 연사 중지
        if (Input.GetMouseButtonUp(0))
        {
            _isFiring = false;
        }
    }

    /// <summary>
    /// 총알 발사 처리
    /// </summary>
    private void Fire()
    {
        // 다음 발사 가능 시간 설정
        _nextFireTime = Time.time + _playerStat.FireRate;

        // 레이를 생성하고 발사 위치와 진행 방향을 설정
        Ray ray = new Ray(_firePosition.transform.position, Camera.main.transform.forward);

        // 레이와 부딛힌 물체의 정보를 저장할 변수를 생성
        RaycastHit hitInfo = new RaycastHit();

        // 레이를 발사한 다음,
        bool isHit = Physics.Raycast(ray, out hitInfo);
        if (isHit) // 데이터가 있다면(부딛혔다면)
        {
            // 피격 이펙트 생성(표시) - 오브젝트 풀링 사용
            CreateBulletHitEffect(hitInfo.point, hitInfo.normal);
        }
    }

    /// <summary>
    /// 총알 피격 이펙트 생성
    /// </summary>
    private void CreateBulletHitEffect(Vector3 position, Vector3 normal)
    {
        if (_bulletEffectPrefab != null)
        {
            // 오브젝트 풀에서 이펙트 가져오기
            GameObject effectObj = ObjectPoolManager.Instance.GetFromPool(_bulletEffectPrefab.name);

            if (effectObj == null)
            {
                // 풀에서 가져오기 실패 시 직접 생성
                ParticleSystem hitEffect = Instantiate(_bulletEffectPrefab);
                hitEffect.transform.position = position;
                hitEffect.transform.forward = normal;
                hitEffect.Play();
                return;
            }

            // 풀에서 가져온 이펙트 설정
            effectObj.transform.position = position;
            effectObj.transform.forward = normal;
            effectObj.SetActive(true);

            // 파티클 시스템 재생
            ParticleSystem particleSystem = effectObj.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                particleSystem.Play();
            }

            // 일정 시간 후 풀로 반환
            StartCoroutine(ReturnEffectToPool(effectObj, _bulletEffectDuration));
        }
    }

    /// <summary>
    /// 지정된 시간 후 이펙트를 풀에 반환
    /// </summary>
    private IEnumerator ReturnEffectToPool(GameObject effect, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (effect != null && effect.activeSelf)
        {
            ObjectPoolManager.Instance.ReturnToPool(effect);
        }
    }
    #endregion

    #region Public Methods
    // 폭탄 개수 추가 메서드 (나중에 아이템 획득 시 사용 가능)
    public void AddBomb(int amount = 1)
    {
        _playerStat.AddBomb(amount);
        Debug.Log($"폭탄 획득: 현재 개수 {_playerStat.CurrentBombCount}/{_playerStat.MaxBombCount}");
    }
    #endregion
}
