using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PotionUIManager : MonoBehaviour
{
    public PotionInventoryManager inventory;
    //public Transform contentRoot;
    //public PotionPanelUI itemPrefab;

    //private Dictionary<string, PotionPanelUI> _items = new();

    [SerializeField] private TextMeshProUGUI curPotionName;
    [SerializeField] private TextMeshProUGUI curPotionDesc;
    [SerializeField] private GameObject[] curAmountPanel;

    private void OnEnable()
    {
        Invoke(nameof(SubscribeWInventory), 0.1f);
    }

    private void SubscribeWInventory()
    {
        inventory = PotionInventoryManager.Instance;

        //inventory.OnInventoryChanged += Rebuild;
        inventory.OnPotionCountChanged += OnCountChanged;
        inventory.OnSelectedPotionChanged += OnSelectedChanged;
        //Rebuild();
    }

    //private void Rebuild()
    //{
    //    foreach (Transform c in contentRoot)
    //        Destroy(c.gameObject);
    //    _items.Clear();

    //    foreach (var kvp in inventory.GetAllCounts())
    //    {
    //        var item = Instantiate(itemPrefab, contentRoot);
    //        item.Bind(inventory, kvp.Key, kvp.Value);
    //        _items[kvp.Key] = item;
    //    }

    //    OnSelectedChanged(inventory.SelectedPotionId);
    //}

    private void OnCountChanged(string potionId, int count)
    {
        //if (_items.TryGetValue(potionId, out var item))
        //    item.SetCount(count);
        if (potionId == inventory.SelectedPotionId)
            SetCountVisual(count);
    }

    private void OnSelectedChanged(string potionId, int count)
    {
        //foreach (var kv in _items)
        //    kv.Value.SetSelected(kv.Key == potionId);
        PotionSO curPotion = inventory.GetPotionDef(potionId);
        curPotionName.text = curPotion != null ? curPotion.displayName : "None";
        curPotionDesc.text = curPotion != null ? curPotion.description : "No Potion Selected";

        SetCountVisual(count);
    }

    private void SetCountVisual(int count) { 
        for (int i = 0; i < curAmountPanel.Length; i++) {
            curAmountPanel[i].SetActive(i < count);
        }
    }
}
