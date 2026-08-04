// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does a lighting/color-temperature shift alone create felt unease?
// Date: 2026-08-01

using UnityEngine;

public class Interactor : MonoBehaviour
{
    public Camera eyeCamera;
    public float interactRange = 2.5f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
            TryInteract();
    }

    void TryInteract()
    {
        if (eyeCamera == null) return;

        Ray ray = new Ray(eyeCamera.transform.position, eyeCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            IInteractable target = hit.collider.GetComponent<IInteractable>();
            target?.OnInteract();
        }
    }
}
