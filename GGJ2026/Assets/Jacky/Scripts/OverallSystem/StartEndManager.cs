using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartEndManager : MonoBehaviour
{
    [Serializable]
    public class CutsceneConfig
    {
        public string sceneName;                 // 例如 "StartScene" / "EndScene"
        public string targetRendererObjectName;  // 场景里 SpriteRenderer 所在 GameObject 的名字
        public List<Sprite> sprites = new List<Sprite>();

        public string nextSceneName;             // 播完要加载的场景
        public KeyCode advanceKey = KeyCode.Space;
        public bool allowMouseClick = true;
    }

    [Header("Cutscene Configs (Start / End)")]
    [SerializeField] private List<CutsceneConfig> configs = new List<CutsceneConfig>();

    private static StartEndManager _instance;

    private CutsceneConfig _activeConfig;
    private SpriteRenderer _targetRenderer;
    private int _index;
    private bool _isActiveCutscene;
    private bool _isLoading;

    void Awake()
    {
        // 单例常驻
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 监听切场景
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (_instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        // 游戏一开始就在 StartScene 的话，也能立刻初始化
        SetupForScene(SceneManager.GetActiveScene().name);
    }

    void Update()
    {
        if (!_isActiveCutscene || _isLoading) return;

        bool advance =
            Input.GetKeyDown(_activeConfig.advanceKey) ||
            (_activeConfig.allowMouseClick && Input.GetMouseButtonDown(0));

        if (!advance) return;

        Advance();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetupForScene(scene.name);
    }

    private void SetupForScene(string sceneName)
    {
        _isLoading = false;
        _isActiveCutscene = false;
        _activeConfig = null;
        _targetRenderer = null;
        _index = 0;

        // 找到该 scene 的配置
        foreach (var cfg in configs)
        {
            if (cfg != null && cfg.sceneName == sceneName)
            {
                _activeConfig = cfg;
                break;
            }
        }

        // 不是 cutscene 场景（比如 MainScene）就不做任何事
        if (_activeConfig == null) return;

        // 校验 sprites
        if (_activeConfig.sprites == null || _activeConfig.sprites.Count == 0)
        {
            Debug.LogError($"[StartEndManager] Config for scene '{sceneName}' has empty sprites list.");
            return;
        }

        // 在当前 scene 找 SpriteRenderer（通过对象名）
        var go = GameObject.Find(_activeConfig.targetRendererObjectName);
        if (go == null)
        {
            Debug.LogError($"[StartEndManager] Cannot find GameObject '{_activeConfig.targetRendererObjectName}' in scene '{sceneName}'.");
            return;
        }

        _targetRenderer = go.GetComponentInChildren<SpriteRenderer>();
        if (_targetRenderer == null)
        {
            Debug.LogError($"[StartEndManager] No SpriteRenderer found under '{go.name}' in scene '{sceneName}'.");
            return;
        }

        // 初始化显示第一张
        _index = 0;
        ApplySprite(_index);

        _isActiveCutscene = true;
        Debug.Log($"[StartEndManager] Cutscene active in '{sceneName}'. Next: '{_activeConfig.nextSceneName}'");
    }

    private void Advance()
    {
        _index++;

        if (_index >= _activeConfig.sprites.Count)
        {
            LoadNextScene();
            return;
        }

        ApplySprite(_index);
    }

    private void ApplySprite(int idx)
    {
        var s = _activeConfig.sprites[idx];
        _targetRenderer.sprite = s;
    }

    private void LoadNextScene()
    {
        if (string.IsNullOrEmpty(_activeConfig.nextSceneName))
        {
            Debug.LogError("[StartEndManager] nextSceneName is empty.");
            return;
        }

        _isLoading = true;
        SceneManager.LoadScene(_activeConfig.nextSceneName, LoadSceneMode.Single);
    }
}
