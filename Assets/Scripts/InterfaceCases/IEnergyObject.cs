using UnityEngine;

/// Базовий контракт для будь-якого вузла енергомережі.
/// Реалізується всіма будівлями: генераторами, споживачами, підстанціями.
public interface IEnergyObject
{
    GameObject GameObject { get; }
    bool IsConnected { get; set; }
    int MaxSlots { get; }
    int UsedSlots { get; }
}
