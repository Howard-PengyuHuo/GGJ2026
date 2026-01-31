using System.Collections.Generic;
using UnityEngine;

public class PotionUIManager : MonoBehaviour
{
    public PotionInventoryManager inventory;
    public Transform contentRoot;
    public PotionPanelUI itemPrefab;

    private Dictionary<string, PotionPanelUI> _items = new();

    private void OnEnable()
    {
        inventory = PotionInventoryManager.Instance;

        inventory.OnInventoryChanged += Rebuild;
        inventory.OnPotionCountChanged += OnCountChanged;
        inventory.OnSelectedPotionChanged += OnSelectedChanged;
        Rebuild();
    }

    private void Rebuild()
    {
        foreach (Transform c in contentRoot)
            Destroy(c.gameObject);
        _items.Clear();

        foreach (var kvp in inventory.GetAllCounts())
        {
            var item = Instantiate(itemPrefab, contentRoot);
            item.Bind(inventory, kvp.Key, kvp.Value);
            _items[kvp.Key] = item;
        }

        OnSelectedChanged(inventory.SelectedPotionId);
    }

    private void OnCountChanged(string potionId, int count)
    {
        if (_items.TryGetValue(potionId, out var item))
            item.SetCount(count);
    }

    private void OnSelectedChanged(string potionId)
    {
        foreach (var kv in _items)
            kv.Value.SetSelected(kv.Key == potionId);
    }
}
