using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuilderScript : MonoBehaviour
{
/*--------------------------------Interface-----------------------------------------*/

    public enum BuildMode { None = 0, House = 1, SolarPanel = 2 }
    public BuildMode currentMode = BuildMode.None;

    [Header("Settings")]
    [SerializeField] private GameObject housePrefab; 
    [SerializeField] private GameObject solarPanelPrefab; 
    [SerializeField] private Grid grid; 

    [Header("Economics")]
    public float buildCost;
    public float sellRefundMultiplier = 0.8f; 

 /*-------------------------------- DATA --------------------------------*/

    private Dictionary<Vector3Int, (GameObject obj, float cost)> builtBuildings = new Dictionary<Vector3Int, (GameObject obj, float cost)>();
    private Camera mainCamera;



    /*--------------------------------Realization-----------------------------------------*/

    private void Start()
    {
        mainCamera = Camera.main;
    }
    private void Update()
    {
        if (mainCamera == null) return;
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return; 
        }
      
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

       
        Vector3Int cellPos = grid.WorldToCell(mousePos);

      
        if (Input.GetMouseButtonDown(0))
        {
           
            Build(cellPos);
           
        }

       
        if (Input.GetMouseButtonDown(1))
        {
            Sell(cellPos);
        }
    }

    private void Build(Vector3Int cellpos)
    {
        if (builtBuildings.ContainsKey(cellpos))
        {
            Debug.Log("Already built here!");
            return;
        }

        GameObject prefabToBuild = null;
        if (currentMode == BuildMode.House)
        {
            buildCost = 100f;
            prefabToBuild = housePrefab;
        }
        else if (currentMode == BuildMode.SolarPanel)
        {
            buildCost = 200f;
            prefabToBuild = solarPanelPrefab;
        }
        else
        {
            Debug.Log("Select a building type!");
            return;
        }

        if (GameManager.Instance.moneyBalance < buildCost)
        {
            Debug.Log("no money:(");
            return;
        }

        GameManager.Instance.AddMoney(-buildCost);
        Vector3 worldPos = grid.GetCellCenterWorld(cellpos);
        GameObject obj = Instantiate(prefabToBuild, worldPos, Quaternion.identity); 

        builtBuildings.Add(cellpos, (obj, buildCost));
        
        RegisterToGameSystems(obj);
              
    }

    private void Sell(Vector3Int cellpos)
    {
        if (builtBuildings.ContainsKey(cellpos))
        {
            var (obj, cost) = builtBuildings[cellpos];
         
            float refund = cost * sellRefundMultiplier;
            GameManager.Instance.AddMoney(refund);

            UnregisterFromGameSystems(obj);

            Destroy(obj);

            builtBuildings.Remove(cellpos);
        }
    }

    private void RegisterToGameSystems(GameObject obj)
    {
        // якщо об'єкт може СПОЖ енергію
        if (obj.TryGetComponent<IEnergyConsumer>(out var consumer))
        {
            GameManager.Instance.consumers.Add(consumer);
        }

        // якщо об'єкт може ГЕН енергію
        if (obj.TryGetComponent<IEnergyProducer>(out var producer))
        {
            GameManager.Instance.producers.Add(producer);
        }
    }

    private void UnregisterFromGameSystems(GameObject obj)
    {
        // прибираємо зі списку споживачів
        if (obj.TryGetComponent<IEnergyConsumer>(out var consumer))
        {
            GameManager.Instance.consumers.Remove(consumer);
        }

        // прибираємо зі списку генераторів
        if (obj.TryGetComponent<IEnergyProducer>(out var producer))
        {
            GameManager.Instance.producers.Remove(producer);
        }
    }

    public void OnDropdownValueChanged(int selectedIndex)
    {
        currentMode = (BuildMode)selectedIndex;
    }
}