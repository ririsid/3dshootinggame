using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 내 여러 적 스포너를 관리하는 매니저 클래스입니다.
/// </summary>
public class EnemySpawnerManager : Singleton<EnemySpawnerManager>
{
    #region 필드
    private List<EnemySpawner> _spawners = new List<EnemySpawner>();
    [SerializeField] private bool _activateAllOnStart = true;
    #endregion

    #region Unity 이벤트 함수
    protected override void Awake()
    {
        base.Awake();
    }

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