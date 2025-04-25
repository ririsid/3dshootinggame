using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 적의 복귀 상태를 구현하는 클래스입니다.
/// </summary>
public class ReturnState : IEnemyState
{
    private Enemy _enemy;
    private Coroutine _returnCoroutine;
    private float _returnTimer = 0f;

    public ReturnState(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void Enter()
    {
        _returnTimer = 0f;

        // NavMeshAgent 속도 설정 (NavMeshUtility 유효성 검사)
        if (_enemy.IsAgentValid())
        {
            _enemy.Agent.speed = _enemy.ReturnSpeed;
        }

        _returnCoroutine = _enemy.StartCoroutine(ReturnCoroutine());
    }

    public void Exit()
    {
        if (_returnCoroutine != null)
        {
            _enemy.StopCoroutine(_returnCoroutine);
            _returnCoroutine = null;
        }
    }

    public void Update()
    {
        // 코루틴에서 처리
    }

    public void CheckTransitions()
    {
        if (_enemy.Player == null) return;

        float distanceToPlayer = _enemy.GetDistanceToDestination(_enemy.Player.transform.position, true);

        if (_enemy.HasReachedDestination(_enemy.StartPosition) && _enemy.CharacterController.isGrounded)
        {
            _enemy.SetState(Enemy.EnemyState.Idle);
        }
        else if (distanceToPlayer < _enemy.FindDistance)
        {
            // 시야 내에 플레이어가 있는지 확인
            if (_enemy.IsTargetInSight(_enemy.Player.transform.position, 120f, _enemy.FindDistance))
            {
                _enemy.SetState(Enemy.EnemyState.Trace);
            }
        }
    }

    private IEnumerator ReturnCoroutine()
    {
        // NavMeshUtility를 통해 목적지 설정
        if (!_enemy.SetDestination(_enemy.StartPosition))
        {
            // 시작 위치로 경로를 설정할 수 없는 경우 안전한 위치로 텔레포트 시도
            Vector3 safePosition = NavMeshUtility.GetNavMeshPosition(_enemy.StartPosition);
            _enemy.transform.position = safePosition;
            _enemy.transform.rotation = _enemy.StartRotation;
            _enemy.SetState(Enemy.EnemyState.Idle);
            yield break;
        }

        while (true)
        {
            _returnTimer += Time.deltaTime;

            // 최대 복귀 시간 초과 시 안전 장치 발동
            if (_returnTimer > _enemy.MaxReturnDuration)
            {
                // NavMeshAgent 작업을 NavMeshUtility를 통해 처리
                if (_enemy.IsAgentValid())
                {
                    _enemy.Agent.isStopped = true;
                    _enemy.Agent.ResetPath();
                }

                // NavMesh 상의 안전한 위치 찾기
                Vector3 safePosition = NavMeshUtility.GetNavMeshPosition(_enemy.StartPosition);
                _enemy.transform.position = safePosition;
                _enemy.transform.rotation = _enemy.StartRotation;
                _enemy.SetState(Enemy.EnemyState.Idle);
                yield break;
            }

            // 경로 디버깅 (Development Build에서만 활성화)
            if (Debug.isDebugBuild)
            {
                NavMeshUtility.DebugDrawAgentPath(_enemy.Agent, Color.blue, 0.1f);
            }

            // 이동 방향으로 회전
            _enemy.RotateToMoveDirection();

            // 유틸리티 메서드를 사용하여 목적지 도달 여부 확인
            if (_enemy.HasReachedDestination(_enemy.StartPosition, _enemy.Agent.stoppingDistance))
            {
                _enemy.SetState(Enemy.EnemyState.Idle);
                yield break;
            }

            // 주기적으로 경로 재계산 (5초마다)
            if (_returnTimer % 5f < Time.deltaTime)
            {
                if (_enemy.IsAgentValid())
                {
                    _enemy.SetDestination(_enemy.StartPosition);
                }
            }

            yield return null;
        }
    }
}