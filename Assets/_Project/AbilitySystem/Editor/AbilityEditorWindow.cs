// ============================================================
//  AbilityEditorWindow.cs
//  AbilitySystem/Editor/AbilityEditorWindow.cs
//
//  ABRIR: Window → Abilities → Ability Debug Window
//
//  TABS: Habilidades | Cooldowns | Efectos Activos | Logs
// ============================================================

#if UNITY_EDITOR
using System.Collections.Generic;
using Abilities.Events;
using Core.Events;
using UnityEditor;
using UnityEngine;

namespace Abilities.Editor
{
    public class AbilityEditorWindow : EditorWindow
    {
        private int            _tab;
        private readonly string[] _tabs = { "Habilidades", "Cooldowns", "Efectos", "Logs" };
        private Vector2        _scroll, _logScroll;

        private AbilityManager             _manager;
        private StatusEffects.StatusEffectManager _effectMgr;

        private readonly List<string> _logs = new();
        private const    int          MAX_LOGS    = 50;
        private bool                  _autoScroll = true;
        private bool                  _subbed;

        [MenuItem("Window/Abilities/Ability Debug Window")]
        public static void Open()
        {
            var w = GetWindow<AbilityEditorWindow>("Ability Debug");
            w.minSize = new Vector2(420, 460);
            w.Show();
        }

        private void OnEnable()
        {
            Subscribe();
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        private void OnDisable()
        {
            Unsubscribe();
            EditorApplication.playModeStateChanged -= OnPlayMode;
        }

        private void OnPlayMode(PlayModeStateChange s)
        {
            if (s == PlayModeStateChange.EnteredPlayMode)  Subscribe();
            if (s == PlayModeStateChange.ExitingPlayMode) { Unsubscribe(); _logs.Clear(); }
        }

        private void Subscribe()
        {
            if (_subbed) return;
            EventBus<OnAbilityActivatedEvent>.Subscribe(e  => Log($"<color=lime>[ABILITY]</color> P{e.OwnerID} '{e.AbilityID}' → {e.TargetType}"));
            EventBus<OnAbilityFailedEvent>.Subscribe(e     => Log($"<color=red>[FAIL]</color> P{e.OwnerID} '{e.AbilityID}' → {e.Reason}"));
            EventBus<OnCooldownStartEvent>.Subscribe(e     => Log($"<color=cyan>[CD START]</color> P{e.OwnerID} '{e.AbilityID}' {e.Duration:F1}s"));
            EventBus<OnCooldownEndEvent>.Subscribe(e       => Log($"<color=green>[CD END]</color> P{e.OwnerID} '{e.AbilityID}' listo"));
            EventBus<OnEffectAppliedEvent>.Subscribe(e     => Log($"<color=yellow>[FX+]</color> P{e.TargetID} '{e.EffectID}' {e.Duration:F1}s i={e.Intensity:F2}"));
            EventBus<OnEffectRemovedEvent>.Subscribe(e     => Log($"<color=orange>[FX-]</color> P{e.TargetID} '{e.EffectID}' removido"));
            EventBus<OnTargetAcquiredEvent>.Subscribe(e    => Log($"[TARGET] P{e.OwnerID} → P{e.TargetID}"));
            _subbed = true;
        }

        private void Unsubscribe()
        {
            if (!_subbed) return;
            EventBus<OnAbilityActivatedEvent>.Clear();
            EventBus<OnAbilityFailedEvent>.Clear();
            EventBus<OnCooldownStartEvent>.Clear();
            EventBus<OnCooldownEndEvent>.Clear();
            EventBus<OnEffectAppliedEvent>.Clear();
            EventBus<OnEffectRemovedEvent>.Clear();
            EventBus<OnTargetAcquiredEvent>.Clear();
            _subbed = false;
        }

        private void OnGUI()
        {
            // Header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("⚡ Ability Debug", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Find", GUILayout.Width(45)) && Application.isPlaying)
            {
                _manager   = Object.FindFirstObjectByType<AbilityManager>();
                _effectMgr = Object.FindFirstObjectByType<StatusEffects.StatusEffectManager>();
            }
            GUI.color = Application.isPlaying ? Color.green : Color.gray;
            GUILayout.Label(Application.isPlaying ? "● PLAY" : "● EDIT", EditorStyles.miniLabel);
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            _tab = GUILayout.Toolbar(_tab, _tabs);
            EditorGUILayout.Space(4);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            switch (_tab)
            {
                case 0: DrawAbilitiesTab(); break;
                case 1: DrawCooldownTab();  break;
                case 2: DrawEffectsTab();   break;
                case 3: DrawLogsTab();      break;
            }
            EditorGUILayout.EndScrollView();

            if (Application.isPlaying) Repaint();
        }

        // ── Tab: Habilidades ──────────────────────────────────

        private void DrawAbilitiesTab()
        {
            if (!Check(_manager, "AbilityManager")) return;

            EditorGUILayout.LabelField("Slots de Habilidades", EditorStyles.boldLabel);

            var abilities = _manager.Abilities;
            for (int i = 0; i < abilities.Count; i++)
            {
                var ab = abilities[i];
                if (ab == null) continue;

                bool ready = _manager.IsReady(i);

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();

                GUI.color = ready ? Color.white : Color.gray;
                EditorGUILayout.LabelField($"[{i}] {ab.Config?.DisplayName ?? "?"}",
                    EditorStyles.boldLabel, GUILayout.Width(180));
                GUI.color = Color.white;

                EditorGUI.BeginDisabledGroup(!ready);
                if (GUILayout.Button("▶ Activar", GUILayout.Width(80)))
                    _manager.ActivateSlot(i);
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndHorizontal();

                if (ab.Config != null)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField("AbilityID",  ab.Config.AbilityID);
                    EditorGUILayout.EnumPopup("Targeting",  ab.Config.TargetType);
                    EditorGUILayout.FloatField("Cooldown",  ab.Config.Cooldown);
                    EditorGUILayout.FloatField("Daño",      ab.Config.Damage);
                    EditorGUILayout.FloatField("Curación",  ab.Config.Heal);
                    EditorGUI.EndDisabledGroup();
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(6);
            if (GUILayout.Button("Reset Todos los Cooldowns"))
                _manager.ResetAllCooldowns();
        }

        // ── Tab: Cooldowns ────────────────────────────────────

        private void DrawCooldownTab()
        {
            if (!Check(_manager, "AbilityManager")) return;

            EditorGUILayout.LabelField("Estado de Cooldowns", EditorStyles.boldLabel);

            for (int i = 0; i < _manager.Abilities.Count; i++)
            {
                var ab = _manager.Abilities[i];
                if (ab?.Config == null) continue;

                float rem  = _manager.GetCooldownRemaining(i);
                float total = ab.Config.Cooldown;
                float prog  = total > 0f ? 1f - (rem / total) : 1f;

                EditorGUILayout.LabelField($"[{i}] {ab.Config.DisplayName}");

                if (rem > 0f)
                    EditorGUI.ProgressBar(
                        EditorGUILayout.GetControlRect(GUILayout.Height(14)),
                        prog, $"{rem:F1}s restante");
                else
                {
                    GUI.color = Color.green;
                    EditorGUILayout.LabelField("  ✓ LISTO");
                    GUI.color = Color.white;
                }
            }
        }

        // ── Tab: Efectos Activos ──────────────────────────────

        private void DrawEffectsTab()
        {
            if (!Application.isPlaying) { EditorGUILayout.HelpBox("Play Mode requerido.", MessageType.Info); return; }
            if (_effectMgr == null) { EditorGUILayout.HelpBox("StatusEffectManager no encontrado.", MessageType.Warning); return; }

            EditorGUILayout.LabelField($"Efectos Activos ({_effectMgr.ActiveCount})", EditorStyles.boldLabel);

            if (_effectMgr.ActiveCount == 0)
            {
                EditorGUILayout.LabelField("  Sin efectos activos.");
                return;
            }

            // Mostrar efectos activos (reflección básica)
            foreach (var effect in _effectMgr.GetComponentsInChildren<StatusEffects.StatusEffectBase>())
            {
                if (!effect.IsActive) continue;

                EditorGUILayout.BeginVertical("box");
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("EffectID",  effect.EffectID);
                EditorGUILayout.FloatField("Duration", effect.Duration);
                EditorGUILayout.FloatField("Intensity",effect.Intensity);
                EditorGUILayout.IntField("Stacks",     effect.StackCount);
                EditorGUI.EndDisabledGroup();

                if (GUILayout.Button($"❌ Remover '{effect.EffectID}'"))
                    _effectMgr.RemoveEffect(effect.EffectID);

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("Remover Todos los Efectos"))
                _effectMgr.RemoveAll();
            GUI.backgroundColor = Color.white;
        }

        // ── Tab: Logs ─────────────────────────────────────────

        private void DrawLogsTab()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Eventos ({_logs.Count}/{MAX_LOGS})", EditorStyles.miniLabel);
            _autoScroll = EditorGUILayout.ToggleLeft("Auto", _autoScroll, GUILayout.Width(50));
            if (GUILayout.Button("Limpiar", GUILayout.Width(60))) _logs.Clear();
            EditorGUILayout.EndHorizontal();

            _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.Height(320));
            var style = new GUIStyle(EditorStyles.miniLabel) { richText = true, wordWrap = true };
            for (int i = _logs.Count - 1; i >= 0; i--)
                EditorGUILayout.LabelField(_logs[i], style);
            if (_autoScroll) _logScroll = new Vector2(0, float.MaxValue);
            EditorGUILayout.EndScrollView();
        }

        // ── Helpers ───────────────────────────────────────────

        private bool Check(Object obj, string name)
        {
            if (!Application.isPlaying)
            { EditorGUILayout.HelpBox("Play Mode requerido.", MessageType.Info); return false; }
            if (obj == null)
            { EditorGUILayout.HelpBox($"{name} no encontrado. Pulsa 'Find'.", MessageType.Warning); return false; }
            return true;
        }

        private void Log(string msg)
        {
            _logs.Add($"<color=#888>{System.DateTime.Now:HH:mm:ss}</color> {msg}");
            if (_logs.Count > MAX_LOGS) _logs.RemoveAt(0);
        }
    }
}
#endif
