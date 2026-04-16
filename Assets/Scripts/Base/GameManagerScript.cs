using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
/*--------------------------------Interface-----------------------------------------*/

    public static GameManager Instance { get; private set; }

    [Header("TimeSettings")]
    [Tooltip("CurrentTime")]
    [Range(0f, 24f)]
    public float currentTime = 0f;
    
    [Tooltip("TimeMultiplySpeed")]
    public float timeSpeed = 1f;

    [Header("Economics")]
    public float moneyBalance = 0f;

    [Header("Buildings")]
    public GameObject selectedBuildingPrefab; // Поточний вибраний будинок для будівництва
    public List<GameObject> availableBuildings = new List<GameObject>(); // Список доступних будинків для вибору
    private int selectedIndex = 0; // Індекс вибраного будинку в списку availableBuildings

    [Tooltip("Energy")]
    public float UnusedEnergy = 0f;

    public List<IEnergyConsumer> consumers = new List<IEnergyConsumer>(); // Список всіх споживачів енергії в грі
    public List<IEnergyProducer> producers = new List<IEnergyProducer>(); // Список всіх виробників енергії в грі

    [Header("UI")]
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI energyText;



/*--------------------------------Realization-----------------------------------------*/

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    private void Update()
    {
        currentTime += Time.deltaTime * timeSpeed;
        
        if (currentTime >= 24f)
        {
            currentTime -= 24f;
        }
        DistributeEnergy();
        if (GameManager.Instance == null) return;
        UpdateClockUI();
        UpdateEconomicsUI();
    }


    private void UpdateClockUI()
    {

        float rawTime = GameManager.Instance.currentTime;

        int hours = Mathf.FloorToInt(rawTime);

        int minutes = Mathf.FloorToInt((rawTime - hours) * 60f);

        timeText.text = $"Час: {hours:00}:{minutes:00}";
    }

    private void UpdateEconomicsUI()
    {
        moneyText.text = $"Бюджет: {GameManager.Instance.moneyBalance:F0} $";

        energyText.text = $"Залишок енергії: {GameManager.Instance.UnusedEnergy:F1} кВт";
    }

    public void DistributeEnergy()
    {
        float generated_en = 0f;

        // Спочатку збираємо всю вироблену енергію в "пул"
        foreach (var p in producers) 
            generated_en += p.ProduceEnergy();

        float remaining_en = generated_en;

        // Потім розподіляємо її між споживачами
        foreach (var c in consumers)
            remaining_en -= c.ConsumeEnergy(remaining_en);

        // Залишок після розподілу - це невикористана енергія
        UnusedEnergy = remaining_en;
    }

    public void AddMoney(float amount)
    {
        moneyBalance += amount;
    }

    public void SelectNextBuilding() // Метод для вибору наступної структури в списку
    {
        if (availableBuildings.Count == 0) return;

        selectedIndex++;
        if (selectedIndex >= availableBuildings.Count)
            selectedIndex = 0;

        selectedBuildingPrefab = availableBuildings[selectedIndex];
    }

    public void SelectPreviousBuilding() // Метод для вибору попередньої структури в списку
    {
        if (availableBuildings.Count == 0) return;

        selectedIndex--;
        if (selectedIndex < 0)
            selectedIndex = availableBuildings.Count - 1;

        selectedBuildingPrefab = availableBuildings[selectedIndex];
    }
}