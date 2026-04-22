// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_Flag.cs  (REEMPLAZA el anterior)           ║
// ║                                                          ║
// ║  CAMBIOS:                                                ║
// ║    + El jugador puede soltar la bandera con "G" (config) ║
// ║      FlagDropInput.cs se añade al prefab del jugador     ║
// ║    + Método público DropByPlayer() para input externo    ║
// ╚══════════════════════════════════════════════════════════╝

using System.Collections;
using Player.Authority;
using UnityEngine;

namespace GMF
{
    // ════════════════════════════════════════════════════════
    //  FLAG DROP INPUT
    //  ► Añadir al prefab del jugador junto con FlagCarrierBridge
    //  ► Permite al jugador soltar la bandera con una tecla
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// ⚠️ SEPARAR: GMF_FlagDropInput.cs (se añade al prefab del jugador)
    /// </summary>
    [RequireComponent(typeof(PlayerAuthority))]
    public class FlagDropInput : MonoBehaviour
    {
        [Header("Tecla para soltar bandera")]
        [SerializeField] private KeyCode _dropKey = KeyCode.G;

        private PlayerAuthority _authority;

        private void Awake() => _authority = GetComponent<PlayerAuthority>();

        private void Update()
        {
            if (!_authority.IsLocalPlayer) return;
            if (!Input.GetKeyDown(_dropKey)) return;

            // Buscar en el registro qué bandera porta este jugador
            var gm = GameModeBase.Instance;
            if (gm?.Context?.Objectives == null) return;

            foreach (var obj in gm.Context.Objectives.GetAll())
            {
                if (obj is Flag flag && flag.IsBeingCarried && flag.CarrierID == _authority.PlayerID)
                {
                    flag.DropByPlayer(_authority.PlayerID);
                    break;
                }
            }
        }
    }
}