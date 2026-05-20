using System.Collections; 
using UnityEngine;

public class BasicStation : MonoBehaviour, IEnergyProducer, IEnergyObject
{
    /*──────────────── IEnergyObject ────────────────*/
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
        public Sprite levelSprite;     
    }

    [Header("Upgrade Settings")]
    [SerializeField] private StationLevel[] _levels; 
    private int _currentLevelIndex = 0;

    [Header("Upgrade Visual Effects")]
    [SerializeField] private GameObject _constructionVisualPrefab; 
    [SerializeField] private GameObject _constructionSmokePrefab;  

    private SpriteRenderer _spriteRenderer;
    private bool _isUpgrading = false; 

    public string CurrentLevelName => _levels[_currentLevelIndex].levelName;
    public bool IsMaxLevel => _currentLevelIndex >= _levels.Length - 1;
    public float NextUpgradeCost => IsMaxLevel ? 0f : _levels[_currentLevelIndex + 1].upgradeCost;
    public bool IsUpgrading => _isUpgrading;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (_levels == null || _levels.Length == 0)
        {
            _levels = new StationLevel[] {
                new StationLevel { levelName = "Базова", generation = 200f, maintenanceCost = 0f, upgradeCost = 0f, upgradeTime = 0f, levelSprite = null }
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

        if (GameManager.Instance.moneyBalance < cost)
        {
            Debug.Log("[BasicStation] Недостатньо коштів для покращення.");
            return false;
        }

        GameManager.Instance.AddMoney(-cost);
        
        float timeToBuild = _levels[_currentLevelIndex + 1].upgradeTime;
        StartCoroutine(UpgradeRoutine(timeToBuild));
        
        return true;
    }

    private IEnumerator UpgradeRoutine(float delay)
    {
        _isUpgrading = true;


        if (_spriteRenderer != null) _spriteRenderer.enabled = false;


        GameObject scaffoldObj = null;
        if (_constructionVisualPrefab != null)
        {
            scaffoldObj = Instantiate(_constructionVisualPrefab, transform.position, Quaternion.identity);
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

    
        if (scaffoldObj != null) Destroy(scaffoldObj);


        if (_spriteRenderer != null)
        {
            _spriteRenderer.enabled = true;
            UpdateStationVisuals();
        }

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
        if (_spriteRenderer != null && _levels[_currentLevelIndex].levelSprite != null)
        {
            _spriteRenderer.sprite = _levels[_currentLevelIndex].levelSprite;
        }
    }

    private void OnMouseDown()
    {
        SelectionManager.Instance.SelectBuilding(this);
    }
}