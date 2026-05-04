using UnityEngine;

// Підписуємо контракт IEnergyProducer
public class BasicStation : MonoBehaviour, IEnergyProducer, IEnergyObject
{
    public GameObject GameObject => gameObject;
    public bool IsConnected { get; set; }
    public int MaxSlots => 1;
    public int UsedSlots => IsConnected ? 1 : 0;
    [Header("Info")]
    [Tooltip("Скільки енергії (кВт) станція генерує щосекунди")]
    public float baseGeneration = 200f; 

    [Tooltip("Витрати на обслуговування (віднімаються з бюджету)")]
    public float maintenanceCost = 0f; 

    public float ProduceEnergy()
    {
        // 1. Віднімаємо гроші за роботу станції (паливо, зарплати)
        float cost = maintenanceCost * Time.deltaTime;
        GameManager.Instance.AddMoney(-cost);

        return baseGeneration;
    }
}