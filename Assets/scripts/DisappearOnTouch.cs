using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DisappearOnTouch : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDisappear(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryDisappear(collision.collider);
    }

    private void TryDisappear(Collider other)
    {
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            Destroy(gameObject);
        }
    }
}
