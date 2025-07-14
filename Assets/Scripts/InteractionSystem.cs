using UnityEngine;

public class InteractionSystem : MonoBehaviour
{
    [Header("交互设置")]
    public float interactionRange = 1.5f;
    public LayerMask interactableLayerMask;
    
    private Transform currentInteractable;
    
    void Update()
    {
        CheckForInteractables();
        
        if (Input.GetKeyDown(KeyCode.E) && currentInteractable != null)
        {
            Interact();
        }
    }
    
    void CheckForInteractables()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, interactionRange, interactableLayerMask);
        
        if (colliders.Length > 0)
        {
            currentInteractable = colliders[0].transform;
        }
        else
        {
            currentInteractable = null;
        }
    }
    
    void Interact()
    {
        IInteractable interactable = currentInteractable.GetComponent<IInteractable>();
        if (interactable != null)
        {
            interactable.Interact();
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}

// 交互接口
public interface IInteractable
{
    void Interact();
}