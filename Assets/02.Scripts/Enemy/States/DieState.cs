using System.Collections;
using UnityEngine;

/// <summary>
/// 적의 사망 상태를 구현하는 클래스입니다.
/// </summary>
public class DieState : IEnemyState
{
    private Enemy _enemy;
    private Coroutine _dieCoroutine;

    public DieState(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void Enter()
    {
        _dieCoroutine = _enemy.StartCoroutine(DieCoroutine());
    }

    public void Exit()
    {
        if (_dieCoroutine != null)
        {
            _enemy.StopCoroutine(_dieCoroutine);
            _dieCoroutine = null;
        }
    }

    public void Update()
    {
        // 사망 상태에서는 특별한 업데이트 없음
    }

    public void CheckTransitions()
    {
        // 사망 상태에서는 다른 상태로 전환되지 않음
    }

    private IEnumerator DieCoroutine()
    {
        // 사망 처리 시작
        _enemy.BeginDeath();

        // 사망 지속 시간 동안 대기
        yield return new WaitForSeconds(_enemy.DeathDuration);

        // 사망 처리 완료
        _enemy.CompleteDeath();
    }
}