using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Відповідальність: клас для зберігання даних гри, які потрібно серіалізувати при збереженні та завантаженні.
[Serializable] // обов'язково для збереження в JSON або бінарному форматі
public class SaveData
{
    public string saveName = "BaseSave"; // базова назва збереження
    public List<BuildingData> buildings = new List<BuildingData>();
    public List<SubstationData> substations = new List<SubstationData>();
    public float moneyBalance;
    public float currentTime;
    public int date;

}

// Дані обнієї будівлі для збереження 
[Serializable]
public class BuildingData
{
    public int buildingType;
    public int cellX;     // Vector3Int не серіалізується JsonUtility
    public int cellY;
    public int cellZ;
}

// 
[Serializable]
public class SubstationData
{
    public int cellX;    
    public int cellY;
    public int cellZ;

    public List<ConnectionData> connections = new List<ConnectionData>();
}

// Дані одного підключеного об'єкта для збереження
[Serializable]
public class ConnectionData
{
     public int x, y, z;

    public ConnectionData(Vector3Int v) { x = v.x; y = v.y; z = v.z; }
    public Vector3Int ToVector3Int() => new Vector3Int(x, y, z);
}


