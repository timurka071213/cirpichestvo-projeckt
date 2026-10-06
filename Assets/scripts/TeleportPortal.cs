using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TeleportPortal : MonoBehaviour
{
    [SerializeField] private Transform destination;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float exitOffset = 1f;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    public void SetDestination(Transform newDestination, float newExitOffset = 0f)
    {
        destination = newDestination;
        exitOffset = newExitOffset;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (destination == null || !other.CompareTag(playerTag)) return;

        Rigidbody body = other.attachedRigidbody;
        Transform player = body != null ? body.transform : other.transform;
        Vector3 exitPosition = destination.position + destination.forward * exitOffset;

        if (body != null)
        {
            body.position = exitPosition;
            body.rotation = destination.rotation;
            body.velocity = Vector3.zero;
        }
        else
        {
            player.SetPositionAndRotation(exitPosition, destination.rotation);
        }
    }
}

