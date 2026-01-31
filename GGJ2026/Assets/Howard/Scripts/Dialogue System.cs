using UnityEngine;
using System.Collections;
using System;

public class DialogueSystem : MonoBehaviour
{
    public enum NodeState
    {
        Typing,
        LineComplete,
        Choosing
    }
    
    [Header("References")]
    public DialogueUI ui;
    
    [Header("Typewriter")]
    public float charDelay = 0.02f;
    public bool allowSkipTyping = true;
    
    [Header("Input")]
    public KeyCode advanceKey = KeyCode.Space;
    
    //当前播放内容
    private  DialogueGraph _graph;
    private  DialogueNode _current;
    private NodeState _state;
    
    //打字机控制
    private Coroutine _typingCo;
    private bool _requestSkipTyping;
    
    //线性段落结束回调
    public Action _onFinished;
    
    public bool IsPlaying => _graph != null;

    private void Update()
    {
        if(_graph == null) return;
        
        if(_state == NodeState.Choosing) return;
        
        if(Input.GetKeyDown(advanceKey))
            HandleAdvance();
    }
    
    /// <summary>
    /// 播放任意 Graph(Intro/NPC/Outro) 如果是linear,播完会调用onFinished.
    /// 如果是HubAndBranch,外部在合适时机StopDialogue().
    /// </summary>
    public void Play(DialogueGraph graph, Action onFinished = null)
    {
        if (graph == null)
        {
            Debug.LogError("DialogueSystem.Play: graph is null");
            return;
        }
        
        _graph = graph;
        _onFinished = onFinished;

        if (ui != null)
        {
            ui.SetNpc(graph.npcProfile);
            
            ui.SetSpeakerVisible(graph.showSpeakerUI);
            ui.HideChoices();
            ui.SetNpcText("");
        }

        var startId = graph.startId;
        if (string.IsNullOrEmpty(startId))
        {
            if (graph.mode == DialogueMode.HubAndBranch && !string.IsNullOrEmpty(graph.hubId))
            {
                GoTo(graph.hubId);
                return;
            }

            EndDialogue();
            return;
        }
        
        GoTo(startId);
    }

    public void PlayNPC(DialogueGraph npcgraph)
    {
        if (npcgraph == null)
        {
            Debug.LogError("DialogueSystem.PlayNPC: npcgraph is null");
            return;
        }
        
        npcgraph.mode = DialogueMode.HubAndBranch;

        if (!string.IsNullOrEmpty(npcgraph.hubId))
        {
            Play(npcgraph,null);
            GoTo(npcgraph.hubId);
        }
        else
        {
            Play(npcgraph,null);
        }
    }

    public void StopDialogue()
    {
        EndDialogue();
    }

    private void GoTo(string id)
    {
        if(_graph == null) return;

        var node = _graph.Get(id);
        if (node == null)
        {
            Debug.LogError($"Dialogue node not found: {id}");
            EndDialogue();
            return;
        }

        EnterNode(node);
    }

    private void EnterNode(DialogueNode node)
    {
        _current = node;
        if(ui != null) ui.HideChoices();
        
        bool hasChoices = node.choices != null && node.choices.Count > 0;
        bool hasNpcLine = !string.IsNullOrEmpty(node.npcLine);

        if (hasChoices && !hasNpcLine)
        {
            _state = NodeState.Choosing;
            if (ui != null)
            {
                ui.SetSpeakerToPlayer();
                ui.SetNpcText(""); 
                ui.ShowChoices(node.choices, OnChoiceClicked);
            }
            return;
        }
        
        if(ui != null) ui.SetSpeakerToNPC();
        StartTyping(node.npcLine ?? "");
    }

    private void StartTyping(string fulltext)
    {
        if (_typingCo != null)
        {
            StopCoroutine(_typingCo);
            _typingCo = null;
        }
        
        _requestSkipTyping = false;
        _state = NodeState.Typing;
        
        if(ui != null) ui.SetNpcText("");
        
        _typingCo = StartCoroutine(TypeLine(fulltext));
    }

    private IEnumerator TypeLine(string fulltext)
    {
        if (string.IsNullOrEmpty(fulltext))
        {
            if(ui != null) ui.SetNpcText("");
            _state = NodeState.LineComplete;
            _typingCo = null;
            yield break;
        }

        for (int i = 0; i < fulltext.Length; i++)
        {
            if(_requestSkipTyping)
                break;
            
            if(ui != null) ui.AppendNpcChar(fulltext[i]);

            if (charDelay <= 0f)
                yield return null;
            else
                yield return new WaitForSeconds(charDelay);
        }
        
        if (ui != null) ui.SetNpcText(fulltext);
        
        _state = NodeState.LineComplete;
        _typingCo = null;
    }

    private void HandleAdvance()
    {
        if (_state == NodeState.Typing)
        {
            if (allowSkipTyping)
            {
                _requestSkipTyping = true;
            }
            return;
        }

        if (_state == NodeState.LineComplete)
        {
            AdvanceAfterLineComplete();
            return;
        }
    }

    private void AdvanceAfterLineComplete()
    {
        Debug.Log($"[Dialogue] node={_current?.id}, choices={(_current?.choices==null?0:_current.choices.Count)}, nextId={_current?.nextId}");
        
        if(_graph == null || _current == null) return;

        if (_current.choices != null && _current.choices.Count > 0)
        {
            _state = NodeState.Choosing;
            if (ui != null)
            {
                ui.SetSpeakerToPlayer();
                ui.ShowChoices(_current.choices, OnChoiceClicked);
            }
            return;
        }
        
        if(!string.IsNullOrEmpty(_current.nextId))
        {
            GoTo(_current.nextId);
            return;
        }

        EndDialogue();
    }

    private void OnChoiceClicked(DialogueChoice choice)
    {
        if(_graph == null) return;
        if(choice == null) return;

        if (ui != null)
        {
            ui.HideChoices();
            ui.SetSpeakerToNPC();
        }

        if (_graph.mode == DialogueMode.HubAndBranch && choice.backToHub)
        {
            if (string.IsNullOrEmpty(_graph.hubId))
            {
                Debug.LogError("Choice.backToHub = true but graph.hubId is empty");
                EndDialogue();
                return;
            }
            
            GoTo(_graph.hubId);
            return;
        }

        if (string.IsNullOrEmpty(choice.nextId))
        {
            Debug.LogError("Choice nextId is empty");
            EndDialogue();
            return;
        }
        
        GoTo(choice.nextId);
        
        //这边添加GraphManager.UpdateRegion;
    }

    private void EndDialogue()
    {
        if (_typingCo != null)
        {
            StopCoroutine(_typingCo);
            _typingCo = null;
        }
        
        _requestSkipTyping = false;

        if (ui != null)
        {
            ui.HideChoices();
            ui.SetNpcText("");
        }

        var finished = _onFinished;
        
        _graph = null;
        _current = null;
        _onFinished = null;
        
        finished?.Invoke();
    }
}
