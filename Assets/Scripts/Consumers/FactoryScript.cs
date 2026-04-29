using UnityEngine;

public class FactoryScript : MonoBehaviour, IEnergyConsumer
{

/*--------------------------------Interface-----------------------------------------*/

    [Header("Economics")]
    public float pricePerKw = 3500f;

    [Header("Графік споживання (Зміни)")]
    // Завод працює різко: вночі 20 кВт, о 8:00 стрибок до 100 кВт (1 зміна), 
    // о 18:00 спад до 50 кВт (2 зміна), і о 23:00 знову 20 кВт.
    private Vector2[] demandPoints = new Vector2[]
    {
        new Vector2(0f, 20f),
        new Vector2(7f, 20f),   // До 7 ранку спимо
        new Vector2(8f, 100f),  // Плавний, але швидкий запуск
        new Vector2(17f, 100f), // Кінець першої зміни
        new Vector2(18f, 50f),  // Друга зміна (половина станків)
        new Vector2(23f, 20f),  // Завершення роботи
        new Vector2(24f, 20f)
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
        
        currentDemand = CosineInterpolateSchedule(globalTime, demandPoints);

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

    public float ConsumeEnergy(float availableEnergy)
    {
        float consumedEnergy = Mathf.Min(availableEnergy, currentDemand);
        
        float currentPay = consumedEnergy * pricePerKw * Time.deltaTime;
    
        GameManager.Instance.AddMoney(currentPay);

        accumulatedMoney += currentPay;

        return consumedEnergy;
    }


    // Шматково-косинусна інтерполяція для графіка споживання
    private float CosineInterpolateSchedule(float time, Vector2[] points)
    {
        // Захист від помилок
        if (points.Length == 0) return 0f;
        if (points.Length == 1) return points[0].y;

        // Знаходимо, між якими двома точками ми зараз знаходимося
        for (int i = 0; i < points.Length - 1; i++)
        {
            if (time >= points[i].x && time <= points[i + 1].x)
            {
                float x1 = points[i].x;
                float y1 = points[i].y;
                float x2 = points[i + 1].x;
                float y2 = points[i + 1].y;

                // 1. Нормалізуємо час (t буде від 0 до 1)
                float t = (time - x1) / (x2 - x1);

                // 2. Рахуємо коефіцієнт згладжування за формулою
                float mu = (1f - Mathf.Cos(t * Mathf.PI)) / 2f;

                // 3. Інтерполюємо значення
                return y1 * (1f - mu) + y2 * mu;
            }
        }

        return points[points.Length - 1].y;
    }

    public float GetExpectedDemand(float futureTime)
    {
        float t = futureTime % 24f;
        return CosineInterpolateSchedule(t, demandPoints);
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
}