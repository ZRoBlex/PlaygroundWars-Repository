// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_PlayerTeamAssigner.cs  (REEMPLAZA)         ║
// ║                                                          ║
// ║  CAMBIOS:                                                ║
// ║    + ChangeTeam(int teamID) → muere y respawnea          ║
// ║    + [SerializeField] _forceTeamOnStart para elegir      ║
// ║      el equipo inicial desde el Inspector                ║
// ║    + Color del equipo aplicado al renderer               ║
// ║    + Respawn automático al cambiar equipo                ║
// ╚══════════════════════════════════════════════════════════╝

using Core.Debug;
using Core.Events;
using Player.Authority;
using Player.Events;
using Player.Respawn;
using UnityEngine;

namespace GMF
{
    [RequireComponent(typeof(PlayerAuthority))]
    [DisallowMultipleComponent]
    public class PlayerTeamAssigner : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Visual")]
        [Tooltip("Renderer para cambiar color según el equipo.")]
        [SerializeField] private Renderer _bodyRenderer;
        [SerializeField] private int      _materialIndex = 0;

        [Header("Equipo forzado al inicio")]
        [Tooltip("-1 = automático. 0,1,2... = equipo fijo.")]
        [SerializeField] private int _forceTeamOnStart = -1;

        // ── Estado ────────────────────────────────────────────

        public int AssignedTeam { get; private set; } = -1;

        private PlayerAuthority _authority;
        private PlayerRespawn   _respawn;

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            _authority = GetComponent<PlayerAuthority>();
            _respawn   = GetComponent<PlayerRespawn>();
        }

        private void Start()
        {
            if (_forceTeamOnStart >= 0)
                TryAssignTeam(_forceTeamOnStart, forced: true);
            else
                TryAssignTeam(-1, forced: false);
        }

        private void OnEnable()
        {
            EventBus<GameStartedEvt>.Subscribe(OnGameStarted);
            EventBus<PlayerJoinedTeamEvt>.Subscribe(OnTeamAssigned);
        }

        private void OnDisable()
        {
            EventBus<GameStartedEvt>.Unsubscribe(OnGameStarted);
            EventBus<PlayerJoinedTeamEvt>.Unsubscribe(OnTeamAssigned);
        }

        // ── API Pública ───────────────────────────────────────

        /// <summary>
        /// Cambia al equipo indicado, mata al jugador y lo respawnea.
        /// Llamado por GMFTeamSelectUI al hacer click.
        /// </summary>
        public void ChangeTeam(int newTeamID)
        {
            if (newTeamID == AssignedTeam)
            {
                CoreLogger.LogSystemDebug("PlayerTeamAssigner",
                    $"P{_authority.PlayerID} ya está en T{newTeamID}.");
                return;
            }

            CoreLogger.LogSystem("PlayerTeamAssigner",
                $"P{_authority.PlayerID}: T{AssignedTeam} → T{newTeamID}");

            // 1. Asignar en el sistema
            var ctx = GameModeBase.Instance?.Context as GameModeContext;
            ctx?._teams.Assign(_authority.PlayerID, newTeamID);

            // 2. Actualizar estado local (OnTeamAssigned lo hace, pero por si acaso)
            AssignedTeam = newTeamID;
            ApplyTeamColor(newTeamID);

            // 3. Matar al jugador → PlayerRespawn lo respawneará automáticamente
            //    Emitimos PlayerDiedEvent como si hubiera muerto normalmente
            EventBus<PlayerDiedEvent>.Raise(new PlayerDiedEvent
            {
                PlayerID      = _authority.PlayerID,
                KillerID      = _authority.PlayerID, // suicidio de cambio de equipo
                DeathPosition = transform.position
            });
        }

        // ── Asignación interna ────────────────────────────────

        private void TryAssignTeam(int teamID, bool forced)
        {
            var gm = GameModeBase.Instance;
            if (gm?.Context == null) return;

            int current = gm.Context.Teams.GetTeam(_authority.PlayerID);

            if (forced && teamID >= 0)
            {
                var ctx = gm.Context as GameModeContext;
                ctx?._teams.Assign(_authority.PlayerID, teamID);
            }
            else if (current < 0)
            {
                var ctx = gm.Context as GameModeContext;
                ctx?._teams.AutoAssign(_authority.PlayerID);
            }

            AssignedTeam = gm.Context.Teams.GetTeam(_authority.PlayerID);
            if (AssignedTeam >= 0)
            {
                ApplyTeamColor(AssignedTeam);
                CoreLogger.LogSystem("PlayerTeamAssigner",
                    $"P{_authority.PlayerID} → T{AssignedTeam}");
            }
        }

        private void OnGameStarted(GameStartedEvt e)
        {
            TryAssignTeam(_forceTeamOnStart, _forceTeamOnStart >= 0);
        }

        private void OnTeamAssigned(PlayerJoinedTeamEvt e)
        {
            if (e.PlayerID != _authority.PlayerID) return;
            AssignedTeam = e.TeamID;
            ApplyTeamColor(e.TeamID);
        }

        // ── Visual ────────────────────────────────────────────

        private void ApplyTeamColor(int teamID)
        {
            if (_bodyRenderer == null) return;

            Color c = GetTeamColor(teamID);
            var   mats = _bodyRenderer.materials;

            if (_materialIndex < mats.Length)
            {
                // Clonar el material para no afectar el asset
                if (!mats[_materialIndex].name.EndsWith("_inst"))
                    mats[_materialIndex] = new Material(mats[_materialIndex]);

                mats[_materialIndex].color = c;
                _bodyRenderer.materials    = mats;
            }
        }

        private Color GetTeamColor(int teamID)
        {
            var gm      = GameModeBase.Instance;
            var defField = gm?.GetType()
                .GetField("_def", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (defField?.GetValue(gm) is GMF.Config.GMF_Config def
                && def.TeamConfig?.TeamColors != null
                && teamID < def.TeamConfig.TeamColors.Length)
                return def.TeamConfig.TeamColors[teamID];

            Color[] fallback = { Color.red, Color.blue, Color.green, Color.yellow };
            return teamID < fallback.Length ? fallback[teamID] : Color.white;
        }

        // ── Gizmo ─────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = AssignedTeam == 0 ? Color.red  :
                           AssignedTeam == 1 ? Color.blue : Color.white;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.4f, 0.15f);
        }
    }
}