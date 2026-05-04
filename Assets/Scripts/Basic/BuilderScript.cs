using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


/// Відповідальність: обробка вводу гравця для будівництва та продажу.
/// В режимі CableMode повністю делегує обробку CableController.
/// Не містить логіки енергії — тільки спавн/деспавн префабів
/// і реєстрація в GameManager.
public class BuilderScript : MonoBehaviour
{
    /*──────────────────────── Enum ─────────────────────────────*/

    private enum BuildMode
    {
        None         = 0,
        CableMode    = 1, 
        Substation   = 2,
        House        = 3,
        BasicStation = 4,
        SolarPanel   = 5,
        Factory      = 6,
    }

    /*──────────────────────── Inspector ────────────────────────*/

    [Header("Префаби будівель")]
    [SerializeField] private GameObject _housePrefab;
    [SerializeField] private GameObject _basicStationPrefab;
    [SerializeField] private GameObject _solarPanelPrefab;
    [SerializeField] private GameObject _factoryPrefab;
    [SerializeField] private GameObject _substationPrefab;

    [Header("Сцена")]
    [SerializeField] private Grid _grid;

    [Header("Система кабелів")]
    [Tooltip("GameObject з компонентом CableController")]
    [SerializeField] private CableController _cableController;

    [Header("Баланс")]
    [SerializeField] private float _sellRefundMultiplier = 0.8f;

    /*──────────────────────── Вартість будівель ────────────────*/

   // Вартість будівництва для кожного типу об'єкта.
    private static readonly Dictionary<BuildMode, float> BuildCosts
        = new Dictionary<BuildMode, float>
    {
        { BuildMode.House,        100f  },
        { BuildMode.BasicStation, 500f  },
        { BuildMode.SolarPanel,   400f  },
        { BuildMode.Factory,      1000f },
        { BuildMode.Substation,   800f  },
    };

    /*──────────────────────── Data ─────────────────────────────*/

    // Словник побудованих об'єктів.
    // Key: координата клітинки, Value: (об'єкт, вартість будівництва).
    // Вартість потрібна для розрахунку відшкодування при продажу.
    private readonly Dictionary<Vector3Int, (GameObject obj, float cost)> _builtBuildings
        = new Dictionary<Vector3Int, (GameObject obj, float cost)>();

    private BuildMode _currentMode = BuildMode.None;
    private Camera    _mainCamera;

    /*──────────────────────── Realization  ───────────────────────*/

    private void Start()
    {
        _mainCamera = Camera.main;
        Debug.Log("[Builder] Старт.");
    }

    private void Update()
    {
        if (_mainCamera == null) return;

        // Блок кліки по UI
        if (EventSystem.current.IsPointerOverGameObject()) return;

        // Режим кабелів — CableController обробляє все самостійно
        if (_currentMode == BuildMode.CableMode)
        {
            _cableController.HandleUpdate();
            return;
        }

        // Беремо позицію миші в світі та конвертуємо в координати клітинки
        Vector3 mouseWorld = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        Vector3Int cellPos = _grid.WorldToCell(mouseWorld);

        // ЛКМ — будівництво, ПКМ — продаж
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log($"[Builder] LMB: будуємо {_currentMode} на {cellPos}");
            Build(cellPos);
        }

        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log($"[Builder] RMB: продаємо на {cellPos}");
            Sell(cellPos);
        }
    }

    /*──────────────────────── Build / Sell ─────────────────────*/

    private void Build(Vector3Int cellPos)
    {
        // Перевірка: клітинка вже зайнята
        if (_builtBuildings.ContainsKey(cellPos))
        {
            Debug.Log("[Builder] Клітинка вже зайнята.");
            return;
        }

        // Визнач який префаб будуємо
        GameObject prefab = GetPrefabForMode(_currentMode);
        if (prefab == null)
        {
            Debug.Log("[Builder] Оберіть тип будівлі.");
            return;
        }

        // Перевірка балансу
        float cost = BuildCosts.GetValueOrDefault(_currentMode, 0f);
        if (GameManager.Instance.moneyBalance < cost)
        {
            Debug.Log("[Builder] Недостатньо коштів.");
            return;
        }

        // Будуємо, списуємо гроші, реєструємо в системах
        GameManager.Instance.AddMoney(-cost);
        Vector3    worldPos = _grid.GetCellCenterWorld(cellPos);
        GameObject obj      = Instantiate(prefab, worldPos, Quaternion.identity);

        _builtBuildings.Add(cellPos, (obj, cost));
        RegisterToGameSystems(obj);

        Debug.Log($"[Builder] Побудовано: {prefab.name} на {cellPos}");
    }

    private void Sell(Vector3Int cellPos)
    {
        if (!_builtBuildings.TryGetValue(cellPos, out var entry)) return;

        float refund = entry.cost * _sellRefundMultiplier;
        GameManager.Instance.AddMoney(refund);

        UnregisterFromGameSystems(entry.obj);
        Destroy(entry.obj);
        _builtBuildings.Remove(cellPos);

        Debug.Log($"[Builder] Продано на {cellPos}. Повернуто: {refund:F0}$");
    }

    /*──────────────────────── Реєстрація ───────────────────────*/

    
    // Підстанція реєструється в GameManager окремо — через RegisterSubstation().
    // Всі інші об'єкти додаються до старих списків consumers/producers
    // для сумісності з GraphScript і GetBlackoutForecast().
    private void RegisterToGameSystems(GameObject obj)
    {
        if (obj.TryGetComponent<SubstationScript>(out var sub))
        {
            GameManager.Instance.RegisterSubstation(sub);
            return;
        }

        if (obj.TryGetComponent<IEnergyConsumer>(out var consumer))
            GameManager.Instance.consumers.Add(consumer);

        if (obj.TryGetComponent<IEnergyProducer>(out var producer))
            GameManager.Instance.producers.Add(producer);
    }

    private void UnregisterFromGameSystems(GameObject obj)
    {
        if (obj.TryGetComponent<SubstationScript>(out var sub))
        {
            GameManager.Instance.UnregisterSubstation(sub);
            return;
        }

        if (obj.TryGetComponent<IEnergyConsumer>(out var consumer))
            GameManager.Instance.consumers.Remove(consumer);

        if (obj.TryGetComponent<IEnergyProducer>(out var producer))
            GameManager.Instance.producers.Remove(producer);
    }

    /*──────────────────────── Helpers ──────────────────────────*/

    // Визначає який префаб спавнити для поточного режиму.
    private GameObject GetPrefabForMode(BuildMode mode) => mode switch
    {
        BuildMode.House        => _housePrefab,
        BuildMode.BasicStation => _basicStationPrefab,
        BuildMode.SolarPanel   => _solarPanelPrefab,
        BuildMode.Factory      => _factoryPrefab,
        BuildMode.Substation   => _substationPrefab,
        _                      => null
    };

    /*──────────────────────── UI Callback ──────────────────────*/


    /// Викликається Dropdown у UI.
    /// Індекс відповідає значенню enum BuildMode.
    /// При перемиканні в/з CableMode — сповіщає CableController.

    public void OnDropdownValueChanged(int selectedIndex)
    {
        BuildMode newMode = (BuildMode)selectedIndex;

        //  CableMode викл якщо переходимо на інший режим
        if (_currentMode == BuildMode.CableMode && newMode != BuildMode.CableMode)
            _cableController.SetActive(false);

        _currentMode = newMode;

        // Вкл CableMode
        if (_currentMode == BuildMode.CableMode)
            _cableController.SetActive(true);

        Debug.Log($"[Builder] Режим: {_currentMode}");
    }
}
