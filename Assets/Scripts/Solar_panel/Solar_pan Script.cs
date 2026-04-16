using UnityEngine;

public class Solar_panScript : MonoBehaviour, IEnergyProducer
{
/*--------------------------------Interface-----------------------------------------*/
     [Header("Energy Generation Price")]
    public float energyMultiplier = 1f;

      [Header("Generation Schedule (0-24h)")]
    public Vector2[] generationPoints = new Vector2[]
    {
        new Vector2(0f, 0f),   // ніч
        new Vector2(6f, 0.2f), // світанок
        new Vector2(9f, 0.6f), // ранок
        new Vector2(12f, 1f),  // пік сонця
        new Vector2(15f, 0.7f),
        new Vector2(18f, 0.3f),
        new Vector2(21f, 0.1f),
        new Vector2(24f, 0f)   // ніч
    };

    private float currentGeneration;

    /*--------------------------------Realization-----------------------------------------*/

    private void Update()
    {
        float time = GameManager.Instance.currentTime;

        currentGeneration = LagrangeInterpolation(time, generationPoints);

        currentGeneration = Mathf.Max(0f, currentGeneration);
    }

    public float ProduceEnergy()
    {
        // базова генерація
        float produced = currentGeneration * energyMultiplier;

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
