// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_RoundBanner.cs                             ║
// ║  CLASE: GMFRoundBanner                                   ║
// ║                                                          ║
// ║  Listón animado que aparece al ganar una ronda.          ║
// ║  Generado 100% por código. Sin Canvas, sin prefabs.      ║
// ║                                                          ║
// ║  ANIMACIÓN:                                              ║
// ║    - Entra desde izquierda (o derecha según config)      ║
// ║    - Muestra nombre del equipo ganador                   ║
// ║    - Tabla de clasificación debajo                       ║
// ║    - Sale hacia el mismo lado después de _displayTime    ║
// ║                                                          ║
// ║  AÑADIR: a cualquier GO en la escena de juego            ║
// ╚══════════════════════════════════════════════════════════╝

using Core.Events;
using UnityEngine;

namespace GMF
{
    public class GMFRoundBanner : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Animación")]
        [Tooltip("Entra desde la izquierda si true, desde la derecha si false.")]
        [SerializeField] private bool  _fromLeft       = true;

        [SerializeField] private float _slideInTime    = 0.45f;
        [SerializeField] private float _displayTime    = 3.5f;
        [SerializeField] private float _slideOutTime   = 0.35f;

        [Header("Layout")]
        [SerializeField] private float _bannerHeight   = 90f;
        [SerializeField] private float _yPosition      = 0.35f;  // 0=top 1=bottom
        [SerializeField] private float _overWidth      = 120f;   // extra fuera de pantalla

        [Header("Estilo")]
        [SerializeField] private Color _bannerColor      = new Color(0.08f, 0.08f, 0.12f, 0.97f);
        [SerializeField] private Color _accentColor      = new Color(1f, 0.85f, 0.2f, 1f);   // borde dorado
        [SerializeField] private float _accentHeight     = 5f;
        [SerializeField] private float _tablePadding     = 8f;
        [SerializeField] private float _tableRowHeight   = 22f;

        // ── Estado de la animación ────────────────────────────

        private enum BannerState { Hidden, SlideIn, Display, SlideOut }
        private BannerState _state = BannerState.Hidden;

        private float  _timer;
        private float  _xOffset;          // posición X actual del banner
        private float  _targetX;          // posición X final (0 = centrado)
        private float  _startX;           // posición X de inicio

        // ── Datos del ganador ─────────────────────────────────

        private string  _winnerName;
        private Color   _winnerColor;
        private int     _winnerRoundWins;
        private int     _totalRoundsToWin;

        // Standings (teamID, name, roundWins, score) ordenados
        private (int teamID, string name, int rWins, int score)[] _standings;

        private bool     _stylesReady;
        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _tableStyle;
        private GUIStyle _tableHeaderStyle;

        // ── Lifecycle ─────────────────────────────────────────

        private void OnEnable()
        {
            EventBus<RoundEndedEvt>.Subscribe(OnRoundEnded);
        }

        private void OnDisable()
        {
            EventBus<RoundEndedEvt>.Unsubscribe(OnRoundEnded);
        }

        // ── Trigger ───────────────────────────────────────────

        private void OnRoundEnded(RoundEndedEvt e)
        {
            // Recopilar datos del GameMode
            var gm  = GameModeBase.Instance;
            var ctx = gm?.Context;

            // Obtener nombre y color del ganador
            _winnerName  = GetTeamName(gm, e.WinnerTeamID);
            _winnerColor = GetTeamColor(gm, e.WinnerTeamID);

            // Rondas ganadas del ganador (del tracking interno de GameModeBase)
            _winnerRoundWins  = GetRoundWins(gm, e.WinnerTeamID);
            _totalRoundsToWin = GetRoundsToWin(gm);

            // Construir tabla de clasificación
            BuildStandings(gm, ctx, e.WinnerTeamID);

            // Iniciar animación
            Show();
        }

        private void Show()
        {
            float totalWidth = Screen.width + _overWidth * 2f;
            _startX  = _fromLeft ? -totalWidth : Screen.width + _overWidth;
            _targetX = 0f;
            _xOffset = _startX;
            _timer   = 0f;
            _state   = BannerState.SlideIn;
        }

        // ── Update ────────────────────────────────────────────

        private void Update()
        {
            if (_state == BannerState.Hidden) return;

            _timer += Time.unscaledDeltaTime;

            switch (_state)
            {
                case BannerState.SlideIn:
                    float t1  = Mathf.Clamp01(_timer / _slideInTime);
                    _xOffset  = Mathf.Lerp(_startX, _targetX, EaseOut(t1));
                    if (_timer >= _slideInTime) { _timer = 0f; _state = BannerState.Display; }
                    break;

                case BannerState.Display:
                    _xOffset  = _targetX;
                    if (_timer >= _displayTime) { _timer = 0f; StartSlideOut(); }
                    break;

                case BannerState.SlideOut:
                    float t3  = Mathf.Clamp01(_timer / _slideOutTime);
                    float exitX = _fromLeft ? Screen.width + _overWidth : -Screen.width - _overWidth;
                    _xOffset  = Mathf.Lerp(_targetX, exitX, EaseIn(t3));
                    if (_timer >= _slideOutTime) { _state = BannerState.Hidden; }
                    break;
            }
        }

        private void StartSlideOut()
        {
            _state = BannerState.SlideOut;
        }

        // ── OnGUI ─────────────────────────────────────────────

        private void OnGUI()
        {
            if (_state == BannerState.Hidden) return;

            InitStyles();

            float sw = Screen.width;
            float sh = Screen.height;
            float tableRows = _standings?.Length > 0 ? _standings.Length : 0;
            float tableH    = tableRows > 0
                ? (_tableRowHeight * tableRows) + _tablePadding * 2f + _tableRowHeight  // header + rows
                : 0f;

            float totalH = _bannerHeight + (tableRows > 0 ? tableH + 8f : 0f);
            float y      = sh * _yPosition - totalH * 0.5f;

            // ── Listón principal ──────────────────────────────

            var bannerRect = new Rect(_xOffset, y, sw, _bannerHeight);

            // Fondo del listón
            GUI.color = _bannerColor;
            GUI.DrawTexture(bannerRect, Texture2D.whiteTexture);

            // Borde izquierdo (acento del color del equipo ganador)
            GUI.color = _winnerColor;
            GUI.DrawTexture(new Rect(_xOffset, y, 8f, _bannerHeight), Texture2D.whiteTexture);

            // Acento inferior dorado
            GUI.color = _accentColor;
            GUI.DrawTexture(new Rect(_xOffset, y + _bannerHeight - _accentHeight, sw, _accentHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Texto principal
            var titleRect = new Rect(_xOffset + 20f, y + 8f, sw - 40f, 38f);
            _titleStyle.normal.textColor = _winnerColor;
            GUI.Label(titleRect, $"🏆  {_winnerName.ToUpper()}  GANA LA RONDA", _titleStyle);

            // Subtítulo rondas ganadas
            string subtext = $"Victorias: {_winnerRoundWins} / {_totalRoundsToWin}";
            _subtitleStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
            GUI.Label(new Rect(_xOffset + 20f, y + 50f, sw - 40f, 24f), subtext, _subtitleStyle);

            // ── Tabla de clasificación ────────────────────────

            if (tableRows > 0)
            {
                float ty = y + _bannerHeight + 4f;
                var tableRect = new Rect(_xOffset, ty, sw, tableH);

                // Fondo tabla
                GUI.color = new Color(0f, 0f, 0f, 0.88f);
                GUI.DrawTexture(tableRect, Texture2D.whiteTexture);
                GUI.color = Color.white;

                // Header
                float ry = ty + _tablePadding;
                _tableHeaderStyle.normal.textColor = _accentColor;
                GUI.Label(new Rect(_xOffset + 20f, ry, 60f,  _tableRowHeight), "POS",   _tableHeaderStyle);
                GUI.Label(new Rect(_xOffset + 90f, ry, 200f, _tableRowHeight), "EQUIPO",_tableHeaderStyle);
                GUI.Label(new Rect(_xOffset + 340f,ry, 80f,  _tableRowHeight), "RONDAS",_tableHeaderStyle);
                GUI.Label(new Rect(_xOffset + 460f,ry, 80f,  _tableRowHeight), "PUNTOS",_tableHeaderStyle);
                ry += _tableRowHeight;

                // Separador
                GUI.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                GUI.DrawTexture(new Rect(_xOffset + 10f, ry, sw - 20f, 1f), Texture2D.whiteTexture);
                GUI.color = Color.white;
                ry += 4f;

                for (int i = 0; i < _standings.Length; i++)
                {
                    var s = _standings[i];
                    Color rowColor = i == 0
                        ? _winnerColor
                        : new Color(0.75f, 0.75f, 0.75f);

                    _tableStyle.normal.textColor = rowColor;

                    string medal = i == 0 ? "🥇" : i == 1 ? "🥈" : i == 2 ? "🥉" : $"  {i + 1}.";

                    GUI.Label(new Rect(_xOffset + 20f, ry, 60f,  _tableRowHeight), medal, _tableStyle);
                    GUI.Label(new Rect(_xOffset + 90f, ry, 230f, _tableRowHeight), s.name,  _tableStyle);
                    GUI.Label(new Rect(_xOffset + 340f,ry, 80f,  _tableRowHeight), s.rWins.ToString(), _tableStyle);
                    GUI.Label(new Rect(_xOffset + 460f,ry, 80f,  _tableRowHeight), s.score.ToString(), _tableStyle);

                    ry += _tableRowHeight;
                }
            }
        }

        // ── Build standings ───────────────────────────────────

        private void BuildStandings(GameModeBase gm, IGameModeContext ctx, int winnerTeamID)
        {
            if (ctx == null) return;

            int tc = ctx.Teams.TeamCount;

            // Obtener rondas ganadas para cada equipo (via reflexión del dict interno)
            var list = new System.Collections.Generic.List<(int, string, int, int)>();
            for (int t = 0; t < tc; t++)
            {
                string name   = GetTeamName(gm, t);
                int    rWins  = GetRoundWins(gm, t);
                int    score  = ctx.Score.GetTeamScore(t);
                list.Add((t, name, rWins, score));
            }

            // Ordenar: 1º por rondas ganadas (desc), 2º por score (desc)
            list.Sort((a, b) =>
            {
                int cmp = b.Item3.CompareTo(a.Item3);
                return cmp != 0 ? cmp : b.Item4.CompareTo(a.Item4);
            });

            _standings = list.ToArray();
        }

        // ── Helpers ───────────────────────────────────────────

        private string GetTeamName(GameModeBase gm, int teamID)
        {
            var f = gm?.GetType().GetField("_def",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f?.GetValue(gm) is GMF.Config.GMF_Config def
                && def.TeamConfig?.TeamNames != null
                && teamID < def.TeamConfig.TeamNames.Length)
                return def.TeamConfig.TeamNames[teamID];
            return $"Equipo {teamID}";
        }

        private Color GetTeamColor(GameModeBase gm, int teamID)
        {
            var f = gm?.GetType().GetField("_def",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f?.GetValue(gm) is GMF.Config.GMF_Config def
                && def.TeamConfig?.TeamColors != null
                && teamID < def.TeamConfig.TeamColors.Length)
                return def.TeamConfig.TeamColors[teamID];
            Color[] fallback = { Color.red, Color.blue, Color.green, Color.yellow };
            return teamID < fallback.Length ? fallback[teamID] : Color.white;
        }

        private int GetRoundWins(GameModeBase gm, int teamID)
        {
            // El diccionario _roundWins está en GameModeBase (private)
            var f = gm?.GetType().GetField("_roundWins",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f?.GetValue(gm) is System.Collections.Generic.Dictionary<int, int> d)
                return d.TryGetValue(teamID, out int v) ? v : 0;
            return 0;
        }

        private int GetRoundsToWin(GameModeBase gm)
        {
            var f = gm?.GetType().GetField("_def",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f?.GetValue(gm) is GMF.Config.GMF_Config def)
                return def.RoundConfig.RoundsToWinMatch;
            return 1;
        }

        // ── Easing ────────────────────────────────────────────

        private float EaseOut(float t) => 1f - (1f - t) * (1f - t);
        private float EaseIn(float t)  => t * t;

        // ── Styles ────────────────────────────────────────────

        private void InitStyles()
        {
            if (_stylesReady) return;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            _subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 14,
                alignment = TextAnchor.MiddleLeft
            };

            _tableStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };

            _tableHeaderStyle = new GUIStyle(_tableStyle)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal
            };

            _stylesReady = true;
        }
    }
}