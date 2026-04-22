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
/*
==============================================================================
                                EVENTOS DE BLOODBAG
==============================================================================
*/

public struct OnBloodBagEmpty : IEvent
{
    public ulong BloodBagID;
}