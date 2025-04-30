using System;
using UnityEngine;

/// <summary>
/// 게임 전체에서 사용되는 이벤트를 관리하는 싱글톤 매니저 클래스
/// </summary>
public class EventManager : Singleton<EventManager>
{
    #region 이벤트 정의
    // 플레이어가 피격됐을 때 발생하는 이벤트
    public event Action<Damage> OnPlayerDamaged;

    // 플레이어가 죽었을 때 발생하는 이벤트
    public event Action OnPlayerDeath;

    // 적이 죽었을 때 발생하는 이벤트 (경험치, 처치한 적 정보)
    public event Action<int, Enemy> OnEnemyDeath;
    #endregion

    #region Unity 이벤트 함수
    protected override void Awake()
    {
        base.Awake();
        // 추가적인 초기화가 필요하다면 여기에 작성
    }
    #endregion

    #region 공개 메서드
    /// <summary>
    /// 플레이어 피격 이벤트를 발생시킵니다
    /// </summary>
    /// <param name="damage">적용된 피해 정보</param>
    public void TriggerPlayerDamaged(Damage damage)
    {
        OnPlayerDamaged?.Invoke(damage);
    }

    /// <summary>
    /// 플레이어 사망 이벤트를 발생시킵니다
    /// </summary>
    public void TriggerPlayerDeath()
    {
        OnPlayerDeath?.Invoke();
    }

    /// <summary>
    /// 적 사망 이벤트를 발생시킵니다
    /// </summary>
    /// <param name="experienceValue">제공되는 경험치 값</param>
    /// <param name="enemy">사망한 적 객체</param>
    public void TriggerEnemyDeath(int experienceValue, Enemy enemy)
    {
        OnEnemyDeath?.Invoke(experienceValue, enemy);
    }
    #endregion
}