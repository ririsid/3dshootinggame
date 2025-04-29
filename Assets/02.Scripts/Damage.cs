

using UnityEngine;

/// <summary>
/// 피해량과 피해를 준 GameObject 정보를 담는 구조체입니다.
/// </summary>
/// <remarks>
/// 이 구조체는 피해 관련 데이터를 캡슐화하여 전달하는 데 사용됩니다.
/// </remarks>

/// <summary>
/// 피해량의 크기를 나타냅니다.
/// </summary>

/// <summary>
/// 피해를 입힌 GameObject를 참조합니다. 누가 피해를 주었는지 식별하는 데 사용될 수 있습니다.
/// </summary>
public struct Damage
{
    public int Amount;
    public GameObject Source;
}

/// <summary>
/// 피해를 받을 수 있는 모든 객체가 구현해야 하는 인터페이스입니다.
/// </summary>
/// <remarks>
/// 이 인터페이스를 구현하는 클래스는 <see cref="TakeDamage"/> 메서드를 통해 피해를 처리할 수 있습니다.
/// </remarks>

/// <summary>
/// 객체가 피해를 받았을 때 호출되는 메서드입니다.
/// </summary>
/// <param name="damage">적용할 피해의 정보 (<see cref="Damage"/> 구조체)입니다.</param>
public interface IDamageable
{
    void TakeDamage(Damage damage);
}
