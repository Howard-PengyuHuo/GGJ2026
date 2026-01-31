using UnityEngine;
using System.Collections.Generic;

public enum DialogueMode
{
    Linear,
    HubAndBranch
}

[System.Serializable]
public class DialogueNode
{
    public string id;
    public string npcLine;
    public string nextId;
    public List<DialogueChoice> choices;
}

[System.Serializable] 
public class DialogueChoice
{
    public string text;
    public string nextId;
    public bool backToHub;

    public string activateRegion;
}
