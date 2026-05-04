using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


/// Контролер режиму прокладки кабелів ("Кусачки").
/// 
///   Реалізує State Machine з двома станами:
///   Idle — очікуємо вибору першого вузла
///   NodeSelected — вузол обрано, очікуємо вибору підстанції
/// 
/// Підсвічення доступних об'єктів:
///   - При активації режиму: всі IEnergyObject з вільним слотом → colorAvailable
///   - При наведенні курсора: об'єкт під курсором → colorHovered
///   - При виборі вузла: обраний вузол → colorSelected
///   - При деактивації: всі об'єкти повертають оригінальний колір
 
public class CableController : MonoBehaviour
{
    /*──────────────────────── Inspector ────────────────────────*/

    [Header("References")]
    [SerializeField] private Grid        _grid;
    [SerializeField] private Camera      _mainCamera;

    [Header("Курсор-кусачки (текстури)")]
    [Tooltip("Текстура курсора на кроці 1: обираємо джерело (сині кусачки)")]
    [SerializeField] private Texture2D _cursorStep1;

    [Tooltip("Текстура курсора на кроці 2: обираємо підстанцію (жовті кусачки)")]
    [SerializeField] private Texture2D _cursorStep2;

    [Tooltip("Точка прив'язки курсора (зазвичай центр або кінчик кусачок)")]
    [SerializeField] private Vector2 _cursorHotspot = new Vector2(8f, 8f);

    [Header("Превью кабелю")]
    [Tooltip("LineRenderer на окремому GameObject для тимчасової лінії з'єднання")]
    [SerializeField] private LineRenderer _cablePreviewLine;

    [Header("Кольори підсвічення")]
    [Tooltip("Колір вузлів що мають вільний слот і доступні для підключення")]
    [SerializeField] private Color _colorAvailable = new Color(0.4f, 0.8f, 1f, 1f);   // синій

    [Tooltip("Колір підстанцій що мають вільний слот")]
    [SerializeField] private Color _colorSubAvailable = new Color(1f, 0.9f, 0.2f, 1f); // жовтий

    [Tooltip("Колір об'єкту під курсором")]
    [SerializeField] private Color _colorHovered  = new Color(1f, 1f, 1f, 1f);         // білий

    [Tooltip("Колір обраного вузла (крок 1 виконано)")]
    [SerializeField] private Color _colorSelected = new Color(0.2f, 1f, 0.4f, 1f);    // зелений

    [Tooltip("Колір вже підключених об'єктів (слот зайнятий)")]
    [SerializeField] private Color _colorConnected = new Color(0.5f, 0.5f, 0.5f, 1f); // сірий

    /*──────────────────────── State ────────────────────────────*/

    private enum CableState { Idle, NodeSelected }
    private CableState _state = CableState.Idle;

    private IEnergyObject    _selectedNode    = null;
    private IEnergyObject    _lastHovered     = null;

    // Зберігаємо оригінальні кольори щоб коректно відновлювати
    // Key: SpriteRenderer об'єкта, Value: колір до входу в CableMode
    private readonly Dictionary<SpriteRenderer, Color> _originalColors
        = new Dictionary<SpriteRenderer, Color>();

    public bool IsActive { get; private set; } = false;


    /// Вмикає або вимикає режим кусачок.
    /// При вмиканні — підсвічує всі доступні вузли.
    /// При вимиканні — знімає всі підсвічення і відновлює курсор.
    public void SetActive(bool active)
    {
        IsActive = active;

        if (active)
        {
            EnterCableMode();
        }
        else
        {
            ExitCableMode();
        }
    }


    /// Головний метод — викликається з BuilderScript.Update()
    /// лише коли IsActive == true.
 
    public void HandleUpdate()
    {
        if (!IsActive) return;
        if (EventSystem.current.IsPointerOverGameObject()) return;

        HandleHoverHighlight();
        UpdateCablePreview();

        if (Input.GetMouseButtonDown(0)) HandleLeftClick();
        if (Input.GetMouseButtonDown(1)) HandleRightClick();
    }

    /*──────────────────────── Вхід / Вихід з режиму ────────────*/

    private void EnterCableMode()
    {
        _state        = CableState.Idle;
        _selectedNode = null;

        // Підсвічуємо всі доступні об'єкти на сцені
        HighlightAvailableObjects();

        // Встановлюємо курсор-кусачки (крок 1: вибір джерела)
        SetCursorStep1();

        if (_cablePreviewLine != null)
            _cablePreviewLine.enabled = false;

        Debug.Log("[CableController] Режим кусачок ON");
    }

    private void ExitCableMode()
    {
        // Знімаємо всі підсвічення і відновлюємо оригінальні кольори
        RestoreAllColors();
        _originalColors.Clear();

        // Відновлюємо стандартний системний курсор
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        _selectedNode = null;
        _lastHovered  = null;
        _state        = CableState.Idle;

        if (_cablePreviewLine != null)
            _cablePreviewLine.enabled = false;

        Debug.Log("[CableController] Режим кусачок OFF");
    }

    /*──────────────────────── Підсвічення ──────────────────────*/


    // При вході в CableMode знаходить всі IEnergyObject на сцені і фарбує їх залежно від стану слоту.
    // Викликається один раз при EnterCableMode — не щокадру. 
    private void HighlightAvailableObjects()
    {
        // Шукаємо всі MonoBehaviour що реалізують IEnergyObject
        var allObjects = FindObjectsOfType<MonoBehaviour>();

        foreach (var mb in allObjects)
        {
            if (mb is not IEnergyObject energyObj) continue;
            if (mb is SubstationScript)           continue; // підстанції фарбуємо окремо

            if (!mb.TryGetComponent<SpriteRenderer>(out var sr)) continue;

            // Зберігаємо оригінальний колір
            if (!_originalColors.ContainsKey(sr))
                _originalColors[sr] = sr.color;

            // Фарбуємо залежно від стану
            sr.color = energyObj.IsConnected ? _colorConnected : _colorAvailable;
        }

        // Окремо підсвічуємо підстанції
        var substations = FindObjectsOfType<SubstationScript>();
        foreach (var sub in substations)
        {
            if (!sub.TryGetComponent<SpriteRenderer>(out var sr)) continue;

            if (!_originalColors.ContainsKey(sr))
                _originalColors[sr] = sr.color;

            sr.color = sub.HasFreeSlot() ? _colorSubAvailable : _colorConnected;
        }
    }

    ///  
    /// Щокадровий hover: підсвічує об'єкт під курсором білим,
    /// попередній об'єкт повертає до кольору "доступний/підключений".
    ///  
    private void HandleHoverHighlight()
    {
        IEnergyObject hovered = RaycastEnergyObject();

        // Якщо курсор перейшов на інший об'єкт
        if (hovered != _lastHovered)
        {
            // Відновлюємо колір попереднього об'єкта
            if (_lastHovered != null)
                RestoreAvailabilityColor(_lastHovered);

            // Фарбуємо новий об'єкт під курсором
            if (hovered != null)
            {
                if (hovered.GameObject.TryGetComponent<SpriteRenderer>(out var sr))
                    sr.color = _colorHovered;
            }

            _lastHovered = hovered;
        }
    }


    // Відновлює колір конкретного об'єкта до стану "доступний/підключений/обраний".
    // Не повертає до оригінального кольору — повертає до кольору CableMode.
    private void RestoreAvailabilityColor(IEnergyObject obj)
    {
        if (!obj.GameObject.TryGetComponent<SpriteRenderer>(out var sr)) return;

        if (obj == _selectedNode)
        {
            sr.color = _colorSelected;
        }
        else if (obj is SubstationScript sub)
        {
            sr.color = sub.HasFreeSlot() ? _colorSubAvailable : _colorConnected;
        }
        else
        {
            sr.color = obj.IsConnected ? _colorConnected : _colorAvailable;
        }
    }


    // Відновлює всім об'єктам їхні оригінальні кольори (виклик при ExitCableMode).
    private void RestoreAllColors()
    {
        foreach (var pair in _originalColors)
        {
            if (pair.Key != null) // Guard: об'єкт міг бути знищений
                pair.Key.color = pair.Value;
        }
    }

    /*──────────────────────── Превью кабелю (вигляд коли прокладаєш) ────────────────────*/

   
    /// Малює тимчасову лінію від обраного вузла до позиції курсора.
    /// Активна тільки на кроці 2 (NodeSelected).
    /// Зникає після встановлення з'єднання або скасування.
    private void UpdateCablePreview()
    {
        if (_cablePreviewLine == null) return;

        if (_state != CableState.NodeSelected)
        {
            _cablePreviewLine.enabled = false;
            return;
        }

        _cablePreviewLine.enabled = true;
        _cablePreviewLine.SetPosition(0, _selectedNode.GameObject.transform.position);

        Vector3 mouseWorld = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        _cablePreviewLine.SetPosition(1, mouseWorld);
    }

    /*──────────────────────── Обробка кліків ────────────────────────────*/

    private void HandleLeftClick()
    {
        IEnergyObject hit = RaycastEnergyObject();

        // Клік у порожнє місце — скасовуємо вибір
        if (hit == null)
        {
            CancelSelection();
            return;
        }

        switch (_state)
        {
            case CableState.Idle:
                HandleIdleClick(hit);
                break;

            case CableState.NodeSelected:
                HandleNodeSelectedClick(hit);
                break;
        }
    }

    private void HandleIdleClick(IEnergyObject hit)
    {
        // На кроці 1 підстанцію обирати не можна
        if (hit is not SubstationScript)
        {
            Debug.Log("[CableMode] : Обрано генератор або споживача.");
            return;
        }
     
        SelectNode(hit);
    }

    private void HandleNodeSelectedClick(IEnergyObject hit)
    {
        if (hit is SubstationScript)
        {
            RestoreAvailabilityColor(_selectedNode);
            SelectNode(hit);
            return;
        }
        if (_selectedNode is SubstationScript substation)
        {
            ConnectToSubstation(hit, substation);
        }
        else
        {
            // Клік на інший вузол — перевибираємо
            RestoreAvailabilityColor(_selectedNode);
            SelectNode(hit);
        }
    }

    private void HandleRightClick()
    {
        IEnergyObject hit = RaycastEnergyObject();
        if (hit == null) return;

        if (hit is SubstationScript)
        {
            Debug.Log("[Cable] RMB на підстанцію: відключення через UI (TODO).");
            return;
        }

        // RMB на підключеному вузлі — відключаємо
        if (hit.IsConnected)
            DisconnectNode(hit);
    }

    /*──────────────────────── Логіка з'єднання ─────────────────*/

    private void SelectNode(IEnergyObject node)
    {
        _selectedNode = node;
        _state        = CableState.NodeSelected;

        // Фарбуємо обраний вузол у зелений
        if (node.GameObject.TryGetComponent<SpriteRenderer>(out var sr))
            sr.color = _colorSelected;

        // Перемикаємо курсор на крок 2 (жовті кусачки — тепер обираємо підстанцію)
        if (node is SubstationScript)
            SetCursorStep2(); // жовті → тепер обираємо вузол (сині)
        else
            SetCursorStep1();

         Debug.Log($"[Cable] Обрано підстанцію: {node.GameObject.name}. Тепер клікни на споживача або генератора.");
    }

    private void ConnectToSubstation(IEnergyObject node, SubstationScript substation)
    {
        if (substation.TryConnect(node))
        {
            Debug.Log($"[Cable] З'єднано: {_selectedNode.GameObject.name} → {substation.name}");

            // Оновлюємо підсвічення після з'єднання
            if (node.GameObject.TryGetComponent<SpriteRenderer>(out var sr))
                sr.color = _colorConnected;
        }

        CancelSelection();
        SetCursorStep1(); // Повертаємось до кроку 1
    }

    private void DisconnectNode(IEnergyObject node)
    {
        var substations = FindObjectsOfType<SubstationScript>();

        foreach (var sub in substations)
        {
            if (!sub.TryDisconnect(node)) continue;

            // Об'єкт знову доступний — підсвічуємо як доступний
            if (node.GameObject.TryGetComponent<SpriteRenderer>(out var sr))
                sr.color = _colorAvailable;

            // Оновлюємо колір підстанції (з'явився вільний слот)
            if (sub.TryGetComponent<SpriteRenderer>(out var subSr))
                subSr.color = _colorSubAvailable;

            Debug.Log($"[Cable] Відключено: {node.GameObject.name}");
            return;
        }

        Debug.LogWarning($"[Cable] {node.GameObject.name} не знайдено в жодній підстанції.");
    }

    private void CancelSelection()
    {
        if (_selectedNode != null)
            RestoreAvailabilityColor(_selectedNode);

        _selectedNode = null;
        _state        = CableState.Idle;
        SetCursorStep1();

        if (_cablePreviewLine != null)
            _cablePreviewLine.enabled = false;
    }

    /*──────────────────────── Raycast ──────────────────────────*/

 
    // Повертає IEnergyObject під курсором або null якщо курсор у порожньому місці.
    private IEnergyObject RaycastEnergyObject()
    {
        Vector3 mouseWorld = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        
        // Фільтруємо тільки шар "Buildings" — ізолюємо від тайлмапу
        int buildingLayer = LayerMask.GetMask("Buildings");
        Collider2D hit    = Physics2D.OverlapCircle(mouseWorld, 0.4f, buildingLayer);

        if (hit == null) return null;

        hit.TryGetComponent<IEnergyObject>(out var energyObj);
        return energyObj;
    }

    /*──────────────────────── Курсор ───────────────────────────*/

    ///  Крок 1: сині кусачки — обираємо джерело/споживача. 
    private void SetCursorStep1()
    {
        if (_cursorStep1 != null)
            Cursor.SetCursor(_cursorStep1, _cursorHotspot, CursorMode.Auto);
    }

    ///  Крок 2: жовті кусачки — обираємо підстанцію. 
    private void SetCursorStep2()
    {
        if (_cursorStep2 != null)
            Cursor.SetCursor(_cursorStep2, _cursorHotspot, CursorMode.Auto);
    }
}
