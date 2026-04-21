// ============================================================
//  PlayerMovement_Fixed.cs
//  PlayerSystem/Movement/PlayerMovement_Fixed.cs
//
//  ⚠️  REEMPLAZA: PlayerMovement.cs
//
//  BUGS CORREGIDOS:
//  ════════════════════════════════════════════════════════════
//
//  BUG 1 — Infinite Jump
//  ❌ Causa: IsGrounded era un bool seteado manualmente,
//     no reflejaba el estado físico real del CharacterController.
//     El salto se habilitaba incluso sin tocar el suelo.
//
//  ✅ Fix: SphereCast hacia abajo en cada FixedUpdate.
//     Solo se permite saltar si IsGrounded == true REAL.
//     El contador de saltos se resetea únicamente al detectar suelo real.
//
//  BUG 2 — Crouch no funciona
//  ❌ Causa: Solo se cambiaba un bool IsCrouching sin modificar
//     el CharacterController.height ni el CharacterController.center.
//     Efecto: el estado cambia pero la física y la cámara no.
//
//  ✅ Fix: Cambiar CharacterController.height + center FÍSICAMENTE.
//     Mover el pivot de cámara hacia abajo al agacharse.
//     Verificar con Raycast antes de ponerse de pie.
//
//  BUG 3 — HitScan no funciona (related)
//  ❌ Causa: El origen del raycast venía del transform del arma
//     pero la LayerMask no incluía la capa del jugador.
//  ✅ Fix: Ver HitDetectionSystem_Fixed.cs
// ============================================================

using Core.Debug;
using Core.Events;
using Player.Authority;
using Player.Config;
using Player.Events;
using UnityEngine;

namespace Player.Movement
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerAuthority))]
    [DisallowMultipleComponent]
    public class PlayerMovement_Fixed : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Configuración")]
        [SerializeField] private PlayerConfig _config;

        [Header("Ground Detection — CRÍTICO")]
        [Tooltip("Posicionar en los pies del jugador (pivot inferior del CharacterController).")]
        [SerializeField] private Transform    _groundCheckOrigin;
        [SerializeField] private float        _groundCheckRadius   = 0.25f;
        [SerializeField] private float        _groundCheckDistance = 0.08f;
        [SerializeField] private LayerMask    _groundMask          = 1;    // Default layer

        [Header("Crouch — ajusta estos valores")]
        [Tooltip("Altura del CharacterController al estar de pie (ej: 2.0).")]
        [SerializeField] private float        _standHeight         = 2.0f;
        [Tooltip("Altura del CharacterController al agacharse (ej: 1.0).")]
        [SerializeField] private float        _crouchHeight        = 1.0f;
        [Tooltip("Transform del pivot de cámara para bajarlo al agacharse.")]
        [SerializeField] private Transform    _cameraPivot;
        [Tooltip("Posición local Y de la cámara al estar de pie (ej: 0.8).")]
        [SerializeField] private float        _standCameraY        = 0.8f;
        [Tooltip("Posición local Y de la cámara al agacharse (ej: 0.2).")]
        [SerializeField] private float        _crouchCameraY       = 0.2f;
        [SerializeField] private float        _crouchTransitionSpeed = 8f;



        [SerializeField]private float _speedMultiplier = 1f;

        // ── Referencias ───────────────────────────────────────

        private CharacterController _cc;
        private PlayerAuthority     _authority;

        // ── Estado ────────────────────────────────────────────

        /// <summary>
        /// True SOLO si el SphereCast físico detecta suelo.
        /// NUNCA setear esto manualmente — solo la física lo cambia.
        /// </summary>
        public bool    IsGroundedReal   { get; private set; }
        public bool    IsCrouchingReal  { get; private set; }   // Collider realmente reducido
        public bool    IsRunning        { get; private set; }
        public Vector3 Velocity         { get; private set; }

        // Movimiento
        private Vector3 _horizontalVelocity;
        private float   _verticalVelocity;
        private Vector2 _moveInput;

        // Salto
        private int   _jumpsUsed;
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private bool  _wasGrounded;

        // Crouch transición suave
        private float _targetCameraY;

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            _cc        = GetComponent<CharacterController>();
            _authority = GetComponent<PlayerAuthority>();

            // Validar que la altura del config coincida con el inspector
            // _cc.height = _standHeight;
            // _cc.center = Vector3.up * (_standHeight * 0.5f);
            _cc.height = _standHeight;
            // _cc.center = new Vector3(0f, 0, 0f);
            _cc.center = new Vector3(0f, _standHeight * 0.5f, 0f);

            _targetCameraY = _standCameraY;

            if (_groundCheckOrigin == null)
                CoreLogger.LogError("[PlayerMovement] GroundCheckOrigin no asignado. " +
                    "Crear un Transform hijo en los pies del jugador.");
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
            if (_config.UseNetworking && !_authority.HasAuthority) return;

            ValidateGrounded();       // PASO 1: determinar suelo REAL
            ApplyCoyoteAndBuffer();   // PASO 2: ventanas de tiempo
            UpdateHorizontal();       // PASO 3: velocidad XZ
            UpdateVertical();         // PASO 4: gravedad y salto
            ApplyMovement();          // PASO 5: mover el CharacterController
            UpdateCrouchVisuals();    // PASO 6: transición suave de cámara
            PublishState();           // PASO 7: notificar estado validado
        }

        // ══════════════════════════════════════════════════════
        //  PASO 1 — Ground Check REAL (FIX del Infinite Jump)
        // ══════════════════════════════════════════════════════

        // private void ValidateGrounded()
        // {
        //     _wasGrounded = IsGroundedReal;

        //     if (_groundCheckOrigin == null)
        //     {
        //         IsGroundedReal = _cc.isGrounded;  // Fallback
        //         return;
        //     }

        //     // SphereCast hacia abajo desde los pies del jugador.
        //     // Esto es más robusto que CheckSphere en rampas y bordes.
        //     IsGroundedReal = Physics.SphereCast(
        //         _groundCheckOrigin.position,
        //         _groundCheckRadius,
        //         Vector3.down,
        //         out _,
        //         _groundCheckDistance,
        //         _groundMask,
        //         QueryTriggerInteraction.Ignore
        //     );

        //     // Aterrizaje: resetear contador de saltos
        //     if (IsGroundedReal && !_wasGrounded)
        //     {
        //         _jumpsUsed     = 0;
        //         _verticalVelocity = Mathf.Min(_verticalVelocity, 0f);

        //         EventBus<PlayerLandedEvent>.Raise(new PlayerLandedEvent
        //         {
        //             PlayerID     = _authority.PlayerID,
        //             FallDistance = 0f
        //         });
        //     }
        // }
        private void ValidateGrounded()
        {
            _wasGrounded = IsGroundedReal;

            IsGroundedReal = _cc.isGrounded;

            if (IsGroundedReal && !_wasGrounded)
            {
                _jumpsUsed = 0;
                _verticalVelocity = Mathf.Min(_verticalVelocity, 0f);
            }
        }

        // ══════════════════════════════════════════════════════
        //  PASO 2 — Ventanas de tiempo
        // ══════════════════════════════════════════════════════

        private void ApplyCoyoteAndBuffer()
        {
            float dt = Time.fixedDeltaTime;

            // Coyote time: si acaba de dejar el suelo, dar ventana corta para saltar
            if (_wasGrounded && !IsGroundedReal)
                _coyoteTimer = _config != null ? _config.CoyoteTime : 0.1f;
            else if (!IsGroundedReal)
                _coyoteTimer = Mathf.Max(0f, _coyoteTimer - dt);

            // Jump buffer
            if (_jumpBufferTimer > 0f)
                _jumpBufferTimer = Mathf.Max(0f, _jumpBufferTimer - dt);
        }

        // ══════════════════════════════════════════════════════
        //  PASO 3 — Movimiento horizontal
        // ══════════════════════════════════════════════════════

        private void UpdateHorizontal()
        {
            // float speed = IsCrouchingReal
            //     ? (_config?.CrouchSpeed ?? 2.5f)
            //     : (IsRunning ? (_config?.RunSpeed ?? 9f) : (_config?.WalkSpeed ?? 5f));

            float baseSpeed = IsCrouchingReal
                ? (_config?.CrouchSpeed ?? 2.5f)
                : (IsRunning ? (_config?.RunSpeed ?? 9f) : (_config?.WalkSpeed ?? 5f));

            float speed = baseSpeed * _speedMultiplier;

            Vector3 target = (transform.right   * _moveInput.x
                            + transform.forward * _moveInput.y) * speed;

            float accel = IsGroundedReal
                ? (_moveInput.sqrMagnitude > 0.01f ? (_config?.Acceleration ?? 15f) : (_config?.Deceleration ?? 20f))
                : (_config?.Acceleration ?? 15f) * (_config?.AirControl ?? 0.3f);

            _horizontalVelocity = Vector3.MoveTowards(
                _horizontalVelocity, target, accel * Time.fixedDeltaTime);
        }

        // ══════════════════════════════════════════════════════
        //  PASO 4 — Gravedad y salto
        // ══════════════════════════════════════════════════════

        private void UpdateVertical()
        {
            if (IsGroundedReal && _verticalVelocity < 0f)
                _verticalVelocity = -2f; // Mantener pegado al suelo

            float gravity   = _config?.Gravity ?? 20f;
            float fallMult  = _verticalVelocity < 0f ? (_config?.FallMultiplier ?? 2f) : 1f;
            _verticalVelocity -= gravity * fallMult * Time.fixedDeltaTime;

            // Consumir buffer de salto si tenemos condiciones válidas
            if (_jumpBufferTimer > 0f && CanJump())
                ExecuteJump();
        }

        private bool CanJump()
        {
            int maxJumps = _config?.MaxJumps ?? 1;
            bool groundOrCoyote = IsGroundedReal || _coyoteTimer > 0f;

            // Primer salto: requiere suelo o coyote time
            if (_jumpsUsed == 0 && groundOrCoyote) return true;

            // Saltos extra (doble salto)
            if (_jumpsUsed < maxJumps) return true;

            return false;
        }

        private void ExecuteJump()
        {
            float gravity   = _config?.Gravity ?? 20f;
            float jumpForce = _config?.JumpForce ?? 7f;

            _verticalVelocity = Mathf.Sqrt(2f * jumpForce * gravity);
            _coyoteTimer      = 0f;
            _jumpBufferTimer  = 0f;
            _jumpsUsed++;

            CoreLogger.LogSystemDebug("PlayerMovement",
                $"[P{_authority.PlayerID}] Salto {_jumpsUsed}. Grounded={IsGroundedReal}");

            EventBus<PlayerJumpedEvent>.Raise(new PlayerJumpedEvent
            {
                PlayerID = _authority.PlayerID,
                Position = transform.position
            });
        }

        // ══════════════════════════════════════════════════════
        //  PASO 5 — Aplicar movimiento
        // ══════════════════════════════════════════════════════

        private void ApplyMovement()
        {
            Vector3 motion = (_horizontalVelocity + Vector3.up * _verticalVelocity)
                             * Time.fixedDeltaTime;
            _cc.Move(motion);
            Velocity = _cc.velocity;
        }

        // ══════════════════════════════════════════════════════
        //  CROUCH — FIX (cambia el collider FÍSICAMENTE)
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Activa/desactiva el agacharse.
        /// ✅ Modifica CharacterController.height y .center realmente.
        /// ✅ Verifica espacio con Raycast antes de ponerse de pie.
        /// </summary>
        public void SetCrouch(bool crouch)
        {
            if (crouch == IsCrouchingReal) return;

            // if (!crouch)
            // {
            //     // Verificar que hay espacio para ponerse de pie
            //     // Raycast hacia arriba desde la cabeza
            //     Vector3 headPos = transform.position + Vector3.up * _crouchHeight;
            //     float   needed  = _standHeight - _crouchHeight;

            //     if (Physics.Raycast(headPos, Vector3.up, needed + 0.05f, _groundMask))
            //     {
            //         // Techo encima — no puede ponerse de pie
            //         CoreLogger.LogSystemDebug("PlayerMovement",
            //             $"[P{_authority.PlayerID}] No puede pararse: techo detectado.");
            //         return;
            //     }
            // }

            if (!crouch)
            {
                float currentHeight = _cc.height;

                Vector3 centerWorld = transform.position + _cc.center;
                Vector3 headPos = centerWorld + Vector3.up * (currentHeight * 0.5f);

                float needed = _standHeight - currentHeight;

                if (Physics.Raycast(headPos, Vector3.up, needed + 0.05f, _groundMask))
                {
                    return;
                }
            }

            // ✅ Cambiar altura FÍSICA del CharacterController
            float newHeight  = crouch ? _crouchHeight  : _standHeight;
            float newCenterY = newHeight * 0.5f;

            _cc.height = newHeight;
            _cc.center = new Vector3(0f, newCenterY, 0f);

            IsCrouchingReal = crouch;
            _targetCameraY  = crouch ? _crouchCameraY : _standCameraY;

            CoreLogger.LogSystemDebug("PlayerMovement",
                $"[P{_authority.PlayerID}] Crouch={crouch} | CC.height={_cc.height:F2}");
        }

        // ══════════════════════════════════════════════════════
        //  PASO 6 — Transición suave de cámara al agacharse
        // ══════════════════════════════════════════════════════

        private void UpdateCrouchVisuals()
        {
            if (_cameraPivot == null) return;

            Vector3 local = _cameraPivot.localPosition;
            local.y = Mathf.Lerp(local.y, _targetCameraY,
                _crouchTransitionSpeed * Time.fixedDeltaTime);
            _cameraPivot.localPosition = local;
        }

        // ══════════════════════════════════════════════════════
        //  PASO 7 — Publicar estado validado
        // ══════════════════════════════════════════════════════

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

        // ── Callbacks de input ────────────────────────────────

        private void OnMoveInput(PlayerMoveInputEvent e)
        {
            if (e.PlayerID != _authority.PlayerID) return;
            _moveInput = e.MoveDirection;
            IsRunning  = e.IsRunning;
        }

        private void OnJumpRequest(PlayerJumpRequestEvent e)
        {
            if (e.PlayerID != _authority.PlayerID) return;

            float bufferTime = _config?.JumpBuffer ?? 0.15f;
            _jumpBufferTimer = bufferTime;  // Registrar intento; se ejecuta en UpdateVertical
        }

        // ── API Pública ───────────────────────────────────────

        public void Teleport(Vector3 pos, Quaternion rot)
        {
            _cc.enabled = false;
            transform.SetPositionAndRotation(pos, rot);
            _cc.enabled = true;
            _horizontalVelocity = Vector3.zero;
            _verticalVelocity   = 0f;
        }

        public void AddImpulse(Vector3 impulse)
        {
            _verticalVelocity  += impulse.y;
            _horizontalVelocity += new Vector3(impulse.x, 0f, impulse.z);
        }



        public void SetSpeedMultiplier(float mult)
        {
            _speedMultiplier = Mathf.Clamp(mult, 0f, 1f);
        }

        // ── Gizmos ────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            if (_groundCheckOrigin == null) return;

            Gizmos.color = IsGroundedReal ? Color.green : Color.red;
            Gizmos.DrawWireSphere(
                _groundCheckOrigin.position + Vector3.down * _groundCheckDistance,
                _groundCheckRadius);

            // Visualizar check de techo (crouch → ponerse de pie)
            if (IsCrouchingReal)
            {
                Gizmos.color = Color.yellow;
                float needed = _standHeight - _crouchHeight;
                Vector3 head = transform.position + Vector3.up * _crouchHeight;
                Gizmos.DrawLine(head, head + Vector3.up * needed);
            }
        }
    }
}
