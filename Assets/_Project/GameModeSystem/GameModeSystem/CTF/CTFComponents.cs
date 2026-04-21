// ============================================================
//  FlagBase.cs
//  GameMode/CTF/FlagBase.cs
//
//  RESPONSABILIDAD ÚNICA: Representar la base de una bandera.
//  Contiene la posición de spawn y la FlagController asociada.
// ============================================================

using GameMode.Config;
using Core.Debug;
using UnityEngine;

namespace GameMode.CTF
{
    [DisallowMultipleComponent]
    public class FlagBase : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] private CTFConfig      _config;
        [SerializeField] private int            _teamID = 0;
        [SerializeField] private FlagController _flag;
        [SerializeField] private Renderer       _baseRenderer;

        public int            TeamID => _teamID;
        public FlagController Flag   => _flag;

        private void Start()
        {
            if (_flag == null)
                _flag = GetComponentInChildren<FlagController>();

            // Colorear la base con el color del equipo
            if (_baseRenderer != null && _config != null)
            {
                var col  = _teamID == 0 ? _config.TeamAColor : _config.TeamBColor;
                var mat  = new Material(_baseRenderer.material);
                col.a    = 0.5f;
                mat.color = col;
                _baseRenderer.material = mat;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_config == null) return;
            Gizmos.color = _teamID == 0 ? new Color(1, 0.2f, 0.2f, 0.3f) : new Color(0.2f, 0.2f, 1f, 0.3f);
            Gizmos.DrawSphere(transform.position, 0.8f);
        }
    }
}