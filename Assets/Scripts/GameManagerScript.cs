using UnityEngine;

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
    public HomeScript[] allHouses;

    [Tooltip("Unused Energy")]
    public float UnusedEnergy = 0f;

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
    }

    public void DistributeEnergy()
    {
        if (allHouses == null || allHouses.Length == 0) return;

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