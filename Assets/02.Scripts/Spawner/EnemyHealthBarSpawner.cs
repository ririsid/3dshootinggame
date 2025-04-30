using UnityEngine;

/// <summary>
/// 게임 시작 시 모든 적에게 체력 바를 추가하는 스크립트입니다.
/// </summary>
public class EnemyHealthBarSpawner : MonoBehaviour
{
    /// <summary>
    /// 적에게 추가할 체력 바 프리팹입니다.
    /// </summary>
    [SerializeField] private GameObject _healthBarPrefab;

    /// <summary>
    /// 게임 시작 시 자동으로 모든 적에게 체력 바를 추가할지 여부입니다.
    /// </summary>
    [SerializeField] private bool _autoSpawnOnStart = true;

    #region Unity 이벤트 함수
    /// <summary>
    /// 게임 시작 시 초기화를 수행합니다.
    /// </summary>
    private void Start()
    {
        if (_autoSpawnOnStart)
        {
            SpawnHealthBarsForAllEnemies();
        }
    }
    #endregion

    #region 공개 메서드
    /// <summary>
    /// 모든 적에게 체력 바를 추가합니다.
    /// </summary>
    [ContextMenu("모든 적에게 체력 바 추가")]
    public void SpawnHealthBarsForAllEnemies()
    {
        if (_healthBarPrefab == null)
        {
            Debug.LogError("체력 바 프리팹이 설정되지 않았습니다!");
            return;
        }

        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        int count = 0;

        foreach (Enemy enemy in enemies)
        {
            if (enemy.GetComponentInChildren<UI_EnemyHealthBar>() == null)
            {
                GameObject healthBar = Instantiate(_healthBarPrefab, enemy.transform);

                // 참조 설정
                UI_EnemyHealthBar healthBarComponent = healthBar.GetComponent<UI_EnemyHealthBar>();
                if (healthBarComponent != null)
                {
                    // 개발 빌드에서만 로그 출력
                    if (Debug.isDebugBuild)
                    {
                        Debug.Log($"체력 바를 {enemy.name}에게 추가했습니다.");
                    }
                    count++;
                }
            }
        }

        // 개발 빌드에서만 로그 출력
        if (Debug.isDebugBuild)
        {
            Debug.Log($"{count}개의 적에게 체력 바를 추가했습니다.");
        }
    }

    /// <summary>
    /// 특정 적에게 체력 바를 추가합니다.
    /// </summary>
    /// <param name="enemy">체력 바를 추가할 적 객체</param>
    public void SpawnHealthBarForEnemy(Enemy enemy)
    {
        if (_healthBarPrefab == null || enemy == null)
            return;

        if (enemy.GetComponentInChildren<UI_EnemyHealthBar>() == null)
        {
            GameObject healthBar = Instantiate(_healthBarPrefab, enemy.transform);

            // 개발 빌드에서만 로그 출력
            if (Debug.isDebugBuild)
            {
                Debug.Log($"체력 바를 {enemy.name}에게 추가했습니다.");
            }
        }
    }
    #endregion
}