// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_RoundBanner.cs  (REEMPLAZA el anterior)    ║
// ║                                                          ║
// ║  NUEVO: Pantalla de fin de PARTIDA (EndGame)             ║
// ║    - Tabla final con todos los equipos                   ║
// ║    - Rondas ganadas por equipo                           ║
// ║    - Puntos totales                                      ║
// ║    - Banner del ganador arriba                           ║
// ║    - Indicador de muerte súbita si aplica                ║
// ╚══════════════════════════════════════════════════════════╝

using Core.Events;
using UnityEngine;

namespace GMF
{
    public class GMFRoundBanner : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Animación del listón")]
        [SerializeField] private bool  _fromLeft        = true;
        [SerializeField] private float _slideInTime     = 0.45f;
        [SerializeField] private float _displayTime     = 4.5f;
        [SerializeField] private float _slideOutTime    = 0.35f;

        [Header("Layout")]
        [SerializeField] private float _bannerHeight    = 90f;
        [SerializeField] private float _bannerY         = 0.30f;
        [SerializeField] private float _tableRowH       = 24f;
        [SerializeField] private float _tableColName    = 200f;
        [SerializeField] private float _tableColRounds  = 100f;
        [SerializeField] private float _tableColScore   = 100f;
        [SerializeField] private float _tableColKills   = 100f;

        [Header("Colores")]
        [SerializeField] private Color _bannerBg        = new Color(0.07f, 0.07f, 0.11f, 0.97f);
        [SerializeField] private Color _accentGold      = new Color(1f, 0.82f, 0.2f, 1f);
        [SerializeField] private Color _endGameBg       = new Color(0.04f, 0.04f, 0.08f, 0.98f);

        // ── Estado ────────────────────────────────────────────

        private enum Mode { Round, EndGame }
        private enum AnimState { Hidden, SlideIn, Display, SlideOut }

        private AnimState _anim  = AnimState.Hidden;
        private Mode      _mode  = Mode.Round;
        private float     _timer;
        private float     _xOff;

        // Datos del ganador de ronda
        private string  _winnerName;
        private Color   _winnerColor;
        private int     _winnerRounds;
        private int     _roundsNeeded;
        private bool    _isSuddenDeath;

        // Tabla de clasificación
        private (int id, string name, Color col, int rWins, int score, int kills)[] _table;

        private bool     _styles;
        private GUIStyle _titleSt, _subSt, _tblSt, _tblHdSt, _endTitleSt;

        // ── Lifecycle ─────────────────────────────────────────

        private void OnEnable()
        {
            EventBus<RoundEndedEvt>.Subscribe(OnRoundEnded);
            EventBus<GameEndedEvt>.Subscribe(OnGameEnded);
            EventBus<SuddenDeathStartedEvt>.Subscribe(e =>
            {
                // Mostrar brevemente "MUERTE SÚBITA"
            });
        }

        private void OnDisable()
        {
            EventBus<RoundEndedEvt>.Unsubscribe(OnRoundEnded);
            EventBus<GameEndedEvt>.Unsubscribe(OnGameEnded);
        }

        // ── Eventos ───────────────────────────────────────────

        private void OnRoundEnded(RoundEndedEvt e)
        {
            var gm = GameModeBase.Instance;
            if (gm == null) return;

            _mode          = Mode.Round;
            _winnerName    = GetTeamName(gm, e.WinnerTeamID);
            _winnerColor   = GetTeamColor(gm, e.WinnerTeamID);
            _winnerRounds  = GetRoundWins(gm, e.WinnerTeamID);
            _roundsNeeded  = gm.RoundsToWin;
            _isSuddenDeath = gm.IsSuddenDeath;

            BuildTable(gm);
            Show(_displayTime);
        }

        private void OnGameEnded(GameEndedEvt e)
        {
            var gm = GameModeBase.Instance;
            if (gm == null) return;

            _mode          = Mode.EndGame;
            _winnerName    = GetTeamName(gm, e.WinnerTeamID);
            _winnerColor   = GetTeamColor(gm, e.WinnerTeamID);
            _isSuddenDeath = gm.IsSuddenDeath;

            BuildTable(gm);
            Show(gm.Definition?.RoundConfig.EndGameDuration ?? 8f);
        }

        // ── Animación ─────────────────────────────────────────

        private void Show(float displayDuration)
        {
            float sw = Screen.width;
            _xOff  = _fromLeft ? -(sw + 200f) : (sw + 200f);
            _timer = 0f;
            _anim  = AnimState.SlideIn;

            // Guardar displayDuration como campo temporal
            _storedDisplay = displayDuration;
        }

        private float _storedDisplay = 4.5f;

        private void Update()
        {
            if (_anim == AnimState.Hidden) return;
            _timer += Time.unscaledDeltaTime;

            switch (_anim)
            {
                case AnimState.SlideIn:
                    _xOff = Mathf.Lerp(_fromLeft ? -(Screen.width + 200f) : (Screen.width + 200f),
                        0f, EaseOut(_timer / _slideInTime));
                    if (_timer >= _slideInTime) { _timer = 0f; _anim = AnimState.Display; }
                    break;

                case AnimState.Display:
                    _xOff = 0f;
                    if (_timer >= _storedDisplay) { _timer = 0f; _anim = AnimState.SlideOut; }
                    break;

                case AnimState.SlideOut:
                    _xOff = Mathf.Lerp(0f,
                        _fromLeft ? (Screen.width + 200f) : -(Screen.width + 200f),
                        EaseIn(_timer / _slideOutTime));
                    if (_timer >= _slideOutTime) _anim = AnimState.Hidden;
                    break;
            }
        }

        // ── OnGUI ─────────────────────────────────────────────

        private void OnGUI()
        {
            if (_anim == AnimState.Hidden) return;
            InitStyles();

            if (_mode == Mode.Round) DrawRoundBanner();
            else                     DrawEndGamePanel();
        }

        // ── Round Banner ──────────────────────────────────────

        private void DrawRoundBanner()
        {
            float sw = Screen.width;
            float y  = Screen.height * _bannerY;

            // Listón principal
            var bRect = new Rect(_xOff, y, sw, _bannerHeight);
            GUI.color = _bannerBg;
            GUI.DrawTexture(bRect, Texture2D.whiteTexture);

            // Borde izquierdo del color del equipo
            GUI.color = _winnerColor;
            GUI.DrawTexture(new Rect(_xOff, y, 8f, _bannerHeight), Texture2D.whiteTexture);

            // Línea dorada inferior
            GUI.color = _accentGold;
            GUI.DrawTexture(new Rect(_xOff, y + _bannerHeight - 5f, sw, 5f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Título
            _titleSt.normal.textColor = _winnerColor;
            string sdTag = _isSuddenDeath ? "  ⚡MUERTE SÚBITA" : "";
            GUI.Label(new Rect(_xOff + 20f, y + 6f, sw - 40f, 40f),
                $"🏆  {_winnerName.ToUpper()}  GANA LA RONDA{sdTag}", _titleSt);

            // Subtítulo
            _subSt.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            GUI.Label(new Rect(_xOff + 20f, y + 52f, sw - 40f, 24f),
                $"Victorias de ronda:  {_winnerRounds} / {_roundsNeeded}", _subSt);

            // Tabla
            DrawStandingsTable(_xOff, y + _bannerHeight + 4f, sw);
        }

        // ── End Game Panel ────────────────────────────────────

        private void DrawEndGamePanel()
        {
            float sw = Screen.width;
            float sh = Screen.height;
            float tableH = (_table?.Length ?? 0) * _tableRowH + _tableRowH + 16f;
            float panelH = 130f + tableH;
            float x      = sw * 0.5f - sw * 0.4f + _xOff;
            float y      = sh * 0.5f - panelH * 0.5f;
            float pw     = sw * 0.8f;

            // Fondo panel
            GUI.color = _endGameBg;
            GUI.DrawTexture(new Rect(x, y, pw, panelH), Texture2D.whiteTexture);

            // Borde superior del color del ganador
            GUI.color = _winnerColor;
            GUI.DrawTexture(new Rect(x, y, pw, 7f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Título grande
            _endTitleSt.normal.textColor = _winnerColor;
            string sdLine = _isSuddenDeath ? "\n⚡ Decisión por Muerte Súbita" : "";
            GUI.Label(new Rect(x + 20f, y + 15f, pw - 40f, 60f),
                $"🏆  {_winnerName.ToUpper()}  GANA LA PARTIDA{sdLine}", _endTitleSt);

            // Tabla final
            DrawStandingsTable(x, y + 90f, pw);
        }

        // ── Tabla compartida ──────────────────────────────────

        private void DrawStandingsTable(float x, float y, float w)
        {
            if (_table == null || _table.Length == 0) return;

            // Fondo tabla
            float rows   = _table.Length + 1;  // +1 header
            float tableH = rows * _tableRowH + 12f;
            GUI.color = new Color(0f, 0f, 0f, 0.88f);
            GUI.DrawTexture(new Rect(x, y, w, tableH), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float ry = y + 6f;

            // Header
            _tblHdSt.normal.textColor = _accentGold;
            float col0 = x + 20f, col1 = x + 80f, col2 = x + col1 - x + _tableColName;
            float col3 = col2 + _tableColRounds, col4 = col3 + _tableColScore;
            float col5 = col4 + _tableColKills;

            GUI.Label(new Rect(col0, ry, 50f,         _tableRowH), "POS",    _tblHdSt);
            GUI.Label(new Rect(col1, ry, _tableColName, _tableRowH), "EQUIPO",  _tblHdSt);
            GUI.Label(new Rect(col3, ry, _tableColRounds, _tableRowH), "RONDAS", _tblHdSt);
            GUI.Label(new Rect(col4, ry, _tableColScore,  _tableRowH), "PUNTOS", _tblHdSt);
            GUI.Label(new Rect(col5, ry, _tableColKills,  _tableRowH), "KILLS",  _tblHdSt);
            ry += _tableRowH;

            for (int i = 0; i < _table.Length; i++)
            {
                var e     = _table[i];
                string pos = i == 0 ? "🥇" : i == 1 ? "🥈" : i == 2 ? "🥉" : $" {i + 1}.";
                Color rowC = i == 0 ? e.col : new Color(0.72f, 0.72f, 0.72f);
                _tblSt.normal.textColor = rowC;

                GUI.Label(new Rect(col0, ry, 50f,          _tableRowH), pos,                       _tblSt);
                GUI.Label(new Rect(col1, ry, _tableColName, _tableRowH), e.name,                   _tblSt);
                GUI.Label(new Rect(col3, ry, _tableColRounds,_tableRowH), e.rWins.ToString(),      _tblSt);
                GUI.Label(new Rect(col4, ry, _tableColScore, _tableRowH), e.score.ToString(),      _tblSt);
                GUI.Label(new Rect(col5, ry, _tableColKills, _tableRowH), e.kills.ToString(),      _tblSt);
                ry += _tableRowH;
            }
        }

        // ── Helpers ───────────────────────────────────────────

        private void BuildTable(GameModeBase gm)
        {
            var ctx = gm.Context;
            if (ctx == null) return;

            int tc  = ctx.Teams.TeamCount;
            var list = new System.Collections.Generic.List<(int,string,Color,int,int,int)>();

            for (int t = 0; t < tc; t++)
            {
                list.Add((t,
                    GetTeamName(gm, t),
                    GetTeamColor(gm, t),
                    GetRoundWins(gm, t),
                    ctx.Score.GetTeamScore(t),
                    (ctx.Score as ScoreSystem)?.GetTeamKills(t) ?? 0
                ));
            }

            list.Sort((a, b) =>
            {
                int c = b.Item4.CompareTo(a.Item4);  // rondas desc
                if (c != 0) return c;
                c = b.Item5.CompareTo(a.Item5);       // puntos desc
                if (c != 0) return c;
                return b.Item6.CompareTo(a.Item6);    // kills desc
            });

            _table = list.ToArray();
        }

        private string GetTeamName(GameModeBase gm, int id)
        {
            var def = gm.Definition;
            return def?.TeamConfig?.TeamNames != null && id < def.TeamConfig.TeamNames.Length
                ? def.TeamConfig.TeamNames[id] : $"Equipo {id}";
        }

        private Color GetTeamColor(GameModeBase gm, int id)
        {
            var def = gm.Definition;
            if (def?.TeamConfig?.TeamColors != null && id < def.TeamConfig.TeamColors.Length)
                return def.TeamConfig.TeamColors[id];
            Color[] fb = { Color.red, Color.blue, Color.green, Color.yellow };
            return id < fb.Length ? fb[id] : Color.white;
        }

        private int GetRoundWins(GameModeBase gm, int id)
            => gm.RoundWinsPerTeam.TryGetValue(id, out int w) ? w : 0;

        private float EaseOut(float t) => 1f - (1f - t) * (1f - t);
        private float EaseIn (float t) => t * t;

        private void InitStyles()
        {
            if (_styles) return;

            _titleSt = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 22, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _subSt = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14, alignment = TextAnchor.MiddleLeft
            };
            _tblSt = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 14, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _tblHdSt = new GUIStyle(_tblSt)
            {
                fontSize  = 12, fontStyle = FontStyle.Normal
            };
            _endTitleSt = new GUIStyle(_titleSt)
            {
                fontSize = 26, wordWrap = true
            };

            _styles = true;
        }
    }
}