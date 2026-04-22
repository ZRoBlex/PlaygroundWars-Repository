// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: PlayerMovement_Fixed.cs  (REEMPLAZA completo)  ║
// ║  CLASE: PlayerMovement_Fixed — mismo nombre              ║
// ║                                                          ║
// ║  FIXES:                                                  ║
// ║    ✅ Sprint funciona SIEMPRE (lee IsSprinting directo)  ║
// ║    ✅ Crouch: CC.height cambia con Lerp (1↔2)            ║
// ║    ✅ MovementMode: Instant / Snappy / Acceleration      ║
// ║    ✅ Sin sensación de hielo (Instant/Snappy por defecto) ║
// ║    ✅ SetSpeedMultiplier para efectos de estado (Slow)    ║
// ║    ✅ CC.isGrounded como fuente de verdad                 ║
// ╚══════════════════════════════════════════════════════════╝

using Core.Debug;
using Core.Events;
using Player.Authority;
using Player.Config;
using Player.Events;
using UnityEngine;

namespace Player.Movement
{
    public enum MovementMode
    {
        Instant,        // Sin transición. Dirección cambia al frame.
        Snappy,         // Pequeña aceleración (recomendado). Natural en FPS.
        Acceleration    // Aceleración completa desde PlayerConfig.
    }

    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerAuthority))]
    [DisallowMultipleComponent]
    public class PlayerMovement_Fixed : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Configuración")]
        [SerializeField] private PlayerConfig _config;

        [Header("Modo de Movimiento")]
        [Tooltip("Instant = sin inercia. Snappy = mínima. Acceleration = desde Config.")]
        [SerializeField] private MovementMode _movementMode = MovementMode.Snappy;

        [Tooltip("Aceleración en modo Snappy (recomendado: 40-60).")]
        [SerializeField] private float _snappyAccel = 50f;

        [Tooltip("Desaceleración en modo Snappy (recomendado: 60-80).")]
        [SerializeField] private float _snappyDecel = 70f;

        [Header("Crouch")]
        [SerializeField] private float _standHeight  = 2.0f;
        [SerializeField] private float _crouchHeight = 1.0f;
        [Tooltip("Velocidad del lerp de altura al agacharse/levantarse.")]
        [SerializeField] private float _crouchHeightLerpSpeed = 12f;

        [Header("Ground")]
        [SerializeField] private LayerMask _groundMask = 1;

        // ── Referencias ───────────────────────────────────────

        private CharacterController              _cc;
        private PlayerAuthority                  _authority;
        private Player.Input.PlayerInput         _playerInput; // referencia directa para sprint

        // ── Estado público ────────────────────────────────────

        public bool    IsGroundedReal   { get; private set; }
        public bool    IsCrouchingReal  { get; private set; }
        public bool    IsRunning        { get; private set; }
        public Vector3 Velocity         { get; private set; }

        // ── Estado interno ────────────────────────────────────

        private Vector3 _horizontalVelocity;
        private float   _verticalVelocity;
        private Vector2 _moveInput;

        private bool  _wasGrounded;
        private int   _jumpsUsed;
        private float _coyoteTimer;
        private float _jumpBufferTimer;

        // Crouch lerp
        private float _targetHeight;
        private float _currentHeight;
        private bool  _wantsCrouch;

        // Modificador externo (para efectos de estado: Slow, Freeze, etc.)
        private float _speedMultiplier = 1f;

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            _cc          = GetComponent<CharacterController>();
            _authority   = GetComponent<PlayerAuthority>();
            _playerInput = GetComponent<Player.Input.PlayerInput>();

            _currentHeight = _standHeight;
            _targetHeight  = _standHeight;
            _cc.height     = _standHeight;
            _cc.center     = new Vector3(0f, _standHeight * 0.5f, 0f);
        }

        private void OnEnable()
        {
            EventBus<PlayerMoveInputEvent>.Subscribe(OnMoveInput);
            EventBus<PlayerJumpRequestEvent>.Subscribe(OnJumpRequest);
        }

        private void OnDisable()
        {
            EventBus<PlayerMoveInputEvent>.Unsubscribe(OnMoveInput);
            EventBus<PlayerJumpRequestEvent>.Unsubscribe(OnJumpRequest);
        }

        private void FixedUpdate()
        {
            if (_config != null && _config.UseNetworking && !_authority.HasAuthority) return;

            // 1. Leer estado de sprint directamente (evita bug de evento no disparado)
            if (_playerInput != null)

            if (_playerInput != null)
            {
                IsRunning = _playerInput.IsSprinting;
                SetCrouch(_playerInput.IsCrouching);
            }

            // 2. Detectar suelo
            UpdateGrounded();

            // 3. Coyote + jump buffer
            UpdateTimers();

            // 4. Movimiento horizontal
            UpdateHorizontal();

            // 5. Gravedad + salto
            UpdateVertical();

            // 6. Mover CC
            ApplyMovement();

            // 7. Altura de crouch con lerp
            UpdateCrouchHeight();

            // 8. Publicar estado
            PublishState();
        }

        // ── Ground ────────────────────────────────────────────

        private void UpdateGrounded()
        {
            _wasGrounded   = IsGroundedReal;
            IsGroundedReal = _cc.isGrounded;

            if (IsGroundedReal && !_wasGrounded)
            {
                _jumpsUsed        = 0;
                _verticalVelocity = Mathf.Min(_verticalVelocity, 0f);

                EventBus<PlayerLandedEvent>.Raise(new PlayerLandedEvent
                {
                    PlayerID     = _authority.PlayerID,
                    FallDistance = 0f
                });
            }
        }

        // ── Timers ────────────────────────────────────────────

        private void UpdateTimers()
        {
            float dt = Time.fixedDeltaTime;

            if (_wasGrounded && !IsGroundedReal)
                _coyoteTimer = _config?.CoyoteTime ?? 0.1f;
            else if (!IsGroundedReal)
                _coyoteTimer = Mathf.Max(0f, _coyoteTimer - dt);

            if (_jumpBufferTimer > 0f)
                _jumpBufferTimer = Mathf.Max(0f, _jumpBufferTimer - dt);
        }

        // ── Horizontal ────────────────────────────────────────

        private void UpdateHorizontal()
        {
            float walkSpeed   = _config?.WalkSpeed   ?? 5f;
            float runSpeed    = _config?.RunSpeed    ?? 9f;
            float crouchSpeed = _config?.CrouchSpeed ?? 2.5f;

            float baseSpeed = IsCrouchingReal ? crouchSpeed : (IsRunning ? runSpeed : walkSpeed);
            float speed     = baseSpeed * _speedMultiplier;

            Vector3 target = (transform.right * _moveInput.x + transform.forward * _moveInput.y) * speed;

            switch (_movementMode)
            {
                case MovementMode.Instant:
                    _horizontalVelocity = target;
                    break;

                case MovementMode.Snappy:
                    float accel = target.sqrMagnitude > 0.01f ? _snappyAccel : _snappyDecel;
                    if (!IsGroundedReal) accel *= _config?.AirControl ?? 0.3f;
                    _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, target, accel * Time.fixedDeltaTime);
                    break;

                case MovementMode.Acceleration:
                    float configAccel = IsGroundedReal
                        ? (target.sqrMagnitude > 0.01f ? (_config?.Acceleration ?? 15f) : (_config?.Deceleration ?? 20f))
                        : (_config?.Acceleration ?? 15f) * (_config?.AirControl ?? 0.3f);
                    _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, target, configAccel * Time.fixedDeltaTime);
                    break;
            }
        }

        // ── Vertical ──────────────────────────────────────────

        private void UpdateVertical()
        {
            if (IsGroundedReal && _verticalVelocity < 0f)
                _verticalVelocity = -2f;

            float gravity  = _config?.Gravity ?? 20f;
            float fallMult = _verticalVelocity < 0f ? (_config?.FallMultiplier ?? 2f) : 1f;
            _verticalVelocity -= gravity * fallMult * Time.fixedDeltaTime;

            if (_jumpBufferTimer > 0f && CanJump())
                ExecuteJump();
        }

        private bool CanJump()
        {
            int  maxJumps       = _config?.MaxJumps ?? 1;
            bool groundOrCoyote = IsGroundedReal || _coyoteTimer > 0f;
            if (_jumpsUsed == 0 && groundOrCoyote) return true;
            if (_jumpsUsed < maxJumps)             return true;
            return false;
        }

        private void ExecuteJump()
        {
            float gravity   = _config?.Gravity    ?? 20f;
            float jumpForce = _config?.JumpForce  ?? 7f;

            _verticalVelocity = Mathf.Sqrt(2f * jumpForce * gravity);
            _coyoteTimer      = 0f;
            _jumpBufferTimer  = 0f;
            _jumpsUsed++;

            EventBus<PlayerJumpedEvent>.Raise(new PlayerJumpedEvent
            {
                PlayerID = _authority.PlayerID,
                Position = transform.position
            });
        }

        // ── Apply ─────────────────────────────────────────────

        private void ApplyMovement()
        {
            Vector3 motion = (_horizontalVelocity + Vector3.up * _verticalVelocity) * Time.fixedDeltaTime;
            _cc.Move(motion);
            Velocity = _cc.velocity;
        }

        // ── Crouch con Lerp de altura ─────────────────────────

        /// <summary>
        /// Actualiza la altura del CharacterController con Lerp.
        /// Solo cambia de _targetHeight. IsCrouchingReal se basa
        /// en si la altura actual está cerca del crouchHeight.
        /// </summary>
        private void UpdateCrouchHeight()
        {
            if (Mathf.Abs(_currentHeight - _targetHeight) < 0.001f)
            {
                _currentHeight    = _targetHeight;
                _cc.height        = _currentHeight;
                _cc.center        = new Vector3(0f, _currentHeight * 0.5f, 0f);
                IsCrouchingReal   = _currentHeight < (_standHeight + _crouchHeight) * 0.5f;
                return;
            }

            // Antes de crecer: verificar techo
            if (_targetHeight > _currentHeight)
            {
                Vector3 top    = transform.position + Vector3.up * (_currentHeight - 0.1f);
                float   needed = _targetHeight - _currentHeight + 0.05f;
                if (Physics.SphereCast(top, 0.15f, Vector3.up, out _, needed, _groundMask, QueryTriggerInteraction.Ignore))
                {
                    // Techo detectado — no crecer
                    return;
                }
            }

            _currentHeight = Mathf.Lerp(_currentHeight, _targetHeight, _crouchHeightLerpSpeed * Time.fixedDeltaTime);
            _cc.height     = _currentHeight;
            _cc.center     = new Vector3(0f, _currentHeight * 0.5f, 0f);
            IsCrouchingReal = _currentHeight < (_standHeight + _crouchHeight) * 0.5f;
        }

        // ── API pública ───────────────────────────────────────

        /// <summary>Activa/desactiva el agacharse.</summary>
        public void SetCrouch(bool crouch)
        {
            if (_wantsCrouch == crouch) return;
            _wantsCrouch   = crouch;
            _targetHeight  = crouch ? _crouchHeight : _standHeight;
        }

        /// <summary>Multiplicador de velocidad para efectos (0=freeze, 0.5=slow).</summary>
        public void SetSpeedMultiplier(float mult)
            => _speedMultiplier = Mathf.Clamp(mult, 0f, 10f);

        public void Teleport(Vector3 pos, Quaternion rot)
        {
            _cc.enabled = false;
            transform.SetPositionAndRotation(pos, rot);
            _cc.enabled         = true;
            _horizontalVelocity = Vector3.zero;
            _verticalVelocity   = 0f;
        }

        public void AddImpulse(Vector3 impulse)
        {
            _verticalVelocity   += impulse.y;
            _horizontalVelocity += new Vector3(impulse.x, 0f, impulse.z);
        }

        // ── Callbacks ─────────────────────────────────────────

        private void OnMoveInput(PlayerMoveInputEvent e)
        {
            if (e.PlayerID != _authority.PlayerID) return;
            _moveInput = e.MoveDirection;
            // IsRunning se lee directamente de PlayerInput en FixedUpdate
            // para evitar el bug de "sprint solo funciona antes de caminar"
        }

        private void OnJumpRequest(PlayerJumpRequestEvent e)
        {
            if (e.PlayerID != _authority.PlayerID) return;
            _jumpBufferTimer = _config?.JumpBuffer ?? 0.15f;
        }

        // ── State publish ─────────────────────────────────────

        private void PublishState()
        {
            EventBus<Core.Events.MovementStateValidatedEvent>.Raise(
                new Core.Events.MovementStateValidatedEvent
                {
                    PlayerID        = _authority.PlayerID,
                    IsGroundedReal  = IsGroundedReal,
                    IsCrouchingReal = IsCrouchingReal,
                    Velocity        = Velocity,
                    HorizontalSpeed = new Vector3(Velocity.x, 0, Velocity.z).magnitude
                });
        }

        // ── Gizmos ────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            // Base del CC
            Gizmos.color = IsGroundedReal ? Color.green : Color.red;
            float r = _cc != null ? _cc.radius : 0.3f;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * r, r);

            // Techo (crouch uncheck)
            if (IsCrouchingReal)
            {
                Gizmos.color = Color.yellow;
                float needed = _standHeight - _currentHeight;
                Vector3 top  = transform.position + Vector3.up * _currentHeight;
                Gizmos.DrawLine(top, top + Vector3.up * needed);
                Gizmos.DrawWireSphere(top + Vector3.up * needed, 0.15f);
            }
        }
    }
}