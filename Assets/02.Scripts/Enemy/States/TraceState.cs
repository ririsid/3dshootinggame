using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 적의 추적 상태를 구현하는 클래스입니다.
/// </summary>
public class TraceState : IEnemyState
{
    private Enemy _enemy;
    private Coroutine _traceCoroutine;
    private float _traceTimer = 0f;
    private float _pathUpdateTimer = 0f;

    public TraceState(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void Enter()
    {
        _traceTimer = 0f;
        _pathUpdateTimer = 0f;

        // NavMeshAgent 속도 설정 (NavMeshUtility 유효성 검사)
        if (_enemy.IsAgentValid())
        {
            _enemy.Agent.speed = _enemy.TraceSpeed;
        }

        _traceCoroutine = _enemy.StartCoroutine(TraceCoroutine());
    }

    public void Exit()
    {
        if (_traceCoroutine != null)
        {
            _enemy.StopCoroutine(_traceCoroutine);
            _traceCoroutine = null;
        }
    }

    public void Update()
    {
        // 코루틴에서 처리하지만, 상태 전환은 더 자주 확인
        CheckTransitions();
    }

    public void CheckTransitions()
    {
        if (_enemy.Player == null) return;

        // NavMeshUtility를 사용하여 실제 경로 거리 계산
        float distanceToPlayer = _enemy.GetDistanceToDestination(_enemy.Player.transform.position, true);

        if (distanceToPlayer > _enemy.ReturnDistance)
        {
            _enemy.SetState(EnemyState.Return);
        }
        else if (distanceToPlayer < _enemy.AttackDistance)
        {
            // 시야 내에 있는지 확인 (공격 전)
            if (_enemy.IsTargetInSight(_enemy.Player.transform.position, 120f, _enemy.AttackDistance * 1.5f))
            {
                _enemy.SetState(EnemyState.Attack);
            }
        }
    }

    private IEnumerator TraceCoroutine()
    {
        while (true)
        {
            _traceTimer += Time.deltaTime;
            _pathUpdateTimer += Time.deltaTime;

            // 최대 추적 시간 초과 시 Return 상태로 전환
            if (_traceTimer > _enemy.MaxTraceDuration)
            {
                _enemy.SetState(EnemyState.Return);
                yield break;
            }

            // 플레이어가 없거나 사망한 경우 처리
            if (_enemy.Player == null)
            {
                _enemy.SetState(EnemyState.Return);
                yield break;
            }

            // 유틸리티 메서드를 사용하여 NavMeshAgent 상태 확인 및 이동 처리
            if (_enemy.IsAgentValid())
            {
                // 일정 간격으로 경로 업데이트
                if (_pathUpdateTimer >= _enemy.PathUpdateInterval)
                {
                    _pathUpdateTimer = 0f;

                    // 플레이어 위치로 이동 설정
                    if (!_enemy.SetDestination(_enemy.Player.transform.position))
                    {
                        // 경로 설정 실패 시 시야 확인
                        if (!NavMeshUtility.CanReachDestination(_enemy.transform.position, _enemy.Player.transform.position))
                        {
                            // 플레이어 주변의 도달 가능한 지점 탐색 시도 코드를 추가할 수 있음
                            // 현재는 Return 상태로 전환
                            _enemy.SetState(EnemyState.Return);
                            yield break;
                        }
                    }

                    // 디버그 모드일 때 경로 시각화
                    if (Debug.isDebugBuild)
                    {
                        NavMeshUtility.DebugDrawAgentPath(_enemy.Agent, Color.red, _enemy.PathUpdateInterval);
                    }
                }

                // 이동 방향으로 회전
                _enemy.RotateToMoveDirection();
            }
            else
            {
                _enemy.SetState(EnemyState.Return);
                yield break;
            }

            yield return null;
        }
    }
}