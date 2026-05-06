using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

// Відповідальність: агрегація даних від усіх підстанцій і виведення у UI.
// GameManager НЕ рахує енергію — тільки збирає готові значення від SubstationScript і передає їх у текстові поля.
// Також зберігає глобальний ігровий час і економіку.

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /*-------------------------------- Час ------------------------------------------*/

    [Header("Time Settings")]
    [Range(0f, 24f)]
    public float currentTime = 0f;

    [SerializeField] private float timeSpeed = 1f;
    [SerializeField] private int date = 1;

    /// Подія зміни дати — підписується WeatherManager
    public UnityEvent onDateChange;

    /*-------------------------------- Освітлення ------------------------------------*/

    [Header("Lighting")]
    [SerializeField] private LightingManager lightingManager;
    // ДОДАНО: посилання на LightingManager, щоб передавати йому поточний час гри.

    /*-------------------------------- Економіка ------------------------------------*/

    [Header("Economics")]
    public float moneyBalance = 0f;

    /*-------------------------------- UI -------------------------------------------*/

    [Header("UI — Час і гроші")]
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI dateText;
    [SerializeField] private TextMeshProUGUI moneyText;

    [Header("UI — Мережа (агрегат по всіх підстанціях)")]
    [SerializeField] private TextMeshProUGUI totalGenerationText;
    [SerializeField] private TextMeshProUGUI totalDemandText;
    [SerializeField] private TextMeshProUGUI networkLoadText;

    [Header("UI — Аналітика")]
    [SerializeField] private TextMeshProUGUI forecastText;

    /*-------------------------------- Реєстр підстанцій ----------------------------*/

    private readonly List<SubstationScript> _substations = new List<SubstationScript>();

    public List<IEnergyConsumer> consumers = new List<IEnergyConsumer>();
    public List<IEnergyProducer> producers = new List<IEnergyProducer>();

    /*-------------------------------- Realization ------------------------------------*/

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        onDateChange = new UnityEvent();
    }

    private void Update()
    {
        TickTime();

        // ДОДАНО: кожен кадр передаємо поточний час у LightingManager.
        // Тепер освітлення автоматично змінюється відповідно до часу гри.
        UpdateLightingByTime();

        UpdateAllUI();
    }

    /*-------------------------------- Час ------------------------------------------*/

    private void TickTime()
    {
        currentTime += Time.deltaTime * timeSpeed;

        if (currentTime >= 24f)
        {
            currentTime -= 24f;
            date = (date < 31) ? date + 1 : 1;
            onDateChange?.Invoke();
        }
    }

    // ДОДАНО: окремий метод для передачі часу в систему освітлення.
    private void UpdateLightingByTime()
    {
        if (lightingManager != null)
        {
            lightingManager.SetHour(currentTime);
        }
    }

    /*-------------------------------- Реєстрація підстанцій ------------------------*/

    public void RegisterSubstation(SubstationScript sub)
    {
        if (!_substations.Contains(sub))
            _substations.Add(sub);
    }

    public void UnregisterSubstation(SubstationScript sub)
    {
        _substations.Remove(sub);
    }

    /*-------------------------------- Економіка ------------------------------------*/

    public void AddMoney(float amount) => moneyBalance += amount;

    /*-------------------------------- UI -------------------------------------------*/

    private void UpdateAllUI()
    {
        UpdateClockUI();
        UpdateMoneyUI();
        UpdateNetworkUI();
    }

    private void UpdateClockUI()
    {
        int h = Mathf.FloorToInt(currentTime);
        int m = Mathf.FloorToInt((currentTime - h) * 60f);

        timeText.text = $"Час: {h:00}:{m:00}";
        dateText.text = $"{date}";
    }

    private void UpdateMoneyUI()
    {
        moneyText.text = $"Бюджет: {moneyBalance:F0} $";
    }

    private void UpdateNetworkUI()
    {
        float totalGen = 0f;
        float totalDem = 0f;
        bool anyBlackout = false;

        foreach (var sub in _substations)
        {
            totalGen += sub.TotalGeneration;
            totalDem += sub.TotalDemand;
            anyBlackout |= sub.IsBlackout;
        }

        if (totalGenerationText != null)
            totalGenerationText.text = $"Генерація: {totalGen:F1} кВт";

        if (totalDemandText != null)
            totalDemandText.text = $"Попит: {totalDem:F1} кВт";

        if (networkLoadText != null)
        {
            if (anyBlackout)
            {
                networkLoadText.text = "АВАРІЯ В МЕРЕЖІ!";
                networkLoadText.color = Color.red;
            }
            else if (BlackoutUI.Instance != null && BlackoutUI.Instance.IsInGracePeriod)
            {
                networkLoadText.text = $"Імунітет: {BlackoutUI.Instance.GraceTimeRemaining:F1} с";
                networkLoadText.color = Color.yellow;
            }
            else if (totalGen > 0f)
            {
                float load = (totalDem / totalGen) * 100f;
                networkLoadText.text = $"Навантаження: {load:F0}%";
                networkLoadText.color = load >= 90f ? Color.red : Color.white;
            }
            else
            {
                networkLoadText.text = "Генерація відсутня";
                networkLoadText.color = Color.grey;
            }
        }
    }

    /*-------------------------------- Аналітика для Графіка ------------------*/

    public List<Vector2> GetGraphData()
    {
        var dataPoints = new List<Vector2>();
        float step = 0.5f;

        for (float t = 0; t <= 24f; t += step)
        {
            float totalDemandAtTime = 0f;

            foreach (var c in consumers)
                totalDemandAtTime += c.GetExpectedDemand(t);

            dataPoints.Add(new Vector2(t, totalDemandAtTime));
        }

        return dataPoints;
    }

    public string GetBlackoutForecast()
    {
        float currentGen = 0f;

        foreach (var p in producers)
            currentGen += p.ProduceEnergy();

        if (currentGen <= 0)
            return "Генерація відсутня";

        for (float offset = 0; offset <= 24f; offset += 0.25f)
        {
            float checkTime = (currentTime + offset) % 24f;
            float totalDemand = 0f;

            foreach (var c in consumers)
                totalDemand += c.GetExpectedDemand(checkTime);

            if (totalDemand > currentGen)
            {
                if (offset == 0)
                    return "Блекаут вже почався!";

                int clockH = Mathf.FloorToInt(checkTime);
                int clockM = Mathf.FloorToInt((checkTime - clockH) * 60);
                int waitH = Mathf.FloorToInt(offset);
                int waitM = Mathf.FloorToInt((offset - waitH) * 60);
                float realSeconds = offset / timeSpeed;

                return $"Аварія о {clockH:00}:{clockM:00}\n(через {waitH}г {waitM}хв / {realSeconds:F1} сек)";
            }
        }

        return "Система стабільна";
    }

    public void UpdateForecastUI()
    {
        if (forecastText != null)
            forecastText.text = GetBlackoutForecast();
    }
}