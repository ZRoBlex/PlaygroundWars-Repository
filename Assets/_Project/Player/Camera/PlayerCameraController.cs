// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: PlayerCameraController.cs  (REEMPLAZA)         ║
// ║  CLASE: PlayerCameraController — mismo nombre            ║
// ║                                                          ║
// ║  NUEVAS FUNCIONES:                                       ║
// ║    • Head Bob (oscilación al caminar/correr)             ║
// ║    • Zoom (scroll o tecla, lerp de FOV)                  ║
// ║    • ADS (apuntar — FOV + offset de posición)            ║
// ║    • Focus / Concentrarse (FOV reducido + sensibilidad)  ║
// ║    • Camera Tilt (inclinación al strafe)                 ║
// ║    • Landing Impact (sacudida al aterrizar)              ║
// ║    • Cursor Lock integrado con toggle key                ║
// ║    • Shake mejorado (traumatismo acumulable)             ║
// ╚══════════════════════════════════════════════════════════╝

using Core.Events;
using Player.Authority;
using Player.Config;
using Player.Events;
using Player.Movement;
using UnityEngine;

namespace Player.Camera
{
    [RequireComponent(typeof(PlayerAuthority))]
    [DisallowMultipleComponent]
    public class PlayerCameraController : MonoBehaviour
    {
        // ── Inspector: Básico ─────────────────────────────────

        [Header("Configuración")]
        [SerializeField] private PlayerConfig _config;
        [SerializeField] private Transform    _cameraPivot;
        [SerializeField] private UnityEngine.Camera _camera;

        // ── Inspector: Cursor ─────────────────────────────────

        [Header("Cursor Lock")]
        [SerializeField] private bool    _lockOnStart    = true;
        [SerializeField] private KeyCode _cursorToggleKey = KeyCode.Escape;

        // ── Inspector: Head Bob ───────────────────────────────

        [Header("Head Bob")]
        [SerializeField] private bool  _headBobEnabled     = true;
        [SerializeField] private float _walkBobFrequency   = 6f;
        [SerializeField] private float _runBobFrequency    = 10f;
        [SerializeField] private float _walkBobAmplitude   = 0.04f;
        [SerializeField] private float _runBobAmplitude    = 0.07f;
        [SerializeField] private float _crouchBobAmplitude = 0.02f;
        [SerializeField] private float _bobSmoothing       = 12f;

        // ── Inspector: Camera Tilt ────────────────────────────

        [Header("Camera Tilt (Strafe)")]
        [SerializeField] private bool  _tiltEnabled  = true;
        [SerializeField] private float _tiltAmount   = 3f;
        [SerializeField] private float _tiltSpeed    = 8f;

        // ── Inspector: Zoom ───────────────────────────────────

        [Header("Zoom")]
        [SerializeField] private bool    _zoomEnabled   = true;
        [SerializeField] private float   _normalFOV     = 60f;
        [SerializeField] private float   _zoomedFOV     = 40f;
        [SerializeField] private float   _zoomSpeed     = 10f;
        [SerializeField] private KeyCode _zoomKey       = KeyCode.Z;

        // ── Inspector: ADS ────────────────────────────────────

        [Header("ADS (Aim Down Sights)")]
        [SerializeField] private bool  _adsEnabled = true;
        [SerializeField] private float _adsFOV     = 35f;
        [SerializeField] private float _adsSensMultiplier = 0.6f;

        // ── Inspector: Focus ──────────────────────────────────

        [Header("Focus")]
        [SerializeField] private bool    _focusEnabled = true;
        [SerializeField] private float   _focusFOV     = 45f;
        [SerializeField] private float   _focusSensMultiplier = 0.5f;
        [SerializeField] private KeyCode _focusKey     = KeyCode.LeftAlt;

        // ── Inspector: Shake ──────────────────────────────────

        [Header("Shake")]
        [SerializeField] private float _shakeDecay    = 3f;
        [SerializeField] private float _shakeFrequency = 25f;

        // ── Inspector: Third Person ───────────────────────────

        [Header("Third Person")]
        [SerializeField] private Transform _thirdPersonTarget;
        [SerializeField] private LayerMask _cameraObstacleMask;

        // ── Referencias ───────────────────────────────────────

        private PlayerAuthority    _authority;
        private PlayerMovement_Fixed _movement;

        // ── Estado de rotación ────────────────────────────────

        public float CurrentYaw   { get; private set; }
        public float CurrentPitch { get; private set; }

        private float _smoothYaw;
        private float _smoothPitch;

        // ── Recoil ────────────────────────────────────────────

        private float _recoilPitch;
        private float _recoilYaw;

        // ── Head Bob ──────────────────────────────────────────

        private float   _bobTimer;
        private Vector3 _bobCurrentOffset;
        private Vector3 _basePivotLocalPos;

        // ── Tilt ──────────────────────────────────────────────

        private float _currentTilt;

        // ── Shake ─────────────────────────────────────────────

        private float _shakeTrauma;    // 0-1, acumulable
        private float _shakeTime;

        // ── Zoom / ADS / Focus ────────────────────────────────

        public bool  IsADS     { get; private set; }
        public bool  IsZooming { get; private set; }
        public bool  IsFocused { get; private set; }

        private float _targetFOV;
        private float _sensMultiplier = 1f;

        // ── Estado de movimiento (desde event) ────────────────

        private Vector2 _lastMoveInput;
        private float   _currentSpeed;
        private bool    _isRunning;
        private bool    _isCrouching;

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            _authority = GetComponent<PlayerAuthority>();
            _movement  = GetComponent<PlayerMovement_Fixed>();

            if (_camera == null)
                _camera = GetComponentInChildren<UnityEngine.Camera>();

            _targetFOV = _normalFOV;
        }

        private void Start()
        {
            if (_cameraPivot != null)
                _basePivotLocalPos = _cameraPivot.localPosition;

            if (_lockOnStart && _authority.IsLocalPlayer)
                LockCursor();
        }

        private void OnEnable()
        {
            EventBus<PlayerLookInputEvent>.Subscribe(OnLookInput);
            EventBus<PlayerReadyEvent>.Subscribe(OnPlayerReady);
            EventBus<PlayerMoveInputEvent>.Subscribe(OnMoveInput);
            EventBus<PlayerLandedEvent>.Subscribe(OnLanded);
            EventBus<Core.Events.MovementStateValidatedEvent>.Subscribe(OnMovementState);
        }

        private void OnDisable()
        {
            EventBus<PlayerLookInputEvent>.Unsubscribe(OnLookInput);
            EventBus<PlayerReadyEvent>.Unsubscribe(OnPlayerReady);
            EventBus<PlayerMoveInputEvent>.Unsubscribe(OnMoveInput);
            EventBus<PlayerLandedEvent>.Unsubscribe(OnLanded);
            EventBus<Core.Events.MovementStateValidatedEvent>.Unsubscribe(OnMovementState);

            if (_authority != null && _authority.IsLocalPlayer)
                ReleaseCursor();
        }

        private void LateUpdate()
        {
            if (!_authority.IsLocalPlayer) return;

            HandleCursorToggle();
            HandleZoomInput();
            UpdateFOV();
            UpdateRecoilDecay();
            UpdateHeadBob();
            UpdateTilt();
            UpdateShake();
            ApplyRotation();

            if (_config != null && _config.CameraMode == CameraMode.ThirdPerson)
                UpdateThirdPerson();
        }

        // ── Cursor ────────────────────────────────────────────

        private void HandleCursorToggle()
        {
            if (UnityEngine.Input.GetKeyDown(_cursorToggleKey))
                ToggleCursor();
        }

        public void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        public void ReleaseCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        public void ToggleCursor()
        {
            if (Cursor.lockState == CursorLockMode.Locked) ReleaseCursor();
            else LockCursor();
        }

        // ── Zoom / ADS / Focus input ──────────────────────────

        private void HandleZoomInput()
        {
            // ADS con clic derecho
            if (_adsEnabled)
            {
                bool adsHeld = UnityEngine.Input.GetMouseButton(1);
                if (adsHeld != IsADS)
                {
                    IsADS        = adsHeld;
                    _sensMultiplier = IsADS ? _adsSensMultiplier : 1f;
                }
            }

            // Focus con tecla
            if (_focusEnabled)
            {
                bool focusHeld = UnityEngine.Input.GetKey(_focusKey);
                if (focusHeld != IsFocused)
                {
                    IsFocused = focusHeld;
                    if (!IsADS) _sensMultiplier = IsFocused ? _focusSensMultiplier : 1f;
                }
            }

            // Zoom extra con tecla
            if (_zoomEnabled)
                IsZooming = UnityEngine.Input.GetKey(_zoomKey);
        }

        private void UpdateFOV()
        {
            if (_camera == null) return;

            // Prioridad: ADS > Zoom > Focus > Normal
            _targetFOV = IsADS      ? _adsFOV   :
                         IsZooming  ? _zoomedFOV :
                         IsFocused  ? _focusFOV  :
                         _normalFOV;

            _camera.fieldOfView = Mathf.Lerp(
                _camera.fieldOfView, _targetFOV, _zoomSpeed * Time.deltaTime);
        }

        // ── Head Bob ──────────────────────────────────────────

        private void UpdateHeadBob()
        {
            if (!_headBobEnabled || _cameraPivot == null) return;

            float speed = _currentSpeed;

            if (speed < 0.1f)
            {
                _bobCurrentOffset = Vector3.Lerp(_bobCurrentOffset, Vector3.zero,
                    Time.deltaTime * _bobSmoothing);
            }
            else
            {
                float freq = _isRunning ? _runBobFrequency : _walkBobFrequency;
                float amp  = _isCrouching ? _crouchBobAmplitude :
                             (_isRunning ? _runBobAmplitude : _walkBobAmplitude);

                _bobTimer += Time.deltaTime * freq;
                float bobY = Mathf.Sin(_bobTimer)         * amp;
                float bobX = Mathf.Sin(_bobTimer * 0.5f)  * amp * 0.5f;

                Vector3 targetBob = new Vector3(bobX, bobY, 0f);
                _bobCurrentOffset = Vector3.Lerp(_bobCurrentOffset, targetBob,
                    Time.deltaTime * _bobSmoothing);
            }

            _cameraPivot.localPosition = _basePivotLocalPos + _bobCurrentOffset;
        }

        // ── Tilt ──────────────────────────────────────────────

        private void UpdateTilt()
        {
            if (!_tiltEnabled) return;

            float targetTilt = -_lastMoveInput.x * _tiltAmount;
            _currentTilt     = Mathf.Lerp(_currentTilt, targetTilt, _tiltSpeed * Time.deltaTime);
        }

        // ── Shake ─────────────────────────────────────────────

        /// <summary>Añade trauma de shake (0-1). Se acumula hasta 1.</summary>
        public void AddShake(float trauma)
        {
            _shakeTrauma = Mathf.Clamp01(_shakeTrauma + trauma);
        }

        /// <summary>Shake de alta magnitud breve (explosiones, golpes).</summary>
        public void AddImpactShake(float magnitude)
            => AddShake(magnitude);

        private void UpdateShake()
        {
            if (_shakeTrauma <= 0f) return;

            _shakeTrauma = Mathf.Max(0f, _shakeTrauma - _shakeDecay * Time.deltaTime);
            _shakeTime  += Time.deltaTime * _shakeFrequency;

            float shakeMag = _shakeTrauma * _shakeTrauma; // cuadrático = más dramático

            float shakeYaw   = Mathf.PerlinNoise(_shakeTime,       0f) * 2f - 1f;
            float shakePitch = Mathf.PerlinNoise(_shakeTime + 100f, 0f) * 2f - 1f;

            _recoilYaw   += shakeYaw   * shakeMag * 3f;
            _recoilPitch += shakePitch * shakeMag * 2f;
        }

        // ── Recoil ────────────────────────────────────────────

        public void AddRecoil(float pitch, float yaw = 0f)
        {
            _recoilPitch -= pitch;
            _recoilYaw   += yaw;
        }

        private void UpdateRecoilDecay()
        {
            float decay  = 10f * Time.deltaTime;
            _recoilPitch = Mathf.Lerp(_recoilPitch, 0f, decay);
            _recoilYaw   = Mathf.Lerp(_recoilYaw,   0f, decay);
        }

        // ── Rotación final ────────────────────────────────────

        private void ApplyRotation()
        {
            float targetYaw   = CurrentYaw   + _recoilYaw;
            float targetPitch = CurrentPitch + _recoilPitch;

            float sens = _config != null ? 1f : 1f; // _config.CameraSmoothing check
            if (_config != null && _config.CameraSmoothing > 0f)
            {
                _smoothYaw   = Mathf.LerpAngle(_smoothYaw,   targetYaw,   _config.CameraSmoothing * Time.deltaTime);
                _smoothPitch = Mathf.LerpAngle(_smoothPitch, targetPitch, _config.CameraSmoothing * Time.deltaTime);
            }
            else
            {
                _smoothYaw   = targetYaw;
                _smoothPitch = targetPitch;
            }

            // Yaw → cuerpo del jugador
            transform.rotation = Quaternion.Euler(0f, _smoothYaw, 0f);

            // Pitch → pivot de cámara + tilt
            if (_cameraPivot != null)
            {
                _cameraPivot.localEulerAngles = new Vector3(_smoothPitch, 0f, _currentTilt);
            }
        }

        // ── Third Person ──────────────────────────────────────

        private void UpdateThirdPerson()
        {
            if (_camera == null || _cameraPivot == null) return;

            float   dist       = _config?.ThirdPersonDistance ?? 4f;
            Vector3 desiredPos = _cameraPivot.position - _cameraPivot.forward * dist;

            if (Physics.Linecast(_cameraPivot.position, desiredPos, out RaycastHit hit, _cameraObstacleMask))
                desiredPos = hit.point + hit.normal * 0.2f;

            _camera.transform.position = desiredPos;
            _camera.transform.LookAt(_cameraPivot.position + Vector3.up * 0.5f);
        }

        // ── Callbacks ─────────────────────────────────────────

        private void OnLookInput(PlayerLookInputEvent e)
        {
            if (e.PlayerID != _authority.PlayerID || !_authority.IsLocalPlayer) return;

            float sensitX = (_config?.MouseSensitivityX ?? 2f) * _sensMultiplier;
            float sensitY = (_config?.MouseSensitivityY ?? 2f) * (_config?.InvertY == true ? 1f : -1f) * _sensMultiplier;

            CurrentYaw   += e.LookDelta.x * sensitX;
            CurrentPitch  = Mathf.Clamp(
                CurrentPitch + e.LookDelta.y * sensitY,
                -(_config?.PitchClampDown ?? 80f),
                _config?.PitchClampUp ?? 80f);
        }

        private void OnMoveInput(PlayerMoveInputEvent e)
        {
            if (e.PlayerID != _authority.PlayerID) return;
            _lastMoveInput = e.MoveDirection;
        }

        private void OnMovementState(Core.Events.MovementStateValidatedEvent e)
        {
            if (e.PlayerID != _authority.PlayerID) return;
            _currentSpeed = e.HorizontalSpeed;
            _isRunning    = e.IsGroundedReal && _currentSpeed > 5f;
            _isCrouching  = e.IsCrouchingReal;
        }

        private void OnLanded(PlayerLandedEvent e)
        {
            if (e.PlayerID != _authority.PlayerID) return;
            // Impacto al aterrizar proporcional a la caída
            AddShake(Mathf.Clamp01(e.FallDistance / 5f) * 0.4f);
        }

        private void OnPlayerReady(PlayerReadyEvent e)
        {
            if (e.PlayerID != _authority.PlayerID) return;
            if (e.IsLocalPlayer)
            {
                if (_camera != null) _camera.enabled = true;
                LockCursor();
                // Inicializar FOV
                if (_camera != null) _camera.fieldOfView = _normalFOV;
            }
            else
            {
                if (_camera != null) _camera.enabled = false;
            }
        }

        // ── API Pública ───────────────────────────────────────

        public void SetRotation(float yaw, float pitch = 0f)
        {
            CurrentYaw   = yaw;
            CurrentPitch = pitch;
            _smoothYaw   = yaw;
            _smoothPitch = pitch;
        }

        public UnityEngine.Camera ActiveCamera => _camera;
    }
}