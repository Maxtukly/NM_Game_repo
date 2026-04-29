using UnityEngine;
using TMPro; // Обов'язково для TextMeshPro

public class MoneyPopup : MonoBehaviour
{
    public float moveSpeed = 1.5f; 
    public float fadeSpeed = 1f;  
    public float lifeTime = 2f;    
    private TextMeshPro textMesh;
    private Color textColor;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    public void Setup(float amount)
    {
        if (amount >= 0)
        {
            textMesh.text = $"+{amount:F1}$";
            textMesh.color = Color.green;
        }        
        textColor = textMesh.color;

        Destroy(gameObject, lifeTime); 
    }

    private void Update()
    {
        // 1. Рухаємо об'єкт вгору
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // 2. Зменшуємо альфа-канал (прозорість)
        textColor.a -= fadeSpeed * Time.deltaTime;
        textMesh.color = textColor;
    }
}