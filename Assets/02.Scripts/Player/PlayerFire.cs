using UnityEngine;
using System;
using System.Collections;
using UnityEngine.EventSystems;

public class PlayerFire : MonoBehaviour
{
    #region 필드
    [Header("발사 설정")]
    [SerializeField] private GameObject _firePosition;
    [SerializeField] private int _bulletDamage = 10; // 총알 피해량
    [SerializeField] private ParticleSystem _bulletEffectPrefab;
    [SerializeField] private float _bulletEffectDuration = 1.5f; // 총알 이펙트 지속 시간
    [SerializeField] private int _bulletEffectPoolSize = 10;     // 총알 이펙트 풀 초기 크기

    [Header("폭탄 설정")]
    [SerializeField] private GameObject _bombPrefab;
    [SerializeField] private int _initialPoolSize = 5;       // 폭탄 풀 초기 크기

    // 폭탄 충전 이벤트 추가
    public event Action<float, float> OnBombChargeChanged; // (현재 충전량, 최대 충전량)
    public event Action<bool> OnBombChargeStateChanged; // (충전 중 여부)

    // 총알/재장전 이벤트 추가
    public event Action<int, int> OnAmmoChanged; // (현재 총알 수, 최대 총알 수)
    public event Action<bool> OnReloadStateChanged; // (재장전 중 여부)
    public event Action OnReloadCancelled; // 재장전 취소 시
    public event Action<float> OnReloadProgressChanged; // (재장전 진행도 0.0f ~ 1.0f)

    // 총 발사 이벤트 추가
    public event Action OnWeaponFired; // 총이 발사될 때마다 호출

    private PlayerStat _playerStat;
    private bool _isChargingBomb = false;
    private float _currentBombCharge = 0f;
    private bool _isBombPoolInitialized = false;
    private bool _isBulletEffectPoolInitialized = false;

    // 연사 관련 변수 추가
    private float _nextFireTime = 0f;
    private bool _isFiring = false;

    // 재장전 관련 변수
    private bool _isReloading = false;
    private Coroutine _reloadCoroutine;
    #endregion

    #region 프로퍼티
    public int CurrentBombCount => _playerStat.CurrentBombCount;
    public int MaxBombCount => _playerStat.MaxBombCount;
    public float BombChargePercentage => _currentBombCharge / _playerStat.BombMaxChargeTime;
    #endregion

    #region Unity 이벤트 함수
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

        // 총알 초기화
        _playerStat.InitializeAmmo();
        OnAmmoChanged?.Invoke(_playerStat.CurrentAmmo, _playerStat.MaxAmmo);

        // 이벤트 구독
        _playerStat.OnAmmoChanged += HandleAmmoChanged;
        _playerStat.OnReloadingChanged += HandleReloadingChanged;
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (_playerStat != null)
        {
            _playerStat.OnAmmoChanged -= HandleAmmoChanged;
            _playerStat.OnReloadingChanged -= HandleReloadingChanged;
        }
    }

    private void Update()
    {
        HandleBombThrow();
        HandleGunFire();
        HandleReload();
    }
    #endregion

    #region 비공개 메서드
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
        bool isButtonDown = Input.GetMouseButtonDown(0);
        bool isButtonUp = Input.GetMouseButtonUp(0);
        bool isButtonHeld = Input.GetMouseButton(0); // 버튼 누르고 있는지 확인 추가

        // 왼쪽 버튼을 처음 눌렀을 때
        if (isButtonDown)
        {
            // 마우스 포인터가 UI 요소 위에 있는지 확인
            if (EventSystem.current.IsPointerOverGameObject())
            {
                // UI 요소 위에서 클릭된 경우, 발사 로직을 실행하지 않고 함수 종료
                return;
            }

            _isFiring = true;
            // 첫 발사 시도 (발사 속도 체크)
            if (Time.time >= _nextFireTime)
            {
                Fire();
            }
        }

        // 버튼을 계속 누르고 있는 경우 연사 처리
        // isButtonDown 대신 isButtonHeld 사용
        if (_isFiring && isButtonHeld)
        {
            // 현재 시간이 다음 발사 가능 시간을 넘었는지 확인
            if (Time.time >= _nextFireTime)
            {
                Fire();
            }
        }

        // 버튼을 떼면 연사 중지
        if (isButtonUp)
        {
            _isFiring = false;
        }
    }

    /// <summary>
    /// 총알 발사 처리
    /// </summary>
    private void Fire()
    {
        // 재장전 중이거나 총알이 없으면 발사하지 않음
        if (_isReloading)
        {
            return;
        }

        // 총알 사용 시도
        if (!_playerStat.UseAmmo())
        {
            // 총알이 없는 경우 발사하지 않고 리턴
            return;
        }

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

            // IDamageable 인터페이스를 가진 컴포넌트를 찾아서 피해를 줍니다.
            IDamageable damageable = hitInfo.collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                Damage damage = new()
                {
                    Value = _bulletDamage,
                    From = gameObject
                };
                damageable.TakeDamage(damage);
            }
        }

        // 총 발사 이벤트 호출
        OnWeaponFired?.Invoke();
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

    /// <summary>
    /// 재장전 처리
    /// </summary>
    private void HandleReload()
    {
        // R 키를 처음 누를 때 재장전 시작
        if (Input.GetKeyDown(KeyCode.R) && !_isReloading && _playerStat.CurrentAmmo < _playerStat.MaxAmmo)
        {
            StartReload();
        }
        // R 키를 뗐을 때 재장전 취소
        else if (Input.GetKeyUp(KeyCode.R) && _isReloading)
        {
            CancelReload();
        }
    }

    /// <summary>
    /// 재장전 시작
    /// </summary>
    private void StartReload()
    {
        if (_isReloading) return;

        _playerStat.StartReloading();
        _isReloading = true;

        // 현재 진행 중인 재장전 코루틴이 있다면 중지
        if (_reloadCoroutine != null)
        {
            StopCoroutine(_reloadCoroutine);
        }

        // 재장전 코루틴 시작
        _reloadCoroutine = StartCoroutine(ReloadCoroutine());
    }

    /// <summary>
    /// 재장전 코루틴
    /// </summary>
    private IEnumerator ReloadCoroutine()
    {
        float reloadTime = _playerStat.ReloadTime;
        float elapsedTime = 0f;

        // 재장전 시작 시 진행도 0으로 설정
        OnReloadProgressChanged?.Invoke(0f);

        // 재장전 시간 동안 진행도 업데이트
        while (elapsedTime < reloadTime)
        {
            // R 키를 계속 누르고 있는지 확인
            if (!Input.GetKey(KeyCode.R))
            {
                // R 키를 놓았다면 재장전 취소
                CancelReload();
                yield break;
            }

            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / reloadTime);

            // 재장전 진행도 이벤트 발생
            OnReloadProgressChanged?.Invoke(progress);

            yield return null;
        }

        // 재장전 완료 시 진행도 1로 설정
        OnReloadProgressChanged?.Invoke(1f);

        // 재장전 완료
        _playerStat.CompleteReloading();
        _isReloading = false;
        _reloadCoroutine = null;
    }

    /// <summary>
    /// 재장전 취소
    /// </summary>
    private void CancelReload()
    {
        if (!_isReloading) return;

        // 코루틴 중지
        if (_reloadCoroutine != null)
        {
            StopCoroutine(_reloadCoroutine);
            _reloadCoroutine = null;
        }

        // 재장전 취소 처리
        _playerStat.CancelReloading();
        _isReloading = false;

        // 재장전 진행도 초기화 (0으로 설정)
        OnReloadProgressChanged?.Invoke(0f);

        // 재장전 취소 이벤트 발생
        OnReloadCancelled?.Invoke();
    }

    /// <summary>
    /// 총알 개수 변경 이벤트 핸들러
    /// </summary>
    private void HandleAmmoChanged(int currentAmmo, int maxAmmo)
    {
        OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
    }

    /// <summary>
    /// 재장전 상태 변경 이벤트 핸들러
    /// </summary>
    private void HandleReloadingChanged(bool isReloading)
    {
        OnReloadStateChanged?.Invoke(isReloading);
    }
    #endregion

    #region 공개 메서드
    // 폭탄 개수 추가 메서드 (나중에 아이템 획득 시 사용 가능)
    public void AddBomb(int amount = 1)
    {
        _playerStat.AddBomb(amount);
        Debug.Log($"폭탄 획득: 현재 개수 {_playerStat.CurrentBombCount}/{_playerStat.MaxBombCount}");
    }
    #endregion
}
