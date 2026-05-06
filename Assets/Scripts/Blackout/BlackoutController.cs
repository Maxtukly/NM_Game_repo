using UnityEngine;


/// Відповідальність: Тільки логіка блекауту — таймери та стани.
/// Не знає про UI, GameObject, Canvas — взяємодія реалізована через події (events).
public class BlackoutController : MonoBehaviour
{
    private readonly float _deficitDuration;
    private float _deficitTimer = 0f;

    public bool IsBlackout { get; private set; } = false;

    // Події — через них BlackoutUI дізнається про зміни стану
    // Підписник отримує дані але BlackoutController не знає хто підписаний
    public event System.Action             OnBlackoutStarted;   // блекаут почався
    public event System.Action             OnBlackoutRestored;  // мережу відновлено
    public event System.Action<float, float> OnDeficitChanged;  // (генерація, попит)

    public BlackoutController(float deficitDuration)
    {
        _deficitDuration = deficitDuration;
    }


    // Щокадровий тік. Накопичує таймер дефіциту або скидає його.
    // При досягненні порогу — тригерить блекаут через подію.
    public void Tick(bool isDeficit, float generation, float demand)
    {
        if (IsBlackout) return;

        if (BlackoutUI.Instance != null && BlackoutUI.Instance.IsInGracePeriod)
        {
            _deficitTimer = 0f;
            return;
        }

        if (isDeficit)
        {
            _deficitTimer += Time.deltaTime;

            // Сповіщаємо UI про поточний дефіцит щокадру
            OnDeficitChanged?.Invoke(generation, demand);

            if (_deficitTimer >= _deficitDuration)
                TriggerBlackout(generation, demand);
        }
        else
        {
            _deficitTimer = 0f;
        }
    }


    /// Нормалізований прогрес таймера [0..1] для шкали попередження.
    public float DeficitProgress => Mathf.Clamp01(_deficitTimer / _deficitDuration);

    // Ручне відновлення — тільки якщо дефіцит усунуто.
    // При успіху тригерить OnBlackoutRestored.
    public bool TryManualRestore(bool isDeficit)
    {
        if (!IsBlackout) return false;

        if (isDeficit)
        {
            Debug.LogWarning("[Blackout] Відновлення неможливе: дефіцит ще існує!");
            return false;
        }

        IsBlackout    = false;
        _deficitTimer = 0f;

        // Сповіщаємо UI що мережу відновлено
        OnBlackoutRestored?.Invoke();

        Debug.Log("[Blackout] Мережу відновлено!");
        return true;
    }

    private void TriggerBlackout(float generation, float demand)
    {
        IsBlackout    = true;
        _deficitTimer = 0f;

        // Сповіщаємо UI що блекаут почався, передаємо цифри
        OnBlackoutStarted?.Invoke();
        OnDeficitChanged?.Invoke(generation, demand);

        Debug.Log("[Blackout] БЛЕКАУТ!");
    }
}
