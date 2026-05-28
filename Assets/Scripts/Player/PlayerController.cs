using UnityEngine;

// 玩家控制器 — 读取 Input System 的 Move 输入，驱动 CharacterController 移动
// Player controller — reads Move input from Input System, drives CharacterController movement
namespace Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField, Tooltip("移动速度 / Movement speed")]
        private float _moveSpeed = 5f;
        public float moveSpeed { get => _moveSpeed; set => _moveSpeed = value; }

        [Tooltip("相机 Transform，用于计算相对移动方向（留空则使用世界空间方向）/ Camera transform for relative movement (world-space if left empty)")]
        public Transform cameraTransform;

        private CharacterController _characterController;
        private InputSystem_Actions _input;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _input = new InputSystem_Actions();
        }

        private void OnEnable()
        {
            _input.Player.Enable();
        }

        private void OnDisable()
        {
            _input.Player.Disable();
        }

        private void Update()
        {
            Vector2 input = _input.Player.Move.ReadValue<Vector2>();
            Vector3 move = Vector3.zero;

            if (input.sqrMagnitude > 0.01f)
            {
                // 相机相对方向投影到水平面 / Project camera-relative direction onto horizontal plane
                Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
                Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();

                move = (forward * input.y + right * input.x).normalized * _moveSpeed;
            }

            // SimpleMove 自带重力，忽略 move.y / SimpleMove applies gravity, ignores move.y
            _characterController.SimpleMove(move);
        }
    }
}
