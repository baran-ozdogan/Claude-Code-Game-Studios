// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does a lighting/color-temperature shift alone create felt unease?
// Date: 2026-08-01

using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonWalker : MonoBehaviour
{
    public float moveSpeed = 2.2f; // meditative pace, not a sprint
    public float mouseSensitivity = 2f;
    public Camera eyeCamera;

    private CharacterController _controller;
    private float _pitch;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);
        _pitch -= mouseY;
        _pitch = Mathf.Clamp(_pitch, -80f, 80f);
        if (eyeCamera != null)
            eyeCamera.transform.localEulerAngles = new Vector3(_pitch, 0f, 0f);

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 move = transform.right * h + transform.forward * v;
        _controller.SimpleMove(move * moveSpeed);

        if (Input.GetKeyDown(KeyCode.Escape))
            Cursor.lockState = CursorLockMode.None;
    }
}
