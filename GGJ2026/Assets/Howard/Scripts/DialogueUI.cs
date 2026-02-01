using System;
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    [Header("NPC Text")]
    public TMP_Text npcText;

    [Header("Choices")] 
    public Transform choicesParent;
    public Button choiceButtonPrefab;

    public GameObject choicePanelPrefab;

    [Header("Speaker UI")] 
    public Image playerPortrait;
    public Image npcPortrait;
    public TMP_Text nameText;
    public GameObject speakerRoot;
    private bool _speakerVisible = true;
    
    private Action<DialogueChoice> _onChoiceClick;
    private NPC _currentNpc;

    public void SetSpeakerVisible(bool visible)
    {
        _speakerVisible = visible;
        
        if(speakerRoot != null) {speakerRoot.SetActive(visible);}

        if (!visible)
        {
            if(nameText != null) nameText.text = "";
            if (playerPortrait != null) playerPortrait.enabled = false;
            if (npcPortrait != null) npcPortrait.enabled = false;
        }
        else
        {
            if (playerPortrait != null) playerPortrait.enabled = true;
            if (npcPortrait != null) npcPortrait.enabled = true;
        }
    }
    
    public void SetNpc(NPC npc)
    {
        _currentNpc = npc;

        if (npcPortrait != null)
        {
            npcPortrait.sprite = npc != null ? npc.portrait : null;
            npcPortrait.enabled = (npc != null && npc.portrait != null);
        }
    }

    public void SetSpeakerToNPC()
    {
        if(!_speakerVisible) return;
        
        if(npcPortrait != null) npcPortrait.enabled = true;
        if(playerPortrait != null) playerPortrait.enabled = false;
        
        if (nameText != null)
            nameText.text = _currentNpc != null && !string.IsNullOrEmpty(_currentNpc.npcName)
                ? _currentNpc.npcName
                : "";

        if (npcPortrait != null)
            npcPortrait.color = Color.white;

        if (playerPortrait != null)
            playerPortrait.color = new Color(1f, 1f, 1f, 0.35f);
    }

    public void SetSpeakerToPlayer()
    {
        if (!_speakerVisible) return;

        if(npcPortrait != null) npcPortrait.enabled = false;
        if (playerPortrait != null) playerPortrait.enabled = true;
        
        if (nameText != null)
            nameText.text = "Player";

        if (playerPortrait != null)
            playerPortrait.color = Color.white;

        if (npcPortrait != null)
            npcPortrait.color = new Color(1f, 1f, 1f, 0.35f);
    }
    
    public void SetNpcText(string text)
    {
        if(npcText != null) npcText.text = text;
    }

    public void AppendNpcChar(char c)
    {
        if(npcText != null) npcText.text += c;
    }

    public void ClearChoices()
    {
        if (choicesParent == null) return;
        for (int i = choicesParent.childCount - 1; i >= 0; i--)
            Destroy(choicesParent.GetChild(i).gameObject);
    }

    public void HideChoices()
    {
        ClearChoices();
        if(choicesParent != null) choicesParent.gameObject.SetActive(false);
    }

    public void ShowChoices(List<DialogueChoice> choices, Action<DialogueChoice> onChoiceClick)
    {
        //_onChoiceClick = onChoiceClick;
        
        //if(choicesParent == null || choiceButtonPrefab == null)
        //    return;
        
        //choicesParent.gameObject.SetActive(true);
        //ClearChoices();
        
        //if(choices == null) return;

        //foreach (var choice in choices)
        //{
        //    //var btn = Instantiate(choiceButtonPrefab, choicesParent);

        //    //var label = btn.GetComponentInChildren<TMP_Text>();
        //    //if (label != null) label.text = choice.text;

        //    //btn.onClick.RemoveAllListeners();
        //    //btn.onClick.AddListener(() => _onChoiceClick?.Invoke(choice));

        //    var panelInstance = Instantiate(choicePanelPrefab, choicesParent);
        //    Button button = panelInstance.GetComponentInChildren<Button>();

        //    var label = panelInstance.GetComponentInChildren<TMP_Text>();
        //    if (label != null) label.text = choice.text;

        //    button.onClick.RemoveAllListeners();
        //    button.onClick.AddListener(() => _onChoiceClick?.Invoke(choice));
        //}

        _onChoiceClick = onChoiceClick;

        if (choicesParent == null)
        {
            Debug.LogWarning("[DialogueUI] ShowChoices: choicesParent is null.", this);
            return;
        }

        if (choicePanelPrefab == null)
        {
            Debug.LogWarning("[DialogueUI] ShowChoices: choicePanelPrefab is null (not assigned in Inspector).", this);
            return;
        }

        choicesParent.gameObject.SetActive(true);
        ClearChoices();

        if (choices == null || choices.Count == 0)
            return;

        foreach (var choice in choices)
        {
            var panelInstance = Instantiate(choicePanelPrefab, choicesParent);

            var button = panelInstance.GetComponentInChildren<Button>(true);
            if (button == null)
            {
                Debug.LogError("[DialogueUI] ShowChoices: choicePanelPrefab has no Button in children.", panelInstance);
                continue;
            }

            var label = panelInstance.GetComponentInChildren<TMP_Text>(true);
            //Debug.Log($"[DialogueUI] ShowChoices: choice text = {choice.text}; label is none {label == null}");
            if (label != null) label.text = choice.text;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onChoiceClick?.Invoke(choice));
        }
    }
}
