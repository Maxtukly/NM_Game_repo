using UnityEngine;

public class HomeScript : MonoBehaviour, IEnergyConsumer
{
/*--------------------------------Interface-----------------------------------------*/

    [Header("Price")]
    [SerializeField] private float pricePerKw = 1488.0f;

    [Header("Consumption Schedule")]
    private Vector2[] demandPoints = new Vector2[]
    {
        new Vector2(4f, 0.1f),
        new Vector2(6f, 0.1f),  
        new Vector2(8f, 0.5f),   
        new Vector2(14f, 0.2f),   
        new Vector2(20f, 0.6f),  
        new Vector2(24f, 0.1f)   
    };
    
    private float currentDemand;
    [Header("Visual")]
    public GameObject moneyPopupPrefab; 
    
    private float accumulatedMoney = 0f; 
    private float popupTimer = 0f; 
    private float popupInterval = 2f; 


/*--------------------------------Realization-----------------------------------------*/
   
    private void Update() 
    {
        float globalTime = GameManager.Instance.currentTime;
        currentDemand = LagrangeInt(globalTime, demandPoints);
        currentDemand = Mathf.Max(0f, currentDemand);
        popupTimer += Time.deltaTime;
        if (popupTimer >= popupInterval)
        {
         
            if (Mathf.Abs(accumulatedMoney) > 0.1f && moneyPopupPrefab != null) 
            {
                ShowMoneyPopup(accumulatedMoney);
                accumulatedMoney = 0f; 
            }
            popupTimer = 0f; 
        }
    }

    private void ShowMoneyPopup(float amount)
    {
        Vector3 spawnPosition = transform.position + Vector3.up; 
        
        GameObject popup = Instantiate(moneyPopupPrefab, spawnPosition, Quaternion.identity);
        
        if (popup.TryGetComponent<MoneyPopup>(out var popupScript))
        {
            popupScript.Setup(amount);
        }
    }
    public float GetExpectedDemand(float futureTime)
    {
        float t = futureTime % 24f; 
        float rawDemand = LagrangeInt(t, demandPoints);
        
        return Mathf.Max(0f, rawDemand); 
    }

    public float ConsumeEnergy(float suppliedEnergy)
    {
        float consumedEnergy = Mathf.Min(suppliedEnergy, currentDemand);
        
        float currentPay = consumedEnergy * pricePerKw * Time.deltaTime;
        
        GameManager.Instance.AddMoney(currentPay);
        
        accumulatedMoney += currentPay;

        return consumedEnergy;
        
    }

    private float LagrangeInt(float x, Vector2[] points)
    {
        float result = 0f;
        int n = points.Length;
        for (int i = 0; i < n; i++)
        {
            float term = points[i].y;
            for (int j = 0; j < n; j++)
            {
                if (i != j)
                {
                    term *= (x - points[j].x) / (points[i].x - points[j].x);
                }
            }
            result += term;
        }

        return result;
    }
}