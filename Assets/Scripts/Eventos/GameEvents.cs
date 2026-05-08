using System;
using UnityEngine;
using UnityEngine.Events;

public struct VictoryEvent : IEvent
{
    
}
public struct GeneratorEvent : IEvent
{
    public bool IsGeneratorOn;
}

public struct OnInject : IEvent
{
    public ulong VictimID;
    public InyeccionType Type;
}

public struct OnDienteSnap : IEvent
{
    public ulong ID;
    public SnappableItem currentItem;
}
/*
==============================================================================
                                EVENTOS DE ALIEN
==============================================================================
*/
public struct OnAlienHealthChanged : IEvent 
{
    public ulong AlienID;
    public float CurrentHealth;
    public float MaxHealth;
}
public struct OnAlienDeath : IEvent
{
    public ulong AlienID;
}
public struct OnAlienParasiteAttack : IEvent
{
    public ulong AlienID;
}
public struct TaraHealedEvent : IEvent
{
    public ulong TaraID;
}
/*
==============================================================================
                                EVENTOS DE INSECTO
==============================================================================
*/

public struct OnInsectExplosion :IEvent
{
    public ulong VictimID;
}

/*
==============================================================================
                                EVENTOS DE BLOODBAG
==============================================================================
*/

public struct OnBloodBagEmpty : IEvent
{
    public ulong BloodBagID;
}
public struct OnBloodBagSnapped : IEvent
{
    public ulong BloodBagID;
}

public struct OnBloodBagDetached : IEvent
{
    public ulong BloodBagID;
}

/*
==============================================================================
                                EVENTOS DE HERIDAS
==============================================================================
*/
[Serializable]
public struct ColliderInteractableEvent : IEvent
{
    public ulong ObjectID;
}