using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class BuilderScript : MonoBehaviour
{
    public static BuilderScript Instance { get; private set; }

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

    [Header("Tilemap (Графіка)")]
    [SerializeField] private Tilemap _buildingsTilemap;
    public Tilemap BuildingsTilemap => _buildingsTilemap; 

    [Header("Тайли будівель (Малюнки)")]
    [SerializeField] private TileBase _houseTile;
    [SerializeField] private TileBase _basicStationTile;
    [SerializeField] private TileBase _solarPanelTile;
    [SerializeField] private TileBase _factoryTile;
    [SerializeField] private TileBase _substationTile;
    [SerializeField] private TileBase _windStationTile;
    
    [Tooltip("Тайл недобудови/риштування (замість префабу)")]
    [SerializeField] private TileBase _constructionTile; 

    [Header("Префаби будівель (Невидима логіка)")]
    [SerializeField] private GameObject _housePrefab;
    [SerializeField] private GameObject _basicStationPrefab;
    [SerializeField] private GameObject _solarPanelPrefab;
    [SerializeField] private GameObject _factoryPrefab;
    [SerializeField] private GameObject _substationPrefab;
    [SerializeField] private GameObject _windStationPrefab;

    [Header("Ефекти будівництва")]
    [SerializeField] private GameObject _constructionSmokePrefab;  
    [SerializeField] private float _constructionDelay = 2.0f;      

    [Header("Сцена")]
    [SerializeField] private Grid _grid;

    [Header("Система кабелів")]
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

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _mainCamera = Camera.main;
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
            Vector2 mousePos2D = new Vector2(mouseWorld.x, mouseWorld.y);
            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
            if (hit.collider == null && SelectionManager.Instance != null)
            {
                SelectionManager.Instance.ClearSelection();
            }

            Build(cellPos);
        }

        if (Input.GetMouseButtonDown(1))
        {
            Sell(cellPos);
        }
    }

    /*──────────────────────── Build / Sell ─────────────────────*/

    private void Build(Vector3Int cellPos)
    {
        if (_builtBuildings.ContainsKey(cellPos)) return;

        GameObject prefab = GetPrefabForMode(_currentMode);
        TileBase tile = GetTileForMode(_currentMode); 

        if (prefab == null || tile == null) return;

        float cost = BuildCosts.GetValueOrDefault(_currentMode, 0f);
        if (GameManager.Instance.moneyBalance < cost) return;

        GameManager.Instance.AddMoney(-cost);
        Vector3 worldPos = _grid.GetCellCenterWorld(cellPos);

        // 1. ОДРАЗУ малюємо тайл риштування на сітці
        if (_buildingsTilemap != null && _constructionTile != null)
        {
            _buildingsTilemap.SetTile(cellPos, _constructionTile);
        }

        GameObject smokeObj = null;
        if (_constructionSmokePrefab != null)
        {
            smokeObj = Instantiate(_constructionSmokePrefab, worldPos, Quaternion.identity);
        }

        // Записуємо (null, cost), бо логічний об'єкт ще не створено
        _builtBuildings.Add(cellPos, (null, cost));

        StartCoroutine(ConstructionRoutine(cellPos, prefab, tile, smokeObj, cost));
    }

    private IEnumerator ConstructionRoutine(Vector3Int cellPos, GameObject finalPrefab, TileBase finalTile, GameObject smoke, float cost)
    {
        yield return new WaitForSeconds(_constructionDelay);

        // Перевіряємо, чи гравець не натиснув "продати" під час очікування (тоді obj залишився б null або запис зник)
        if (!_builtBuildings.TryGetValue(cellPos, out var entry) || entry.obj != null)
        {
            if (smoke != null) StopAndDestroySmoke(smoke);
            yield break;
        }

        Vector3 worldPos = _grid.GetCellCenterWorld(cellPos);
        GameObject finalObj = Instantiate(finalPrefab, worldPos, Quaternion.identity);

        if (finalObj.TryGetComponent<BasicStation>(out var station))
        {
            station.SetGridPosition(cellPos);
        }

        if (_buildingsTilemap != null)
        {
            _buildingsTilemap.SetTile(cellPos, finalTile);
        }

        _builtBuildings[cellPos] = (finalObj, cost);
        RegisterToGameSystems(finalObj);

        if (smoke != null) StopAndDestroySmoke(smoke);
    }

    private void StopAndDestroySmoke(GameObject smoke)
    {
        if (smoke.TryGetComponent<ParticleSystem>(out var ps)) ps.Stop();
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

        if (_buildingsTilemap != null)
        {
            _buildingsTilemap.SetTile(cellPos, null);
        }

        _builtBuildings.Remove(cellPos);
    }

    /*──────────────────────── Реєстрація ───────────────────────*/
    private void RegisterToGameSystems(GameObject obj)
    {
        if (obj.TryGetComponent<SubstationScript>(out var sub)) { GameManager.Instance.RegisterSubstation(sub); return; }
        if (obj.TryGetComponent<IEnergyConsumer>(out var consumer)) GameManager.Instance.consumers.Add(consumer);
        if (obj.TryGetComponent<IEnergyProducer>(out var producer)) GameManager.Instance.producers.Add(producer);
    }

    private void UnregisterFromGameSystems(GameObject obj)
    {
        if (obj.TryGetComponent<SubstationScript>(out var sub)) { GameManager.Instance.UnregisterSubstation(sub); return; }
        if (obj.TryGetComponent<IEnergyConsumer>(out var consumer)) GameManager.Instance.consumers.Remove(consumer);
        if (obj.TryGetComponent<IEnergyProducer>(out var producer)) GameManager.Instance.producers.Remove(producer);
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

    private TileBase GetTileForMode(BuildMode mode) => mode switch
    {
        BuildMode.House        => _houseTile,
        BuildMode.BasicStation => _basicStationTile,
        BuildMode.SolarPanel   => _solarPanelTile,
        BuildMode.Factory      => _factoryTile,
        BuildMode.Substation   => _substationTile,
        BuildMode.WindStation  => _windStationTile,
        _                      => null
    };

    public void OnDropdownValueChanged(int selectedIndex)
    {
        BuildMode newMode = (BuildMode)selectedIndex;
        if (_currentMode == BuildMode.CableMode && newMode != BuildMode.CableMode) _cableController.SetActive(false);
        _currentMode = newMode;
        if (_currentMode == BuildMode.CableMode) _cableController.SetActive(true);
    }
}