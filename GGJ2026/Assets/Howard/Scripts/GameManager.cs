using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    //[Header("Dialogue")]
    //public DialogueSystem dialogueSystem;
    //public DialogueGraph intro;
    //public DialogueGraph level1;
    //public DialogueGraph outro;


    //// Start is called once before the first execution of Update after the MonoBehaviour is created
    //void Start()
    //{
    //    Intro();
    //}


    //// Update is called once per frame
    //void Update()
    //{

    //}

    //void Intro()
    //{
    //    dialogueSystem.Play(intro, () =>
    //    {
    //        StartLevel();
    //        //GraphManager.Instance.BuildLevelWLevelData()
    //    });
    //}

    //void StartLevel()
    //{
    //    dialogueSystem.PlayNPC(level1);
    //}



    //void GameOver()
    //{
    //    dialogueSystem.Play(outro, () =>
    //    {
    //        Intro();
    //    });
    //}

    [Header("References")]
    [SerializeField] private DialogueSystem dialogueSystem;

    [Header("Linear Dialogue Sequence (play in order)")]
    [Tooltip("Main linear sequence of dialogue graphs. Each one finishes -> next one starts.")]
    [SerializeField] private List<DialogueGraph> sequence = new List<DialogueGraph>();

    [Header("Failure Routing (optional)")]
    [Tooltip("Played when the level fails. After it finishes, restart from sequenceStartIndexOnFail.")]
    [SerializeField] private DialogueGraph failGraphA;

    [Tooltip("Another fail graph (e.g., different fail reason). After it finishes, restart from sequenceStartIndexOnFail.")]
    [SerializeField] private DialogueGraph failGraphB;

    [Tooltip("After failure graph ends, restart from this index in the sequence.")]
    [Min(0)]
    [SerializeField] private int sequenceStartIndexOnFail = 0;

    [Header("RunTime Debug")]
    [SerializeField]private int _index = -1;

    [SerializeField] private GraphManager _graphManager;

    private void Start()
    {
        PlayFromStart();
    }

    private void SubscribeAll()
    {
        //_graphManager = GraphManager.Instance;
        //if (_graphManager != null)
        //    _graphManager.OnLevelFinished += OnGraphLevelFinished;
        _graphManager.OnLevelFinished += OnGraphLevelFinished;
        Debug.Log("[GameManager] Subscribed to GraphManager.OnLevelFinished");

    }

    private void OnEnable()
    {
        Invoke(nameof(SubscribeAll), 0.1f);
    }

    private void OnDisable()
    {
        if (_graphManager != null)
            _graphManager.OnLevelFinished -= OnGraphLevelFinished;
    }

    private void OnGraphLevelFinished(GraphLevelData levelData)
    {
        Debug.Log("[GameManager] OnGraphLevelFinished received in GameManager.");
        dialogueSystem.Interrupt();
        PlayNext();
    }

    public void PlayFromStart()
    {
        if (dialogueSystem == null)
        {
            Debug.LogError("[GameManager] DialogueSystem is not assigned.");
            return;
        }

        if (sequence == null || sequence.Count == 0)
        {
            Debug.LogWarning("[GameManager] Sequence is empty.");
            return;
        }

        _index = -1;
        PlayNext();
    }

    public void PlayNext()
    {
        if (dialogueSystem == null) return;

        _index++;

        if (_index < 0)
        {
            Debug.Log("[GameManager] index<0; Sequence finished.");
            return;
        }

        if (_index >= sequence.Count) {
            Debug.Log("[GameManager] All Sequence Finished, End This Game");
            return;
        }

        var graph = sequence[_index];
        if (graph == null)
        {
            Debug.LogWarning($"[GameManager] Sequence graph at index {_index} is null, skipping.");
            PlayNext();
            return;
        }

        // If this graph is a hub graph, Play() still works (DialogueSystem handles hubId fallback),
        // and your gameplay systems can react via their own events.
        dialogueSystem.Play(graph, PlayNext);
    }

    /// <summary>
    /// Call this when fail case A happens.
    /// </summary>
    public void FailA()
    {
        JumpToFailure(failGraphA);
    }

    /// <summary>
    /// Call this when fail case B happens.
    /// </summary>
    public void FailB()
    {
        JumpToFailure(failGraphB);
    }

    private void JumpToFailure(DialogueGraph failGraph)
    {
        if (dialogueSystem == null) return;

        // Cut current dialogue immediately.
        dialogueSystem.Interrupt();

        if (failGraph == null)
        {
            Debug.LogWarning("[GameManager] failGraph is null. Restarting sequence directly.");
            RestartAfterFail();
            return;
        }

        dialogueSystem.Play(failGraph, RestartAfterFail);
    }

    private void RestartAfterFail()
    {
        if (sequence == null || sequence.Count == 0) return;

        // Clamp to valid range.
        if (sequenceStartIndexOnFail < 0) sequenceStartIndexOnFail = 0;
        if (sequenceStartIndexOnFail >= sequence.Count) sequenceStartIndexOnFail = sequence.Count - 1;

        _index = sequenceStartIndexOnFail - 1;
        PlayNext();
    }
}
