using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BasicStation : MonoBehaviour, IEnergyProducer, IEnergyObject
{
    public GameObject GameObject => gameObject;
    public bool IsConnected { get; set; }
    public int MaxSlots => 1;
    public int UsedSlots => IsConnected ? 1 : 0;

    [System.Serializable]
    public struct StationLevel
    {
        public string levelName;       
        public float generation;       
        public float maintenanceCost;  
        public float upgradeCost;      
        public float upgradeTime;      
        public TileBase levelTile;     
    }

    [Header("Upgrade Settings")]
    [SerializeField] private StationLevel[] _levels; 
    private int _currentLevelIndex = 0;

    [Header("Upgrade Visual Effects")]
    [Tooltip("Тайл недобудови/риштування (замість префабу)")]
    [SerializeField] private TileBase _constructionTile; // НОВЕ
    [SerializeField] private GameObject _constructionSmokePrefab;  

    private bool _isUpgrading = false; 
    
    private Vector3Int _gridPosition;
    private bool _isPositionCached = false;

    public string CurrentLevelName => _levels[_currentLevelIndex].levelName;
    public bool IsMaxLevel => _currentLevelIndex >= _levels.Length - 1;
    public float NextUpgradeCost => IsMaxLevel ? 0f : _levels[_currentLevelIndex + 1].upgradeCost;
    public bool IsUpgrading => _isUpgrading;

    public void SetGridPosition(Vector3Int pos)
    {
        _gridPosition = pos;
        _isPositionCached = true;
    }

    private void Awake()
    {
        if (_levels == null || _levels.Length == 0)
        {
            _levels = new StationLevel[] {
                new StationLevel { levelName = "Базова", generation = 200f, upgradeCost = 0f, upgradeTime = 0f, levelTile = null }
            };
        }
    }

    private void Start()
    {
        UpdateStationVisuals();
    }

    public float ProduceEnergy()
    {
        if (_isUpgrading) return 0f;

        StationLevel current = _levels[_currentLevelIndex];
        float cost = current.maintenanceCost * Time.deltaTime;
        GameManager.Instance.AddMoney(-cost);

        return current.generation;
    }

    public bool TryUpgrade()
    {
        if (IsMaxLevel || _isUpgrading) return false;

        float cost = _levels[_currentLevelIndex + 1].upgradeCost;
        if (GameManager.Instance.moneyBalance < cost) return false;

        GameManager.Instance.AddMoney(-cost);
        float timeToBuild = _levels[_currentLevelIndex + 1].upgradeTime;
        StartCoroutine(UpgradeRoutine(timeToBuild));
        
        return true;
    }

    private IEnumerator UpgradeRoutine(float delay)
    {
        _isUpgrading = true;

        if (BuilderScript.Instance != null && BuilderScript.Instance.BuildingsTilemap != null)
        {
            Tilemap tmap = BuilderScript.Instance.BuildingsTilemap;
            Vector3Int cellPos = _isPositionCached ? _gridPosition : tmap.WorldToCell(transform.position);
            
            if (_constructionTile != null)
                tmap.SetTile(cellPos, _constructionTile);
            else
                tmap.SetTile(cellPos, null); 
        }
        GameObject smokeObj = null;
        if (_constructionSmokePrefab != null)
        {
            smokeObj = Instantiate(_constructionSmokePrefab, transform.position, Quaternion.identity);
        }

        if (SelectionManager.Instance.SelectedStation == this)
        {
            SelectionManager.Instance.upgradeButtonObject.SetActive(false);
        }

        yield return new WaitForSeconds(delay);

        _currentLevelIndex++;
        _isUpgrading = false;

        UpdateStationVisuals();

        if (smokeObj != null)
        {
            ParticleSystem ps = smokeObj.GetComponent<ParticleSystem>();
            if (ps != null) ps.Stop();
            Destroy(smokeObj, 3f);
        }

        if (SelectionManager.Instance.SelectedStation == this)
        {
            SelectionManager.Instance.SelectBuilding(this);
        }
    }

    private void UpdateStationVisuals()
    {
        if (BuilderScript.Instance != null && BuilderScript.Instance.BuildingsTilemap != null)
        {
            Tilemap tmap = BuilderScript.Instance.BuildingsTilemap;
            Vector3Int cellPos = _isPositionCached ? _gridPosition : tmap.WorldToCell(transform.position);
            
            if (_levels[_currentLevelIndex].levelTile != null)
            {
                tmap.SetTile(cellPos, _levels[_currentLevelIndex].levelTile);
            }
        }
    }

    private void OnMouseDown()
    {
        SelectionManager.Instance.SelectBuilding(this);
    }
}