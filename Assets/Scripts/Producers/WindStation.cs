using UnityEngine;

public class WindStation : MonoBehaviour, IEnergyProducer, IEnergyObject
{
    /*──────────────── IEnergyObject ────────────────*/
    public GameObject GameObject => gameObject;
    public bool IsConnected { get; set; }
    public int MaxSlots => 1;
    public int UsedSlots => IsConnected ? 1 : 0;

    [Header("Wind Generator Settings")]
    [Tooltip("Базова генерація вітряка за ясної погоди")]
    public float baseGeneration = 40f; 

    [Tooltip("Витрати на обслуговування")]
    public float maintenanceCost = 1f; 

    public float ProduceEnergy()
    {

        float cost = maintenanceCost * Time.deltaTime;
        GameManager.Instance.AddMoney(-cost);


        float modifier = WeatherManager.Instance != null ? WeatherManager.Instance.WindEnergyModifier : 1f;
        
        return baseGeneration * modifier;
    }
}