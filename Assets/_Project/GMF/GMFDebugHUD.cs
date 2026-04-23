// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_DebugHUD.cs  (REEMPLAZA el anterior)       ║
// ║                                                          ║
// ║  NUEVO:                                                  ║
// ║    + Indicador de muerte súbita                          ║
// ║    + Rondas ganadas por equipo                           ║
// ║    + Kills por equipo                                    ║
// ║    + Timer de ronda en grande                            ║
// ╚══════════════════════════════════════════════════════════╝

using UnityEngine;

namespace GMF
{
    public class GMFDebugHUD : MonoBehaviour
    {
        [Header("Toggle")]
        [SerializeField] private KeyCode _toggleKey   = KeyCode.F1;
        [SerializeField] private bool    _showOnStart = true;

        [Header("Layout")]
        [SerializeField] private float _panelX     = 10f;
        [SerializeField] private float _panelY     = 10f;
        [SerializeField] private float _panelWidth = 300f;
        [SerializeField] private float _fontSize   = 13;

        private bool     _visible;
        private GUIStyle _boxSt, _titleSt, _labelSt, _sdSt;
        private bool     _ready;

        private void Awake()  => _visible = _showOnStart;
        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey)) _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible) return;
            InitStyles();

            var gm  = GameModeBase.Instance;
            var ctx = gm?.Context;

            float h = EstimateHeight(ctx, gm);
            var r   = new Rect(_panelX, _panelY, _panelWidth, h);

            GUI.color = new Color(0, 0, 0, 0.78f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUILayout.BeginArea(r);
            GUILayout.Space(6);

            // Header
            GUILayout.BeginHorizontal();
            GUILayout.Label("🎮 GMF DEBUG", _titleSt);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"[{_toggleKey}]", _labelSt, GUILayout.Width(40));
            GUILayout.EndHorizontal();

            // Muerte súbita
            if (gm != null && gm.IsSuddenDeath)
            {
                GUILayout.Label("⚡ MUERTE SÚBITA", _sdSt);
            }

            Sep();

            if (gm == null || ctx == null)
            {
                GUILayout.Label("Sin partida activa.", _labelSt);
            }
            else
            {
                DrawModeSection(gm, ctx);
                Sep();
                DrawScoreSection(gm, ctx);
                Sep();
                DrawObjectivesSection(ctx);
                Sep();
                DrawPlayersSection(ctx);
            }

            GUILayout.Space(6);
            GUILayout.EndArea();
        }

        private void DrawModeSection(GameModeBase gm, IGameModeContext ctx)
        {
            GUILayout.Label($"Modo:  {ctx.ModeID}", _labelSt);

            Color phaseC = ctx.Phase switch
            {
                GameModePhase.Playing  => Color.green,
                GameModePhase.WarmUp   => Color.yellow,
                GameModePhase.RoundEnd => new Color(1f, 0.5f, 0f),
                GameModePhase.PostGame => Color.cyan,
                _                      => Color.gray
            };
            var ps = new GUIStyle(_labelSt) { normal = { textColor = phaseC } };
            GUILayout.Label($"Fase:  {ctx.Phase}", ps);

            GUILayout.Label($"Ronda: {ctx.CurrentRound}  |  Tiempo: {ctx.ElapsedTime:F0}s", _labelSt);

            // Rondas necesarias
            GUILayout.Label($"Para ganar: {gm.RoundsToWin} ronda(s)", _labelSt);
        }

        private void DrawScoreSection(GameModeBase gm, IGameModeContext ctx)
        {
            GUILayout.Label("PUNTOS / RONDAS / KILLS", _titleSt);
            int tc = ctx.Teams.TeamCount;
            for (int t = 0; t < tc; t++)
            {
                int   pts   = ctx.Score.GetTeamScore(t);
                int   rWins = gm.RoundWinsPerTeam.TryGetValue(t, out int w) ? w : 0;
                int   kills = (ctx.Score as ScoreSystem)?.GetTeamKills(t) ?? 0;
                Color col   = t == 0 ? new Color(1f, 0.4f, 0.4f) : new Color(0.4f, 0.6f, 1f);
                var   st    = new GUIStyle(_labelSt) { normal = { textColor = col } };

                string emoji = t == 0 ? "🔴" : "🔵";
                GUILayout.Label($"{emoji} T{t}: {pts}pts  |  {rWins}/{gm.RoundsToWin} rondas  |  {kills}K", st);
            }
        }

        private void DrawObjectivesSection(IGameModeContext ctx)
        {
            var objs = ctx.Objectives?.GetAll();
            if (objs == null || objs.Count == 0) return;

            GUILayout.Label("OBJETIVOS", _titleSt);
            foreach (var obj in objs)
            {
                Color c = obj.State switch
                {
                    "Idle"    => Color.green,
                    "Carried" => Color.yellow,
                    "Dropped" => new Color(1f,0.5f,0f),
                    _         => Color.white
                };
                var st = new GUIStyle(_labelSt) { normal = { textColor = c } };
                string line = $"• {obj.ObjectiveID} [{obj.State}]";
                if (obj is Flag f && f.IsBeingCarried) line += $" (P{f.CarrierID})";
                GUILayout.Label(line, st);
            }
        }

        private void DrawPlayersSection(IGameModeContext ctx)
        {
            GUILayout.Label("JUGADORES", _titleSt);
            for (int t = 0; t < ctx.Teams.TeamCount; t++)
            {
                Color col = t == 0 ? new Color(1f,0.4f,0.4f) : new Color(0.4f,0.6f,1f);
                var   hd  = new GUIStyle(_labelSt) { normal = { textColor = col }, fontStyle = FontStyle.Bold };
                GUILayout.Label($"T{t}:", hd);
                foreach (int pid in ctx.Teams.GetPlayers(t))
                    GUILayout.Label($"  P{pid} — {ctx.Score.GetPlayerScore(pid)}pts", _labelSt);
            }
        }

        private void Sep()
        {
            var s = new GUIStyle(_labelSt) { normal = { textColor = new Color(0.4f,0.4f,0.4f) }, fontSize = 8 };
            GUILayout.Label("───────────────────────────", s);
        }

        private float EstimateHeight(IGameModeContext ctx, GameModeBase gm)
        {
            int lines = 10;
            if (ctx != null)
            {
                lines += ctx.Teams?.TeamCount ?? 0;
                lines += ctx.Objectives?.GetAll()?.Count ?? 0;
                lines += ctx.Teams?.TeamCount ?? 0;
                for (int t = 0; t < (ctx.Teams?.TeamCount ?? 0); t++)
                    lines += ctx.Teams.GetPlayers(t).Count;
            }
            if (gm?.IsSuddenDeath == true) lines += 2;
            return lines * (_fontSize + 6f) + 30f;
        }

        private void InitStyles()
        {
            if (_ready) return;
            _titleSt = new GUIStyle(GUI.skin.label) { fontSize = (int)(_fontSize + 1), fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            _labelSt = new GUIStyle(GUI.skin.label) { fontSize = (int)_fontSize, normal  = { textColor = new Color(0.9f, 0.9f, 0.9f) } };
            _sdSt    = new GUIStyle(_titleSt) { fontSize = (int)(_fontSize + 3), normal  = { textColor = new Color(1f, 0.5f, 0f) } };
            _boxSt   = new GUIStyle(GUI.skin.box);
            _ready   = true;
        }
    }
}