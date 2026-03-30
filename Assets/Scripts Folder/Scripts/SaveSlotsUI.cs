using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveSlotUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private GameObject emptySlotPanel;
    [SerializeField] private GameObject dataSlotPanel;
    [SerializeField] private TMP_Text slotNumberText;
    [SerializeField] private TMP_Text zoneNameText;
    [SerializeField] private TMP_Text playTimeText;
    [SerializeField] private TMP_Text completionText;
    [SerializeField] private TMP_Text lastSaveDateText;
    [SerializeField] private Button playButton;
    [SerializeField] private Button deleteButton;

    private int slotIndex;

    public void UpdateUI(SaveData data, int index)
    {
        slotIndex = index;
        if (slotNumberText != null)
            slotNumberText.text = $"PARTIDA {index + 1}";

        if (data.isEmpty)
        {
            if (emptySlotPanel != null) emptySlotPanel.SetActive(true);
            if (dataSlotPanel  != null) dataSlotPanel.SetActive(false);
            if (deleteButton   != null) deleteButton.gameObject.SetActive(false);
        }
        else
        {
            if (emptySlotPanel != null) emptySlotPanel.SetActive(false);
            if (dataSlotPanel  != null) dataSlotPanel.SetActive(true);
            if (deleteButton   != null) deleteButton.gameObject.SetActive(true);
            if (zoneNameText   != null) zoneNameText.text = data.currentZone;
            if (completionText != null) completionText.text = $"{data.completionPercent:F1}%";
            if (lastSaveDateText != null) lastSaveDateText.text = data.lastSaveDate;

            if (playTimeText != null)
            {
                System.TimeSpan t = System.TimeSpan.FromSeconds(data.playTimeSeconds);
                playTimeText.text = $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
            }
        }
    }

    public void OnPlayClicked()   => SaveSlotManager.Instance?.OnSlotSelected(slotIndex);
    public void OnDeleteClicked() => SaveSlotManager.Instance?.OnDeleteSlot(slotIndex);
}