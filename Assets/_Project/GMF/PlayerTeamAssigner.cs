// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_PlayerTeamAssigner.cs  (REEMPLAZA)         ║
// ║                                                          ║
// ║  CAMBIO: ChangeTeam() ahora llama PlayerRespawn.ResetFull
// ║  en lugar de PlayerDiedEvent, para que:                  ║
// ║    - El jugador reaparezca en la base del nuevo equipo   ║
// ║    - Se restaure HP/estado completo                      ║
// ║    - NO cuente como muerte/kill                          ║
// ╚══════════════════════════════════════════════════════════╝

using Core.Debug;
using Core.Events;
using Player.Authority;
using Player.Respawn;
using UnityEngine;

namespace GMF
{
    [RequireComponent(typeof(PlayerAuthority))]
    [DisallowMultipleComponent]
    public class PlayerTeamAssigner : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private Renderer _bodyRenderer;
        [SerializeField] private int      _materialIndex = 0;

        [Header("Equipo forzado al inicio (-1 = auto)")]
        [SerializeField] private int _forceTeamOnStart = -1;

        public int AssignedTeam { get; private set; } = -1;

        private PlayerAuthority _authority;
        private PlayerRespawn   _respawn;

        private void Awake()
        {
            _authority = GetComponent<PlayerAuthority>();
            _respawn   = GetComponent<PlayerRespawn>();
        }

        private void Start()
        {
            TryAssignTeam(_forceTeamOnStart, _forceTeamOnStart >= 0);
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

        // ── Cambio de equipo (sin muerte) ─────────────────────

        public void ChangeTeam(int newTeamID)
        {
            if (newTeamID == AssignedTeam) return;

            CoreLogger.LogSystem("PlayerTeamAssigner",
                $"P{_authority.PlayerID}: T{AssignedTeam} → T{newTeamID}");

            // 1. Asignar en TeamSystem
            var ctx = GameModeBase.Instance?.Context as GameModeContext;
            ctx?._teams.Assign(_authority.PlayerID, newTeamID);

            AssignedTeam = newTeamID;
            ApplyTeamColor(newTeamID);

            // 2. Obtener posición de la base del nuevo equipo
            Vector3    spawnPos = GetSpawnForTeam(newTeamID);
            Quaternion spawnRot = GetRotationForTeam(newTeamID);

            // 3. Reset completo (HP, estado, etc.) sin contar como muerte
            _respawn?.ResetFull(spawnPos, spawnRot);
        }

        // ── Asignación interna ────────────────────────────────

        private void TryAssignTeam(int teamID, bool forced)
        {
            var gm = GameModeBase.Instance;
            if (gm?.Context == null) return;

            int current = gm.Context.Teams.GetTeam(_authority.PlayerID);

            if (forced && teamID >= 0)
                (gm.Context as GameModeContext)?._teams.Assign(_authority.PlayerID, teamID);
            else if (current < 0)
                (gm.Context as GameModeContext)?._teams.AutoAssign(_authority.PlayerID);

            AssignedTeam = gm.Context.Teams.GetTeam(_authority.PlayerID);
            if (AssignedTeam >= 0) ApplyTeamColor(AssignedTeam);
        }

        private void OnGameStarted(GameStartedEvt e)
            => TryAssignTeam(_forceTeamOnStart, _forceTeamOnStart >= 0);

        private void OnTeamAssigned(PlayerJoinedTeamEvt e)
        {
            if (e.PlayerID != _authority.PlayerID) return;
            AssignedTeam = e.TeamID;
            ApplyTeamColor(e.TeamID);
        }

        // ── Spawn position helpers ────────────────────────────

        private Vector3 GetSpawnForTeam(int teamID)
        {
            var areas = FindObjectsByType<GMFSpawnArea>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var a in areas)
                if (a.TeamID == teamID && a.TryGetSpawnPosition(out Vector3 p))
                    return p;
            return transform.position + Vector3.up;
        }

        private Quaternion GetRotationForTeam(int teamID)
        {
            var areas = FindObjectsByType<GMFSpawnArea>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var a in areas)
                if (a.TeamID == teamID)
                    return a.transform.rotation;
            return Quaternion.identity;
        }

        // ── Visual ────────────────────────────────────────────

        private void ApplyTeamColor(int teamID)
        {
            if (_bodyRenderer == null) return;
            var mats = _bodyRenderer.materials;
            if (_materialIndex >= mats.Length) return;
            if (!mats[_materialIndex].name.EndsWith("_inst"))
                mats[_materialIndex] = new Material(mats[_materialIndex]);
            mats[_materialIndex].color = GetTeamColor(teamID);
            _bodyRenderer.materials = mats;
        }

        private Color GetTeamColor(int teamID)
        {
            var def = GameModeBase.Instance?.Definition;
            if (def?.TeamConfig?.TeamColors != null && teamID < def.TeamConfig.TeamColors.Length)
                return def.TeamConfig.TeamColors[teamID];
            Color[] fb = { Color.red, Color.blue, Color.green, Color.yellow };
            return teamID < fb.Length ? fb[teamID] : Color.white;
        }
    }
}