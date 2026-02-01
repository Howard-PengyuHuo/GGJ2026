using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PotionBehaviour : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string potionId;

    [Header("Highlight (optional)")]
    [SerializeField] private GameObject highlightObject; // e.g. glow sprite/particle as child

    [Header("Render Order")]
    [SerializeField] private bool restoreOrderOnRelease = false;

    private SpriteRenderer _sr;
    private int _originalOrder;

    public string PotionId => potionId;

    private void Awake()
    {
        _sr = GetComponentInChildren<SpriteRenderer>();
        if (_sr != null) _originalOrder = _sr.sortingOrder;
    }

    private void OnEnable()
    {
        //var inv = PotionInventoryManager.Instance;
        //if (inv != null) inv.RegisterWorldPotion(this);
        Invoke(nameof(RegisterToPotionManager), 1f);
    }

    private void RegisterToPotionManager()
    {
        var inv = PotionInventoryManager.Instance;
        if (inv == null) { Debug.LogWarning($"Failed To Register to PotionInventoryManager"); }
        if (inv != null) inv.RegisterWorldPotion(this);
    }

    private void OnDisable()
    {
        var inv = PotionInventoryManager.Instance;
        if (inv != null) inv.UnregisterWorldPotion(this);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (highlightObject != null)
            highlightObject.SetActive(highlighted);
    }

    /// <summary>
    /// Called by DraggableFalling2D when drag starts.
    /// </summary>
    public void BringToFront()
    {
        if (_sr == null) return;

        _originalOrder = _sr.sortingOrder;
        _sr.sortingOrder = SortingOrderUtility.GetNextTopOrder();

        //Debug.Log($"PotionBehaviour: BringToFront " + potionId + " new order: " + _sr.sortingOrder);
    }

    /// <summary>
    /// Called by DraggableFalling2D when drag ends.
    /// </summary>
    public void RestoreOrderIfNeeded()
    {
        if (!restoreOrderOnRelease) return;
        if (_sr == null) return;
        _sr.sortingOrder = _originalOrder;
    }

    public void SelectThisPotion()
    {
        Debug.Log("PotionBehaviour: SelectThisPotion " + potionId);
        var inv = PotionInventoryManager.Instance;
        if (inv == null) return;
        Debug.Log("PotionBehaviour: SelectThisPotion found inv");   
        inv.SetSelectedPotion(potionId);
    }

    public void DeselectThisPotion()
    {
        var inv = PotionInventoryManager.Instance;
        if (inv == null) return;

        // Only clear if THIS potion is the currently selected one
        if (inv.SelectedPotionId == potionId)
            inv.ClearSelectedPotion();
    }
}