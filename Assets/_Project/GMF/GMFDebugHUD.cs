// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_DebugHUD.cs                                ║
// ║                                                          ║
// ║  CLASE: GMFDebugHUD                                      ║
// ║    ► UI generada 100% por código (sin prefabs, sin Canvas)║
// ║    ► Toggle con F1                                       ║
// ║    ► Muestra: Modo, Fase, Score, Equipos, Banderas       ║
// ║    ► Añadir a cualquier GameObject en escena             ║
// ║    ► No requiere ninguna referencia manual               ║
// ╚══════════════════════════════════════════════════════════╝

using UnityEngine;

namespace GMF
{
    public class GMFDebugHUD : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Toggle")]
        [SerializeField] private KeyCode _toggleKey    = KeyCode.F1;
        [SerializeField] private bool    _showOnStart  = true;

        [Header("Posición y estilo")]
        [SerializeField] private float   _panelX       = 10f;
        [SerializeField] private float   _panelY       = 10f;
        [SerializeField] private float   _panelWidth   = 280f;
        [SerializeField] private float   _fontSize     = 13;

        // ── Estado ────────────────────────────────────────────

        private bool _visible;
        private GUIStyle _boxStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _teamRedStyle;
        private GUIStyle _teamBlueStyle;
        private bool     _stylesReady;

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            _visible = _showOnStart;
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
                _visible = !_visible;
        }

        private void InitStyles()
        {
            if (_stylesReady) return;

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding  = new RectOffset(10, 10, 8, 8),
                fontSize = (int)_fontSize
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = (int)(_fontSize + 2),
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Color.white }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = (int)_fontSize,
                normal   = { textColor = new Color(0.9f, 0.9f, 0.9f) }
            };

            _teamRedStyle = new GUIStyle(_labelStyle)
            {
                normal = { textColor = new Color(1f, 0.4f, 0.4f) }
            };

            _teamBlueStyle = new GUIStyle(_labelStyle)
            {
                normal = { textColor = new Color(0.4f, 0.6f, 1f) }
            };

            _stylesReady = true;
        }

        // ── OnGUI ─────────────────────────────────────────────

        private void OnGUI()
        {
            if (!_visible) return;

            InitStyles();

            var gm = GameModeBase.Instance;

            // Calcular altura dinámica según contenido
            float height = gm != null ? EstimateHeight(gm) : 120f;

            var rect = new Rect(_panelX, _panelY, _panelWidth, height);

            // Fondo semitransparente
            GUI.color = new Color(0, 0, 0, 0.75f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUILayout.BeginArea(rect);

            DrawHeader(gm);

            if (gm != null)
            {
                DrawModeInfo(gm);
                DrawTeamScores(gm);
                DrawObjectives(gm);
                DrawPlayers(gm);
            }
            else
            {
                GUILayout.Label("Sin partida activa.", _labelStyle);
                GUILayout.Label($"Presiona [F1] para ocultar.", _labelStyle);
            }

            GUILayout.EndArea();
        }

        private void DrawHeader(GameModeBase gm)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("🎮 GMF DEBUG HUD", _titleStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"[{_toggleKey}] Ocultar", _labelStyle, GUILayout.Width(100));
            GUILayout.EndHorizontal();

            DrawSeparator();
        }

        private void DrawModeInfo(GameModeBase gm)
        {
            var ctx = gm.Context;
            if (ctx == null) return;

            GUILayout.Label($"Modo:   {ctx.ModeID}", _labelStyle);

            // Fase con color
            Color phaseColor = ctx.Phase switch
            {
                GameModePhase.Playing  => Color.green,
                GameModePhase.WarmUp   => Color.yellow,
                GameModePhase.RoundEnd => new Color(1f, 0.6f, 0f),
                GameModePhase.PostGame => Color.cyan,
                _                      => Color.gray
            };

            var phaseStyle = new GUIStyle(_labelStyle) { normal = { textColor = phaseColor } };
            GUILayout.Label($"Fase:   {ctx.Phase}", phaseStyle);
            GUILayout.Label($"Ronda:  {ctx.CurrentRound}", _labelStyle);
            GUILayout.Label($"Tiempo: {ctx.ElapsedTime:F0}s", _labelStyle);
        }

        private void DrawTeamScores(GameModeBase gm)
        {
            var ctx = gm.Context;
            if (ctx?.Score == null || ctx.Teams == null) return;

            DrawSeparator();
            GUILayout.Label("SCORE", _titleStyle);

            int tc = ctx.Teams.TeamCount;
            for (int t = 0; t < tc; t++)
            {
                int   score = ctx.Score.GetTeamScore(t);
                int   count = ctx.Teams.GetPlayers(t).Count;
                GUIStyle s = t == 0 ? _teamRedStyle : _teamBlueStyle;
                string emoji = t == 0 ? "🔴" : "🔵";

                string bar = new string('█', Mathf.Min(score, 20));
                GUILayout.Label($"{emoji} T{t} ({count}p): {score}  {bar}", s);
            }
        }

        private void DrawObjectives(GameModeBase gm)
        {
            var objs = gm.Context?.Objectives?.GetAll();
            if (objs == null || objs.Count == 0) return;

            DrawSeparator();
            GUILayout.Label("OBJETIVOS", _titleStyle);

            foreach (var obj in objs)
            {
                string stateIcon = obj.State switch
                {
                    "Idle"     => "🏠",
                    "Carried"  => "🏃",
                    "Dropped"  => "⬇️",
                    "Captured" => "⭐",
                    "Contested"=> "⚔️",
                    _          => "○"
                };

                Color stateColor = obj.State switch
                {
                    "Idle"    => Color.green,
                    "Carried" => Color.yellow,
                    "Dropped" => new Color(1f, 0.5f, 0f),
                    _         => Color.white
                };

                var style = new GUIStyle(_labelStyle) { normal = { textColor = stateColor } };
                GUILayout.Label($"{stateIcon} {obj.ObjectiveID} [T{obj.TeamID}]: {obj.State}", style);

                // Mostrar portador si es una flag
                if (obj is Flag flag && flag.IsBeingCarried)
                    GUILayout.Label($"   └ Portador: P{flag.CarrierID}", _labelStyle);
            }
        }

        private void DrawPlayers(GameModeBase gm)
        {
            var ctx = gm.Context;
            if (ctx?.Teams == null) return;

            DrawSeparator();
            GUILayout.Label("JUGADORES", _titleStyle);

            int tc = ctx.Teams.TeamCount;
            for (int t = 0; t < tc; t++)
            {
                var players = ctx.Teams.GetPlayers(t);
                if (players.Count == 0) continue;

                GUIStyle s = t == 0 ? _teamRedStyle : _teamBlueStyle;
                GUILayout.Label($"T{t}:", s);
                foreach (int pid in players)
                {
                    int pscore = ctx.Score.GetPlayerScore(pid);
                    GUILayout.Label($"   P{pid} — score: {pscore}", _labelStyle);
                }
            }
        }

        private void DrawSeparator()
        {
            GUILayout.Space(3);
            var sepStyle = new GUIStyle(_labelStyle)
            {
                normal   = { textColor = new Color(0.5f, 0.5f, 0.5f) },
                fontSize = 8
            };
            GUILayout.Label("────────────────────────────────", sepStyle);
            GUILayout.Space(3);
        }

        private float EstimateHeight(GameModeBase gm)
        {
            int lines = 8; // header + modo info
            var ctx   = gm.Context;

            if (ctx != null)
            {
                lines += ctx.Teams?.TeamCount ?? 0;                            // scores
                lines += ctx.Objectives?.GetAll()?.Count ?? 0;                // objetivos
                lines += ctx.Teams?.TeamCount ?? 0;                            // encabezados equipo
                for (int t = 0; t < (ctx.Teams?.TeamCount ?? 0); t++)
                    lines += ctx.Teams.GetPlayers(t).Count;
            }

            return lines * (_fontSize + 6f) + 30f;
        }
    }
}