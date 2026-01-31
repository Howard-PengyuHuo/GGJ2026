using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueGraph", menuName = "Scriptable Objects/DialogueGraph")]

public class DialogueGraph : ScriptableObject
{
    public DialogueMode mode;
    public string startId;
    public string hubId;
    public List<DialogueNode> nodes = new List<DialogueNode>();
    public NPC npcProfile;
    public bool showSpeakerUI;
    private Dictionary<string, DialogueNode> _map;
    public GraphLevelData nextLevelData;

    public DialogueNode Get(string id)
    {
        if (_map == null)
        {
            _map = new Dictionary<string, DialogueNode>();
            foreach (var n in nodes)
            {
                if(!string.IsNullOrEmpty(n.id))
                    _map[n.id] = n;
            }
        }
        
        if(string.IsNullOrEmpty(id))
            return null;
        
        _map.TryGetValue(id, out var node);
        return node;
    }

    public void RebuildCache()
    {
        _map = null;
    }
}