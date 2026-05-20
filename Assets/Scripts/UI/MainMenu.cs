using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Обов'язково для роботи з Image

public class MainMenu : MonoBehaviour
{
    [Header("Назва ігрової сцени")]
    [SerializeField] private string _gameSceneName = "GameScene";

    [Header("Об'єкти для анімації")]
    [Tooltip("Посилання на Transform твоєї сітки (Grid), яку ти скопіював у сцену меню")]
    [SerializeField] private Transform _gridTransform;
    
    [Tooltip("Порожній об'єкт UI, в якому лежать кнопки та назва гри (щоб сховати їх)")]
    [SerializeField] private GameObject _uiContainer;

    [Tooltip("Чорне зображення на весь екран для ефекту затемнення")]
    [SerializeField] private Image _fadeOverlay;

    [Header("Налаштування масштабу сітки")]
    [Tooltip("Початковий (зменшений) масштаб сітки, коли гравець тільки зайшов у меню")]
    [SerializeField] private Vector3 _initialGridScale = new Vector3(0.2f, 0.2f, 1f);
    
    [Tooltip("Фінальний масштаб сітки в кінці польоту (наближений/величезний)")]
    [SerializeField] private Vector3 _targetGridScale = new Vector3(1.5f, 1.5f, 1f);

    [Header("Часові налаштування")]
    [Tooltip("Тривалість наближення та затемнення в секундах")]
    [SerializeField] private float _transitionDuration = 2.0f;

    private void Start()
    {
        // 1. При завантаженні меню автоматично стискаємо сітку до початкового стану
        if (_gridTransform != null)
        {
            _gridTransform.localScale = _initialGridScale;
        }

        // 2. Переконуємося, що екран повністю видимий
        if (_fadeOverlay != null)
        {
            _fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
            _fadeOverlay.raycastTarget = false; // Дозволяємо клікати по кнопках меню
        }
    }

    // Метод для кнопки "Звичайний режим"
    public void PlayRegularMode()
    {
        GameManager.ChosenSandboxMode = false;
        StartCoroutine(AnimateMenuTransition());
    }

    // Метод для кнопки "Безкінечні гроші"
    public void PlaySandboxMode()
    {
        GameManager.ChosenSandboxMode = true;
        StartCoroutine(AnimateMenuTransition());
    }

    private IEnumerator AnimateMenuTransition()
    {

        if (_uiContainer != null)
        {
            _uiContainer.SetActive(false);
        }

        if (_fadeOverlay != null)
        {
            _fadeOverlay.raycastTarget = true;
        }

        float elapsedTime = 0f;

        // 2. Головний цикл анімації
        while (elapsedTime < _transitionDuration)
        {
            _fadeOverlay.gameObject.SetActive(true);
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / _transitionDuration;


            float smoothT = Mathf.SmoothStep(0f, 1f, t);


            if (_gridTransform != null)
            {
                _gridTransform.localScale = Vector3.Lerp(_initialGridScale, _targetGridScale, smoothT);
            }


            if (_fadeOverlay != null)
            {
                _fadeOverlay.color = new Color(0f, 0f, 0f, smoothT);
            }

            yield return null; 
        }

        SceneManager.LoadScene(_gameSceneName);
    }
}