using UnityEngine;
using UnityEngine.UI;

public class LightingManager : MonoBehaviour
{
    [Header("Overlay")]
    public Image lightingOverlay;

    [Header("Game Time")]
    [Range(0, 24)]
    public float hour = 12f;

    [Header("Weather")]
    public bool isRaining = false;

    void Update()
    {
        UpdateLighting();
    }

    void UpdateLighting()
    {
        Color finalColor = GetTimeColor(hour);

        if (isRaining)
        {
            Color rainColor = new Color(0.55f, 0.60f, 0.65f, 70f / 255f);
            finalColor = Color.Lerp(finalColor, rainColor, 0.35f);
        }

        lightingOverlay.color = finalColor;
    }

    Color GetTimeColor(float hour)
    {
        float alpha255 = 0f;

        // 01:00–03:00 — темна-темна ніч, alpha = 150
        if (hour >= 1f && hour < 3f)
        {
            alpha255 = 150f;
        }
        // 03:00–05:00 — поступово світлішає: 150 → 120
        else if (hour >= 3f && hour < 5f)
        {
            float t = (hour - 3f) / 2f;
            alpha255 = Mathf.Lerp(150f, 120f, t);
        }
        // 05:00–07:00 — світлішає: 120 → 30
        else if (hour >= 5f && hour < 7f)
        {
            float t = (hour - 5f) / 2f;
            alpha255 = Mathf.Lerp(120f, 30f, t);
        }
        // 07:00–09:00 — повністю світлішає: 30 → 0
        else if (hour >= 7f && hour < 9f)
        {
            float t = (hour - 7f) / 2f;
            alpha255 = Mathf.Lerp(30f, 0f, t);
        }
        // 09:00–19:00 — день, alpha = 0
        else if (hour >= 9f && hour < 19f)
        {
            alpha255 = 0f;
        }
        // 19:00–21:00 — плавно темніє: 0 → 40
        else if (hour >= 19f && hour < 21f)
        {
            float t = (hour - 19f) / 2f;
            alpha255 = Mathf.Lerp(0f, 40f, t);
        }
        // 21:00–23:00 — темніє: 40 → 120
        else if (hour >= 21f && hour < 23f)
        {
            float t = (hour - 21f) / 2f;
            alpha255 = Mathf.Lerp(40f, 120f, t);
        }
        // 23:00–01:00 — темніє до глибокої ночі: 120 → 150
        else
        {
            if (hour >= 23f)
            {
                float t = (hour - 23f) / 2f;
                alpha255 = Mathf.Lerp(120f, 150f, t);
            }
            else
            {
                float t = (hour + 1f) / 2f;
                alpha255 = Mathf.Lerp(120f, 150f, t);
            }
        }

        return new Color(0f, 0f, 0f, alpha255 / 255f);
    }

    public void SetHour(float newHour)
    {
        hour = newHour;
    }

    public void SetRain(bool rain)
    {
        isRaining = rain;
    }
}