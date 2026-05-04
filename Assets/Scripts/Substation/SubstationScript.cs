using System.Collections.Generic;
using UnityEngine;
using TMPro;



// SubstationScript — це центральний координаційний клас підстанції, який об'єднує всі блоки в єдину систему.

// Відповідальність: координація блоків.
// - ConnectionRegistry  → управляє списком підключених вузлів
// - EnergyBalancer      → рахує генерацію, попит, розподіляє енергію  
// - BlackoutController  → слідкує за дефіцитом і тригерить блекаут
// 
// SubstationScript сам по собі НЕ рахує ніяких цифр

public class SubstationScript : MonoBehaviour, IEnergyObject
{
    /*-------------------------------- IEnergyObject --------------------------------*/

    public GameObject GameObject => gameObject;
    public bool IsConnected { get; set; }
    public int MaxSlots => _maxSlots;
    public int UsedSlots => _registry?.UsedSlots ?? 0;

    /*-------------------------------- Inspector ------------------------------------*/

    [Header("Substation Settings")]
    [SerializeField] private int _maxSlots = 8;
    [SerializeField] private float _deficitDuration = 3f;

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer _statusIndicator;
    [SerializeField] private Color _colorNormal   = Color.green;
    [SerializeField] private Color _colorWarning  = Color.yellow;
    [SerializeField] private Color _colorBlackout = Color.red;

    /*-------------------------------- Блоки (композиція) ---------------------------*/

    private ConnectionRegistry  _registry;
    private EnergyBalancer      _balancer;
    private BlackoutController  _blackout;

    /*-------------------------------- Публічні дані для GameManager ----------------*/

    //  Поточна сумарна генерація цього острова (кВт). 
    public float TotalGeneration => _balancer.TotalGeneration;

    //  Поточний сумарний попит цього острова (кВт). 
    public float TotalDemand => _balancer.TotalDemand;

    //  Коефіцієнт покриття [0..1]. 
    public float SupplyRatio => _balancer.SupplyRatio;

    //  Стан блекауту для відображення у GameManager. 
    public bool IsBlackout => _blackout.IsBlackout;

    [Header("UI Блекаут")]
    public GameObject _blackoutPanel;
    public TextMeshProUGUI _blackoutTimerText;
    [SerializeField] private float _graceDuration = 18f;

    private float _graceTimer   = 0f;
    private bool  _wasBlackout  = false;

    //  Прогрес таймера дефіциту [0..1] для попереджувальної шкали. 
    public float DeficitProgress => _blackout.DeficitProgress;

    //  Кількість використаних слотів. 
    public int ConnectedCount => _registry.UsedSlots;

    /*-------------------------------- Realization ------------------------------------*/

    private void Awake()
    {
        // Ініціалізуємо всі блоки через конструктори (Composition Root)
        _registry = new ConnectionRegistry(_maxSlots);
        _balancer = new EnergyBalancer();
        _blackout = new BlackoutController(_deficitDuration);
    }

    private void Update()
    {
        // викликає блоки у правильному порядку

        // 1. Розраховуємо баланс і розподіляємо енергію
        _balancer.Tick(_registry.Connected, GameManager.Instance.currentTime, _blackout.IsBlackout);

        // 2. Перевіряємо дефіцит і оновлюємо стан блекауту
        _blackout.Tick(_balancer.IsDeficit);

        UpdateBlackoutUI();

        UpdateVisualStatus();

    }

    private void OnDestroy()
    {
        // Знімаємо IsConnected з усіх вузлів при знищенні підстанції
        _registry.Clear();
    }

    /*-------------------------------- Connect CableMode ---------------------------*/

    // Підключення вузлів (викликаються з CableMode при прокладці кабелів).
    public bool TryConnect(IEnergyObject obj)
    {
        bool success = _registry.TryAdd(obj);
        if (success)
            Debug.Log($"[Substation] {obj.GameObject.name} підключено. {UsedSlots}/{MaxSlots}");
        return success;
    }

    public bool TryDisconnect(IEnergyObject obj)
    {
        bool success = _registry.TryRemove(obj);
        if (success)
            Debug.Log($"[Substation] {obj.GameObject.name} відключено.");
        return success;
    }

    // Відключення вузлів якщо продати підстанцію (викликається з SellMode).
    public bool HasFreeSlot() => _registry.HasFreeSlot;

  
    // Публічний метод для кнопки UI "Відновити мережу".
    // Працює тільки якщо гравець усунув дефіцит.
    public void ManualRestore() => _blackout.TryManualRestore(_balancer.IsDeficit);

    /*-------------------------------- Save/Load Sys --------------------------------*/

    public List<Vector3Int> GetConnectedCoordinatesForSave(Grid grid)
        => _registry.GetCoordinatesForSave(grid);

    /*-------------------------------- UI --------------------------------------*/

    private void UpdateBlackoutUI()
{
    if (_blackout.IsBlackout)
    {
        // Показуємо панельку
        if (_blackoutPanel != null)
            _blackoutPanel.SetActive(true);

        // Оновлюємо текст таймера прогресу дефіциту
        if (_blackoutTimerText != null)
            _blackoutTimerText.text = $"БЛЕКАУТ!\nУсуньте дефіцит: " +
                                      $"{(_balancer.TotalDemand - _balancer.TotalGeneration):F1} кВт";

        _wasBlackout = true;
        _graceTimer  = _graceDuration; // скидаємо таймер імунітету
    }
    else
    {
        // Ховаємо панельку
        if (_blackoutPanel != null)
            _blackoutPanel.SetActive(false);

        // Імунітет після відновлення — відлічуємо
        if (_wasBlackout && _graceTimer > 0f)
        {
            _graceTimer -= Time.deltaTime;

            if (_blackoutTimerText != null)
                _blackoutTimerText.text = $"Імунітет: {_graceTimer:F1} с";

            if (_graceTimer <= 0f)
            {
                _wasBlackout = false;
                if (_blackoutTimerText != null)
                    _blackoutTimerText.text = "";
            }
        }
    }
}

    private void UpdateVisualStatus()
    {
        if (_statusIndicator == null) return;

        if (_blackout.IsBlackout)
            _statusIndicator.color = _colorBlackout;
        else if (_balancer.IsDeficit)
            _statusIndicator.color = _colorWarning;
        else
            _statusIndicator.color = _colorNormal;
    }
}
