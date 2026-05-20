using System.Collections; // ОБОВ'ЯЗКОВО для роботи корутин
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
        WindStation  = 7,
    }

    /*──────────────────────── Inspector ────────────────────────*/

    [Header("Префаби будівель")]
    [SerializeField] private GameObject _housePrefab;
    [SerializeField] private GameObject _basicStationPrefab;
    [SerializeField] private GameObject _solarPanelPrefab;
    [SerializeField] private GameObject _factoryPrefab;
    [SerializeField] private GameObject _substationPrefab;
    [SerializeField] private GameObject _windStationPrefab;

    [Header("Ефекти будівництва")]
    [SerializeField] private GameObject _constructionSmokePrefab;  
    [SerializeField] private GameObject _constructionVisualPrefab; 
    [SerializeField] private float _constructionDelay = 2.0f;      

    [Header("Сцена")]
    [SerializeField] private Grid _grid;

    [Header("Система кабелів")]
    [Tooltip("GameObject з компонентом CableController")]
    [SerializeField] private CableController _cableController;

    [Header("Баланс")]
    [SerializeField] private float _sellRefundMultiplier = 0.8f;

    /*──────────────────────── Вартість будівель ────────────────*/

    private static readonly Dictionary<BuildMode, float> BuildCosts
        = new Dictionary<BuildMode, float>
    {
        { BuildMode.House,        100f  },
        { BuildMode.BasicStation, 500f  },
        { BuildMode.SolarPanel,   400f  },
        { BuildMode.Factory,      1000f },
        { BuildMode.Substation,   800f  },
        { BuildMode.WindStation,  600f  },
    };

    /*──────────────────────── Data ─────────────────────────────*/

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
        if (EventSystem.current.IsPointerOverGameObject()) return;

        if (_currentMode == BuildMode.CableMode)
        {
            _cableController.HandleUpdate();
            return;
        }

        Vector3 mouseWorld = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        Vector3Int cellPos = _grid.WorldToCell(mouseWorld);

        if (Input.GetMouseButtonDown(0))
        {
            // Скидання виділення в SelectionManager, якщо клікнули на порожнє місце
            Vector2 mousePos2D = new Vector2(mouseWorld.x, mouseWorld.y);
            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
            if (hit.collider == null && SelectionManager.Instance != null)
            {
                SelectionManager.Instance.ClearSelection();
            }

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
        if (_builtBuildings.ContainsKey(cellPos))
        {
            Debug.Log("[Builder] Клітинка вже зайнята або будується.");
            return;
        }

        GameObject prefab = GetPrefabForMode(_currentMode);
        if (prefab == null)
        {
            Debug.Log("[Builder] Не обрано тип будівні.");
            return;
        }

        float cost = BuildCosts.GetValueOrDefault(_currentMode, 0f);
        if (GameManager.Instance.moneyBalance < cost)
        {
            Debug.Log("[Builder] Недостатньо коштів.");
            return;
        }

        GameManager.Instance.AddMoney(-cost);
        Vector3 worldPos = _grid.GetCellCenterWorld(cellPos);

        GameObject scaffoldObj = null;
        if (_constructionVisualPrefab != null)
        {
            scaffoldObj = Instantiate(_constructionVisualPrefab, worldPos, Quaternion.identity);
        }

        GameObject smokeObj = null;
        if (_constructionSmokePrefab != null)
        {
            smokeObj = Instantiate(_constructionSmokePrefab, worldPos, Quaternion.identity);
        }

        _builtBuildings.Add(cellPos, (scaffoldObj, cost));

        StartCoroutine(ConstructionRoutine(cellPos, prefab, scaffoldObj, smokeObj, cost));
    }

    private IEnumerator ConstructionRoutine(Vector3Int cellPos, GameObject finalPrefab, GameObject scaffold, GameObject smoke, float cost)
    {
        yield return new WaitForSeconds(_constructionDelay);

        if (!_builtBuildings.TryGetValue(cellPos, out var entry) || entry.obj != scaffold)
        {
            if (smoke != null) StopAndDestroySmoke(smoke);
            yield break;
        }

        if (scaffold != null) Destroy(scaffold);

        Vector3 worldPos = _grid.GetCellCenterWorld(cellPos);
        GameObject finalObj = Instantiate(finalPrefab, worldPos, Quaternion.identity);

        _builtBuildings[cellPos] = (finalObj, cost);
        
        RegisterToGameSystems(finalObj);

        // Зупиняємо дим
        if (smoke != null) StopAndDestroySmoke(smoke);
        
        Debug.Log($"[Builder] Побудовано: {finalPrefab.name} на {cellPos}");
    }

    private void StopAndDestroySmoke(GameObject smoke)
    {
        if (smoke.TryGetComponent<ParticleSystem>(out var ps))
        {
            ps.Stop();
        }
        Destroy(smoke, 3f);
    }

    private void Sell(Vector3Int cellPos)
    {
        if (!_builtBuildings.TryGetValue(cellPos, out var entry)) return;

        float refund = entry.cost * _sellRefundMultiplier;
        GameManager.Instance.AddMoney(refund);

        if (entry.obj != null)
        {
            if (entry.obj.GetComponent<IEnergyObject>() != null || entry.obj.GetComponent<SubstationScript>() != null)
            {
                UnregisterFromGameSystems(entry.obj);
            }
            
            Destroy(entry.obj);
        }

        _builtBuildings.Remove(cellPos);
        Debug.Log($"[Builder] Продано/Скасовано на {cellPos}. Повернуто: {refund:F0}$");
    }

    /*──────────────────────── Реєстрація ───────────────────────*/

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

    private GameObject GetPrefabForMode(BuildMode mode) => mode switch
    {
        BuildMode.House        => _housePrefab,
        BuildMode.BasicStation => _basicStationPrefab,
        BuildMode.SolarPanel   => _solarPanelPrefab,
        BuildMode.Factory      => _factoryPrefab,
        BuildMode.Substation   => _substationPrefab,
        BuildMode.WindStation  => _windStationPrefab,
        _                      => null
    };

    /*──────────────────────── UI Callback ──────────────────────*/

    public void OnDropdownValueChanged(int selectedIndex)
    {
        BuildMode newMode = (BuildMode)selectedIndex;

        if (_currentMode == BuildMode.CableMode && newMode != BuildMode.CableMode)
            _cableController.SetActive(false);

        _currentMode = newMode;

        if (_currentMode == BuildMode.CableMode)
            _cableController.SetActive(true);

        Debug.Log($"[Builder] Режим: {_currentMode}");
    }
}