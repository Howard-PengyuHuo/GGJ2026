using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PotionPanelUI : MonoBehaviour
{
    public Button button;
    public Image icon;
    public Image highlight;
    public TMP_Text nameText;
    public TMP_Text countText;

    private string _potionId;
    private PotionInventoryManager _mgr;

    public void Bind(PotionInventoryManager mgr, string potionId, int count)
    {
        _mgr = mgr;
        _potionId = potionId;

        var def = mgr.GetPotionDef(potionId);

        nameText.text = def.displayName;
        icon.sprite = def.icon;
        countText.text = count.ToString();

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => _mgr.SetSelectedPotion(_potionId));

        SetSelected(_mgr.SelectedPotionId == _potionId);
    }

    public void SetCount(int count)
    {
        countText.text = count.ToString();
        button.interactable = count > 0;
    }

    public void SetSelected(bool selected)
    {
        if (highlight) highlight.enabled = selected;
    }
}
