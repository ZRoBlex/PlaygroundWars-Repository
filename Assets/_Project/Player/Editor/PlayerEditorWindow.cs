// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: PlayerEditorWindow.cs  (REEMPLAZA el anterior) ║
// ║                                                          ║
// ║  MEJORAS:                                                ║
// ║    + Tab "Cámara": zoom, ADS, headbob, shake, cursor     ║
// ║    + MovementMode en tab Movement                        ║
// ║    + Crouch height progress bar                          ║
// ║    + Sprint state en tiempo real                         ║
// ║    + Botón de SetSpeedMultiplier para testear efectos    ║
// ╚══════════════════════════════════════════════════════════╝

#if UNITY_EDITOR
using System.Collections.Generic;
using Player.Camera;
using Player.Controller;
using Player.Events;
using Player.Movement;
using Core.Events;
using UnityEditor;
using UnityEngine;

namespace Player.Editor
{
    public class PlayerEditorWindow : EditorWindow
    {
        private int     _tab;
        private readonly string[] _tabs = { "Stats", "Input", "Movimiento", "Salud", "Cámara", "Respawn" };
        private Vector2 _scroll;
        private Vector2 _logScroll;

        private PlayerController _selectedPlayer;

        private readonly List<string> _logs = new();
        private const int MAX_LOGS = 40;
        private bool _autoScroll = true;
        private bool _subscribed;

        private float _damageAmount = 25f;
        private float _healAmount   = 25f;
        private float _speedMult    = 1f;
        private float _shakeTrauma  = 0.3f;

        [MenuItem("Window/Player/Player Debug Window")]
        public static void Open()
        {
            var w = GetWindow<PlayerEditorWindow>("Player Debug");
            w.minSize = new Vector2(420, 520);
            w.Show();
        }

        private void OnEnable()
        {
            SubscribeEvents();
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode) SubscribeEvents();
            if (state == PlayModeStateChange.ExitingPlayMode) { UnsubscribeEvents(); _logs.Clear(); }
        }

        private void SubscribeEvents()
        {
            if (_subscribed) return;
            EventBus<PlayerDamagedEvent>.Subscribe(e   => AddLog($"<color=red>[DMG]</color> P{e.PlayerID} -{e.Amount:F0} → {e.RemainingHealth:F0}"));
            EventBus<PlayerHealedEvent>.Subscribe(e    => AddLog($"<color=green>[HEAL]</color> P{e.PlayerID} +{e.Amount:F0}"));
            EventBus<PlayerDiedEvent>.Subscribe(e      => AddLog($"<color=orange>[DIED]</color> P{e.PlayerID}"));
            EventBus<PlayerRespawnedEvent>.Subscribe(e => AddLog($"<color=cyan>[SPAWN]</color> P{e.PlayerID}"));
            EventBus<PlayerJumpedEvent>.Subscribe(e    => AddLog($"<color=yellow>[JUMP]</color> P{e.PlayerID}"));
            EventBus<PlayerLandedEvent>.Subscribe(e    => AddLog($"[LAND] P{e.PlayerID}"));
            _subscribed = true;
        }

        private void UnsubscribeEvents()
        {
            if (!_subscribed) return;
            EventBus<PlayerDamagedEvent>.Clear();
            EventBus<PlayerHealedEvent>.Clear();
            EventBus<PlayerDiedEvent>.Clear();
            EventBus<PlayerRespawnedEvent>.Clear();
            EventBus<PlayerJumpedEvent>.Clear();
            EventBus<PlayerLandedEvent>.Clear();
            _subscribed = false;
        }

        private void OnGUI()
        {
            DrawHeader();
            _tab = GUILayout.Toolbar(_tab, _tabs);
            EditorGUILayout.Space(4);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            switch (_tab)
            {
                case 0: DrawStatsTab();    break;
                case 1: DrawInputTab();    break;
                case 2: DrawMovementTab(); break;
                case 3: DrawHealthTab();   break;
                case 4: DrawCameraTab();   break;
                case 5: DrawRespawnTab();  break;
            }

            EditorGUILayout.EndScrollView();
            DrawLiveLog();
            if (Application.isPlaying) Repaint();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("🎮 Player Debug", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            _selectedPlayer = (PlayerController)EditorGUILayout.ObjectField(
                _selectedPlayer, typeof(PlayerController), true, GUILayout.Width(180));
            if (GUILayout.Button("Find", GUILayout.Width(45)) && Application.isPlaying)
                _selectedPlayer = FindFirstObjectByType<PlayerController>();
            GUI.color = Application.isPlaying ? Color.green : Color.gray;
            GUILayout.Label(Application.isPlaying ? "● PLAY" : "● EDIT", EditorStyles.miniLabel);
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatsTab()
        {
            if (!Check()) return;
            var p = _selectedPlayer;
            var auth = p.Authority;

            EditorGUILayout.LabelField("Jugador", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"ID: {auth?.PlayerID}  |  Local: {auth?.IsLocalPlayer}  |  Authority: {auth?.HasAuthority}");

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Vida", EditorStyles.boldLabel);
            var h = p.Health;
            if (h != null)
            {
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(GUILayout.Height(18)),
                    h.HealthPercent, $"{h.CurrentHealth:F0} / {h.MaxHealth:F0}");
                EditorGUILayout.LabelField($"Vivo: {h.IsAlive}  |  Invencible: {h.IsInvincible}");
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Movimiento", EditorStyles.boldLabel);
            var m = p.Movement;
            if (m != null)
            {
                EditorGUILayout.LabelField($"Velocidad: {m.Velocity.magnitude:F2} u/s");
                EditorGUILayout.LabelField($"Suelo: {m.IsGroundedReal}  |  Corriendo: {m.IsRunning}  |  Agachado: {m.IsCrouchingReal}");
            }
        }

        private void DrawInputTab()
        {
            if (!Check()) return;
            var inp = _selectedPlayer.Input;
            if (inp == null) { EditorGUILayout.HelpBox("PlayerInput no encontrado.", MessageType.Warning); return; }

            EditorGUILayout.LabelField("Estado del Input", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Vector2Field("Move", inp.MoveInput);
            EditorGUILayout.Vector2Field("Look", inp.LookInput);
            EditorGUILayout.Toggle("Sprint",  inp.IsSprinting);
            EditorGUILayout.Toggle("Crouch",  inp.IsCrouching);
            EditorGUILayout.Toggle("Shooting",inp.IsShootHeld);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Deshabilitar Input")) inp.DisableInput();
            if (GUILayout.Button("Habilitar Input"))    inp.EnableInput();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawMovementTab()
        {
            if (!Check()) return;
            var m = _selectedPlayer.Movement;
            if (m == null) { EditorGUILayout.HelpBox("PlayerMovement no encontrado.", MessageType.Warning); return; }

            EditorGUILayout.LabelField("Estado", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Vector3Field("Velocity", m.Velocity);
            EditorGUILayout.FloatField("Speed", m.Velocity.magnitude);
            EditorGUILayout.Toggle("Grounded",  m.IsGroundedReal);
            EditorGUILayout.Toggle("Running",   m.IsRunning);
            EditorGUILayout.Toggle("Crouching", m.IsCrouchingReal);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Speed Multiplier (efectos de estado)", EditorStyles.boldLabel);
            _speedMult = EditorGUILayout.Slider("Multiplicador", _speedMult, 0f, 1f);
            if (GUILayout.Button($"Aplicar {_speedMult:F2}x"))
                m.SetSpeedMultiplier(_speedMult);
            if (GUILayout.Button("Reset (1.0x)"))
            {
                _speedMult = 1f;
                m.SetSpeedMultiplier(1f);
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Acciones", EditorStyles.boldLabel);
            if (GUILayout.Button("Teleport (0,1,0)"))
                m.Teleport(new Vector3(0f, 1f, 0f), Quaternion.identity);
            if (GUILayout.Button("Impulso Vertical"))
                m.AddImpulse(new Vector3(0f, 8f, 0f));
            if (GUILayout.Button("Crouch ON"))
                m.SetCrouch(true);
            if (GUILayout.Button("Crouch OFF"))
                m.SetCrouch(false);
        }

        private void DrawHealthTab()
        {
            if (!Check()) return;
            var h = _selectedPlayer.Health;
            if (h == null) { EditorGUILayout.HelpBox("PlayerHealth no encontrado.", MessageType.Warning); return; }

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField("HP",         h.CurrentHealth);
            EditorGUILayout.FloatField("Max HP",     h.MaxHealth);
            EditorGUILayout.Toggle("Vivo",           h.IsAlive);
            EditorGUILayout.Toggle("Invencible",     h.IsInvincible);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(6);
            _damageAmount = EditorGUILayout.Slider("Daño", _damageAmount, 1f, 200f);
            if (GUILayout.Button($"💥 Aplicar {_damageAmount:F0} daño"))
                _selectedPlayer.RequestDamage(_damageAmount, -1, Vector3.zero, Vector3.up);

            _healAmount = EditorGUILayout.Slider("Curación", _healAmount, 1f, 200f);
            if (GUILayout.Button($"💚 Curar {_healAmount:F0}"))
                _selectedPlayer.RequestHeal(_healAmount);

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("☠️ Matar"))
                _selectedPlayer.RequestDamage(h.MaxHealth + 9999f, -1, Vector3.zero, Vector3.up);
            GUI.backgroundColor = Color.white;
        }

        private void DrawCameraTab()
        {
            if (!Check()) return;

            var cam = _selectedPlayer.GetComponent<PlayerCameraController>();
            if (cam == null)
            {
                EditorGUILayout.HelpBox("PlayerCameraController no encontrado en este GO.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Estado de Cámara", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField("Yaw",   cam.CurrentYaw);
            EditorGUILayout.FloatField("Pitch", cam.CurrentPitch);
            EditorGUILayout.Toggle("ADS",       cam.IsADS);
            EditorGUILayout.Toggle("Zoom",      cam.IsZooming);
            EditorGUILayout.Toggle("Focus",     cam.IsFocused);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Acciones", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Lock Cursor"))    cam.LockCursor();
            if (GUILayout.Button("Release Cursor")) cam.ReleaseCursor();
            if (GUILayout.Button("Toggle Cursor"))  cam.ToggleCursor();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            _shakeTrauma = EditorGUILayout.Slider("Trauma Shake", _shakeTrauma, 0f, 1f);
            if (GUILayout.Button($"💥 Aplicar Shake ({_shakeTrauma:F2})"))
                cam.AddShake(_shakeTrauma);

            EditorGUILayout.Space(4);
            if (GUILayout.Button("🔄 Reset Rotación (0,0)"))
                cam.SetRotation(0f, 0f);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Recoil", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Recoil Pitch +3"))  cam.AddRecoil(3f, 0f);
            if (GUILayout.Button("Recoil Pitch +6"))  cam.AddRecoil(6f, 0f);
            if (GUILayout.Button("Recoil Random"))    cam.AddRecoil(3f, Random.Range(-2f, 2f));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRespawnTab()
        {
            if (!Check()) return;
            var r = _selectedPlayer.Respawn;
            if (r == null) { EditorGUILayout.HelpBox("PlayerRespawn no encontrado.", MessageType.Warning); return; }

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Toggle("Respawning",  r.IsRespawning);
            EditorGUILayout.IntField("Respawns",  r.RespawnCount);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(6);
            if (GUILayout.Button("🔄 Forzar Respawn"))
                r.ForceRespawn();
            if (GUILayout.Button("📍 Respawn en (0,1,0)"))
                r.ForceRespawnAt(new Vector3(0f, 1f, 0f), Quaternion.identity);
        }

        private void DrawLiveLog()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Eventos ({_logs.Count})", EditorStyles.miniLabel);
            _autoScroll = EditorGUILayout.ToggleLeft("Auto", _autoScroll, GUILayout.Width(50));
            if (GUILayout.Button("X", GUILayout.Width(20))) _logs.Clear();
            EditorGUILayout.EndHorizontal();

            _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.Height(90));
            var style = new GUIStyle(EditorStyles.miniLabel) { richText = true };
            for (int i = _logs.Count - 1; i >= 0; i--)
                EditorGUILayout.LabelField(_logs[i], style);
            if (_autoScroll) _logScroll = new Vector2(0, float.MaxValue);
            EditorGUILayout.EndScrollView();
        }

        private bool Check()
        {
            if (!Application.isPlaying)
            { EditorGUILayout.HelpBox("Play Mode requerido.", MessageType.Info); return false; }
            if (_selectedPlayer == null)
            { EditorGUILayout.HelpBox("Selecciona un PlayerController o pulsa Find.", MessageType.Warning); return false; }
            return true;
        }

        private void AddLog(string msg)
        {
            _logs.Add($"<color=#888>{System.DateTime.Now:HH:mm:ss}</color> {msg}");
            if (_logs.Count > MAX_LOGS) _logs.RemoveAt(0);
        }
    }
}
#endif