using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 내 여러 적 스포너를 관리하는 매니저 클래스입니다.
/// </summary>
public class EnemySpawnerManager : Singleton<EnemySpawnerManager>
{
    [Header("스포너 설정")]
    /// <summary>
    /// 게임 시작 시 모든 스포너를 자동으로 활성화할지 여부
    /// </summary>
    [Tooltip("게임 시작 시 모든 스포너를 자동으로 활성화합니다")]
    [SerializeField] private bool _activateAllOnStart = true;

    /// <summary>
    /// 등록된 모든 스포너 목록
    /// </summary>
    private List<EnemySpawner> _spawners = new List<EnemySpawner>();

    #region Unity 이벤트 함수
    /// <summary>
    /// 초기화 작업을 수행합니다.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
    }

    /// <summary>
    /// 게임 시작 시 스포너를 찾고 등록합니다.
    /// </summary>
    private void Start()
    {
        // 씬에 있는 모든 스포너 찾기
        EnemySpawner[] foundSpawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
        foreach (EnemySpawner spawner in foundSpawners)
        {
            RegisterSpawner(spawner);
        }

        if (_activateAllOnStart)
        {
            ActivateAllSpawners();
        }
    }
    #endregion

    #region 공개 메서드
    /// <summary>
    /// 스포너를 등록합니다.
    /// </summary>
    /// <param name="spawner">등록할 스포너</param>
    public void RegisterSpawner(EnemySpawner spawner)
    {
        if (!_spawners.Contains(spawner))
        {
            _spawners.Add(spawner);
        }
    }

    /// <summary>
    /// 스포너 등록을 해제합니다.
    /// </summary>
    /// <param name="spawner">등록 해제할 스포너</param>
    public void UnregisterSpawner(EnemySpawner spawner)
    {
        if (_spawners.Contains(spawner))
        {
            _spawners.Remove(spawner);
        }
    }

    /// <summary>
    /// 모든 스포너를 활성화합니다.
    /// </summary>
    public void ActivateAllSpawners()
    {
        foreach (EnemySpawner spawner in _spawners)
        {
            spawner.StartSpawning();
        }
    }

    /// <summary>
    /// 모든 스포너를 비활성화합니다.
    /// </summary>
    public void DeactivateAllSpawners()
    {
        foreach (EnemySpawner spawner in _spawners)
        {
            spawner.StopSpawning();
        }
    }

    /// <summary>
    /// 모든 적을 제거합니다.
    /// </summary>
    public void ClearAllEnemies()
    {
        foreach (EnemySpawner spawner in _spawners)
        {
            spawner.ClearAllEnemies();
        }
    }
    #endregion
}