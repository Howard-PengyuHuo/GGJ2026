using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class InjectorBehaviour : MonoBehaviour
{
    private void Reset()
    {
        var c = GetComponent<Collider2D>();
        if (c != null) c.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        //Debug.Log("InjectorBehaviour: OnTriggerEnter2D with " + other.name);

        var potion = other.GetComponent<PotionBehaviour>();
        if (potion == null) return;

        potion.SelectThisPotion();
    }
}