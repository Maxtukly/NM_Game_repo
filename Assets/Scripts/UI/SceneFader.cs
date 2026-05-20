using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SceneFader : MonoBehaviour
{
    [SerializeField] private Image _fadeOverlay;
    [SerializeField] private float _fadeDuration = 1.0f;

    private void Start()
    {
        if (_fadeOverlay != null)
        {
            _fadeOverlay.color = new Color(0, 0, 0, 1);
            _fadeOverlay.raycastTarget = true; 
            
            StartCoroutine(FadeIn());
        }
    }

    private IEnumerator FadeIn()
    {
        _fadeOverlay.gameObject.SetActive(true);
        float elapsedTime = 0f;

        while (elapsedTime < _fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / _fadeDuration;
            
            _fadeOverlay.color = new Color(0, 0, 0, 1f - t);
            
            yield return null;
        }

        _fadeOverlay.color = new Color(0, 0, 0, 0);
        _fadeOverlay.raycastTarget = false; 
        _fadeOverlay.gameObject.SetActive(false);
    }
}