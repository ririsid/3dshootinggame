using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 스포너를 구현하는 클래스입니다.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    #region 스포너 설정
    [Header("스포너 설정")]
    [Tooltip("생성할 적 프리팹")]
    [SerializeField] private GameObject _enemyPrefab;

    [Tooltip("적이 생성될 위치들")]
    [SerializeField] private Transform[] _spawnPoints; // 여러 스폰 지점 지정 가능

    [Tooltip("스폰 지점이 없을 경우 현재 위치에서의 스폰 범위")]
    [SerializeField] private float _spawnRadius = 10f;

    [Tooltip("생성 간격 (초)")]
    [SerializeField] private float _spawnInterval = 5f;

    [Tooltip("최대 생성 수량 (0 = 무제한)")]
    [SerializeField] private int _maxSpawnCount = 10;

    [Tooltip("스포너 활성화 시 자동 시작")]
    [SerializeField] private bool _autoStart = true;

    [Header("UI 설정")]
    [Tooltip("체력바 스포너 참조")]
    [SerializeField] private EnemyHealthBarSpawner _healthBarSpawner;
    #endregion

    #region 디버그 옵션
    [Header("디버그 옵션")]
    [Tooltip("디버그 스피어 표시 여부")]
    [SerializeField] private bool _showDebugSphere = true;

    [Tooltip("디버그 스피어 색상")]
    [SerializeField] private Color _debugColor = new Color(1f, 0f, 0f, 0.3f);
    #endregion

    #region 비공개 변수
    /// <summary>
    /// 스폰 진행 중 여부
    /// </summary>
    private bool _isSpawning = false;

    /// <summary>
    /// 스폰 코루틴 참조
    /// </summary>
    private Coroutine _spawnCoroutine;

    /// <summary>
    /// 현재 스폰된 적의 수
    /// </summary>
    private int _currentSpawnCount = 0;

    /// <summary>
    /// 스폰된 적 오브젝트 목록
    /// </summary>
    private List<GameObject> _spawnedEnemies = new List<GameObject>();
    #endregion

    #region Unity 이벤트 함수
    private void Awake()
    {
        // 체력바 스포너가 할당되지 않았을 경우에만 자동으로 찾기
        if (_healthBarSpawner == null)
        {
            _healthBarSpawner = FindObjectsByType<EnemyHealthBarSpawner>(FindObjectsSortMode.None)[0];
        }
    }

    private void Start()
    {
        // 프리팹이 설정되어 있는지 확인
        if (_enemyPrefab == null)
        {
            Debug.LogError("적 프리팹이 설정되지 않았습니다!");
            return;
        }

        // ObjectPoolManager에 적 프리팹 등록
        ObjectPoolManager.Instance.InitializePool(_enemyPrefab, 5);

        // 자동 시작 설정이 켜져있다면 스폰 시작
        if (_autoStart)
        {
            StartSpawning();
        }
    }

    private void OnDestroy()
    {
        StopSpawning();
    }

    private void OnDrawGizmosSelected()
    {
        if (!_showDebugSphere) return;

        Gizmos.color = _debugColor;

        // 스폰 지점이 있다면 각 지점의 범위 표시
        if (_spawnPoints != null && _spawnPoints.Length > 0)
        {
            foreach (Transform point in _spawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, _spawnRadius);
                }
            }
        }
        else
        {
            // 스폰 지점이 없다면 현재 위치 기준으로 표시
            Gizmos.DrawWireSphere(transform.position, _spawnRadius);
        }
    }
    #endregion

    #region 공개 메서드
    /// <summary>
    /// 적 스폰을 시작합니다.
    /// </summary>
    public void StartSpawning()
    {
        if (_isSpawning) return;

        _isSpawning = true;
        _spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    /// <summary>
    /// 적 스폰을 중지합니다.
    /// </summary>
    public void StopSpawning()
    {
        if (!_isSpawning) return;

        _isSpawning = false;
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }
    }

    /// <summary>
    /// 적을 즉시 스폰합니다.
    /// </summary>
    /// <returns>생성된 적 오브젝트, 생성 실패 시 null</returns>
    public GameObject SpawnEnemy()
    {
        if (_maxSpawnCount > 0 && _currentSpawnCount >= _maxSpawnCount)
        {
            return null;
        }

        // 오브젝트 풀에서 적 가져오기
        GameObject enemy = ObjectPoolManager.Instance.GetFromPool(_enemyPrefab.name);
        if (enemy == null)
        {
            Debug.LogError($"적 '{_enemyPrefab.name}' 스폰 실패!");
            return null;
        }

        // 스폰 위치 결정
        Vector3 spawnPosition = GetSpawnPosition();

        // 적 위치 및 회전 설정
        enemy.transform.position = spawnPosition;
        enemy.transform.rotation = Quaternion.identity;

        // 생성된 적 정보 업데이트
        _currentSpawnCount++;
        _spawnedEnemies.Add(enemy);

        // Enemy 컴포넌트가 있다면 사망 이벤트 연결
        Enemy enemyComponent = enemy.GetComponent<Enemy>();
        if (enemyComponent != null)
        {
            // 기존에 연결된 이벤트가 있다면 제거 후 새로 연결
            enemyComponent.OnDeath -= OnEnemyDied;
            enemyComponent.OnDeath += OnEnemyDied;

            // 체력바 캔버스 추가
            AddHealthBarToEnemy(enemyComponent);
        }

        return enemy;
    }

    /// <summary>
    /// 모든 스폰된 적을 제거합니다.
    /// </summary>
    public void ClearAllEnemies()
    {
        foreach (GameObject enemy in _spawnedEnemies)
        {
            if (enemy != null && enemy.activeInHierarchy)
            {
                ObjectPoolManager.Instance.ReturnToPool(enemy);
            }
        }

        _spawnedEnemies.Clear();
        _currentSpawnCount = 0;
    }
    #endregion

    #region 비공개 메서드
    /// <summary>
    /// 적 스폰 코루틴
    /// </summary>
    /// <returns>코루틴 IEnumerator</returns>
    private IEnumerator SpawnRoutine()
    {
        while (_isSpawning)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(_spawnInterval);
        }
    }

    /// <summary>
    /// 스폰 위치를 결정합니다.
    /// </summary>
    /// <returns>결정된 스폰 위치</returns>
    private Vector3 GetSpawnPosition()
    {
        Vector3 basePosition;

        // 스폰 지점이 있으면 랜덤하게 선택
        if (_spawnPoints != null && _spawnPoints.Length > 0)
        {
            basePosition = _spawnPoints[Random.Range(0, _spawnPoints.Length)].position;
        }
        else
        {
            // 스폰 지점이 없으면 현재 위치 사용
            basePosition = transform.position;
        }

        // NavMeshUtility를 사용하여 NavMesh 상의 랜덤 위치 가져오기
        return NavMeshUtility.GetRandomPointInNavMesh(basePosition, _spawnRadius);
    }

    /// <summary>
    /// 적에게 체력바 캔버스를 추가합니다.
    /// </summary>
    /// <param name="enemy">체력바를 추가할 적 객체</param>
    private void AddHealthBarToEnemy(Enemy enemy)
    {
        if (enemy == null) return;

        // 체력바 스포너가 없으면 경고
        if (_healthBarSpawner == null)
        {
            Debug.LogWarning("체력바 스포너가 할당되지 않았습니다.");
            return;
        }

        // 기존 체력바가 있는지 타입으로 확인
        UI_EnemyHealthBar existingHealthBar = enemy.GetComponentInChildren<UI_EnemyHealthBar>();
        if (existingHealthBar != null) return;

        // 체력바 스포너를 사용하여 체력바 생성
        _healthBarSpawner.SpawnHealthBarForEnemy(enemy);
    }

    /// <summary>
    /// 적이 사망했을 때 호출되는 메서드
    /// </summary>
    /// <param name="enemy">사망한 적 오브젝트</param>
    private void OnEnemyDied(GameObject enemy)
    {
        if (_spawnedEnemies.Contains(enemy))
        {
            _spawnedEnemies.Remove(enemy);
            _currentSpawnCount--;
        }
    }
    #endregion
}