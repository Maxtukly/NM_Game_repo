using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance;
    public SubstationScript SelectedSubstation; // Посилання на вибрану зараз підстанцію

    private void Awake() => Instance = this;

    // Метод для кнопки на сцені
    public void CallManualRestoreOnSelected()
    {
        if (SelectedSubstation != null)
        {
            SelectedSubstation.ManualRestore();
        }
        else
        {
            Debug.LogWarning("Жодна підстанція не вибрана!");
        }
    }
}
