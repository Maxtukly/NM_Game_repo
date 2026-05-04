using UnityEngine;
using System.Collections.Generic;   


/// Відповідальність: математика енергобалансу.
/// Отримує список вузлів від ConnectionRegistry,
/// рахує генерацію, попит, коефіцієнт покриття та розподіляє енергію.
/// Не знає про блекаут чи UI — тільки цифри.
public class EnergyBalancer
{


    // Сумарна генерація всіх підключених виробників в кВт
    public float TotalGeneration { get; private set; }

    // Сумарний попит всіх підключених споживачів у кВт
    public float TotalDemand { get; private set; }


    // Коефіцієнт покриття попиту: TotalGeneration / TotalDemand.
    // Clamp01 → значення завжди в діапазоні [0..1].
    // Якщо генерація = 0, SupplyRatio = 0.
    public float SupplyRatio { get; private set; }

    // Чи існує дефіцит (попит перевищує генерацію)
    public bool IsDeficit => TotalDemand > TotalGeneration;

    /// Головний тік балансувальника. Викликається з SubstationScript.Update().

    /// Алгоритм:
    /// 1. Збираємо TotalGeneration від усіх IEnergyProducer
    /// 2. Збираємо TotalDemand від усіх IEnergyConsumer через GetExpectedDemand()
    /// 3. Рахуємо Коеф постачання (SupplyRatio = TotalGeneration / TotalDemand (0..1)) 
    /// 4. Розподіляємо енергію: кожен споживач отримує свій попит * SupplyRatio

    public void Tick(IReadOnlyList<IEnergyObject> nodes, float currentTime, bool isBlackout)
    {
        TotalGeneration = 0f;
        TotalDemand     = 0f;

        // Під час блекауту — генерація не рахується, споживачі отримують 0
        if (isBlackout)
        {
            SupplyRatio = 0f;
            return;
        }

        // Крок 1 і 2: Збір генерації та попиту
        foreach (var node in nodes)
        {
            if (node is IEnergyProducer producer)
                TotalGeneration += producer.ProduceEnergy();

            if (node is IEnergyConsumer consumer)
                TotalDemand += consumer.GetExpectedDemand(currentTime);
        }

        // Крок 3: SupplyRatio — скільки відсотків попиту ми покриваємо
        SupplyRatio = (TotalDemand > 0f)
            ? Mathf.Clamp01(TotalGeneration / TotalDemand)
            : 1f; // Якщо споживачів немає — система стабільна

        // Крок 4: Пропорційний розподіл енергії по споживачах
        float remaining = TotalGeneration;
        foreach (var node in nodes)
        {
            if (node is IEnergyConsumer consumer)
            {
                // Кожен споживач отримує рівно свою частку від загального пирога
                float allocation = consumer.GetExpectedDemand(currentTime) * SupplyRatio;
                remaining -= consumer.ConsumeEnergy(allocation);
            }
        }
    }
}
