using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    [SerializeField] float chanceOfRain = 0.3f; // 30% шанс на дощ
    [SerializeField] ParticleSystem rainEffect; // Ефект дощу
    public float SolarEnergyModifier { get; private set; } = 1f; // Модифікатор для сонячної енергії (1 - нормальна погода, <1 - дощ)

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    void Start()
    {
        GameManager.Instance.onDateChange.AddListener(OnDateChange);
    }

    private void OnDateChange()
    {
        if(Random.value < chanceOfRain)
        {
            Debug.Log("Сьогодні йде дощ!");
            SolarEnergyModifier = 0.1f;
            if (rainEffect != null)
            {
                rainEffect.Play();
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
        }
    }
}
