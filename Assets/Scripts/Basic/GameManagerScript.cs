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
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI networkLoadText;
    private int date = 1;
    private float networkLoad;

    [Header("Blackout Settings")]
    public bool isBlackout = false; 
    public float BlackoutTime = 10f;
    private float blackoutTimer = 0f; // Скільки секунд залишилося до увімкнення
    public GameObject blackoutPanel; // ui панелька
    public TextMeshProUGUI blackoutPopupTimerText; 
    private float graceTimer = 0f; // Таймер імунітету
    public float graceDuration = 18f; // Скільки секунд даємо гравцю на виправлення проблеми



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
            if (date < 31) date += 1;
            else date = 1;
            
            currentTime -= 24f;
        }
       if (isBlackout)
        {
            blackoutTimer -= Time.deltaTime; 
            
            if (blackoutPopupTimerText != null)
                blackoutPopupTimerText.text = $"Відновлення через: {blackoutTimer:F1} с";

            if (blackoutTimer <= 0f)
            {
                isBlackout = false;
                
                // ІМУНІТЕТ на 18 секунд
                graceTimer = graceDuration; 
                
                if (blackoutPanel != null) 
                    blackoutPanel.SetActive(false); 
            }
        }
        else if (graceTimer > 0f) // Якщо світло є, але імунітет ще активний
        {
            graceTimer -= Time.deltaTime; 
        }
        
        DistributeEnergy();
        
        UpdateClockUI();
        UpdateEconomicsUI();
    }

    private void UpdateClockUI()
    {
        int hours = Mathf.FloorToInt(currentTime);
        int minutes = Mathf.FloorToInt((currentTime - hours) * 60f);

        timeText.text = $"Час: {hours:00}:{minutes:00}";
        dateText.text = $"{date}";
    }

   private void UpdateEconomicsUI()
    {
        moneyText.text = $"Бюджет: {moneyBalance:F0} $";
        energyText.text = $"Залишок енергії: {UnusedEnergy:F1} кВт";

        // Стан 1: Повний блекаут
        if (isBlackout)
        {
            networkLoadText.text = "АВАРІЯ!";
            networkLoadText.color = Color.red;
        }
        // Стан 2: імунітет після блекауту
        else if (graceTimer > 0f)
        {
            //  зворотний відлік до можливого нового блекауту
            networkLoadText.text = $"Імунітет: {graceTimer:F1} с";
            
          
            networkLoadText.color = Color.yellow; 
        }
        // Стан 3: Звичайна робота мережі
        else
        {
            networkLoadText.text = $"Навантаження: {networkLoad:0}%";

            // Якщо навантаження критичне 
            if (networkLoad >= 90f)
            {
                networkLoadText.color = Color.red;
            }
            else
            {
                networkLoadText.color = Color.white; 
            }
        }
    }

  public void DistributeEnergy()
    {
        float generated_en = 0f;

        if (!isBlackout)
        {
            foreach (var p in producers) 
                generated_en += p.ProduceEnergy();
        }

        float remaining_en = generated_en;

        foreach (var c in consumers)
            remaining_en -= c.ConsumeEnergy(remaining_en);

        UnusedEnergy = remaining_en;

        //Підрахунок навантаження 
        if (generated_en > 0f) 
        {
            float consumed_en = generated_en - UnusedEnergy; 
            networkLoad = (consumed_en / generated_en) * 100f; 

            // блекаут
            if (networkLoad >= 100f && !isBlackout && graceTimer <= 0f) 
            {
                isBlackout = true;
                blackoutTimer = 10f; 
                if (blackoutPanel != null) 
                    blackoutPanel.SetActive(true); 
            }
        }
        else
        {
            networkLoad = 0f; 
        }
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