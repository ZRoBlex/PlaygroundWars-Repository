// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_TeamSelectUI.cs                            ║
// ║  CLASE: GMFTeamSelectUI                                  ║
// ║                                                          ║
// ║  UI de selección de equipo generada 100% por código.     ║
// ║  Sin Canvas, sin prefabs, sin referencias manuales.      ║
// ║                                                          ║
// ║  LAYOUT CIRCULAR:                                        ║
// ║    2 equipos → 180° separación (izq/der)                 ║
// ║    3 equipos → 120° separación (triángulo)               ║
// ║    4 equipos → 90°  (rombo)                              ║
// ║    5 equipos → 72°  (cara de dado)                       ║
// ║    N equipos → 360/N grados entre cada uno               ║
// ║                                                          ║
// ║  FLUJO:                                                  ║
// ║    1. Jugador presiona "." → se abre el menú             ║
// ║    2. Pasa el cursor sobre un equipo → highlight         ║
// ║    3. Hace click → PlayerTeamAssigner.ChangeTeam()       ║
// ║    4. El jugador muere y respawnea con el color nuevo     ║
// ║                                                          ║
// ║  AÑADIR: al mismo GO que tiene PlayerTeamAssigner        ║
// ╚══════════════════════════════════════════════════════════╝

using Core.Events;
using Player.Authority;
using Player.Events;
using UnityEngine;

namespace GMF
{
    [RequireComponent(typeof(PlayerAuthority))]
    [DisallowMultipleComponent]
    public class GMFTeamSelectUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Toggle")]
        [SerializeField] private KeyCode _openKey       = KeyCode.Period;

        [Header("Layout")]
        [SerializeField] private float   _circleRadius   = 110f;   // radio del círculo de botones
        [SerializeField] private float   _buttonRadius   = 38f;    // radio de cada botón (círculo)
        [SerializeField] private float   _centerX        = 0.5f;   // 0-1 relativo a pantalla
        [SerializeField] private float   _centerY        = 0.5f;

        [Header("Estilo")]
        [SerializeField] private float   _bgAlpha        = 0.82f;
        [SerializeField] private float   _hoverScale     = 1.25f;  // escala al pasar el cursor

        // ── Estado ────────────────────────────────────────────

        private bool  _isOpen;
        private int   _hoveredTeam  = -1;
        private bool  _stylesReady;

        private GUIStyle _teamLabelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _hintStyle;

        private PlayerAuthority     _authority;
        private PlayerTeamAssigner  _assigner;

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            _authority = GetComponent<PlayerAuthority>();
            _assigner  = GetComponent<PlayerTeamAssigner>();
        }

        private void Update()
        {
            if (!_authority.IsLocalPlayer) return;

            if (Input.GetKeyDown(_openKey))
            {
                _isOpen = !_isOpen;

                // Gestión del cursor
                if (_isOpen)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible   = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible   = false;
                }
            }
        }

        // ── OnGUI ─────────────────────────────────────────────

        private void OnGUI()
        {
            if (!_isOpen || !_authority.IsLocalPlayer) return;

            InitStyles();

            var gm = GameModeBase.Instance;
            int teamCount = gm?.Context?.Teams?.TeamCount ?? 0;

            float cx = Screen.width  * _centerX;
            float cy = Screen.height * _centerY;

            // ── Fondo semitransparente ─────────────────────────

            float panelSize = _circleRadius * 2f + _buttonRadius * 3f;
            var   panelRect = new Rect(cx - panelSize * 0.5f, cy - panelSize * 0.5f, panelSize, panelSize);

            GUI.color = new Color(0f, 0f, 0f, _bgAlpha);
            GUI.DrawTexture(panelRect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = Color.white;

            // ── Título ────────────────────────────────────────

            GUI.Label(new Rect(cx - 120f, cy - panelSize * 0.5f + 8f, 240f, 28f),
                "Seleccionar Equipo", _titleStyle);

            GUI.Label(new Rect(cx - 120f, cy + panelSize * 0.5f - 28f, 240f, 22f),
                $"[{_openKey}] Cerrar", _hintStyle);

            if (teamCount == 0)
            {
                GUI.Label(new Rect(cx - 100f, cy - 12f, 200f, 24f),
                    "Sin modo activo", _hintStyle);
                return;
            }

            // ── Botones de equipo en círculo ──────────────────

            _hoveredTeam = -1;
            Vector2 mousePos = Event.current.mousePosition;

            for (int t = 0; t < teamCount; t++)
            {
                float angle   = GetAngleDeg(t, teamCount);
                float rad     = angle * Mathf.Deg2Rad;
                float bx      = cx + Mathf.Sin(rad) * _circleRadius;
                float by      = cy - Mathf.Cos(rad) * _circleRadius;  // Y invertida en GUI

                Color teamColor = GetTeamColor(gm, t);

                bool isHovered   = Vector2.Distance(mousePos, new Vector2(bx, by)) < _buttonRadius * _hoverScale;
                bool isCurrent   = (_assigner != null && _assigner.AssignedTeam == t);
                float drawRadius = isHovered ? _buttonRadius * _hoverScale : _buttonRadius;
                if (isCurrent) drawRadius *= 1.1f;

                if (isHovered) _hoveredTeam = t;

                // Dibujar círculo del equipo
                DrawFilledCircle(new Vector2(bx, by), drawRadius, teamColor,
                    isCurrent ? 4f : 2f, isHovered ? Color.white : new Color(1f, 1f, 1f, 0.5f));

                // Label con ID del equipo
                var labelStyle = new GUIStyle(_teamLabelStyle);
                labelStyle.normal.textColor = teamColor.GetLuminance() > 0.5f ? Color.black : Color.white;
                if (isHovered) labelStyle.fontStyle = FontStyle.Bold;

                float labelW = drawRadius * 2f;
                GUI.Label(new Rect(bx - labelW * 0.5f, by - 14f, labelW, 28f),
                    $"T{t}", labelStyle);

                // Nombre del equipo debajo
                var nameStyle = new GUIStyle(_hintStyle) { alignment = TextAnchor.UpperCenter };
                GUI.Label(new Rect(bx - 50f, by + drawRadius + 4f, 100f, 20f),
                    GetTeamName(gm, t), nameStyle);

                // Score debajo del nombre
                int score = gm?.Context?.Score?.GetTeamScore(t) ?? 0;
                GUI.Label(new Rect(bx - 30f, by + drawRadius + 22f, 60f, 18f),
                    $"{score}pts", nameStyle);
            }

            // ── Línea central (equipo actual) ─────────────────

            if (_assigner?.AssignedTeam >= 0)
            {
                string current = $"Equipo actual: T{_assigner.AssignedTeam}";
                GUI.Label(new Rect(cx - 100f, cy - 12f, 200f, 24f), current, _hintStyle);
            }

            // ── Click para cambiar de equipo ──────────────────

            if (Event.current.type == EventType.MouseDown
                && Event.current.button == 0
                && _hoveredTeam >= 0)
            {
                RequestTeamChange(_hoveredTeam);
            }
        }

        // ── Team change ───────────────────────────────────────

        private void RequestTeamChange(int teamID)
        {
            if (_assigner == null)
            {
                Core.Debug.CoreLogger.LogError("[GMFTeamSelectUI] PlayerTeamAssigner no encontrado.");
                return;
            }

            _assigner.ChangeTeam(teamID);
            _isOpen = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        // ── Helpers ───────────────────────────────────────────

        /// <summary>
        /// Distribución circular: ángulo en grados para el equipo t de teamCount.
        /// Empieza arriba (0°) y va en sentido horario.
        /// Con 2 equipos: 0° y 180°. Con 4: 0°, 90°, 180°, 270°. Etc.
        /// </summary>
        private float GetAngleDeg(int team, int total)
        {
            if (total == 1) return 0f;
            return (360f / total) * team;
        }

        private Color GetTeamColor(GameModeBase gm, int teamID)
        {
            // Acceso interno al TeamConfig via cast
            var defField = gm?.GetType()
                .GetField("_def", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (defField?.GetValue(gm) is GMF.Config.GMF_Config def
                && def.TeamConfig?.TeamColors != null
                && teamID < def.TeamConfig.TeamColors.Length)
            {
                return def.TeamConfig.TeamColors[teamID];
            }

            // Fallback: rojo, azul, verde, amarillo...
            Color[] fallback = { Color.red, Color.blue, Color.green, Color.yellow,
                                 Color.cyan, Color.magenta, Color.white, Color.gray };
            return teamID < fallback.Length ? fallback[teamID] : Color.white;
        }

        private string GetTeamName(GameModeBase gm, int teamID)
        {
            var defField = gm?.GetType()
                .GetField("_def", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (defField?.GetValue(gm) is GMF.Config.GMF_Config def
                && def.TeamConfig?.TeamNames != null
                && teamID < def.TeamConfig.TeamNames.Length)
            {
                return def.TeamConfig.TeamNames[teamID];
            }
            return $"Team {teamID}";
        }

        // ── Draw helpers ──────────────────────────────────────

        private void DrawFilledCircle(Vector2 center, float radius, Color fillColor,
            float borderWidth, Color borderColor)
        {
            // Relleno
            GUI.color = fillColor;
            GUI.DrawTexture(
                new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f),
                GetCircleTexture(), ScaleMode.StretchToFill, true);

            // Borde
            if (borderWidth > 0f)
            {
                GUI.color = borderColor;
                float br = radius + borderWidth;
                GUI.DrawTexture(
                    new Rect(center.x - br, center.y - br, br * 2f, br * 2f),
                    GetCircleTexture(), ScaleMode.StretchToFill, true);
                // Rellenar de nuevo sobre el borde
                GUI.color = fillColor;
                GUI.DrawTexture(
                    new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f),
                    GetCircleTexture(), ScaleMode.StretchToFill, true);
            }

            GUI.color = Color.white;
        }

        private static Texture2D _circleTexture;
        private static Texture2D GetCircleTexture()
        {
            if (_circleTexture != null) return _circleTexture;

            int size = 128;
            _circleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = size * 0.5f;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                float a    = Mathf.Clamp01(1f - Mathf.Max(0f, dist - c + 1f));
                _circleTexture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }

            _circleTexture.Apply();
            return _circleTexture;
        }

        // ── Styles ────────────────────────────────────────────

        private void InitStyles()
        {
            if (_stylesReady) return;

            _teamLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                normal    = { textColor = Color.white }
            };

            _hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 12,
                alignment = TextAnchor.UpperCenter,
                normal    = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            };

            _stylesReady = true;
        }
    }

    // ── Extension: luminance ──────────────────────────────────

    public static class ColorExtensions
    {
        public static float GetLuminance(this Color c)
            => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
    }
}