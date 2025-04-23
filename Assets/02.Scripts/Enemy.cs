using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Trace,
        Return,
        Attack,
        Damaged,
        Die,
    }

    public EnemyState CurrentState = EnemyState.Idle;

    private GameObject _player;
    private CharacterController _characterController;
    private Vector3 _startPosition;

    public float FindDistance = 7f;
    public float ReturnDistance = 10f;
    public float AttackDistance = 1.5f;
    public float MoveSpeed = 5f;
    public float AttackCooldown = 1f;
    private float _attackCooldownTimer = 0f;
    public int Health = 100;
    public float DamagedTime = 0.5f;
    public float DeathTime = 1f;

    private void Start()
    {
        // 시작 위치 저장
        _startPosition = transform.position;

        // 캐릭터 컨트롤러 컴포넌트를 찾는다.
        _characterController = GetComponent<CharacterController>();

        // 플레이어 오브젝트를 찾는다.
        _player = GameObject.FindGameObjectWithTag("Player");

        // 초기 상태 설정
        CurrentState = EnemyState.Idle;
    }

    private void Update()
    {
        switch (CurrentState)
        {
            case EnemyState.Idle:
                Idle();
                break;
            case EnemyState.Trace:
                Trace();
                break;
            case EnemyState.Return:
                Return();
                break;
            case EnemyState.Attack:
                Attack();
                break;
        }
    }

    public void TakeDamage(Damage damage)
    {
        // 사망했거나 공격받고 있는 중이면..
        if (CurrentState == EnemyState.Damaged || CurrentState == EnemyState.Die)
        {
            return;
        }

        Health -= damage.Value;

        if (Health <= 0)
        {
            Debug.Log($"상태전환: {CurrentState} -> Die");
            CurrentState = EnemyState.Die;
            StartCoroutine(Die_Coroutine());
            return;
        }

        Debug.Log($"상태전환: {CurrentState} -> Damaged");
        CurrentState = EnemyState.Damaged;

        StartCoroutine(Damaged_Coroutine());
    }

    private void Idle()
    {
        // 행동: 가만히 있는다.
        if (Vector3.Distance(transform.position, _player.transform.position) < FindDistance)
        {
            // 플레이어가 범위 안에 들어오면 상태 전환
            Debug.Log("상태전환: Idle -> Trace");
            CurrentState = EnemyState.Trace;
        }
    }

    private void Trace()
    {
        // 전이: 플레이어와 멀어지면 -> Return
        if (Vector3.Distance(transform.position, _player.transform.position) > ReturnDistance)
        {
            Debug.Log("상태전환: Trace -> Return");
            CurrentState = EnemyState.Return;
            return;
        }

        // 전이: 공격 범위만큼 가까워지면 -> Attack
        if (Vector3.Distance(transform.position, _player.transform.position) < AttackDistance)
        {
            Debug.Log("상태전환: Trace -> Attack");
            CurrentState = EnemyState.Attack;
            return;
        }

        // 행동: 플레이어를 추적한다.
        Vector3 direction = (_player.transform.position - transform.position).normalized;
        _characterController.Move(direction * MoveSpeed * Time.deltaTime);
    }

    private void Return()
    {
        // 전이: 시작 위치와 가까워지면 -> Idle
        if (Vector3.Distance(transform.position, _startPosition) <= _characterController.minMoveDistance)
        {
            Debug.Log("상태전환: Return -> Idle");
            transform.position = _startPosition;
            CurrentState = EnemyState.Idle;
            return;
        }

        // 전이: 플레이어가 범위 안에 들어오면 -> Trace
        if (Vector3.Distance(transform.position, _player.transform.position) < FindDistance)
        {
            Debug.Log("상태전환: Return -> Trace");
            CurrentState = EnemyState.Trace;
            return;
        }

        // 행동: 시작 위치로 되돌아간다.
        Vector3 direction = (_startPosition - transform.position).normalized;
        _characterController.Move(direction * MoveSpeed * Time.deltaTime);
    }

    private void Attack()
    {
        // 전이: 공격 범위보다 멀어지면 -> Trace
        if (Vector3.Distance(transform.position, _player.transform.position) > AttackDistance)
        {
            Debug.Log("상태전환: Attack -> Trace");
            CurrentState = EnemyState.Trace;
            _attackCooldownTimer = 0f;
            return;
        }

        // 행동: 플레이어를 공격한다.
        _attackCooldownTimer += Time.deltaTime;
        if (_attackCooldownTimer >= AttackCooldown)
        {
            Debug.Log("플레이어 공격!");
            _attackCooldownTimer = 0f;
        }
    }

    private IEnumerator Damaged_Coroutine()
    {
        // 코루틴 방식으로 변경
        yield return new WaitForSeconds(DamagedTime);
        Debug.Log("상태전환: Damaged -> Trace");
        CurrentState = EnemyState.Trace;
    }

    private IEnumerator Die_Coroutine()
    {
        yield return new WaitForSeconds(DeathTime);
        gameObject.SetActive(false);
    }
}
