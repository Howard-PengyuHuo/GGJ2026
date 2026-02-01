using System;
using System.Collections.Generic;
using UnityEngine;

public class PotionInventoryManager : MonoBehaviour
{
    public static PotionInventoryManager Instance { get; private set; }

    [Serializable]
    public class Entry
    {
        public string potionId;
        public int amount;
    }

    [Header("Database")]
    public PotionDatabase database;

    [Header("Initial Inventory")]
    public int maxPotionAmount = 7;

    public List<Entry> initialInventory = new();

    private readonly Dictionary<string, int> _counts = new();
    private readonly Dictionary<string, PotionSO> _defs = new();

    // NEW: world potion instances mapping
    private readonly Dictionary<string, PotionBehaviour> _worldPotions = new();

    public string SelectedPotionId { get; private set; }

    // EVENTS —— 只暴露 potionId
    public event Action<string, int> OnSelectedPotionChanged;
    public event Action<string, int> OnPotionCountChanged;
    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BuildDatabase();
        BuildInventory();
    }

    private void BuildDatabase()
    {
        if (database == null)
        {
            Debug.LogError("PotionDatabase missing!");
            return;
        }

        database.Build();
        _defs.Clear();
        foreach (var p in database.All())
            _defs[p.potionId] = p;
    }

    private void BuildInventory()
    {
        _counts.Clear();
        foreach (var e in initialInventory)
        {
            if (!_defs.ContainsKey(e.potionId)) continue;
            _counts[e.potionId] = Mathf.Max(0, e.amount);
        }

        //SelectedPotionId = PickDefaultPotion();
        OnInventoryChanged?.Invoke();
        //if (!string.IsNullOrEmpty(SelectedPotionId))
        //    OnSelectedPotionChanged?.Invoke(SelectedPotionId);
    }

    //private string PickDefaultPotion()
    //{
    //    foreach (var kvp in _counts)
    //        if (kvp.Value > 0)
    //            return kvp.Key;

    //    foreach (var kvp in _counts)
    //        return kvp.Key;

    //    return null;
    //}

    // ---------- Public API ----------
    public PotionSO GetPotionDef(string potionId)
    {
        _defs.TryGetValue(potionId, out var so);
        return so;
    }

    public IReadOnlyDictionary<string, int> GetAllCounts() => _counts;

    public int GetCount(string potionId)
    {
        return _counts.TryGetValue(potionId, out var c) ? c : 0;
    }

    public bool SetSelectedPotion(string potionId)
    {
        if (string.IsNullOrEmpty(potionId)) return false;
        //if (!_counts.ContainsKey(potionId)) return false;

        if (SelectedPotionId == potionId)
        {
            ApplyWorldSelectionHighlight(potionId);
            return true;
        }

        SelectedPotionId = potionId;
        ApplyWorldSelectionHighlight(potionId);

        var count = GetCount(potionId);
        OnSelectedPotionChanged?.Invoke(potionId,count);
        return true;
    }

    /// <summary>
    ///显示对应的ui的数量变化
    /// </summary>
    /// <param name="potionId"></param>
    /// <param name="amount"></param>
    /// <returns></returns>

    public bool TryConsume(string potionId, int amount = 1)
    {
        if (!_counts.TryGetValue(potionId, out var c)) return false;
        if (c < amount) return false;

        c -= amount;
        _counts[potionId] = c;

        OnPotionCountChanged?.Invoke(potionId, c);
        OnInventoryChanged?.Invoke();

        //if (potionId == SelectedPotionId && c == 0)
        //    SetSelectedPotion(PickDefaultPotion());

        return true;
    }

    public void Add(string potionId, int amount = 1)
    {
        if (!_defs.ContainsKey(potionId)) return;
        _counts.TryGetValue(potionId, out var c);
        c += amount;
        c = Mathf.Min(c, maxPotionAmount);
        _counts[potionId] = c;
        OnPotionCountChanged?.Invoke(potionId, c);
        OnInventoryChanged?.Invoke();
    }

    public void RefillPotions()
    {
        Add("P_BND-delta3", maxPotionAmount);
        Add("P_Herschline", maxPotionAmount);
        Add("P_Ludopeptide", maxPotionAmount);
        Add("P_Myel-9", maxPotionAmount);
    }


    // ---------------- NEW: world potion registry & highlight ----------------

    public void RegisterWorldPotion(PotionBehaviour potion)
    {
        if (potion == null || string.IsNullOrEmpty(potion.PotionId))
            return;

        _worldPotions[potion.PotionId] = potion;

        // If this is current selected one, sync highlight immediately.
        if (!string.IsNullOrEmpty(SelectedPotionId) && potion.PotionId == SelectedPotionId)
            potion.SetHighlighted(true);
        else
            potion.SetHighlighted(false);
    }

    public void UnregisterWorldPotion(PotionBehaviour potion)
    {
        if (potion == null || string.IsNullOrEmpty(potion.PotionId))
            return;

        if (_worldPotions.TryGetValue(potion.PotionId, out var cur) && cur == potion)
            _worldPotions.Remove(potion.PotionId);
    }

    private void ApplyWorldSelectionHighlight(string selectedPotionId)
    {
        foreach (var kv in _worldPotions)
        {
            if (kv.Value == null) continue;
            kv.Value.SetHighlighted(kv.Key == selectedPotionId);
        }
    }
}
