// ============================================================
//  RecoilSystem.cs
//  Combat/Systems/RecoilSystem.cs
//
//  RESPONSABILIDAD ÚNICA: Calcular y aplicar recoil de disparo.
//
//  CARACTERÍSTICAS:
//  • Patrón fijo (array en WeaponConfig) o aleatoriedad configurada
//  • Acumulación de recoil en ráfaga
//  • Recovery automático pasado un umbral de tiempo sin disparar
//  • La cámara es notificada via EventBus (sin referencia directa)
//
//  INTEGRACIÓN CON CÁMARA:
//  PlayerCameraController.cs debe escuchar OnRecoilEvent y
//  llamar a AddRecoil(pitch, yaw). Sin acoplamiento directo.
// ============================================================

using Combat.Events;
using Core.Events;
using UnityEngine;

namespace Combat.Systems
{
    public class RecoilSystem
    {
        // ── Estado ────────────────────────────────────────────

        public float AccumulatedPitch  { get; private set; }
        public float AccumulatedYaw    { get; private set; }
        public int   ShotCount         { get; private set; }

        // ── Config ────────────────────────────────────────────

        private readonly WeaponConfig _config;
        private readonly int          _ownerID;

        private float _timeSinceLastShot;
        private const float RECOVERY_START_DELAY = 0.35f;

        // ── Constructor ───────────────────────────────────────

        public RecoilSystem(WeaponConfig config, int ownerID)
        {
            _config  = config;
            _ownerID = ownerID;
        }

        // ── Update (llamado por WeaponBase.Update) ────────────

        /// <summary>
        /// Procesa el recovery frame a frame.
        /// WeaponBase.Update() lo llama solo cuando el arma está equipada.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (Mathf.Approximately(AccumulatedPitch, 0f) &&
                Mathf.Approximately(AccumulatedYaw, 0f))
                return;

            _timeSinceLastShot += deltaTime;

            if (_timeSinceLastShot < RECOVERY_START_DELAY) return;

            float decay = deltaTime * 7f * (1f - _config.RecoilRecoveryRate);

            AccumulatedPitch = Mathf.Lerp(AccumulatedPitch, 0f, decay);
            AccumulatedYaw   = Mathf.Lerp(AccumulatedYaw,   0f, decay);

            if (Mathf.Abs(AccumulatedPitch) < 0.01f) AccumulatedPitch = 0f;
            if (Mathf.Abs(AccumulatedYaw)   < 0.01f) { AccumulatedYaw = 0f; ShotCount = 0; }
        }

        // ── API Pública ───────────────────────────────────────

        /// <summary>
        /// Aplica el recoil de un disparo y notifica a la cámara via EventBus.
        /// </summary>
        public void ApplyShot()
        {
            _timeSinceLastShot = 0f;

            float pitch, yaw;

            bool hasPattern = _config.RecoilPattern != null && _config.RecoilPattern.Length > 0;

            if (hasPattern)
            {
                int idx = ShotCount % _config.RecoilPattern.Length;
                pitch   = _config.RecoilPattern[idx].y;
                yaw     = _config.RecoilPattern[idx].x;
            }
            else
            {
                pitch = _config.RecoilPitch;
                yaw   = Random.Range(-_config.RecoilYawVariance, _config.RecoilYawVariance);
            }

            AccumulatedPitch += pitch;
            AccumulatedYaw   += yaw;
            ShotCount++;

            // Notificar a cámara via EventBus — sin referencia directa
            EventBus<OnRecoilEvent>.Raise(new OnRecoilEvent
            {
                OwnerID    = _ownerID,
                PitchDelta = pitch,
                YawDelta   = yaw
            });
        }

        /// <summary>Resetea todo (cambio de arma, respawn).</summary>
        public void Reset()
        {
            AccumulatedPitch   = 0f;
            AccumulatedYaw     = 0f;
            ShotCount          = 0;
            _timeSinceLastShot = 0f;
        }
    }
}
