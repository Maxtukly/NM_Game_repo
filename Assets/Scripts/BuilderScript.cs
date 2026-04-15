using System.Collections.Generic;
using UnityEngine;

public class BuilderScript : MonoBehaviour
{
/*--------------------------------Interface-----------------------------------------*/




    [Header("Settings")]
    [SerializeField] private HomeScript housePrefab; 
    [SerializeField] private Grid grid; 

    [Header("Economics")]
    public float buildCost = 100f;
    public float sellRefundMultiplier = 0.8f; 




/*--------------------------------Realization-----------------------------------------*/



    private Dictionary<Vector3Int, HomeScript> builtHouses = new Dictionary<Vector3Int, HomeScript>();

    void Update()
    {
        Vector3 mousepos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousepos.z = 0; 
    
        Vector3Int cellpos = grid.WorldToCell(mousepos);

        if (Input.GetMouseButtonDown(0))
        {
            BuildHouse(cellpos);
        }

        if (Input.GetMouseButtonDown(1))
        {
            SellHouse(cellpos);
        }
    }

    private void BuildHouse(Vector3Int cellpos)
    {
        if (!builtHouses.ContainsKey(cellpos) && GameManager.Instance.moneyBalance >= buildCost)
        {
            GameManager.Instance.AddMoney(-buildCost);

            Vector3 worldPos = grid.GetCellCenterWorld(cellpos);

            HomeScript newHouse = Instantiate(housePrefab, worldPos, Quaternion.identity);

            builtHouses.Add(cellpos, newHouse);

            GameManager.Instance.allHouses.Add(newHouse);
        }
        else if (GameManager.Instance.moneyBalance < buildCost)
        {
            Debug.Log("no money:(");
        }
    }

    private void SellHouse(Vector3Int cellpos)
    {
        if (builtHouses.ContainsKey(cellpos))
        {
            HomeScript houseToSell = builtHouses[cellpos];

            float refund = buildCost * sellRefundMultiplier;
            GameManager.Instance.AddMoney(refund);

            GameManager.Instance.allHouses.Remove(houseToSell);

            Destroy(houseToSell.gameObject);

            builtHouses.Remove(cellpos);
        }
    }
}