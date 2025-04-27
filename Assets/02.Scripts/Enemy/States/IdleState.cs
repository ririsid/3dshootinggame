using System.Collections;
using UnityEngine;

/// <summary>
/// 적의 대기 상태를 구현하는 클래스입니다.
/// </summary>
public class IdleState : IEnemyState
{
    private Enemy _enemy;
    private Coroutine _idleCoroutine;
    private Coroutine _lookAroundCoroutine;

    // 필드들을 제거하고 Enemy 클래스의 프로퍼티 사용

    public IdleState(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void Enter()
    {
        _idleCoroutine = _enemy.StartCoroutine(IdleCoroutine());
        _lookAroundCoroutine = _enemy.StartCoroutine(LookAroundCoroutine());

        // NavMeshAgent 속도를 0으로 설정
        if (_enemy.IsAgentValid())
        {
            _enemy.Agent.speed = 0f;
        }
    }

    public void Exit()
    {
        if (_idleCoroutine != null)
        {
            _enemy.StopCoroutine(_idleCoroutine);
            _idleCoroutine = null;
        }

        if (_lookAroundCoroutine != null)
        {
            _enemy.StopCoroutine(_lookAroundCoroutine);
            _lookAroundCoroutine = null;
        }
    }

    public void Update()
    {
        // Idle 상태에서 플레이어 감지 확인
        CheckTransitions();
    }

    public void CheckTransitions()
    {
        if (_enemy.Player == null) return;

        // NavMeshUtility를 사용하여 실제 경로 거리 계산
        float distanceToPlayer = _enemy.GetDistanceToDestination(_enemy.Player.transform.position, true);

        if (distanceToPlayer < _enemy.FindDistance)
        {
            // 시야 내에 플레이어가 있는지 확인
            if (_enemy.IsTargetInSight(_enemy.Player.transform.position, _enemy.ViewAngle, _enemy.FindDistance))
            {
                _enemy.SetState(EnemyState.Trace);
            }
        }
    }

    private IEnumerator IdleCoroutine()
    {
        yield return new WaitForSeconds(_enemy.IdleDuration);

        if (!_enemy.IsDead)
        {
            _enemy.SetState(EnemyState.Patrol);
        }
    }

    /// <summary>
    /// 적이 주변을 무작위로 둘러보는 코루틴입니다.
    /// </summary>
    private IEnumerator LookAroundCoroutine()
    {
        while (true)
        {
            // 회전 간격 대기
            yield return new WaitForSeconds(_enemy.LookAroundInterval);

            if (_enemy.IsDead)
                yield break;

            // NavMesh를 활용하여 둘러볼 유효한 위치 찾기
            Vector3 currentPosition = _enemy.transform.position;
            Vector3 lookTarget = NavMeshUtility.GetRandomLookAtPosition(
                currentPosition,
                currentPosition,
                _enemy.ViewAngle,
                _enemy.MinLookDistance,
                _enemy.MaxLookDistance,
                _enemy.ObstacleLayer
            );

            // 시작 회전값 저장
            Quaternion startRotation = _enemy.transform.rotation;

            // 목표 회전 계산 (y축 회전만 적용)
            Vector3 lookDirection = lookTarget - currentPosition;
            lookDirection.y = 0;

            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

                float elapsedTime = 0f;

                // 시각화 디버깅 (개발 빌드에서만)
                if (Debug.isDebugBuild)
                {
                    Debug.DrawLine(currentPosition, lookTarget, Color.yellow, _enemy.LookAroundInterval);
                    Debug.DrawRay(currentPosition, _enemy.transform.forward * 2f, Color.blue, 0.2f);
                }

                // 부드러운 회전 실행
                while (elapsedTime < _enemy.RotationDuration)
                {
                    if (_enemy.IsDead)
                        yield break;

                    elapsedTime += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsedTime / _enemy.RotationDuration);

                    // EaseInOut 커브 적용
                    t = t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;

                    // Enemy의 RotateToTarget 메서드를 직접 호출하는 대신, 수동 회전 처리
                    // 이는 커스텀 보간 및 타이밍 컨트롤이 필요하기 때문임
                    _enemy.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                    yield return null;
                }

                // 회전 완료 후 환경 조사
                InvestigateArea(lookTarget);
            }

            // 대기 시간에 약간의 변화 추가
            yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));
        }
    }

    /// <summary>
    /// 특정 위치를 관찰하고 환경을 조사합니다.
    /// </summary>
    private void InvestigateArea(Vector3 targetPosition)
    {
        // 플레이어 존재 여부 확인
        if (_enemy.Player != null)
        {
            // 플레이어가 관찰 방향의 전방 시야각 내에 있는지 확인
            Vector3 playerDirection = (_enemy.Player.transform.position - _enemy.transform.position).normalized;
            float playerAngle = Vector3.Angle(_enemy.transform.forward, playerDirection);

            if (playerAngle < _enemy.ViewAngle / 2f) // 시야각의 절반 범위로 체크
            {
                // 플레이어와의 거리 확인 (NavMesh 경로 사용)
                float distanceToPlayer = _enemy.GetDistanceToDestination(_enemy.Player.transform.position, true);

                if (distanceToPlayer < _enemy.FindDistance)
                {
                    // 시야 내에 장애물이 없는지 확인
                    if (_enemy.IsTargetInSight(_enemy.Player.transform.position, _enemy.ViewAngle / 2f, _enemy.FindDistance))
                    {
                        // 플레이어 발견, 추적 상태로 전환
                        _enemy.SetState(EnemyState.Trace);
                        return;
                    }
                }
            }
        }
    }
}