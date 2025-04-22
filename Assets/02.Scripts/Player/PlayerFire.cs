using UnityEngine;
using System;

public class PlayerFire : MonoBehaviour
{
    #region Fields
    [Header("발사 설정")]
    [SerializeField] private GameObject _firePosition;
    [SerializeField] private ParticleSystem _bulletEffectPrefab;

    [Header("폭탄 설정")]
    [SerializeField] private GameObject _bombPrefab;
    [SerializeField] private Transform _bombChargeIndicator;  // 충전 상태를 보여줄 UI 요소

    // 폭탄 충전 이벤트 추가
    public event Action<float, float> OnBombChargeChanged; // (현재 충전량, 최대 충전량)
    public event Action<bool> OnBombChargeStateChanged; // (충전 중 여부)

    private PlayerStat _playerStat;
    private bool _isChargingBomb = false;
    private float _currentBombCharge = 0f;
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

        // 충전 인디케이터가 없으면 빈 게임 오브젝트 생성
        if (_bombChargeIndicator == null)
        {
            GameObject indicator = new GameObject("BombChargeIndicator");
            indicator.transform.SetParent(transform);
            _bombChargeIndicator = indicator.transform;
            _bombChargeIndicator.localScale = Vector3.zero;
        }
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        HandleBombThrow();
        HandleGunFire();
    }
    #endregion

    #region Private Methods
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
            UpdateBombChargeIndicator();
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
            UpdateBombChargeIndicator();
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

                // 발사 위치에 수류탄 생성하기
                GameObject bomb = Instantiate(_bombPrefab);
                bomb.transform.position = _firePosition.transform.position;

                // 생성된 수류탄을 카메라 방향으로 물리적인 힘 가하기
                Rigidbody bombRigidbody = bomb.GetComponent<Rigidbody>();
                bombRigidbody.AddForce(Camera.main.transform.forward * throwPower, ForceMode.Impulse);
                bombRigidbody.AddTorque(Vector3.one);

                Debug.Log($"폭탄 사용: 파워 {throwPower:F1}, 남은 개수 {_playerStat.CurrentBombCount}/{_playerStat.MaxBombCount}");
            }

            // 충전 초기화
            _currentBombCharge = 0f;
            OnBombChargeChanged?.Invoke(_currentBombCharge, _playerStat.BombMaxChargeTime);

            // 충전 인디케이터 초기화
            _bombChargeIndicator.localScale = Vector3.zero;
        }
    }

    private void UpdateBombChargeIndicator()
    {
        if (_bombChargeIndicator != null)
        {
            float chargePercentage = _currentBombCharge / _playerStat.BombMaxChargeTime;
            _bombChargeIndicator.localScale = new Vector3(chargePercentage, chargePercentage, chargePercentage);
        }
    }

    private void HandleGunFire()
    {
        // 왼쪽 버튼 입력 받기
        if (Input.GetMouseButtonDown(0))
        {
            // 레이를 생성하고 발사 위치와 진행 방향을 설정
            Ray ray = new Ray(_firePosition.transform.position, Camera.main.transform.forward);

            // 레이와 부딛힌 물체의 정보를 저장할 변수를 생성
            RaycastHit hitInfo = new RaycastHit();

            // 레이를 발사한 다음,
            bool isHit = Physics.Raycast(ray, out hitInfo);
            if (isHit) // 데이터가 있다면(부딛혔다면)
            {
                // 피격 이펙트 생성(표시)
                ParticleSystem hitEffect = Instantiate(_bulletEffectPrefab);
                hitEffect.transform.position = hitInfo.point;
                hitEffect.transform.forward = hitInfo.normal; // 법선 벡터: 직선에 대하여 수직인 벡터
                hitEffect.Play();
            }
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
