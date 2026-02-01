using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    [SerializeField] private GraphManager graphManager;

    [Header("Typewriter")]
    public float charDelay = 0.02f;
    public bool allowSkipTyping = true;
    
    [Header("Input")]
    public KeyCode advanceKey = KeyCode.Space;

    [Header("Warning")]
    [SerializeField] private int maxRepeatCount = 5;
    //当前播放内容
    private  DialogueGraph _graph;
    private  DialogueNode _current;
    private NodeState _state;
    
    //打字机控制
    private Coroutine _typingCo;
    private bool _requestSkipTyping;
    
    //线性段落结束回调
    public Action _onFinished;

    ////缓存后续关卡信息
    //private GraphLevelData _nextLevelData = null;

    public bool IsPlaying => _graph != null;


    /// <summary>
    /// Natural end of a graph (reaching the end node).
    /// </summary>
    public event Action<DialogueGraph> OnFinished;

    /// <summary>
    /// Forced stop (e.g., level failed and we need to cut dialogue immediately).
    /// </summary>
    public event Action<DialogueGraph> OnInterrupted;

    /// <summary>
    /// Fired when player clicks a choice (useful to drive gameplay side effects).
    /// </summary>
    public event Action<DialogueChoice> OnChoiceSelected;

    /// <summary>
    /// 用于确认目前的dialogue是哪一个并且是否需要直接graphmanager来接入新关卡
    /// </summary>
    public event Action<DialogueGraph> OnDialogueStarted;


    private void Start()
    {
        Invoke(nameof(SubscribeAll), 1f);
    }

    private void SubscribeAll()
    {
        if (graphManager == null)
        {
            graphManager = GraphManager.Instance;
            graphManager.OnLevelFinished += OnLevelFinished;
            Debug.Log("[DialogueSystem] Subscribed to GraphManager.OnLevelFinished");
        }
    }   

    private void Update()
    {
        if(_graph == null) return;
        
        if(_state == NodeState.Choosing) return;
        
        if(Input.GetKeyDown(advanceKey))
            HandleAdvance();
    }
    
    ///// <summary>
    ///// 播放任意 Graph(Intro/NPC/Outro) 如果是linear,播完会调用onFinished.
    ///// 如果是HubAndBranch,外部在合适时机StopDialogue().
    ///// </summary>
    //public void Play(DialogueGraph graph, Action onFinished = null)
    //{
    //    if (graph == null)
    //    {
    //        Debug.LogError("DialogueSystem.Play: graph is null");
    //        return;
    //    }

    //    //_nextLevelData = graph.nextLevelData;
    //    //Debug.Log($"[Dialogue System] Next Level Data is {graph.nextLevelData.name}");

    //    _graph = graph;
    //    _onFinished = onFinished;

    //    if (ui != null)
    //    {
    //        ui.SetNpc(graph.npcProfile);
            
    //        ui.SetSpeakerVisible(graph.showSpeakerUI);
    //        ui.HideChoices();
    //        ui.SetNpcText("");
    //    }

    //    var startId = graph.startId;
    //    if (string.IsNullOrEmpty(startId))
    //    {
    //        if (graph.mode == DialogueMode.HubAndBranch && !string.IsNullOrEmpty(graph.hubId))
    //        {
    //            GoTo(graph.hubId);
    //            return;
    //        }

    //        EndDialogue();
    //        return;
    //    }
        
    //    GoTo(startId);
    //}


    /// <summary>
    /// Plays a DialogueGraph. DialogueSystem is ONLY responsible for playback.
    /// Flow/what to play next should be handled by GameManager.
    /// </summary>
    public void Play(DialogueGraph graph, Action onFinished = null)
    {
        if (graph == null)
        {
            Debug.LogError("DialogueSystem.Play: graph is null");
            return;
        }

        // If something is already playing, interrupt it first (flow decides what happens next).
        if (IsPlaying)
            Interrupt();

        Debug.Log($"[Dialogue System] OnDialogueStarted Invoked");
        OnDialogueStarted?.Invoke(graph);

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

            EndDialogue(naturalEnd: true);
            return;
        }

        GoTo(startId);
    }

    //public void PlayNPC(DialogueGraph npcgraph)
    //{
    //    if (npcgraph == null)
    //    {
    //        Debug.LogError("DialogueSystem.PlayNPC: npcgraph is null");
    //        return;
    //    }

    //    //_nextLevelData = npcgraph.nextLevelData;
    //    Debug.Log($"[Dialogue System] Next Level Data is {npcgraph.nextLevelData.name}");
    //    graphManager.BuildLevelWLevelData(npcgraph.nextLevelData);


    //    npcgraph.mode = DialogueMode.HubAndBranch;

    //    if (!string.IsNullOrEmpty(npcgraph.hubId))
    //    {
    //        Play(npcgraph,null);
    //        GoTo(npcgraph.hubId);
    //    }
    //    else
    //    {
    //        Play(npcgraph,null);
    //    }
    //}


    /// <summary>
    /// Force stop current dialogue immediately (no "finished" callback).
    /// </summary>
    public void Interrupt()
    {
        if (_graph == null) return;
        EndDialogue(naturalEnd: false);
    }

    //public void StopDialogue()
    //{
    //    EndDialogue();
    //}

    //private void GoTo(string id)
    //{
    //    if(_graph == null) return;

    //    var node = _graph.Get(id);
    //    if (node == null)
    //    {
    //        Debug.LogError($"Dialogue node not found: {id}");
    //        EndDialogue();
    //        return;
    //    }

    //    EnterNode(node);
    //}

    private void GoTo(string id)
    {
        if (_graph == null) return;

        var node = _graph.Get(id);
        if (node == null)
        {
            Debug.LogError($"Dialogue node not found: {id}");
            EndDialogue(naturalEnd: true);
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

    //private void AdvanceAfterLineComplete()
    //{
    //    Debug.Log($"[Dialogue] node={_current?.id}, choices={(_current?.choices==null?0:_current.choices.Count)}, nextId={_current?.nextId}");
        
    //    if(_graph == null || _current == null) return;

    //    if (_current.choices != null && _current.choices.Count > 0)
    //    {
    //        _state = NodeState.Choosing;
    //        if (ui != null)
    //        {
    //            ui.SetSpeakerToPlayer();
    //            ui.ShowChoices(_current.choices, OnChoiceClicked);
    //        }
    //        return;
    //    }
        
    //    if(!string.IsNullOrEmpty(_current.nextId))
    //    {
    //        GoTo(_current.nextId);
    //        return;
    //    }

    //    EndDialogue();
    //}
    private void AdvanceAfterLineComplete()
    {
        if (_graph == null || _current == null) return;

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

        if (!string.IsNullOrEmpty(_current.nextId))
        {
            GoTo(_current.nextId);
            return;
        }

        EndDialogue(naturalEnd: true);
    }


    //private void OnChoiceClicked(DialogueChoice choice)
    //{
    //    if(_graph == null) return;
    //    if(choice == null) return;

    //    if (ui != null)
    //    {
    //        ui.HideChoices();
    //        ui.SetSpeakerToNPC();
    //    }

    //    if (_graph.mode == DialogueMode.HubAndBranch && choice.backToHub)
    //    {
    //        if (string.IsNullOrEmpty(_graph.hubId))
    //        {
    //            Debug.LogError("Choice.backToHub = true but graph.hubId is empty");
    //            EndDialogue();
    //            return;
    //        }

    //        GoTo(_graph.hubId);
    //        return;
    //    }

    //    if (string.IsNullOrEmpty(choice.nextId))
    //    {
    //        Debug.LogError("Choice nextId is empty");
    //        EndDialogue();
    //        return;
    //    }

    //    GoTo(choice.nextId);

    //    //这边添加GraphManager.UpdateRegion;
    //    if (graphManager == null) { 
    //        Debug.LogWarning("GraphManager is not assigned in DialogueSystem.");
    //        return;
    //    }

    //    graphManager.SetActivatedRegion(
    //        new List<RegionId> { choice.regionId }
    //    );
    //}

    private void OnChoiceClicked(DialogueChoice choice)
    {
        if (_graph == null) return;
        if (choice == null) return;
        
        //var pickCount = IncrementChoicePickCount(choice);   
        //OnChoiceSelected?.Invoke(choice);

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
                EndDialogue(naturalEnd: true);
                return;
            }

            GoTo(_graph.hubId);
            return;
        }

        if (string.IsNullOrEmpty(choice.nextId))
        {
            Debug.LogError($"Choice nextId is empty, current choice is {choice.text}, nextId is {choice.nextId}");
            EndDialogue(naturalEnd: true);
            return;
        }

        var pickCount = IncrementChoicePickCount(choice);
        bool exceeded = UpdateWarningBar();

        if (exceeded) { 
            OnLevelFinished(_graph.nextLevelData);
            graphManager.ClearLevel();
            graphManager.UnlockInput();
            return;
        }

        OnChoiceSelected?.Invoke(choice);
        GoTo(choice.nextId);
    }


    //private void EndDialogue()
    //{
    //    if (_typingCo != null)
    //    {
    //        StopCoroutine(_typingCo);
    //        _typingCo = null;
    //    }

    //    _requestSkipTyping = false;

    //    if (ui != null)
    //    {
    //        ui.HideChoices();
    //        ui.SetNpcText("");
    //    }

    //    var finished = _onFinished;

    //    _graph = null;
    //    _current = null;
    //    _onFinished = null;

    //    finished?.Invoke();

    //    //if (_nextLevelData == null)
    //    //{
    //    //    graphManager.ClearLevel();
    //    //}
    //    //else {
    //    //    graphManager.BuildLevelWLevelData(_nextLevelData);
    //    //}
    //}

    private void EndDialogue(bool naturalEnd)
    {
        var endedGraph = _graph;

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

        var finishedCallback = _onFinished;

        _graph = null;
        _current = null;
        _onFinished = null;

        if (naturalEnd)
        {
            finishedCallback?.Invoke();
            OnFinished?.Invoke(endedGraph);
        }
        else
        {
            OnInterrupted?.Invoke(endedGraph);
        }
    }

    private void OnLevelFinished(GraphLevelData levelData)
    {
        //if (levelData == null)
        //{
        //    Debug.LogWarning("[DialogueSystem] PlayNextDialogue: levelData is null");
        //    return;
        //}
        //var nextGraph = levelData.nextLinearLevelDialogueGraph;

        //Debug.Log($"[DialogueSystem] PlayNextDialogue: nextGraph is {(nextGraph == null ? "null" : nextGraph.name)}");

        //if (nextGraph == null)
        //{
        //    Debug.LogWarning("[DialogueSystem] PlayNextDialogue: nextGraph is null");
        //    return;
        //}
        //Play(nextGraph, null);
        ClearChoicePickCounts();
    }


    // ================= Choice repeat tracking (runtime) =================
    private readonly Dictionary<string, int> _choicePickCounts = new Dictionary<string, int>(256);

    private string BuildChoiceKey(DialogueGraph graph, DialogueNode node, DialogueChoice choice)
    {
        // Prefer stable identity: graph asset + node id + choice index in that node.
        // This avoids collisions when multiple nodes share same text/nextId.
        var graphKey = graph != null ? graph.name : "nullGraph";
        var nodeKey = node != null ? node.id : "nullNode";

        var index = -1;
        if (node != null && node.choices != null && choice != null)
            index = node.choices.IndexOf(choice);

        return $"{graphKey}|{nodeKey}|{index}";
    }

    /// <summary>
    /// Returns how many times the player has picked this choice (in current DialogueSystem lifetime / since last clear).
    /// </summary>
    public int GetChoicePickCount(DialogueChoice choice)
    {
        var key = BuildChoiceKey(_graph, _current, choice);
        return _choicePickCounts.TryGetValue(key, out var c) ? c : 0;
    }

    /// <summary>
    /// Clears all pick counts (call when you want a fresh session).
    /// </summary>
    public void ClearChoicePickCounts()
    {
        _choicePickCounts.Clear();
        UpdateWarningBar();
    }

    private int IncrementChoicePickCount(DialogueChoice choice)
    {
        var key = BuildChoiceKey(_graph, _current, choice);

        _choicePickCounts.TryGetValue(key, out var current);
        current++;
        _choicePickCounts[key] = current;
        return current;
    }
    // ===================================================================

    private bool UpdateWarningBar()
    {
        int totalRepeats = 0;

        if (_choicePickCounts.Count == 0)
        {
            Debug.Log("[DialogueSystem] _choicePickCounts: <empty>");
        }
        else
        {
            string msg = string.Join(
                ", ",
                _choicePickCounts.Select(kvp => $"'{kvp.Key}': {kvp.Value}")
            );
            
            Debug.Log($"[DialogueSystem] _choicePickCounts: {msg}");
        }

        foreach (var kvp in _choicePickCounts)
        {
            totalRepeats += Mathf.Max(0, kvp.Value - 1);
        }

        Debug.Log($"[DialogueSystem] Total choice repeats: {totalRepeats} (max allowed: {maxRepeatCount})");

        var fill = maxRepeatCount <= 0 ? 1f : (float)totalRepeats / maxRepeatCount;

        if (ui != null)
            ui.SetWarningBarFill(fill);

        if (totalRepeats > maxRepeatCount) { 
            ClearChoicePickCounts();

            var gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogWarning("[DialogueSystem] totalRepeats exceeded but GameManager not found in scene.");
                return false;
            }

            gm.PlayFailedDialogueThenResumeCurrent(0);

            return true;
        }
        return false;
    }
}
