using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Оркестратор енергоострова. Координує блоки та підписує
/// BlackoutUI на події BlackoutController.
/// Не містить жодної UI-логіки — тільки делегує.
/// </summary>
public class SubstationScript : MonoBehaviour, IEnergyObject
{
    /*──────────────── IEnergyObject ────────────────*/

    public GameObject GameObject => gameObject;
    public bool IsConnected { get; set; }
    public int MaxSlots  => _maxSlots;
    public int UsedSlots => _registry?.UsedSlots ?? 0;

    /*──────────────── Inspector ────────────────────*/

    [Header("Substation Settings")]
    [SerializeField] private int   _maxSlots        = 8;
    [SerializeField] private float _deficitDuration = 3f;

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer _statusIndicator;
    [SerializeField] private Color _colorNormal   = Color.green;
    [SerializeField] private Color _colorWarning  = Color.yellow;
    [SerializeField] private Color _colorBlackout = Color.red;

   
    /*──────────────── Блоки ────────────────────────*/

    private ConnectionRegistry _registry;
    private EnergyBalancer     _balancer;
    private BlackoutController _blackout;

    /*──────────────── Публічні дані ────────────────*/

    public float TotalGeneration  => _balancer.TotalGeneration;
    public float TotalDemand      => _balancer.TotalDemand;
    public float SupplyRatio      => _balancer.SupplyRatio;
    public bool  IsBlackout       => _blackout.IsBlackout;
    public float DeficitProgress  => _blackout.DeficitProgress;
    public bool  IsDeficitActive  => _balancer.IsDeficit;
    public int   ConnectedCount   => _registry.UsedSlots;

    /*──────────────── Realization ────────────────────*/

    private void Awake()
    {
        _registry = new ConnectionRegistry(_maxSlots);
        _balancer = new EnergyBalancer();
        _blackout = new BlackoutController(_deficitDuration);

        // Знаходимо UIHandler через singleton — без Inspector-посилання
        // Instance гарантовано існує бо BlackoutUI на постійному GameObject сцени
        SubscribeToUIHandler();
       
    }

    private void OnDestroy()
    {
        _registry.Clear();

        UnsubscribeFromUIHandler();
    }

    private void Update()
    {
        // Передаємо generation і demand в Tick для передачі в події
        _balancer.Tick(_registry.Connected, GameManager.Instance.currentTime, _blackout.IsBlackout);
        _blackout.Tick(_balancer.IsDeficit, _balancer.TotalGeneration, _balancer.TotalDemand);

        UpdateVisualStatus();
    }

    // Підписуємося на події BlackoutController через UIHandler, щоб делегувати показ/приховування UI та оновлення інформації
    private void SubscribeToUIHandler()
    {
        if (BlackoutUI.Instance == null)
        {
            Debug.LogWarning("[Substation] BlackoutUI.Instance не знайдено!");
            return;
        }

        _blackout.OnBlackoutStarted  += BlackoutUI.Instance.ShowBlackout;
        _blackout.OnBlackoutRestored += BlackoutUI.Instance.HideBlackout;
        _blackout.OnDeficitChanged   += BlackoutUI.Instance.UpdateDeficitInfo;
    }

    // Відписуємося від подій при знищенні, щоб уникнути потенційних помилок
    private void UnsubscribeFromUIHandler()
    {
        if (BlackoutUI.Instance == null) return;

        _blackout.OnBlackoutStarted  -= BlackoutUI.Instance.ShowBlackout;
        _blackout.OnBlackoutRestored -= BlackoutUI.Instance.HideBlackout;
        _blackout.OnDeficitChanged   -= BlackoutUI.Instance.UpdateDeficitInfo;
    }

    /*──────────────── Connection API ───────────────*/

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

    public bool HasFreeSlot() => _registry.HasFreeSlot;

    /// <summary>
    /// Публічний метод для кнопки "Відновити мережу".
    /// </summary>
    public void ManualRestore() => _blackout.TryManualRestore(_balancer.IsDeficit);

    /*──────────────── Save/Load ─────────────────────*/

    public List<Vector3Int> GetConnectedCoordinatesForSave(Grid grid)
        => _registry.GetCoordinatesForSave(grid);

    /*──────────────── Візуал ───────────────────────*/

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

    private void OnMouseDown()
    {
        SelectionManager.Instance.SelectedSubstation = this;
        Debug.Log($"Підстанція {gameObject.name} вибрана для керування");
    }
}
