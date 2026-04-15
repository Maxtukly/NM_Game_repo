using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomeScript : MonoBehaviour
{
/*--------------------------------Interface-----------------------------------------*/

    [Header("Price")]
    public float pricePerKw = 1488.0f;

    [Header("Consumption Schedule")]
    public Vector2[] demandPoints = new Vector2[]
    {
        new Vector2(4f, 1f),
        new Vector2(6f, 1f),  
        new Vector2(8f, 5f),   
        new Vector2(14f, 2f),  
        new Vector2(20f, 6f),  
        new Vector2(24f, 1f)   
    };
    
    private float currentDemand;


/*--------------------------------Realization-----------------------------------------*/
   
    private void Update() 
    {
        float globalTime = GameManager.Instance.currentTime;
        currentDemand = LagrangeInt(globalTime, demandPoints);
        currentDemand = Mathf.Max(0f, currentDemand);
    }

    public float ConsumeEnergy(float suppliedEnergy)
    {
        float consumedEnergy = Mathf.Min(suppliedEnergy, currentDemand);
        
        float currentPay = consumedEnergy * pricePerKw * Time.deltaTime;
        
        GameManager.Instance.AddMoney(currentPay);
        
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