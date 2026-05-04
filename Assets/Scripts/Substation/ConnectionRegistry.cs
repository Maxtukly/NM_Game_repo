using System.Collections.Generic;
using UnityEngine;


// Відповідальність: зберігання та управління списком підключених вузлів.
// Знає лише про слоти та колекцію — не знає про енергію чи блекаут.
 
public class ConnectionRegistry
{
    private readonly int _maxSlots;
    private readonly List<IEnergyObject> _connected = new List<IEnergyObject>();

    // Публічний read-only доступ для інших блоків підстанції
    public IReadOnlyList<IEnergyObject> Connected => _connected;
    public int UsedSlots => _connected.Count;
    public bool HasFreeSlot => _connected.Count < _maxSlots;

    public ConnectionRegistry(int maxSlots)
    {
        _maxSlots = maxSlots;
    }


    // Додає вузол до реєстру. Повертає false якщо слоти заповнені
    // або вузол вже зареєстрований — O(n) перевірка на дублікати.
    public bool TryAdd(IEnergyObject obj)
    {
        if (!HasFreeSlot)
        {
            Debug.LogWarning("[ConnectionRegistry] Всі слоти зайняті!");
            return false;
        }
        if (_connected.Contains(obj))
        {
            Debug.LogWarning("[ConnectionRegistry] Об'єкт вже підключено!");
            return false;
        }

        _connected.Add(obj);
        obj.IsConnected = true;
        return true;
    }


    // Видаляє вузол з реєстру. Повертає false якщо вузол не знайдено — O(n).
    public bool TryRemove(IEnergyObject obj)
    {
        if (!_connected.Remove(obj)) return false;

        obj.IsConnected = false;
        return true;
    }


    // Повертає координати всіх підключених об'єктів для серіалізації Save/Load.
    // GridManager використовує ці координати щоб відновити посилання після завантаження.
    public List<Vector3Int> GetCoordinatesForSave(Grid grid)
    {
        var result = new List<Vector3Int>();
        foreach (var obj in _connected)
            result.Add(grid.WorldToCell(obj.GameObject.transform.position));
        return result;
    }


    /// Очищає реєстр і знімає IsConnected з усіх вузлів (якщо продати підстанцію).
    public void Clear()
    {
        foreach (var obj in _connected)
            obj.IsConnected = false;
        _connected.Clear();
    }
}
