using UnityEngine;

public class PlayerFire : MonoBehaviour
{
    #region Fields
    [Header("발사 설정")]
    [SerializeField] private GameObject _firePosition;
    [SerializeField] private ParticleSystem _bulletEffectPrefab;

    [Header("폭탄 설정")]
    [SerializeField] private GameObject _bombPrefab;

    private PlayerStat _playerStat;
    #endregion

    #region Properties
    public int CurrentBombCount => _playerStat.CurrentBombCount;
    public int MaxBombCount => _playerStat.MaxBombCount;
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
        // 오른쪽 버튼 입력 받기
        if (Input.GetMouseButtonDown(1))
        {
            // PlayerStat을 통해 폭탄 사용 시도
            if (_playerStat.UseBomb())
            {
                // 발사 위치에 수류탄 생성하기
                GameObject bomb = Instantiate(_bombPrefab);
                bomb.transform.position = _firePosition.transform.position;

                // 생성된 수류탄을 카메라 방향으로 물리적인 힘 가하기
                Rigidbody bombRigidbody = bomb.GetComponent<Rigidbody>();
                bombRigidbody.AddForce(Camera.main.transform.forward * _playerStat.BombThrowPower, ForceMode.Impulse);
                bombRigidbody.AddTorque(Vector3.one);

                Debug.Log($"폭탄 사용: 남은 개수 {_playerStat.CurrentBombCount}/{_playerStat.MaxBombCount}");
            }
            else
            {
                Debug.Log("폭탄을 모두 사용했습니다!");
            }
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
