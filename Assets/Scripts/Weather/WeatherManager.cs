using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    [Header("Weather Settings")]
    [SerializeField] private float chanceOfRain = 0.3f; // 30% шанс дощу
    [SerializeField] private ParticleSystem rainEffect; // ефект дощу

    [Header("Lighting")]
    [SerializeField] private LightingManager lightingManager; // ДОДАНО: посилання на освітлення

    public float SolarEnergyModifier { get; private set; } = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        GameManager.Instance.onDateChange.AddListener(OnDateChange);

        // ДОДАНО: на старті вважаємо, що дощу немає
        if (lightingManager != null)
        {
            lightingManager.SetRain(false);
        }
    }

    private void OnDateChange()
    {
        if (Random.value < chanceOfRain)
        {
            Debug.Log("Сьогодні йде дощ!");

            SolarEnergyModifier = 0.1f;

            if (rainEffect != null)
            {
                rainEffect.Play();
            }

            // ДОДАНО: повідомляємо LightingManager, що дощ почався
            if (lightingManager != null)
            {
                lightingManager.SetRain(true);
            }
        }
        else
        {
            Debug.Log("Сьогодні ясна погода!");

            SolarEnergyModifier = 1f;

            if (rainEffect != null)
            {
                rainEffect.Stop();
            }

            // ДОДАНО: повідомляємо LightingManager, що дощу немає
            if (lightingManager != null)
            {
                lightingManager.SetRain(false);
            }
        }
    }
}