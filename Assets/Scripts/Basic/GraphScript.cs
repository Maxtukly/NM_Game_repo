using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GraphScript : MonoBehaviour
{
    [Header("UI")]
    public RectTransform graphContainer; 
    public GameObject pointPrefab;       
    public GameObject linePrefab;        
    public GameObject labelPrefab;       
    
    public float paddingLeft = 50f;
    public float paddingRight = 30f;
    public float paddingTop = 30f;
    public float paddingBottom = 50f;
    public float lineThickness = 2f;
    private float yMax = 1000f; 

    private List<GameObject> activeObjects = new List<GameObject>();

    public void ShowGraph()
    {
        foreach (var p in activeObjects) Destroy(p);
        activeObjects.Clear();

        List<Vector2> data = GameManager.Instance.GetGraphData();
        
        float totalWidth = graphContainer.rect.width;
        float totalHeight = graphContainer.rect.height;

        float usableWidth = totalWidth - paddingLeft - paddingRight;
        float usableHeight = totalHeight - paddingTop - paddingBottom;

        // Автоматичний масштаб по Y
        float currentMaxDemand = 10f;
        foreach (var point in data) if (point.y > currentMaxDemand) currentMaxDemand = point.y;
        yMax = currentMaxDemand * 1.2f; 


        // Вісь X (Час)
        for (int i = 0; i <= 24; i += 4)
        {
            float xNormalized = i / 24f;
            float xPos = paddingLeft + (xNormalized * usableWidth);
            
            // Текст під віссю
            CreateLabel(new Vector2(xPos, paddingBottom - 10f), $"{i}:00", new Vector2(0.5f, 1f));
            
            // Вертикальна лінія сітки (від нижнього падінга до верхнього)
            CreateLine(new Vector2(xPos, paddingBottom), new Vector2(xPos, totalHeight - paddingTop), new Color(1, 1, 1, 0.1f), 1f);
        }

        // Вісь Y (Споживання)
        int ySteps = 4;
        for (int i = 0; i <= ySteps; i++)
        {
            float yNormalized = (float)i / ySteps;
            float val = (yMax / ySteps) * i;
            float yPos = paddingBottom + (yNormalized * usableHeight);

            // Текст зліва від осі
            CreateLabel(new Vector2(paddingLeft - 10f, yPos), $"{Mathf.RoundToInt(val)}", new Vector2(1f, 0.5f));
            
            // Горизонтальна лінія сітки
            CreateLine(new Vector2(paddingLeft, yPos), new Vector2(totalWidth - paddingRight, yPos), new Color(1, 1, 1, 0.1f), 1f);
        }

        // ---  ГРАФІК ---

        List<Vector2> anchoredPoints = new List<Vector2>();
        foreach (var point in data)
        {
            float xNormalized = point.x / 24f;
            float xPos = paddingLeft + (xNormalized * usableWidth);

            float yNormalized = Mathf.Max(0f, point.y) / yMax;
            float yPos = paddingBottom + (yNormalized * usableHeight);

            anchoredPoints.Add(new Vector2(xPos, yPos));
        }

        // Лінії
        for (int i = 0; i < anchoredPoints.Count - 1; i++)
        {
            CreateLine(anchoredPoints[i], anchoredPoints[i + 1], Color.green, lineThickness);
        }

        // Точки
        foreach (var pos in anchoredPoints) CreateDot(pos);
    }

    private void CreateLabel(Vector2 anchoredPosition, string text, Vector2 pivot)
    {
        GameObject label = Instantiate(labelPrefab, graphContainer);
        activeObjects.Add(label);
        RectTransform rt = label.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPosition;
        label.GetComponent<TextMeshProUGUI>().text = text;
    }

    private void CreateLine(Vector2 startPos, Vector2 endPos, Color color, float thickness)
    {
        GameObject line = Instantiate(linePrefab, graphContainer);
        activeObjects.Add(line);
        RectTransform rt = line.GetComponent<RectTransform>();
        Vector2 dir = endPos - startPos;
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0, 0.5f);
        rt.sizeDelta = new Vector2(dir.magnitude, thickness);
        rt.anchoredPosition = startPos;
        rt.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        line.GetComponent<Image>().color = color;
    }

    private void CreateDot(Vector2 anchoredPosition)
    {
        GameObject dot = Instantiate(pointPrefab, graphContainer);
        activeObjects.Add(dot);
        RectTransform rt = dot.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
    }

    public void ToggleGraph()
    {
        bool isActive = graphContainer.gameObject.activeSelf;
        graphContainer.gameObject.SetActive(!isActive);
        if (!isActive) ShowGraph();
    }
}