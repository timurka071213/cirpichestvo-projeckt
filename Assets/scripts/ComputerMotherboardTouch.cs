using UnityEngine;

/// <summary>Removes the dropped motherboard when the player touches it.</summary>
public class ComputerMotherboardTouch : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private void OnCollisionEnter(Collision collision)
    {
        TryDisappear(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDisappear(other);
    }

    private void TryDisappear(Collider other)
    {
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
            Destroy(gameObject);
    }
}
