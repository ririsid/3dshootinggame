using UnityEngine;

public struct Damage
{
    public int Value;
    public GameObject From;
}

// IDamageable 인터페이스 예시 (프로젝트에 없을 경우 추가 필요)
public interface IDamageable
{
    void TakeDamage(Damage damage);
}
