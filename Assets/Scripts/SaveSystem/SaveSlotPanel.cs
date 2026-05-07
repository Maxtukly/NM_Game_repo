using UnityEngine;
using UnityEngine.UI;
using TMPro;


// Керує всією логікою панелі вибору слоту.
// Три стани: SlotList → NameInput (Save) або Confirm (Load/Overwrite).
public class SaveSlotPanel : MonoBehaviour
{
    public enum PanelMode { Save, Load }

    /*──────────────── Inspector ──────────────────────*/

    [Header("Головна панель")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private Transform _slotsContent;
    [SerializeField] private GameObject _slotItemPrefab;
    [SerializeField] private int _slotCount = 3;
    
    [Header("Закриття по кліку поза панеллю")]
    [SerializeField] private GameObject _backdrop;

    [Header("Субпанель: введення назви (Save)")]
    [SerializeField] private GameObject      _nameInputPanel;
    [SerializeField] private TMP_InputField  _nameInputField;
    [SerializeField] private Button          _confirmSaveButton;
    [SerializeField] private Button          _cancelNameButton;

    [Header("Субпанель: підтвердження перезапису")]
    [SerializeField] private GameObject      _overwritePanel;
    [SerializeField] private TextMeshProUGUI _overwriteInfoText;
    [SerializeField] private Button          _confirmOverwriteButton;
    [SerializeField] private Button          _cancelOverwriteButton;

    private PanelMode _mode;
    private int _selectedSlot = -1;

    /*──────────────── Realization ──────────────────────*/

    private void Start()
    {
        // Прив'язуємо кнопки підтвердження
        _confirmSaveButton.onClick.AddListener(OnConfirmSave);
        _cancelNameButton.onClick.AddListener(OnCancelSubPanel);
        _confirmOverwriteButton.onClick.AddListener(OnConfirmOverwrite);
        _cancelOverwriteButton.onClick.AddListener(OnCancelSubPanel);

        _panel.SetActive(false);
    }

    // Відкриває панель в потрібному режимі.
    // В режимі Save показує список слотів і дозволяє вибрати для збереження.
    public void Open(PanelMode mode)
    {
        _mode = mode;
        _titleText.text = mode == PanelMode.Save ? "Зберегти гру" : "Завантажити гру";

        ShowSubPanel(none: true);
        PopulateSlots();
        _panel.SetActive(true);
    }

    // Закриває панель і скидає вибір.
    public void Close()
    {
        _panel.SetActive(false);
        _selectedSlot = -1;
    }

    /*──────────────── Список слотів ─────────────────*/

    private void PopulateSlots()
    {
        foreach (Transform child in _slotsContent)
            Destroy(child.gameObject);

        for (int i = 0; i < _slotCount; i++)
        {
            SaveData meta = SaveLoad.Instance.ReadSlotMeta(i);
            GameObject   item = Instantiate(_slotItemPrefab, _slotsContent);
            int captured = i;

            item.GetComponent<SaveSlotItem>().Setup(
                slotIndex: captured,
                meta:      meta,
                onClick:   OnSlotClicked
            );
        }
    }

    /*──────────────── Логіка кліку на слот ──────────*/

    private void OnSlotClicked(int slotIndex)
    {
        _selectedSlot = slotIndex;

        if (_mode == PanelMode.Save)
            HandleSaveClick(slotIndex);
        else
            HandleLoadClick(slotIndex);
    }

    private void HandleSaveClick(int slotIndex)
    {
        if (SaveLoad.Instance.SlotExists(slotIndex))
        {
            // Слот зайнятий → показуємо попередження про перезапис
            SaveData existing = SaveLoad.Instance.ReadSlotMeta(slotIndex);
            _overwriteInfoText.text = $"Слот {slotIndex + 1} вже містить збереження:\n" +
                                      $"\"{existing.saveName}\"\n\n" +
                                      $"День {existing.date}  •  ${existing.moneyBalance:F0}\n\n" +
                                      $"Перезаписати?";
            ShowSubPanel(overwrite: true);
        }
        else
        {
            // Слот вільний → показуємо поле вводу назви
            _nameInputField.text = "BaseSave";
            ShowSubPanel(nameInput: true);
        }
    }

    // в режимі завантаження можна клікнути тільки по зайнятому слоту.
    private void HandleLoadClick(int slotIndex)
    {
        if (!SaveLoad.Instance.SlotExists(slotIndex))
        {
            Debug.Log($"[SavePanel] Слот {slotIndex + 1} порожній — завантаження неможливе.");
            return;
        }

        // Завантажуємо одразу
        SaveLoad.Instance.Load(slotIndex);
        Close();
    }

    /*──────────────── Підтвердження ─────────────────*/


    // Гравець ввів назву і натиснув "Зберегти".
    private void OnConfirmSave()
    {
        string name = string.IsNullOrWhiteSpace(_nameInputField.text)
            ? "BaseSave"
            : _nameInputField.text.Trim();

        SaveLoad.Instance.Save(_selectedSlot, name);
        PopulateSlots(); // оновлюємо список після збереження
        ShowSubPanel(none: true);
    }

    // Гравець підтвердив перезапис зайнятого слоту.
    // Відкриваємо поле вводу назви (може змінити назву).
    private void OnConfirmOverwrite()
    {
        SaveData existing = SaveLoad.Instance.ReadSlotMeta(_selectedSlot);
        _nameInputField.text  = existing?.saveName ?? "BaseSave";
        ShowSubPanel(nameInput: true);
    }

    private void OnCancelSubPanel() => ShowSubPanel(none: true);

    /*──────────────── Переключення субпанелей (підствердження перезп, введення назви, ...) ───────*/

    private void ShowSubPanel(
        bool none      = false,
        bool nameInput = false,
        bool overwrite = false)
    {
        _nameInputPanel.SetActive(nameInput);
        _overwritePanel.SetActive(overwrite);
    }
}
