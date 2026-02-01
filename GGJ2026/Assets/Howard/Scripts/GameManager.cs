using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DialogueSystem dialogueSystem;
    public static GameManager Instance { get; private set; }

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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        if (_graphManager == null)
        {
            _graphManager = GraphManager.Instance;
        }
    }

    private void Start()
    {
        PlayFromStart();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T)) {
            //PlayFailedDialogueThenResumeCurrent(0);
            SceneManager.LoadScene(2);
        }
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
        //dialogueSystem.ui.SetSpeakerVisible(false);
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
            _index = 0;
            //return;
        }

        if (_index >= sequence.Count) {
            Debug.Log("[GameManager] All Sequence Finished, End This Game");
            SceneManager.LoadScene(2);
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
    /// 直接跳到 sequence 的某个 index 播放（并且保持线性链路：播完会继续 PlayNext）。
    /// </summary>
    public void PlayByIndex(int index)
    {
        if (dialogueSystem == null) return;

        if (sequence == null || sequence.Count == 0)
        {
            Debug.LogWarning("[GameManager] PlayByIndex: sequence is empty.");
            return;
        }

        if (index < 0 || index >= sequence.Count)
        {
            Debug.LogWarning($"[GameManager] PlayByIndex: index out of range: {index}");
            return;
        }

        // Cut current dialogue immediately.
        dialogueSystem.Interrupt();

        _index = index - 1; // 让 PlayNext() 把它 +1 到目标 index
        PlayNext();
    }

    /// <summary>
    /// 给 DialogueSystem 用：播放失败对话（默认用 failGraphA），播完后回到“当前 index”对应的对话重新播放。
    /// </summary>
    public void PlayFailedDialogueThenResumeCurrent(int failedGraphIndex)
    {
        var resumeIndex = _index;

        // Cut current dialogue immediately.
        if (dialogueSystem != null)
            dialogueSystem.Interrupt();

        //var failGraph = failGraphA != null ? failGraphA : failGraphB;
        var failGraph = failedGraphIndex == 0 ? failGraphA : failGraphB;
        if (failGraph == null)
        {
            Debug.LogWarning("[GameManager] No failGraph assigned, resuming directly.");
            PlayByIndex(resumeIndex);
            return;
        }

        dialogueSystem.Play(failGraph, () =>
        {
            PlayByIndex(resumeIndex);
            Debug.Log("[GameManager] Failed dialogue finished, resuming current dialogue.");
        });
    }


    #region 错误的失败处理方法（保留以备参考）
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
    #endregion
}
