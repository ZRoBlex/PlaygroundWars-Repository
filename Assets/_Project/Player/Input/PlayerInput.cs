// ============================================================
//  PlayerInput.cs
//  PlayerSystem/Input/PlayerInput.cs
//
//  RESPONSABILIDAD ÚNICA: Capturar y distribuir input del jugador.
//
//  CARACTERÍSTICAS:
//  • Usa Unity New Input System (com.unity.inputsystem)
//  • Keyboard+Mouse y Gamepad con el mismo código
//  • Distribuye TODOS los inputs via EventBus (sin polling externo)
//  • Jump buffer integrado
//  • Keybinding preparado (InputActionAsset serializado)
//  • Solo procesa input si IsLocalPlayer (nunca para remotos)
//
//  SETUP:
//  1. Instalar "Input System" desde Package Manager
//  2. Crear un InputActionAsset (Player Actions) con:
//     - Action Map "Player"
//     - Actions: Move, Look, Jump, Sprint, Shoot, Ability0/1/2, Interact, Crouch
//  3. Asignar el asset al campo _inputActions
//  4. O activar el modo Legacy para usar el Input viejo (fallback)
// ============================================================

using System.Collections;
using Core.Debug;
using Core.Events;
using Player.Authority;
using Player.Config;
using Player.Events;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Input
{
    [RequireComponent(typeof(PlayerAuthority))]
    [DisallowMultipleComponent]
    public class PlayerInput : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Configuración")]
        [SerializeField] private PlayerConfig _config;

        [Header("Input Actions Asset")]
        [Tooltip("Asignar el InputActionAsset del proyecto. Opcional si usas Legacy.")]
        [SerializeField] private InputActionAsset _inputActions;

        [Header("Fallback")]
        [Tooltip("Usa Input.GetAxis() si no hay InputActionAsset. Para proyectos sin New Input System.")]
        [SerializeField] private bool _useLegacyInput = false;

        // ── Referencias ───────────────────────────────────────

        private PlayerAuthority _authority;

        // ── Acciones cacheadas ────────────────────────────────

        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;
        private InputAction _crouchAction;
        private InputAction _shootAction;
        private InputAction _ability0Action;
        private InputAction _ability1Action;
        private InputAction _ability2Action;
        private InputAction _interactAction;

        // ── Estado del input ──────────────────────────────────

        public Vector2 MoveInput    { get; private set; }
        public Vector2 LookInput    { get; private set; }
        public bool    IsSprinting  { get; private set; }
        public bool    IsCrouching  { get; private set; }
        public bool    IsShootHeld  { get; private set; }

        // Jump buffer
        private float _jumpBufferTimer;
        private Coroutine _jumpBufferCoroutine;

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            _authority = GetComponent<PlayerAuthority>();
        }

        private void OnEnable()
        {
            if (!_useLegacyInput)
                SetupNewInputSystem();
        }

        private void OnDisable()
        {
            if (!_useLegacyInput)
                TeardownNewInputSystem();

            // Limpiar estado al deshabilitar
            MoveInput   = Vector2.zero;
            LookInput   = Vector2.zero;
            IsSprinting = false;
        }

        // ── Update: SOLO para Legacy y Look continuo ──────────

        private void Update()
        {
            // Solo procesar si somos el jugador local
            if (!_authority.IsLocalPlayer) return;

            if (_useLegacyInput)
                ProcessLegacyInput();
            else
                DispatchContinuousEvents();
        }

        // ── New Input System Setup ────────────────────────────

        private void SetupNewInputSystem()
        {
            if (_inputActions == null)
            {
                CoreLogger.LogWarning("[PlayerInput] InputActionAsset no asignado. Usa Legacy o asigna el asset.");
                _useLegacyInput = true;
                return;
            }

            var map = _inputActions.FindActionMap("Player", throwIfNotFound: false);
            if (map == null)
            {
                CoreLogger.LogError("[PlayerInput] Action Map 'Player' no encontrado en el InputActionAsset.");
                return;
            }

            _moveAction     = map.FindAction("Move");
            _lookAction     = map.FindAction("Look");
            _jumpAction     = map.FindAction("Jump");
            _sprintAction   = map.FindAction("Sprint");
            _crouchAction   = map.FindAction("Crouch");
            _shootAction    = map.FindAction("Shoot");
            _ability0Action = map.FindAction("Ability0");
            _ability1Action = map.FindAction("Ability1");
            _ability2Action = map.FindAction("Ability2");
            _interactAction = map.FindAction("Interact");

            // Suscribir callbacks de acciones "one-shot"
            if (_jumpAction     != null) _jumpAction.performed     += OnJump;
            if (_sprintAction   != null)
            {
                _sprintAction.performed  += ctx => SetSprint(true);
                _sprintAction.canceled   += ctx => SetSprint(false);
            }
            if (_crouchAction   != null)
            {
                _crouchAction.performed  += ctx => SetCrouch(true);
                _crouchAction.canceled   += ctx => SetCrouch(false);
            }
            if (_shootAction    != null)
            {
                _shootAction.performed   += ctx => OnShoot(true);
                _shootAction.canceled    += ctx => OnShoot(false);
            }
            if (_ability0Action != null) _ability0Action.performed += _ => OnAbility(0);
            if (_ability1Action != null) _ability1Action.performed += _ => OnAbility(1);
            if (_ability2Action != null) _ability2Action.performed += _ => OnAbility(2);
            if (_interactAction != null) _interactAction.performed += _ => OnInteract();

            _inputActions.Enable();
            CoreLogger.LogSystemDebug("PlayerInput", "New Input System configurado.");
        }

        private void TeardownNewInputSystem()
        {
            if (_inputActions == null) return;

            if (_jumpAction     != null) _jumpAction.performed     -= OnJump;
            if (_ability0Action != null) _ability0Action.performed -= _ => OnAbility(0);
            if (_ability1Action != null) _ability1Action.performed -= _ => OnAbility(1);
            if (_ability2Action != null) _ability2Action.performed -= _ => OnAbility(2);
            if (_interactAction != null) _interactAction.performed -= _ => OnInteract();

            _inputActions.Disable();
        }

        // ── Dispatch continuo (New Input System) ─────────────

        private void DispatchContinuousEvents()
        {
            // Leer movimiento y look cada frame (se necesita para física)
            Vector2 move = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
            Vector2 look = _lookAction?.ReadValue<Vector2>() ?? Vector2.zero;

            int id = _authority.PlayerID;

            // Solo disparar si hay cambio (evitar allocations continuas en EventBus)
            if (move != MoveInput)
            {
                MoveInput = move;
                EventBus<PlayerMoveInputEvent>.Raise(new PlayerMoveInputEvent
                {
                    PlayerID      = id,
                    MoveDirection = move,
                    IsRunning     = IsSprinting
                });
            }

            if (look.sqrMagnitude > 0.0001f)
            {
                LookInput = look;
                EventBus<PlayerLookInputEvent>.Raise(new PlayerLookInputEvent
                {
                    PlayerID  = id,
                    LookDelta = look
                });
            }
        }

        // ── Callbacks New Input System ────────────────────────

        private void OnJump(InputAction.CallbackContext ctx)
        {
            if (!_authority.IsLocalPlayer) return;

            // Activar jump buffer
            if (_jumpBufferCoroutine != null)
                StopCoroutine(_jumpBufferCoroutine);
            _jumpBufferCoroutine = StartCoroutine(JumpBufferRoutine());

            EventBus<PlayerJumpRequestEvent>.Raise(new PlayerJumpRequestEvent
            {
                PlayerID = _authority.PlayerID
            });
        }

        private void OnShoot(bool pressed)
        {
            if (!_authority.IsLocalPlayer) return;

            IsShootHeld = pressed;
            EventBus<PlayerShootInputEvent>.Raise(new PlayerShootInputEvent
            {
                PlayerID  = _authority.PlayerID,
                IsPressed = pressed
            });
        }

        private void OnAbility(int slot)
        {
            if (!_authority.IsLocalPlayer) return;

            EventBus<PlayerAbilityInputEvent>.Raise(new PlayerAbilityInputEvent
            {
                PlayerID    = _authority.PlayerID,
                AbilitySlot = slot
            });
        }

        private void OnInteract()
        {
            if (!_authority.IsLocalPlayer) return;

            EventBus<PlayerInteractInputEvent>.Raise(new PlayerInteractInputEvent
            {
                PlayerID = _authority.PlayerID
            });
        }

        private void SetSprint(bool value)
        {
            IsSprinting = value;
        }

        private void SetCrouch(bool value)
        {
            IsCrouching = value;
        }

        // ── Legacy Input (fallback) ───────────────────────────

        private void ProcessLegacyInput()
        {
            int id = _authority.PlayerID;

            // Movimiento
            Vector2 move = new Vector2(
                UnityEngine.Input.GetAxisRaw("Horizontal"),
                UnityEngine.Input.GetAxisRaw("Vertical")
            );

            if (move != MoveInput)
            {
                MoveInput = move;
                EventBus<PlayerMoveInputEvent>.Raise(new PlayerMoveInputEvent
                {
                    PlayerID      = id,
                    MoveDirection = move,
                    IsRunning     = IsSprinting
                });
            }

            // Look (mouse)
            Vector2 look = new Vector2(
                UnityEngine.Input.GetAxis("Mouse X"),
                UnityEngine.Input.GetAxis("Mouse Y")
            );

            if (look.sqrMagnitude > 0.0001f)
            {
                LookInput = look;
                EventBus<PlayerLookInputEvent>.Raise(new PlayerLookInputEvent
                {
                    PlayerID  = id,
                    LookDelta = look
                });
            }

            // Salto
            if (UnityEngine.Input.GetButtonDown("Jump"))
                EventBus<PlayerJumpRequestEvent>.Raise(new PlayerJumpRequestEvent { PlayerID = id });

            // Sprint
            IsSprinting = UnityEngine.Input.GetKey(KeyCode.LeftShift);

            // Crouch
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftControl))
                SetCrouch(true);
            if (UnityEngine.Input.GetKeyUp(KeyCode.LeftControl))
                SetCrouch(false);

            // Disparo
            bool shootNow = UnityEngine.Input.GetMouseButton(0);
            if (shootNow != IsShootHeld)
                OnShoot(shootNow);

            // Habilidades
            if (UnityEngine.Input.GetKeyDown(KeyCode.Q)) OnAbility(0);
            if (UnityEngine.Input.GetKeyDown(KeyCode.E)) OnAbility(1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.R)) OnAbility(2);

            // Interacción
            if (UnityEngine.Input.GetKeyDown(KeyCode.F)) OnInteract();
        }

        // ── Jump Buffer ───────────────────────────────────────

        private IEnumerator JumpBufferRoutine()
        {
            _jumpBufferTimer = _config != null ? _config.JumpBuffer : 0.15f;
            while (_jumpBufferTimer > 0f)
            {
                _jumpBufferTimer -= UnityEngine.Time.deltaTime;
                yield return null;
            }
            _jumpBufferTimer = 0f;
        }

        /// <summary>Consume el jump buffer. Llamado por PlayerMovement al ejecutar el salto.</summary>
        public bool ConsumeJumpBuffer()
        {
            if (_jumpBufferTimer > 0f)
            {
                _jumpBufferTimer = 0f;
                return true;
            }
            return false;
        }

        // ── API Pública ───────────────────────────────────────

        /// <summary>Deshabilita todos los inputs (ej: durante cinemáticas o death).</summary>
        public void DisableInput()
        {
            _inputActions?.Disable();
            MoveInput   = Vector2.zero;
            LookInput   = Vector2.zero;
            IsSprinting = false;
            enabled     = false;
        }

        /// <summary>Reactiva los inputs.</summary>
        public void EnableInput()
        {
            enabled = true;
            _inputActions?.Enable();
        }
    }
}
