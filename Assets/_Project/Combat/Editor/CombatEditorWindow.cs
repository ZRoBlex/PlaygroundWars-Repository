// ============================================================
//  CombatEditorWindow.cs
//  Combat/Editor/CombatEditorWindow.cs
//
//  Herramienta de debug del sistema de combate.
//  ABRIR: Window → Combat → Combat Debug Window
//
//  TABS:
//  • Armas      — estado del arma activa, slots, equip manual
//  • Munición   — HUD de ammo, recarga, añadir reserva
//  • Recoil     — acumulado, patrón, reset
//  • Proyectiles — pool stats, spawn manual, return all
//  • Logs       — eventos en tiempo real
// ============================================================

#if UNITY_EDITOR
using System.Collections.Generic;
using Combat.Events;
using Combat.Pool;
using Combat.Weapons;
using Core.Events;
using UnityEditor;
using UnityEngine;

namespace Combat.Editor
{
    public class CombatEditorWindow : EditorWindow
    {
        // ── Layout ────────────────────────────────────────────

        private int            _tab;
        private readonly string[] _tabs = { "Armas", "Munición", "Recoil", "Proyectiles", "Logs" };
        private Vector2        _scroll, _logScroll;

        // ── Referencias ───────────────────────────────────────

        private WeaponManager _manager;

        // ── Live Logs ─────────────────────────────────────────

        private readonly List<string> _logs = new();
        private const    int          MAX_LOGS   = 60;
        private bool _autoScroll = true;
        private bool _subbed;

        // ── Apertura ──────────────────────────────────────────

        [MenuItem("Window/Combat/Combat Debug Window")]
        public static void Open()
        {
            var w = GetWindow<CombatEditorWindow>("Combat Debug");
            w.minSize = new Vector2(430, 500);
            w.Show();
        }

        // ── Lifecycle de la window ────────────────────────────

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

        // ── Suscripciones ─────────────────────────────────────

        private void Subscribe()
        {
            if (_subbed) return;
            EventBus<OnShootEvent>.Subscribe(e        => Log($"<color=yellow>[FIRE]</color> P{e.ShooterID} {e.WeaponID} ({e.Type})"));
            EventBus<OnShootFailedEvent>.Subscribe(e  => Log($"<color=red>[FAIL]</color> P{e.ShooterID} {e.WeaponID} → {e.Reason}"));
            EventBus<OnHitEvent>.Subscribe(e          => Log($"<color=cyan>[HIT]</color> P{e.ShooterID}→P{e.TargetID} dist={e.Distance:F1}m"));
            EventBus<OnDamageDealtEvent>.Subscribe(e  => Log($"<color=orange>[DMG]</color> P{e.SourceID}→P{e.TargetID} {e.Amount:F0}hp crit={e.IsCritical}"));
            EventBus<OnAmmoChangedEvent>.Subscribe(e  => Log($"[AMMO] {e.WeaponID} {e.Current}/{e.Max} ({e.Reserve} res)"));
            EventBus<OnAmmoEmptyEvent>.Subscribe(e    => Log($"<color=red>[EMPTY]</color> P{e.OwnerID} {e.WeaponID}"));
            EventBus<OnReloadStartEvent>.Subscribe(e  => Log($"<color=#aaffaa>[RELOAD]</color> P{e.OwnerID} {e.WeaponID} {e.Duration:F1}s"));
            EventBus<OnReloadCompleteEvent>.Subscribe(e => Log($"<color=green>[RELOAD✓]</color> {e.WeaponID} → {e.NewAmmo}"));
            EventBus<OnWeaponSwitchedEvent>.Subscribe(e => Log($"[SWITCH] P{e.OwnerID} → {e.NewWeaponID} (slot {e.NewSlot})"));
            _subbed = true;
        }

        private void Unsubscribe()
        {
            if (!_subbed) return;
            EventBus<OnShootEvent>.Clear();
            EventBus<OnShootFailedEvent>.Clear();
            EventBus<OnHitEvent>.Clear();
            EventBus<OnDamageDealtEvent>.Clear();
            EventBus<OnAmmoChangedEvent>.Clear();
            EventBus<OnAmmoEmptyEvent>.Clear();
            EventBus<OnReloadStartEvent>.Clear();
            EventBus<OnReloadCompleteEvent>.Clear();
            EventBus<OnWeaponSwitchedEvent>.Clear();
            _subbed = false;
        }

        // ── GUI ───────────────────────────────────────────────

        private void OnGUI()
        {
            DrawHeader();

            _tab = GUILayout.Toolbar(_tab, _tabs);
            EditorGUILayout.Space(4);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            switch (_tab)
            {
                case 0: DrawWeaponsTab();      break;
                case 1: DrawAmmoTab();         break;
                case 2: DrawRecoilTab();       break;
                case 3: DrawProjectilesTab();  break;
                case 4: DrawLogsTab();         break;
            }
            EditorGUILayout.EndScrollView();

            if (Application.isPlaying) Repaint();
        }

        // ── Header ────────────────────────────────────────────

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("⚔️ Combat Debug", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Find Player", GUILayout.Width(80)) && Application.isPlaying)
                _manager = FindFirstObjectByType<WeaponManager>();

            GUI.color = Application.isPlaying ? Color.green : Color.gray;
            GUILayout.Label(Application.isPlaying ? "● PLAY" : "● EDIT", EditorStyles.miniLabel);
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        // ── Tab: Armas ────────────────────────────────────────

        private void DrawWeaponsTab()
        {
            if (!Check(_manager, "WeaponManager")) return;

            EditorGUILayout.LabelField("Slots", EditorStyles.boldLabel);
            for (int i = 0; i < 4; i++)
            {
                var w = _manager.GetWeapon(i);
                if (w == null) continue;

                bool active = _manager.CurrentSlot == i;
                GUI.backgroundColor = active ? Color.cyan : Color.white;
                if (GUILayout.Button($"[{i}] {w.Config?.WeaponID}{(active ? " ◀" : "")}"))
                    _manager.EquipSlot(i);
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.Space(8);
            var aw = _manager.ActiveWeapon;
            if (aw != null)
            {
                EditorGUILayout.LabelField("Arma Activa", EditorStyles.boldLabel);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("WeaponID",    aw.Config?.WeaponID ?? "?");
                EditorGUILayout.TextField("Tipo",        aw.Config?.ShootingType.ToString() ?? "?");
                EditorGUILayout.Toggle("IsEquipped",     aw.IsEquipped);
                EditorGUILayout.Toggle("CanShoot",       aw.CanShoot());
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("🔫 Forzar Disparo"))   aw.TryShoot();
                if (GUILayout.Button("🔄 Forzar Recarga"))   aw.TryReload();
                EditorGUILayout.EndHorizontal();
                if (GUILayout.Button("Reset Munición"))       aw.Ammo?.Reset();
            }
        }

        // ── Tab: Munición ─────────────────────────────────────

        private void DrawAmmoTab()
        {
            if (!Check(_manager, "WeaponManager")) return;

            var aw = _manager.ActiveWeapon;
            if (aw == null) { EditorGUILayout.HelpBox("Sin arma activa.", MessageType.Info); return; }

            var ammo = aw.Ammo;
            EditorGUILayout.LabelField($"{aw.Config?.WeaponID} — Munición", EditorStyles.boldLabel);

            EditorGUI.ProgressBar(
                EditorGUILayout.GetControlRect(GUILayout.Height(18)),
                ammo.MagazinePercent,
                $"Cargador: {ammo.CurrentMagazine} / {aw.Config?.MagazineSize}"
            );

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField("Reserva",        ammo.ReserveAmmo);
            EditorGUILayout.Toggle("Vacío",            ammo.IsEmpty);
            EditorGUILayout.Toggle("Lleno",            ammo.IsFullMagazine);
            EditorGUILayout.Toggle("Recargando",       aw.Reload.IsReloading);
            EditorGUI.EndDisabledGroup();

            if (aw.Reload.IsReloading)
                EditorGUI.ProgressBar(
                    EditorGUILayout.GetControlRect(GUILayout.Height(14)),
                    aw.Reload.Progress, "Recargando..."
                );

            EditorGUILayout.Space(6);
            if (GUILayout.Button("+ Añadir 30 reserva")) ammo.AddReserve(30);
            if (GUILayout.Button("Vaciar cargador"))      for (int i = 0; i < 999; i++) if (!ammo.Consume()) break;
        }

        // ── Tab: Recoil ───────────────────────────────────────

        private void DrawRecoilTab()
        {
            if (!Check(_manager, "WeaponManager")) return;

            var aw = _manager.ActiveWeapon;
            if (aw == null) { EditorGUILayout.HelpBox("Sin arma activa.", MessageType.Info); return; }

            var r = aw.Recoil;
            EditorGUILayout.LabelField($"{aw.Config?.WeaponID} — Recoil", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField("Acumulado Pitch",  r.AccumulatedPitch);
            EditorGUILayout.FloatField("Acumulado Yaw",    r.AccumulatedYaw);
            EditorGUILayout.IntField("Disparos en ráfaga", r.ShotCount);
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Reset Recoil")) r.Reset();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Patrón de Recoil:", EditorStyles.boldLabel);
            bool hasPattern = aw.Config?.RecoilPattern != null && aw.Config.RecoilPattern.Length > 0;

            if (hasPattern)
            {
                for (int i = 0; i < Mathf.Min(aw.Config.RecoilPattern.Length, 8); i++)
                {
                    var p = aw.Config.RecoilPattern[i];
                    EditorGUILayout.LabelField($"  [{i}] Yaw={p.x:F2}  Pitch={p.y:F2}");
                }
            }
            else
            {
                EditorGUILayout.LabelField(
                    $"  Aleatorio — Pitch: {aw.Config?.RecoilPitch:F2} | " +
                    $"Yaw: ±{aw.Config?.RecoilYawVariance:F2}");
            }
        }

        // ── Tab: Proyectiles ──────────────────────────────────

        private void DrawProjectilesTab()
        {
            if (!Application.isPlaying) { EditorGUILayout.HelpBox("Play Mode requerido.", MessageType.Info); return; }

            var pm = ProjectileManager.Instance;
            if (pm == null) { EditorGUILayout.HelpBox("ProjectileManager no encontrado en escena.", MessageType.Warning); return; }

            EditorGUILayout.LabelField("ProjectileManager", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("El pool se gestiona internamente.\nUsa los botones para debug.", MessageType.None);

            if (GUILayout.Button("↩ Retornar Todos al Pool"))
                pm.ReturnAll();

            EditorGUILayout.Space(6);

            if (_manager?.ActiveWeapon?.Config?.ProjectilePrefab != null)
            {
                var cfg = _manager.ActiveWeapon.Config;
                if (GUILayout.Button($"⚡ Prewarm '{cfg.WeaponID}' (+{20})"))
                    pm.Prewarm(cfg, 20);
            }
        }

        // ── Tab: Logs ─────────────────────────────────────────

        private void DrawLogsTab()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Eventos ({_logs.Count}/{MAX_LOGS})", EditorStyles.miniLabel);
            _autoScroll = EditorGUILayout.ToggleLeft("Auto", _autoScroll, GUILayout.Width(50));
            if (GUILayout.Button("Limpiar", GUILayout.Width(60))) _logs.Clear();
            EditorGUILayout.EndHorizontal();

            _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.Height(340));
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
            { EditorGUILayout.HelpBox("Entra en Play Mode.", MessageType.Info); return false; }
            if (obj == null)
            { EditorGUILayout.HelpBox($"{name} no encontrado. Pulsa 'Find Player'.", MessageType.Warning); return false; }
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
