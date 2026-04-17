// ============================================================
//  PlayerMovement.cs
//  PlayerSystem/Movement/PlayerMovement.cs
//
//  RESPONSABILIDAD ÚNICA: Física y movimiento del jugador.
//
//  CARACTERÍSTICAS:
//  • CharacterController y Rigidbody (toggle en PlayerConfig)
//  • Aceleración / Desaceleración suavizada
//  • Coyote Time + Jump Buffer (via PlayerInput)
//  • Doble salto configurable
//  • Agacharse con ajuste de collider
//  • Correr con toggle de velocidad
//  • Autoridad: en multiplayer solo aplica si HasAuthority
//  • En modo Client: envía MoveRequest al servidor (sin mover localmente)
//  • Eventos: OnMoved, OnJumped, OnLanded
// ============================================================

using Core.Debug;
using Core.Events;
using Player.Authority;
using Player.Config;
using Player.Events;
using Player.Input;
using UnityEngine;

namespace Player.Movement
{
    [RequireComponent(typeof(PlayerAuthority))]
    [DisallowMultipleComponent]
    public class PlayerMovement : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Configuración")]
        [SerializeField] private PlayerConfig _config;

        [Header("Refs (asignar en Inspector o autodetectar)")]
        [SerializeField] private CharacterController _charController;
        [SerializeField] private Rigidbody           _rigidbody;

        [Header("Suelo")]
        [SerializeField] private LayerMask _groundMask = 1;
        [SerializeField] private Transform _groundCheck;
        [SerializeField] private float     _groundCheckRadius = 0.2f;

        // ── Referencias internas ──────────────────────────────

        private PlayerAuthority _authority;
        private PlayerInput     _playerInput;

        // ── Estado ────────────────────────────────────────────

        public Vector3 Velocity        { get; private set; }
        public bool    IsGrounded      { get; private set; }
        public bool    IsRunning       { get; private set; }
        public bool    IsCrouching     { get; private set; }

        // Movimiento horizontal suavizado
        private Vector3 _currentVelocityXZ;
        private Vector3 _targetVelocityXZ;

        // Movimiento vertical
        private float   _verticalVelocity;
        private int     _jumpsRemaining;
        private bool    _wasGrounded;
        private float   _lastGroundedTime;     // Para coyote time
        private float   _lastGroundedHeight;

        // Cache
        private Vector2 _moveInput;
        private bool    _isRunning;

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            _authority   = GetComponent<PlayerAuthority>();
            _playerInput = GetComponent<PlayerInput>();

            AutoDetectComponents();
            ValidateSetup();
        }

        private void OnEnable()
        {
            EventBus<PlayerMoveInputEvent>.Subscribe(OnMoveInput);
            EventBus<PlayerJumpRequestEvent>.Subscribe(OnJumpRequested);
        }

        private void OnDisable()
        {
            EventBus<PlayerMoveInputEvent>.Unsubscribe(OnMoveInput);
            EventBus<PlayerJumpRequestEvent>.Unsubscribe(OnJumpRequested);
        }

        private void FixedUpdate()
        {
            // En modo Client sin autoridad: no procesar movimiento local
            if (_config.UseNetworking && !_authority.HasAuthority) return;

            UpdateGroundCheck();
            UpdateHorizontalMovement();
            UpdateVerticalMovement();
            ApplyMovement();
            DispatchMovedEvent();
        }

        // ── Callbacks de eventos ──────────────────────────────

        private void OnMoveInput(PlayerMoveInputEvent e)
        {
            if (e.PlayerID != _authority.PlayerID) return;

            _moveInput  = e.MoveDirection;
            _isRunning  = e.IsRunning;
            IsRunning   = _isRunning;

            // En modo Client: enviar solicitud al servidor
            if (_config.UseNetworking && !_authority.HasAuthority)
            {
                EventBus<PlayerMoveRequestEvent>.Raise(new PlayerMoveRequestEvent
                {
                    PlayerID       = e.PlayerID,
                    InputDirection = e.MoveDirection,
                    IsRunning      = e.IsRunning,
                    JumpRequested  = false,
                    Timestamp      = Time.time
                });
            }
        }

        private void OnJumpRequested(PlayerJumpRequestEvent e)
        {
            if (e.PlayerID != _authority.PlayerID) return;
            TryJump();
        }

        // ── Movimiento ────────────────────────────────────────

        private void UpdateGroundCheck()
        {
            Vector3 checkPos = _groundCheck != null
                ? _groundCheck.position
                : transform.position + Vector3.down * 0.1f;

            _wasGrounded = IsGrounded;
            IsGrounded   = Physics.CheckSphere(checkPos, _groundCheckRadius, _groundMask);

            if (IsGrounded && !_wasGrounded)
            {
                // Aterrizaje
                float fallDist = Mathf.Max(0f, _lastGroundedHeight - transform.position.y);
                _jumpsRemaining = _config.MaxJumps;
                _verticalVelocity = 0f;

                EventBus<PlayerLandedEvent>.Raise(new PlayerLandedEvent
                {
                    PlayerID     = _authority.PlayerID,
                    FallDistance = fallDist
                });
            }

            if (IsGrounded)
            {
                _lastGroundedTime   = Time.time;
                _lastGroundedHeight = transform.position.y;
            }
        }

        private void UpdateHorizontalMovement()
        {
            float speed = IsCrouching
                ? _config.CrouchSpeed
                : (_isRunning ? _config.RunSpeed : _config.WalkSpeed);

            // Transformar input al espacio del jugador
            Vector3 inputWorld = transform.right   * _moveInput.x
                               + transform.forward * _moveInput.y;

            _targetVelocityXZ = inputWorld * speed;

            // Aceleración diferenciada en aire y suelo
            float accel = IsGrounded
                ? (_moveInput.sqrMagnitude > 0.01f ? _config.Acceleration : _config.Deceleration)
                : _config.Acceleration * _config.AirControl;

            _currentVelocityXZ = Vector3.MoveTowards(
                _currentVelocityXZ,
                _targetVelocityXZ,
                accel * Time.fixedDeltaTime
            );
        }

        private void UpdateVerticalMovement()
        {
            if (IsGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f; // Mantener pegado al suelo

            // Gravedad diferenciada al caer
            float gravityMultiplier = _verticalVelocity < 0f ? _config.FallMultiplier : 1f;
            _verticalVelocity -= _config.Gravity * gravityMultiplier * Time.fixedDeltaTime;
        }

        private void ApplyMovement()
        {
            Vector3 motion = (_currentVelocityXZ + Vector3.up * _verticalVelocity)
                             * Time.fixedDeltaTime;

            Velocity = _currentVelocityXZ + Vector3.up * _verticalVelocity;

            switch (_config.MovementType)
            {
                case MovementType.CharacterController:
                    _charController?.Move(motion);
                    break;

                case MovementType.Rigidbody:
                    if (_rigidbody != null)
                        _rigidbody.MovePosition(_rigidbody.position + motion);
                    break;
            }
        }

        // ── Salto ─────────────────────────────────────────────

        private void TryJump()
        {
            bool coyoteOk = (Time.time - _lastGroundedTime) <= _config.CoyoteTime;
            bool canJump  = IsGrounded || coyoteOk || _jumpsRemaining > 0;

            if (!canJump) return;

            // Consumir jump del buffer si existe
            _playerInput?.ConsumeJumpBuffer();

            _verticalVelocity = Mathf.Sqrt(2f * _config.JumpForce * _config.Gravity);
            _jumpsRemaining   = Mathf.Max(0, _jumpsRemaining - 1);

            CoreLogger.LogSystemDebug("PlayerMovement", $"[P{_authority.PlayerID}] Salto. Remaining={_jumpsRemaining}");

            EventBus<PlayerJumpedEvent>.Raise(new PlayerJumpedEvent
            {
                PlayerID = _authority.PlayerID,
                Position = transform.position
            });

            // En modo Client: enviar solicitud al servidor
            if (_config.UseNetworking && !_authority.HasAuthority)
            {
                EventBus<PlayerMoveRequestEvent>.Raise(new PlayerMoveRequestEvent
                {
                    PlayerID      = _authority.PlayerID,
                    JumpRequested = true,
                    Timestamp     = Time.time
                });
            }
        }

        // ── Agacharse ─────────────────────────────────────────

        public void SetCrouch(bool crouch)
        {
            if (!_config.CanCrouch) return;

            // Verificar que hay espacio para pararse
            if (!crouch && IsCrouching)
            {
                if (Physics.Raycast(transform.position, Vector3.up, _config.StandHeight + 0.1f, _groundMask))
                    return; // Techo encima, no puede pararse
            }

            IsCrouching = crouch;

            if (_charController != null)
            {
                _charController.height = crouch ? _config.CrouchHeight : _config.StandHeight;
                _charController.center = Vector3.up * (_charController.height / 2f);
            }
        }

        // ── Eventos ───────────────────────────────────────────

        private void DispatchMovedEvent()
        {
            if (Velocity.sqrMagnitude < 0.0001f) return;

            EventBus<PlayerMovedEvent>.Raise(new PlayerMovedEvent
            {
                PlayerID    = _authority.PlayerID,
                NewPosition = transform.position,
                Velocity    = Velocity
            });
        }

        // ── API Pública ───────────────────────────────────────

        /// <summary>Teletransporta al jugador. Usado en respawn.</summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            if (_charController != null)
            {
                _charController.enabled = false;
                transform.SetPositionAndRotation(position, rotation);
                _charController.enabled = true;
            }
            else
            {
                transform.SetPositionAndRotation(position, rotation);
                if (_rigidbody != null)
                {
                    _rigidbody.linearVelocity    = Vector3.zero;
                    _rigidbody.angularVelocity = Vector3.zero;
                }
            }

            _currentVelocityXZ = Vector3.zero;
            _verticalVelocity  = 0f;

            CoreLogger.LogSystemDebug("PlayerMovement", $"[P{_authority.PlayerID}] Teleport → {position}");
        }

        /// <summary>Aplica un impulso externo (ej: explosión, habilidad).</summary>
        public void AddImpulse(Vector3 impulse)
        {
            _verticalVelocity  += impulse.y;
            _currentVelocityXZ += new Vector3(impulse.x, 0f, impulse.z);
        }

        // ── Helpers ───────────────────────────────────────────

        private void AutoDetectComponents()
        {
            if (_config.MovementType == MovementType.CharacterController && _charController == null)
                _charController = GetComponent<CharacterController>();

            if (_config.MovementType == MovementType.Rigidbody && _rigidbody == null)
                _rigidbody = GetComponent<Rigidbody>();
        }

        private void ValidateSetup()
        {
            if (_config.MovementType == MovementType.CharacterController && _charController == null)
                CoreLogger.LogError("[PlayerMovement] MovementType=CharacterController pero no hay CharacterController.");

            if (_config.MovementType == MovementType.Rigidbody && _rigidbody == null)
                CoreLogger.LogError("[PlayerMovement] MovementType=Rigidbody pero no hay Rigidbody.");
        }

        // ── Gizmos ────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            if (_groundCheck != null)
            {
                Gizmos.color = IsGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(_groundCheck.position, _groundCheckRadius);
            }
        }
    }
}
