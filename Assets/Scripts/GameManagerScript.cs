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
    public float moneyBalance = 1488f;

    public float totalEnergyAvailable = 74f; 

    [Tooltip("Bring all houses here")]
    public List<HomeScript> allHouses = new List<HomeScript>();

    [Tooltip("Unused Energy")]
    public float UnusedEnergy = 0f;

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
        if (allHouses == null || allHouses.Count == 0) 
        {
            UnusedEnergy = totalEnergyAvailable;
            return;
        }

        float currentFrameEnergyPool = totalEnergyAvailable;

        foreach (HomeScript house in allHouses)
        {
            if(house != null)
            {
                float consumed = house.ConsumeEnergy(currentFrameEnergyPool);

                currentFrameEnergyPool -= consumed;
            }

        }  
        UnusedEnergy = currentFrameEnergyPool;
    }

    public void AddMoney(float amount)
    {
        moneyBalance += amount;
    }
}