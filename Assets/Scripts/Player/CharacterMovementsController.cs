using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Events;

namespace Player
{
    [RequireComponent(typeof(CharacterController))]
    public class CharacterMovementsController : MonoBehaviour
    {
        [Header("Movement Params")]
        public float maxSpeed => sprintInput ? sprintSpeed : walkSpeed;
        public float acceleration = 30f;
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintSpeed = 10f;
        [SerializeField] private float jumpHeight = 2f;
        [SerializeField] private bool canDoublejump = true;
        private int timesJumped = 0;
        public bool isSprinting
        {
            get
            {
                return sprintInput && sprintSpeed > 0.1f;
            }
        }

        [Header("Looking Params")]
        public Vector2 lookSensitivity = new Vector2(0.1f, 0.1f);
        public float pitchLimit = 85f;
        [SerializeField] private float currentPitch = 0f;
        public float CurrentPitch
        {
            get => currentPitch;
            set
            {
                currentPitch = Mathf.Clamp(value, -pitchLimit, pitchLimit);
            }
        }

        [Header("Physics Params")]
        [SerializeField] private float gravityScale = 2f;
        public float verticalVelocity = 0f;
        public Vector3 currentVelocity { get; private set; }
        public float currentSpeed { get; private set; }
        public bool isgrounded => characterController.isGrounded;
        private bool wasGrounded = false;

        [Header("Input")]
        public Vector2 moveInput;
        public Vector2 lookInput;
        public bool sprintInput;

        [Header("Components")]
        [SerializeField] private Camera fpCamera;
        [SerializeField] private CharacterController characterController;

        [Header("Events")]
        public UnityEvent landed;

        private void OnValidate()
        {
            if ( characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }
        }

        private void Update()
        {
            MoveUpdate();
            LookUpdate();

            if (!wasGrounded && isgrounded)
            {
                timesJumped = 0;
                landed?.Invoke();
            }

            wasGrounded = isgrounded;
        }

        public void TryJump()
        {
            if (!isgrounded)
            {
                if (!canDoublejump || timesJumped >= 2)
                {
                    return;
                }
            }

            verticalVelocity = Mathf.Sqrt(jumpHeight * -2 * Physics.gravity.y * gravityScale);
            timesJumped++;
        }

        private void MoveUpdate()
        {
            Vector3 motion = transform.forward * moveInput.y + transform.right * moveInput.x;
            motion.y = 0f;
            motion.Normalize();

            if (motion.sqrMagnitude >= 0.01f )
            {
                currentVelocity = Vector3.MoveTowards(currentVelocity, motion * maxSpeed, acceleration * Time.deltaTime);
            }
            else
            {
                currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, acceleration * Time.deltaTime);
            }

            if (isgrounded && verticalVelocity <= 0.01f)
            {
                verticalVelocity = -3f;
            }
            else
            {
                verticalVelocity += Physics.gravity.y * gravityScale * Time.deltaTime;
            }

            Vector3 fullVelocity = new Vector3(currentVelocity.x, verticalVelocity, currentVelocity.z);

            CollisionFlags flags = characterController.Move(fullVelocity * Time.deltaTime);

            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
            {
                Debug.Log("Fait gaffe à ta tête");
                verticalVelocity = 0f;
            }
            currentSpeed = currentVelocity.magnitude;
        }

        private void LookUpdate()
        {
            Vector2 input = new Vector2(lookInput.x * lookSensitivity.x, lookInput.y * lookSensitivity.y);
            CurrentPitch -= input.y;
            fpCamera.transform.localRotation = Quaternion.Euler(CurrentPitch, 0f, 0f);
            transform.Rotate(Vector3.up * input.x);
        }
    }
}

