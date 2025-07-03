using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    [RequireComponent(typeof(CharacterMovementsController))]
    public class Character : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private CharacterMovementsController controller;

        private void OnValidate()
        {
            if (controller == null) controller = GetComponent<CharacterMovementsController>();
        }

        private void Start()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void OnMove(InputValue value)
        {
            controller.moveInput = value.Get<Vector2>();
        }

        private void OnLook(InputValue value)
        {
            controller.lookInput = value.Get<Vector2>();
        }

        private void OnSprint(InputValue value)
        {
            controller.sprintInput = value.isPressed;
        }

        private void OnJump(InputValue value)
        {
            if (value.isPressed)
            {
                controller.TryJump();
            }
        }
    }
}
