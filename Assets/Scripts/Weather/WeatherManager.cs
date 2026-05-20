using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    [Header("Weather Settings")]
    [SerializeField] private float chanceOfRain = 0.3f; // 30% шанс дощу
    [SerializeField] private ParticleSystem rainEffect; // ефект дощу

    [Header("Lighting")]
    [SerializeField] private LightingManager lightingManager; 

    public float SolarEnergyModifier { get; private set; } = 1f;
    
    public float WindEnergyModifier { get; private set; } = 1f;

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

        if (lightingManager != null)
        {
            lightingManager.SetRain(false);
        }
    }

    private void OnDateChange()
    {
        if (Random.value < chanceOfRain)
        {
            Debug.Log("Сьогодні йде дощ і дує сильний вітер!");

            SolarEnergyModifier = 0.1f;
            WindEnergyModifier = 2.5f;

            if (rainEffect != null)
            {
                rainEffect.Play();
            }

            if (lightingManager != null)
            {
                lightingManager.SetRain(true);
            }
        }
        else
        {
            Debug.Log("Сьогодні ясна погода, вітер помірний.");

            SolarEnergyModifier = 1f;
            WindEnergyModifier = 1.0f; 

            if (rainEffect != null)
            {
                rainEffect.Stop();
            }

            if (lightingManager != null)
            {
                lightingManager.SetRain(false);
            }
        }
    }
}