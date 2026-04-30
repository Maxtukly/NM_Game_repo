using UnityEngine;

public class Solar_panScript : MonoBehaviour, IEnergyProducer
{
/*--------------------------------Interface-----------------------------------------*/

    [Header("Generation Schedule (0-24h)")]

    [System.NonSerialized]
    public Vector2[] generationPoints;

    private float currentGeneration;

    /*--------------------------------Realization-----------------------------------------*/
 

    private void Awake()
    {   
        // значення визначаються в коді, не в префабах
        generationPoints = new Vector2[]
        {
            new Vector2(0f,  0f),   // північ — генерації нема
            new Vector2(5f,  0f),   // до світанку — нема
            new Vector2(6f,  4f),   // світанок — мінімум
            new Vector2(8f,  10f),   // ранок — зростання
            new Vector2(10f, 17f),  // до полудня
            new Vector2(12f, 22f),  // пік (полудень)
            new Vector2(14f, 20f),  // після піку
            new Vector2(16f, 16f),  // вечір — спад
            new Vector2(18f, 10f),   // захід
            new Vector2(19f, 5f),   // сутінки
            new Vector2(20f, 2f),   // майже темно
            new Vector2(24f, 0f)     // темрява — нема
        };
    }

    void Start()
    {
        // реєструємося в GameManager як виробник енергії
        GameManager.Instance.producers.Add(this);
    }
    private void Update()
    {
        float time = GameManager.Instance.currentTime;

        currentGeneration = LagrangeInterpolation(time, generationPoints);

        currentGeneration = Mathf.Max(0f, currentGeneration);
    }


    public float ProduceEnergy()
    {
        // базова генерація
        float produced = currentGeneration * WeatherManager.Instance.SolarEnergyModifier;

        // в GameManager як згенеровану енергію враховувати поточну генерацію сонячної панелі
        return produced;
    }

     private float LagrangeInterpolation(float x, Vector2[] points)
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
                    term *= (x - points[j].x) /
                            (points[i].x - points[j].x);
                }
            }

            result += term;
        }

        return result;
    }

}
