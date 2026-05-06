using UnityEngine;
using TMPro;

// Singleton-слухач UI блекауту.
// Реєструє себе глобально щоб динамічно створені підстанції
// могли підписатись без Inspector-посилань.
public class BlackoutUI : MonoBehaviour
{
    public static BlackoutUI Instance { get; private set; }

    [Header("UI елементи")]
    [SerializeField] private GameObject      _blackoutPanel;
    [SerializeField] private TextMeshProUGUI _blackoutTimerText;
    [SerializeField] private TextMeshProUGUI _deficitInfoText;
  
    [Header("Grace Period")]
    [SerializeField] private float _graceDuration = 10f;

    private float _graceTimer = 0f;
    private bool  _inGrace    = false;

    public bool IsInGracePeriod => _inGrace;
    public float GraceTimeRemaining => _graceTimer;

    private void Awake()
    {
        // Реєструємо singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (_blackoutPanel != null)
            _blackoutPanel.SetActive(false);
    }

    private void Update()
    {
        if (!_inGrace) return;

        _graceTimer -= Time.deltaTime;
        
        if (_graceTimer <= 0f)
        {
            _inGrace = false;
            if (_blackoutTimerText != null)
                _blackoutTimerText.text = "";
        }
    }

    public void ShowBlackout()
    {
        if (_blackoutPanel != null)
            _blackoutPanel.SetActive(true);
           
        if (_blackoutTimerText != null)
            _blackoutTimerText.text = "БЛЕКАУТ!";

        _inGrace = false;
    }

    public void HideBlackout()
    {
        if (_blackoutPanel != null)
            _blackoutPanel.SetActive(false);

        _graceTimer = _graceDuration;
        _inGrace    = true;
    }

    public void UpdateDeficitInfo(float generation, float demand)
    {
        if (_deficitInfoText == null) return;

        float deficit = demand - generation;
        _deficitInfoText.text = $"Усуньте дефіцит: {deficit:F1} кВт\n" +
                                $"Генерація: {generation:F1} | Попит: {demand:F1}";
    }
}
