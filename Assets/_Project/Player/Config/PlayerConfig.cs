// ============================================================
//  PlayerConfig.cs
//  PlayerSystem/Config/PlayerConfig.cs
//
//  ScriptableObject central de configuración del jugador.
//  CERO hardcoding. Todo ajustable desde Inspector.
//
//  CREAR: Assets → Right Click → Create → Player → PlayerConfig
// ============================================================

using UnityEngine;

namespace Player.Config
{
    public enum MovementType
    {
        CharacterController,
        Rigidbody
    }

    public enum CameraMode
    {
        FirstPerson,
        ThirdPerson
    }

    public enum NetworkMode
    {
        Offline,        // Singleplayer, sin red
        Host,           // Este cliente es host/servidor
        Client,         // Este cliente conecta a un host
        DedicatedServer // Modo servidor puro (sin render)
    }

    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "Player/PlayerConfig", order = 0)]
    public class PlayerConfig : ScriptableObject
    {
        // ── Red ───────────────────────────────────────────────

        [Header("Network")]
        [Tooltip("Modo de red. Offline = singleplayer puro.")]
        public NetworkMode NetworkMode = NetworkMode.Offline;

        [Tooltip("Si es true, activa lógica de autoridad de red.")]
        public bool UseNetworking = false;

        // ── Movimiento ────────────────────────────────────────

        [Header("Movement - General")]
        public MovementType MovementType = MovementType.CharacterController;

        [Tooltip("Velocidad base de caminar (u/s).")]
        [Range(1f, 20f)]
        public float WalkSpeed = 5f;

        [Tooltip("Velocidad al correr (u/s).")]
        [Range(5f, 30f)]
        public float RunSpeed = 9f;

        [Tooltip("Velocidad al agacharse (u/s).")]
        [Range(0.5f, 5f)]
        public float CrouchSpeed = 2.5f;

        [Tooltip("Aceleración (qué tan rápido alcanza la velocidad objetivo).")]
        [Range(1f, 50f)]
        public float Acceleration = 15f;

        [Tooltip("Desaceleración en el suelo.")]
        [Range(1f, 50f)]
        public float Deceleration = 20f;

        [Tooltip("Fricción en el aire (menor = más deslizamiento).")]
        [Range(0f, 1f)]
        public float AirControl = 0.3f;

        // ── Salto ─────────────────────────────────────────────

        [Header("Movement - Jump")]
        [Tooltip("Fuerza del salto.")]
        [Range(1f, 20f)]
        public float JumpForce = 7f;

        [Tooltip("Gravedad aplicada al jugador (positivo = hacia abajo).")]
        [Range(5f, 50f)]
        public float Gravity = 20f;

        [Tooltip("Gravedad adicional al caer (cae más rápido que sube).")]
        [Range(1f, 5f)]
        public float FallMultiplier = 2f;

        [Tooltip("Número de saltos permitidos (1 = normal, 2 = doble salto).")]
        [Range(1, 3)]
        public int MaxJumps = 1;

        [Tooltip("Ventana de coyote time en segundos.")]
        [Range(0f, 0.3f)]
        public float CoyoteTime = 0.1f;

        [Tooltip("Ventana de jump buffer en segundos.")]
        [Range(0f, 0.3f)]
        public float JumpBuffer = 0.15f;

        // ── Agacharse ─────────────────────────────────────────

        [Header("Movement - Crouch")]
        public bool CanCrouch = true;

        [Tooltip("Altura del CharacterController al agacharse.")]
        [Range(0.5f, 2f)]
        public float CrouchHeight = 1f;

        [Tooltip("Altura normal del CharacterController.")]
        [Range(1f, 3f)]
        public float StandHeight = 2f;

        // ── Cámara ────────────────────────────────────────────

        [Header("Camera")]
        public CameraMode CameraMode = CameraMode.FirstPerson;

        [Tooltip("Sensibilidad horizontal del mouse.")]
        [Range(0.01f, 10f)]
        public float MouseSensitivityX = 2f;

        [Tooltip("Sensibilidad vertical del mouse.")]
        [Range(0.01f, 10f)]
        public float MouseSensitivityY = 2f;

        [Tooltip("Suavizado de la cámara. 0 = sin suavizado.")]
        [Range(0f, 20f)]
        public float CameraSmoothing = 0f;

        [Tooltip("Límite de pitch hacia arriba (grados).")]
        [Range(10f, 90f)]
        public float PitchClampUp = 80f;

        [Tooltip("Límite de pitch hacia abajo (grados).")]
        [Range(10f, 90f)]
        public float PitchClampDown = 80f;

        [Tooltip("Distancia de la cámara en tercera persona.")]
        [Range(1f, 10f)]
        public float ThirdPersonDistance = 4f;

        [Tooltip("Invertir eje Y de la cámara.")]
        public bool InvertY = false;

        // ── Salud ─────────────────────────────────────────────

        [Header("Health")]
        [Tooltip("Vida máxima del jugador.")]
        [Range(10f, 500f)]
        public float MaxHealth = 100f;

        [Tooltip("Permite regeneración de vida automática.")]
        public bool HealthRegenEnabled = false;

        [Tooltip("Cantidad de vida regenerada por segundo.")]
        [Range(1f, 50f)]
        public float HealthRegenRate = 5f;

        [Tooltip("Segundos sin daño antes de empezar a regenerar.")]
        [Range(0f, 10f)]
        public float HealthRegenDelay = 3f;

        [Tooltip("El jugador tiene invincibilidad por N segundos tras recibir daño.")]
        [Range(0f, 2f)]
        public float InvincibilityDuration = 0f;

        // ── Respawn ───────────────────────────────────────────

        [Header("Respawn")]
        [Tooltip("Tiempo en segundos hasta que el jugador hace respawn.")]
        [Range(0f, 30f)]
        public float RespawnDelay = 3f;

        [Tooltip("Si es false, el jugador no hace respawn (GameOver).")]
        public bool AllowRespawn = true;

        [Tooltip("Restaurar salud al hacer respawn.")]
        public bool ResetHealthOnRespawn = true;
    }
}
