using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PotionDatabase", menuName = "Scriptable Objects/PotionDatabase")]
public class PotionDatabase : ScriptableObject
{
    public List<PotionSO> potions = new();

    private Dictionary<string, PotionSO> _map;

    public void Build()
    {
        _map = new Dictionary<string, PotionSO>();
        foreach (var p in potions)
        {
            if (p == null || string.IsNullOrEmpty(p.potionId)) continue;
            if (_map.ContainsKey(p.potionId))
            {
                Debug.LogError($"Duplicate potionId: {p.potionId}");
                continue;
            }
            _map[p.potionId] = p;
        }
    }

    public PotionSO Get(string potionId)
    {
        if (_map == null) Build();
        _map.TryGetValue(potionId, out var so);
        return so;
    }

    public IEnumerable<PotionSO> All()
    {
        if (_map == null) Build();
        return _map.Values;
    }
}
