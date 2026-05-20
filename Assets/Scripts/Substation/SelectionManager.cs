using UnityEngine;
using TMPro; // Для доступу до тексту

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; private set; }

    [Header("Selected Objects")]
    public SubstationScript SelectedSubstation; 
    public BasicStation SelectedStation;

    [Header("Спливаючий UI")]
    public GameObject upgradeButtonObject;    
    public float floatOffsetY = 1.5f;         
    private RectTransform _upgradeButtonRect;
    public TextMeshProUGUI upgradeButtonText;
    private Camera _mainCamera;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        _mainCamera = Camera.main;
        
        if (upgradeButtonObject != null)
        {
            _upgradeButtonRect = upgradeButtonObject.GetComponent<RectTransform>();
            upgradeButtonObject.SetActive(false); // Ховаємо кнопку на старті гри
        }
    }

    private void Update()
    {
        if (upgradeButtonObject != null && upgradeButtonObject.activeSelf && SelectedStation != null)
        {

            Vector3 worldPos = SelectedStation.transform.position + (Vector3.up * floatOffsetY);
            
   
            Vector2 screenPos = _mainCamera.WorldToScreenPoint(worldPos);
            

            RectTransform parentRect = _upgradeButtonRect.parent as RectTransform;
            

            Canvas canvas = _upgradeButtonRect.GetComponentInParent<Canvas>();
            

            Camera uiCamera = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : _mainCamera;
            
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPos, uiCamera, out Vector2 localPos))
            {
                _upgradeButtonRect.anchoredPosition = localPos;
            }
        }
    }

    public void SelectSubstation(SubstationScript substation)
    {
        ClearSelection();
        SelectedSubstation = substation;
        Debug.Log($"Підстанція {substation.gameObject.name} вибрана для керування");
    }

    public void SelectBuilding(BasicStation station)
    {
        ClearSelection();
        SelectedStation = station;
        
        // ЗМІНЕНО: Показуємо кнопку, тільки якщо це не макс. рівень І станція зараз НЕ перебудовується
        if (upgradeButtonObject != null && !station.IsMaxLevel && !station.IsUpgrading)
        {
            upgradeButtonObject.SetActive(true);
            if (upgradeButtonText != null)
            {
                upgradeButtonText.text = $"{station.NextUpgradeCost}$";
            }
        }
        
        Debug.Log($"Станція {station.gameObject.name} вибрана для покращення.");
    }

    public void ClearSelection()
    {
        SelectedSubstation = null;
        SelectedStation = null;
        
        // Ховаємо кнопку, якщо вибір скинуто
        if (upgradeButtonObject != null)
        {
            upgradeButtonObject.SetActive(false);
        }
    }

    public void CallManualRestoreOnSelected()
    {
        if (SelectedSubstation != null)
            SelectedSubstation.ManualRestore();
        else
            Debug.LogWarning("Жодна підстанція не вибрана!");
    }

    public void UpgradeSelectedStation()
    {
        if (SelectedStation != null)
        {
            bool success = SelectedStation.TryUpgrade();
            
            if (success && SelectedStation.IsMaxLevel)
            {
                upgradeButtonObject.SetActive(false);
            }
        }
    }
}