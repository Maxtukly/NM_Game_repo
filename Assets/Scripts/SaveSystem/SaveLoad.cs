using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Відповідальність: запис і читання файлу збереження.
// Використовує JsonUtility — вбудований в Unity, не потребує пакетів.
// Файл зберігається в Application.persistentDataPath (різний на кожній ОС).
public class SaveLoad : MonoBehaviour
{
    public static SaveLoad Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Grid          _grid;
    [SerializeField] private BuilderScript _builder;
     [SerializeField] private SaveSlotPanel _saveSlotPanel;

     /*──────────────── Realization ──────────────────*/
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

   /*──────────────── Шлях до файлу ──────────────────*/

    public string GetSavePath(int slotIndex)
        => Path.Combine(Application.persistentDataPath, $"save_slot_{slotIndex}.json");

   
    // Перевірка чи слот зайнятий.
    public bool SlotExists(int slotIndex)
        => File.Exists(GetSavePath(slotIndex));


    /// Зчитує метадані слоту без повного завантаження.
    /// Повертає null якщо слот порожній.
    
    public SaveData ReadSlotMeta(int slotIndex)
    {
        string path = GetSavePath(slotIndex);
        if (!File.Exists(path)) return null;

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<SaveData>(json);
    }

    /*──────────────── UI Entrypoints ─────────────────*/

    
    // Викликається кнопкою "Зберегти"
    // Відкриває панель вибору слоту в режимі Save.
    public void OpenSavePanel()
        => _saveSlotPanel.Open(SaveSlotPanel.PanelMode.Save);

   
    // Викликається кнопкою "Завантажити"
    // Відкриває панель вибору слоту в режимі Load.
    public void OpenLoadPanel()
        => _saveSlotPanel.Open(SaveSlotPanel.PanelMode.Load);

    /*──────────────── SAVE ───────────────────────────*/

    // Збирає стан сцени і записує в слот.
    // Викликається з SaveSlotPanel після підтвердження.
    public void Save(int slotIndex, string saveName)
    {
        SaveData data = CollectSaveData(saveName);

        string json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(GetSavePath(slotIndex), json);

        Debug.Log($"[SaveSystem] ✅ Збережено в слот {slotIndex} під назвою '{saveName}'");
    }

    private SaveData CollectSaveData(string saveName)
    {
        SaveData data = new SaveData
        {
            saveName     = saveName,
            moneyBalance = GameManager.Instance.moneyBalance,
            currentTime  = GameManager.Instance.currentTime,
            date         = GameManager.Instance.date
        };

        // Зберігаємо будівлі
        foreach (var pair in _builder.GetBuiltBuildings())
        {
            data.buildings.Add(new BuildingData
            {
                buildingType = pair.Value.buildingType,
                cellX        = pair.Key.x,
                cellY        = pair.Key.y,
                cellZ        = pair.Key.z
            });
        }

        // Зберігаємо з'єднання підстанцій
        var substations = FindObjectsOfType<SubstationScript>();
        foreach (var sub in substations)
        {
            Vector3Int subCell = _grid.WorldToCell(sub.transform.position);
            var subData = new SubstationData
            {
                cellX = subCell.x,
                cellY = subCell.y,
                cellZ = subCell.z
            };

            foreach (var coord in sub.GetConnectedCoordinatesForSave(_grid))
                subData.connections.Add(new ConnectionData(coord));

            data.substations.Add(subData);
        }

        return data;
    }

    /*──────────────── LOAD ───────────────────────────*/

    // Відновлює стан сцени зі слоту.
    // Викликається з SaveSlotPanel після кліку на зайнятий слот в режимі Load.
    public void Load(int slotIndex)
    {
        string path = GetSavePath(slotIndex);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[SaveSystem] Слот {slotIndex} порожній.");
            return;
        }

        string       json = File.ReadAllText(path);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        // 1. Очищаємо сцену перед завантаженням
        _builder.ClearAllBuildings();

        // 2. Відновлюємо економіку і час
        GameManager.Instance.moneyBalance = data.moneyBalance;
        GameManager.Instance.currentTime  = data.currentTime;
        GameManager.Instance.date         = data.date;

        // 3. Відновлюємо будівлі
        foreach (var bData in data.buildings)
        {
            Vector3Int cell = new Vector3Int(bData.cellX, bData.cellY, bData.cellZ);
            _builder.BuildFromSave(cell, bData.buildingType);
        }

        // 4. Відновлюємо з'єднання підстанцій
        RestoreConnections(data.substations);

        Debug.Log($"[SaveSystem] Завантажено слот {slotIndex}: '{data.saveName}'");
    }

    private void RestoreConnections(List<SubstationData> substationsData)
    {
        // Будуємо словник координата → IEnergyObject — O(n)
        var cellMap = new Dictionary<Vector3Int, IEnergyObject>();
        foreach (var mb in FindObjectsOfType<MonoBehaviour>())
        {
            if (mb is not IEnergyObject obj) continue;
            Vector3Int cell = _grid.WorldToCell(mb.transform.position);
            if (!cellMap.ContainsKey(cell))
                cellMap[cell] = obj;
        }

        foreach (var subData in substationsData)
        {
            var subCell = new Vector3Int(subData.cellX, subData.cellY, subData.cellZ);
            if (!cellMap.TryGetValue(subCell, out var subObj))    continue;
            if (subObj is not SubstationScript substation)        continue;

            foreach (var coord in subData.connections)
            {
                var nodeCell = coord.ToVector3Int();
                if (cellMap.TryGetValue(nodeCell, out var nodeObj))
                    substation.TryConnect(nodeObj);
            }
        }
    }
}

