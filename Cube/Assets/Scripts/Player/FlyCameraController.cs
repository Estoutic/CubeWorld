using UnityEngine;
using UnityEngine.InputSystem;

namespace CubeWorld.Player
{
    /// <summary>
    /// Простой контроллер летающей камеры (New Input System).
    /// WASD — движение, мышь (ПКМ зажат) — вращение, Space/Ctrl — вверх/вниз.
    /// </summary>
    public class FlyCameraController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 20f;
        public float fastSpeed = 60f;

        [Header("Rotation")]
        public float mouseSensitivity = 0.3f;

        private float _yaw;
        private float _pitch;

        private void Start()
        {
            Vector3 angles = transform.eulerAngles;
            _yaw = angles.y;
            _pitch = angles.x;
        }

        private void Update()
        {
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            if (mouse == null || keyboard == null) return;

            // Вращение — зажать правую кнопку мыши
            if (mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                _yaw += delta.x * mouseSensitivity;
                _pitch -= delta.y * mouseSensitivity;
                _pitch = Mathf.Clamp(_pitch, -90f, 90f);
                transform.rotation = Quaternion.Euler(_pitch, _yaw, 0);
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
            }

            // Скорость: Shift = быстро
            float speed = keyboard.leftShiftKey.isPressed ? fastSpeed : moveSpeed;

            // Движение WASD
            Vector3 move = Vector3.zero;
            if (keyboard.wKey.isPressed) move += transform.forward;
            if (keyboard.sKey.isPressed) move -= transform.forward;
            if (keyboard.aKey.isPressed) move -= transform.right;
            if (keyboard.dKey.isPressed) move += transform.right;
            if (keyboard.spaceKey.isPressed) move += Vector3.up;
            if (keyboard.leftCtrlKey.isPressed) move -= Vector3.up;

            transform.position += move.normalized * speed * Time.deltaTime;
        }
    }
}
