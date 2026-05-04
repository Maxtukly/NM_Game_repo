using UnityEngine;


/// Відповідальність: логіка блекауту підстанції.
/// Слідкує за таймером дефіциту, тригерить блекаут, чекає на ручне відновлення гравцем.
/// Не знає про UI, енергію чи слоти — тільки стани та таймери.
public class BlackoutController
{

    private readonly float _deficitDuration; // секунд стійкого дефіциту до блекауту
    
    // Внутрішній таймер, який накопичує час дефіциту. Скидається при відновленні або зникненні дефіциту.
    private float _deficitTimer = 0f;

    public bool IsBlackout { get; private set; } = false;

    public BlackoutController(float deficitDuration) // Конструктор приймає тривалість дефіциту до блекауту
    {
        _deficitDuration = deficitDuration;
    }


    // Викликається щокадру з SubstationScript.
    // Якщо є дефіцит — накопичує таймер.
    // Якщо дефіцит зник — скидає таймер.
    // Блекаут не відновлюється автоматично — тільки через ManualRestore().
    public void Tick(bool isDeficit)
    {
        if (IsBlackout) return; // Під час блекауту — не рахуємо таймер

        if (isDeficit)
        {
            _deficitTimer += Time.deltaTime;

            if (_deficitTimer >= _deficitDuration)
            {
                TriggerBlackout();
            }
        }
        else
        {
            // Дефіцит усунуто — скидаємо таймер (гравець встиг виправити)
            _deficitTimer = 0f;
        }
    }


    // Нормалізований прогрес таймера дефіциту [0..1].
    // Використовується UI для відображення "попереджувальної" шкали.
    public float DeficitProgress => Mathf.Clamp01(_deficitTimer / _deficitDuration);

 
    // Ручне відновлення мережі гравцем.
    // Повертає true якщо відновлення успішне (дефіциту більше немає).
    // Повертає false якщо дефіцит все ще існує — відновлення неможливе. 
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
        Debug.Log("[Blackout] Мережу відновлено!");
        return true;
    }

    private void TriggerBlackout()
    {
        IsBlackout    = true;
        _deficitTimer = 0f;
        Debug.Log("[Blackout] БЛЕКАУТ!");
    }
}
