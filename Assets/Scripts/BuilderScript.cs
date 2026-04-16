using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuilderScript : MonoBehaviour
{
/*--------------------------------Interface-----------------------------------------*/


    public enum BuildMode { None = 0, House = 1, PowerPlant = 2 }

    [Header("Settings")]
    public HomeScript housePrefab;
    public PowerPlant powerPlantPrefab;
    public Grid grid;
    public BuildMode currentMode = BuildMode.None;

    [Header("Economics")]
    public float housePrice = 100f;
    public float powerPlantPrice = 2000f;
    public float sellRefundMultiplier = 0.8f; 


    private Dictionary<Vector3Int, HomeScript> builtHouses = new Dictionary<Vector3Int, HomeScript>();
    private Dictionary<Vector3Int, PowerPlant> builtPlants = new Dictionary<Vector3Int, PowerPlant>();

/*--------------------------------Realization-----------------------------------------*/




    void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return; 
        }
        Vector3 mousepos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousepos.z = 0; 
    
        Vector3Int cellpos = grid.WorldToCell(mousepos);

        if (Input.GetMouseButtonDown(0))
        {
            if (currentMode == BuildMode.House) BuildHouse(cellpos);
            else if (currentMode == BuildMode.PowerPlant) BuildPowerPlant(cellpos);
        }

        if (Input.GetMouseButtonDown(1))
        {
            SellBuilding(cellpos);
        }
    }

    private void BuildHouse(Vector3Int cellpos)
    {
        if (!builtHouses.ContainsKey(cellpos) && GameManager.Instance.moneyBalance >= housePrice)
        {
            GameManager.Instance.AddMoney(-housePrice);

            Vector3 worldPos = grid.GetCellCenterWorld(cellpos);

            HomeScript newHouse = Instantiate(housePrefab, worldPos, Quaternion.identity);

            builtHouses.Add(cellpos, newHouse);

            GameManager.Instance.allHouses.Add(newHouse);
        }
        else if (GameManager.Instance.moneyBalance < housePrice)
        {
            Debug.Log("no money:(");
        }
    }


    private void BuildPowerPlant(Vector3Int cellpos)
    {
        if (!IsCellOccupied(cellpos) && GameManager.Instance.moneyBalance >= powerPlantPrice)
        {
            GameManager.Instance.AddMoney(-powerPlantPrice);
            Vector3 worldPos = grid.GetCellCenterWorld(cellpos);
            
            PowerPlant newPlant = Instantiate(powerPlantPrefab, worldPos, Quaternion.identity);
            builtPlants.Add(cellpos, newPlant);
            GameManager.Instance.allPowerPlants.Add(newPlant);
        }
    }


    private void SellBuilding(Vector3Int cellpos)
    {
       if (builtHouses.ContainsKey(cellpos))
        {
            HomeScript house = builtHouses[cellpos];
            GameManager.Instance.AddMoney(housePrice * sellRefundMultiplier);
            GameManager.Instance.allHouses.Remove(house);
            Destroy(house.gameObject);
            builtHouses.Remove(cellpos);
        }
        else if (builtPlants.ContainsKey(cellpos))
        {
            PowerPlant plant = builtPlants[cellpos];
            GameManager.Instance.AddMoney(powerPlantPrice * sellRefundMultiplier);
            GameManager.Instance.allPowerPlants.Remove(plant);
            Destroy(plant.gameObject);
            builtPlants.Remove(cellpos);
        }
    }


    private bool IsCellOccupied(Vector3Int cellpos)
    {
        return builtHouses.ContainsKey(cellpos) || builtPlants.ContainsKey(cellpos);
    }
    public void OnDropdownValueChanged(int selectedIndex)
    {
        currentMode = (BuildMode)selectedIndex;
    }
}