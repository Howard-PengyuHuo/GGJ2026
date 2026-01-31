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
    public List<Entry> initialInventory = new();

    private Dictionary<string, int> _counts = new();
    private Dictionary<string, PotionSO> _defs = new();

    public string SelectedPotionId { get; private set; }

    // EVENTS —— 只暴露 potionId
    public event Action<string> OnSelectedPotionChanged;
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

        SelectedPotionId = PickDefaultPotion();
        OnInventoryChanged?.Invoke();
        if (!string.IsNullOrEmpty(SelectedPotionId))
            OnSelectedPotionChanged?.Invoke(SelectedPotionId);
    }

    private string PickDefaultPotion()
    {
        // 优先选择有数量的药水
        foreach (var kvp in _counts)
            if (kvp.Value > 0)
                return kvp.Key;

        // 否则选择任意一种药水
        foreach (var kvp in _counts)
            return kvp.Key;

        // 否则没有可选的药水
        return null;
    }

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
        if (!_counts.ContainsKey(potionId)) return false;

        if (SelectedPotionId == potionId) return true;

        SelectedPotionId = potionId;
        OnSelectedPotionChanged?.Invoke(potionId);
        return true;
    }

    public bool TryConsume(string potionId, int amount = 1)
    {
        if (!_counts.TryGetValue(potionId, out var c)) return false;
        if (c < amount) return false;

        c -= amount;
        _counts[potionId] = c;

        OnPotionCountChanged?.Invoke(potionId, c);
        OnInventoryChanged?.Invoke();

        if (potionId == SelectedPotionId && c == 0)
            SetSelectedPotion(PickDefaultPotion());

        return true;
    }

    public void Add(string potionId, int amount = 1)
    {
        if (!_defs.ContainsKey(potionId)) return;
        _counts.TryGetValue(potionId, out var c);
        c += amount;
        _counts[potionId] = c;
        OnPotionCountChanged?.Invoke(potionId, c);
        OnInventoryChanged?.Invoke();
    }
}
