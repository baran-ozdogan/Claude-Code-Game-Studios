// PROTOTYPE ONLY — no production dependencies.
using UnityEngine;
using UnityEngine.InputSystem;

namespace Yankilar.Greybox
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class GreyboxPlayer : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float walkSpeed = 2.25f;
        [SerializeField] private float lookSensitivity = 0.13f;

        private CharacterController controller;
        private float pitch;
        private bool gameplayEnabled = true;
        private bool carrying;

        public void SetGameplayEnabled(bool value) => gameplayEnabled = value;
        public void SetCarrying(bool value) => carrying = value;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (Keyboard.current == null || Mouse.current == null) return;
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            if (Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            if (Cursor.lockState != CursorLockMode.Locked || !gameplayEnabled) return;

            Vector2 mouse = Mouse.current.delta.ReadValue() * lookSensitivity;
            transform.Rotate(Vector3.up * mouse.x);
            pitch = Mathf.Clamp(pitch - mouse.y, -80f, 80f);
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            Vector2 input = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
            Vector3 movement = (transform.forward * input.y + transform.right * input.x).normalized;
            float carriedSpeed = carrying ? walkSpeed * .67f : walkSpeed;
            controller.Move((movement * carriedSpeed + Physics.gravity) * Time.deltaTime);
        }
    }
}
