using System;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Gameplay/Disappear On Shot")]
public class DisappearOnShot : MonoBehaviour
{
    public event Action<DisappearOnShot> Shot;
    private bool wasShot;

    public void OnShot()
    {
        if (wasShot) return;
        wasShot = true;
        gameObject.SetActive(false);
        Shot?.Invoke(this);
        Destroy(gameObject);
    }
}
