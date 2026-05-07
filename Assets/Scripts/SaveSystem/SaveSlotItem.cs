using UnityEngine;
using UnityEngine.UI;
using TMPro;


// Один рядок у списку слотів. Відображає назву, дату збереження і реагує на клік.
public class SaveSlotItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _slotNumberText;
    [SerializeField] private TextMeshProUGUI _saveNameText;
    [SerializeField] private TextMeshProUGUI _saveDateText;
    [SerializeField] private Button          _button;
    [SerializeField] private GameObject      _emptyLabel;   // "[ Порожньо ]"
    [SerializeField] private GameObject      _filledContent; // група з назвою і датою

    private int _slotIndex;
    private System.Action<int> _onClick;

    public void Setup(int slotIndex, SaveData meta, System.Action<int> onClick)
    {
        _slotIndex = slotIndex;
        _onClick   = onClick;

        _slotNumberText.text = $"Слот {slotIndex + 1}";

        bool isEmpty = meta == null;

        // Показуємо або порожній стан або дані збереження
        _emptyLabel.SetActive(isEmpty);
        _filledContent.SetActive(!isEmpty);

        if (!isEmpty)
        {
            _saveNameText.text = meta.saveName;

            int h = Mathf.FloorToInt(meta.currentTime);
            int m = Mathf.FloorToInt((meta.currentTime - h) * 60f);
            _saveDateText.text = $"День {meta.date}  •  {h:00}:{m:00}  •  ${meta.moneyBalance:F0}";
        }

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() => _onClick?.Invoke(_slotIndex));
    }
}
