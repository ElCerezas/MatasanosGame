public struct VictoryEvent : IEvent
{
    
}

public struct OnInject : IEvent
{
    public ulong VictimID;
    public InyeccionType Type;
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

public struct TaraGeneratedEvent : IEvent
{
    public ulong TaraID;
}
public struct TaraHealedEvent : IEvent
{
    public ulong TaraID;
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