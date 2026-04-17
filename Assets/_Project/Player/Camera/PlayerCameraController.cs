// ============================================================
//  PlayerCameraController.cs
//  PlayerSystem/Camera/PlayerCameraController.cs
//
//  RESPONSABILIDAD ÚNICA: Control de la cámara del jugador.
//
//  CARACTERÍSTICAS:
//  • First Person y Third Person (toggle en PlayerConfig)
//  • Yaw en el body del jugador (rotación horizontal del transform)
//  • Pitch en el camera pivot (rotación vertical, clamped)
//  • Suavizado de cámara configurable
//  • Soporte para recoil (vía AddRecoil()) externo
//  • Soporte para camera shake (vía AddShake()) externo
//  • Solo activo en IsLocalPlayer (remotos no controlan cámara)
//  • Cursor lock/unlock automático
// ============================================================

using Core.Debug;
using Core.Events;
using Player.Authority;
using Player.Config;
using Player.Events;
using UnityEngine;

namespace Player.Camera
{
    [RequireComponent(typeof(PlayerAuthority))]
    [DisallowMultipleComponent]
    public class PlayerCameraController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Configuración")]
        [SerializeField] private PlayerConfig _config;

        [Header("Refs")]
        [Tooltip("Pivot vertical (hijo del player). La cámara es hija de este.")]
        [SerializeField] private Transform _cameraPivot;

        [SerializeField] private UnityEngine.Camera _camera;

        [Header("Third Person")]
        [SerializeField] private Transform _thirdPersonTarget;
        [Tooltip("LayerMask para obstáculos entre cámara y jugador.")]
        [SerializeField] private LayerMask _cameraObstacleMask;

        // ── Referencias ───────────────────────────────────────

        private PlayerAuthority _authority;

        // ── Estado ────────────────────────────────────────────

        public float CurrentYaw    { get; private set; }
        public float CurrentPitch  { get; private set; }

        // Suavizado
        private float _smoothYaw;
        private float _smoothPitch;

        // Recoil
        private float _recoilPitch;
        private float _recoilYaw;

        // Shake
        private float  _shakeDuration;
        private float  _shakeMagnitude;
        private Vector3 _shakeOffset;

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            _authority = GetComponent<PlayerAuthority>();

            if (_camera == null)
                _camera = GetComponentInChildren<UnityEngine.Camera>();

            if (_cameraPivot == null)
                CoreLogger.LogError("[PlayerCameraController] CameraPivot no asignado.");
        }

        private void OnEnable()
        {
            EventBus<PlayerLookInputEvent>.Subscribe(OnLookInput);
            EventBus<PlayerReadyEvent>.Subscribe(OnPlayerReady);
        }

        private void OnDisable()
        {
            EventBus<PlayerLookInputEvent>.Unsubscribe(OnLookInput);
            EventBus<PlayerReadyEvent>.Unsubscribe(OnPlayerReady);

            if (_authority != null && _authority.IsLocalPlayer)
                ReleaseCursor();
        }

        private void LateUpdate()
        {
            // La cámara solo se actualiza para el jugador local
            if (!_authority.IsLocalPlayer) return;

            UpdateRecoilDecay();
            UpdateShake();
            ApplyRotation();

            if (_config.CameraMode == CameraMode.ThirdPerson)
                UpdateThirdPersonPosition();
        }

        // ── Callbacks de eventos ──────────────────────────────

        private void OnLookInput(PlayerLookInputEvent e)
        {
            if (e.PlayerID != _authority.PlayerID) return;
            if (!_authority.IsLocalPlayer) return;

            float sensitX = _config.MouseSensitivityX;
            float sensitY = _config.MouseSensitivityY * (_config.InvertY ? -1f : 1f);

            CurrentYaw   += e.LookDelta.x * sensitX;
            CurrentPitch -= e.LookDelta.y * sensitY;
            CurrentPitch  = Mathf.Clamp(CurrentPitch, -_config.PitchClampDown, _config.PitchClampUp);
        }

        private void OnPlayerReady(PlayerReadyEvent e)
        {
            if (e.PlayerID != _authority.PlayerID) return;

            if (e.IsLocalPlayer)
            {
                LockCursor();

                // Activar solo la cámara del jugador local
                if (_camera != null)
                    _camera.enabled = true;
            }
            else
            {
                // Jugador remoto: deshabilitar su cámara
                if (_camera != null)
                    _camera.enabled = false;
            }
        }

        // ── Rotación ──────────────────────────────────────────

        private void ApplyRotation()
        {
            float targetYaw   = CurrentYaw   + _recoilYaw;
            float targetPitch = CurrentPitch + _recoilPitch;

            if (_config.CameraSmoothing > 0f)
            {
                float lerpSpeed = _config.CameraSmoothing;
                _smoothYaw   = Mathf.LerpAngle(_smoothYaw,   targetYaw,   lerpSpeed * Time.deltaTime);
                _smoothPitch = Mathf.LerpAngle(_smoothPitch, targetPitch, lerpSpeed * Time.deltaTime);
            }
            else
            {
                _smoothYaw   = targetYaw;
                _smoothPitch = targetPitch;
            }

            // Yaw → rotación horizontal del cuerpo del jugador
            transform.rotation = Quaternion.Euler(0f, _smoothYaw, 0f);

            // Pitch → rotación del pivot de cámara
            if (_cameraPivot != null)
            {
                Vector3 pivotEuler = _cameraPivot.localEulerAngles;
                _cameraPivot.localEulerAngles = new Vector3(
                    _smoothPitch,
                    pivotEuler.y,
                    _shakeOffset.z
                );
            }
        }

        // ── Third Person ──────────────────────────────────────

        private void UpdateThirdPersonPosition()
        {
            if (_camera == null || _cameraPivot == null) return;

            float   targetDist  = _config.ThirdPersonDistance;
            Vector3 desiredPos  = _cameraPivot.position - _cameraPivot.forward * targetDist;

            // Collision con geometría
            if (Physics.Linecast(_cameraPivot.position, desiredPos, out RaycastHit hit, _cameraObstacleMask))
                desiredPos = hit.point + hit.normal * 0.2f;

            _camera.transform.position = desiredPos;
            _camera.transform.LookAt(_cameraPivot.position + Vector3.up * 0.5f);
        }

        // ── Recoil ────────────────────────────────────────────

        /// <summary>
        /// Aplica recoil a la cámara (ej: al disparar).
        /// pitch positivo = cámara sube. yaw = deriva lateral.
        /// </summary>
        public void AddRecoil(float pitch, float yaw = 0f)
        {
            _recoilPitch -= pitch;  // Negativo = hacia arriba
            _recoilYaw   += yaw;
        }

        private void UpdateRecoilDecay()
        {
            float decay = 10f * Time.deltaTime;
            _recoilPitch = Mathf.Lerp(_recoilPitch, 0f, decay);
            _recoilYaw   = Mathf.Lerp(_recoilYaw,   0f, decay);
        }

        // ── Camera Shake ──────────────────────────────────────

        /// <summary>Activa camera shake. magnitude en unidades locales.</summary>
        public void AddShake(float magnitude, float duration)
        {
            _shakeMagnitude = Mathf.Max(_shakeMagnitude, magnitude);
            _shakeDuration  = Mathf.Max(_shakeDuration, duration);
        }

        private void UpdateShake()
        {
            if (_shakeDuration <= 0f)
            {
                _shakeOffset   = Vector3.zero;
                _shakeMagnitude = 0f;
                return;
            }

            _shakeDuration  -= Time.deltaTime;
            float mag        = _shakeMagnitude * (_shakeDuration / Mathf.Max(_shakeDuration, 0.01f));
            _shakeOffset     = Random.insideUnitSphere * mag;
        }

        // ── Cursor ────────────────────────────────────────────

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

        // ── API Pública ───────────────────────────────────────

        /// <summary>
        /// Fuerza una rotación específica (ej: al hacer respawn).
        /// </summary>
        public void SetRotation(float yaw, float pitch = 0f)
        {
            CurrentYaw   = yaw;
            CurrentPitch = pitch;
            _smoothYaw   = yaw;
            _smoothPitch = pitch;
        }

        /// <summary>Acceso a la cámara activa para efectos externos.</summary>
        public UnityEngine.Camera ActiveCamera => _camera;
    }
}
