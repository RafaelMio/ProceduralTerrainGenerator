using UnityEngine;
using Unity.Cinemachine;

namespace Player
{
    [RequireComponent(typeof(CharacterController))]
    public class CharacterMovementsController : MonoBehaviour
    {
        [Header("Movement Params")]
        public float _maxSpeed = 4f;

        [Header("Input")]
        public Vector2 _moveInput;
        public Vector2 _lookInput;

        [Header("Components")]
        [SerializeField] CinemachineCamera _FpCamera;
        [SerializeField] CharacterController _characterController;

        #region Unity Method
        private void OnValidate()
        {
            if ( _characterController == null)
            {
                _characterController = GetComponent<CharacterController>();
            }
        }

        private void Update()
        {
            MoveUpdate();
            LookUpdate();
        }
        #endregion

        #region Controller Methods
        void MoveUpdate()
        {
            Vector3 motion = transform.forward * _moveInput.y + transform.right * _moveInput.x;
            motion.y = 0f;
            motion.Normalize();

            _characterController.Move(motion * _maxSpeed * Time.deltaTime);
        }

        void LookUpdate()
        {

        }
        #endregion
    }
}

