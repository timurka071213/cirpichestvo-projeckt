using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ShotTargetGoal : MonoBehaviour
{
    private static readonly Vector3 RewardPortalDestination = new Vector3(66.7f, 2f, -6.1f);
    private readonly HashSet<DisappearOnShot> remainingTargets = new HashSet<DisappearOnShot>();
    private bool completed;

    private void Start()
    {
        // Include inactive targets that may be enabled later in this scene.
        foreach (DisappearOnShot target in FindObjectsOfType<DisappearOnShot>(true))
        {
            if (target.gameObject.scene != gameObject.scene) continue;
            remainingTargets.Add(target);
            target.Shot += OnTargetShot;
        }
    }

    private void OnTargetShot(DisappearOnShot target)
    {
        if (completed || !remainingTargets.Remove(target)) return;
        target.Shot -= OnTargetShot;
        if (remainingTargets.Count != 0) return;

        completed = true;
        Vector3 spawnPosition = target.transform.position + Vector3.up * 0.5f;
        GameObject reward = GameObject.CreatePrimitive(PrimitiveType.Cube);
        reward.name = "Блок за уничтожение всех сфер";
        reward.transform.position = spawnPosition;
        Vector3 rewardScale = new Vector3(2f, 1f, 0.5f);
        reward.transform.localScale = Vector3.zero;
        reward.GetComponent<Collider>().isTrigger = false;
        BoxCollider pickupTrigger = reward.AddComponent<BoxCollider>();
        pickupTrigger.isTrigger = true;
        Rigidbody body = reward.AddComponent<Rigidbody>();
        body.isKinematic = false;
        body.useGravity = true;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        reward.AddComponent<DisappearOnTouch>();
        RewardPortalActivator portalActivator = reward.AddComponent<RewardPortalActivator>();
        portalActivator.destinationPosition = RewardPortalDestination;

        StartCoroutine(AnimateRewardAppearance(reward.transform, rewardScale));
    }

    private IEnumerator AnimateRewardAppearance(Transform reward, Vector3 targetScale)
    {
        const float duration = 0.45f;
        float elapsed = 0f;

        while (elapsed < duration && reward != null)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            reward.localScale = Vector3.LerpUnclamped(Vector3.zero, targetScale, easedProgress);
            yield return null;
        }

        if (reward != null)
            reward.localScale = targetScale;
    }

    private void OnDestroy()
    {
        foreach (DisappearOnShot target in remainingTargets)
        {
            if (target != null) target.Shot -= OnTargetShot;
        }
        remainingTargets.Clear();
    }
}

public class RewardPortalActivator : MonoBehaviour
{
    [HideInInspector] public Vector3 destinationPosition;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float portalDistance = 2f;

    private bool activated;

    private void OnTriggerEnter(Collider other)
    {
        TryActivate(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryActivate(collision.collider);
    }

    private void TryActivate(Collider other)
    {
        if (activated) return;
        if (!other.CompareTag(playerTag) && !other.transform.root.CompareTag(playerTag)) return;

        activated = true;
        Rigidbody playerBody = other.attachedRigidbody;
        Transform player = playerBody != null ? playerBody.transform : other.transform.root;
        TeleportPortal originalPortal = FindObjectOfType<TeleportPortal>(true);
        if (originalPortal == null) return;

        Vector3 portalPosition = player.position + player.forward * portalDistance;
        Vector3 floorRayOrigin = portalPosition + Vector3.up * 2f;
        if (Physics.Raycast(
                floorRayOrigin,
                Vector3.down,
                out RaycastHit floorHit,
                10f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            portalPosition.y = floorHit.point.y + 0.06f;
        }

        GameObject newPortal = Instantiate(
            originalPortal.gameObject,
            portalPosition,
            originalPortal.transform.rotation);
        newPortal.name = "Новый портал после прямоугольника";

        GameObject destination = new GameObject("Точка назначения нового портала");
        destination.transform.position = destinationPosition;
        TeleportPortal newPortalComponent = newPortal.GetComponent<TeleportPortal>();
        if (newPortalComponent != null)
            newPortalComponent.SetDestination(destination.transform);
    }
}
