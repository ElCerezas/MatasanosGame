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

public struct OnGeneratorChargeChanged : IEvent
{
    public float CurrentCharge;
    public float MaxCharge;
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
==============================================================================*/
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
public struct OnAlienStateChanged : IEvent
{
    public ulong AlienID;
    public AlienStateEnum NewState;
}

public struct OnAlienCalmantUsed : IEvent
{
    public ulong AlienID;
}
public struct OnCalmantChanged : IEvent
{
    public ulong AlienID;
    public float CurrentCalmant;
    public float MaxCalmant;
}

public struct OnCalmantEnded : IEvent
{
    public ulong AlienID;
}
/*
==============================================================================
                                EVENTOS DE INSECTO
==============================================================================*/

public struct OnInsectExplosion : IEvent
{
    public ulong VictimID;
}

/*
==============================================================================
                                EVENTOS DE BLOODBAG
==============================================================================*/

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
==============================================================================*/
[Serializable]
public struct ColliderInteractableEvent : IEvent
{
    public ulong ObjectID;
}

public struct OnWoundFocoHealStarted : IEvent
{
    public ulong TaraID;
}

public struct OnWoundFocoHealEnded : IEvent
{
    public ulong TaraID;
}

public struct OnDienteUnSnap : IEvent
{
    public ulong SnapZoneID;
    public ulong DienteID;
    public GameObject UnSnappedTooth;
}

public struct TaraCreated : IEvent
{
    public ulong TaraID;
    public WoundType Type;
}
public struct TaraHealedEvent : IEvent
{
    public ulong TaraID;
    public WoundType Type;
}
public struct OnDientesCountChanged : IEvent
{
    public int value;
}
public struct OnWoundFocoCountChanged : IEvent
{
    public int value;
}
public struct OnWoundVendaCountChanged : IEvent
{
    public int value;
}
#region PlayerHUDEffects
public struct OnPlayerSlipped : IEvent
{
    public ulong VictimID;
}
public struct OnPlayerSneezes : IEvent
{
    public ulong VictimID;
}
public struct OnPlayerBlinded : IEvent
{
    public ulong VictimID;
    public float Duration;
}
public struct OnHUDCleaned : IEvent
{
    public ulong VictimID;
    public bool CleanMoco;
    public bool CleanSangre;
    public bool CleanParasito;
}
#endregion