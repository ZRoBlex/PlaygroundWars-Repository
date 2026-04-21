// ============================================================
//  CTFConfig.cs
//  GameMode/Config/CTFConfig.cs
//
//  ScriptableObject central del modo CTF.
//  CREAR: Assets → Right Click → Create → GameMode → CTFConfig
// ============================================================

using UnityEngine;

namespace GameMode.Config
{
    [CreateAssetMenu(fileName = "CTFConfig", menuName = "GameMode/CTFConfig", order = 0)]
    public class CTFConfig : ScriptableObject
    {
        [Header("Identificación")]
        public string GameModeID   = "CTF";
        public string DisplayName  = "Capture the Flag";

        [Header("Partida")]
        [Range(1, 10)]  public int   ScoreToWin       = 3;
        [Range(1, 5)]   public int   RoundsToWin      = 2;
        [Range(0, 600)] public float RoundDuration    = 300f;  // 0 = sin límite de tiempo
        [Range(0, 30)]  public float WarmUpDuration   = 5f;
        [Range(2, 15)]  public float RoundEndDuration = 5f;

        [Header("Bandera")]
        [Range(5f, 60f)]  public float FlagAutoReturnTime  = 15f;
        [Range(0.5f, 3f)] public float FlagPickupRadius    = 1.2f;
        [Range(0.5f, 5f)] public float CaptureZoneRadius   = 2f;
        [Range(0f, 1f)]   public float CarrierSpeedPenalty = 0.2f;

        [Header("Respawn")]
        [Range(1f, 15f)] public float RespawnDelay = 4f;

        [Header("Equipos")]
        public Color TeamAColor = Color.red;
        public Color TeamBColor = Color.blue;

        [Header("Red")]
        public bool UseNetworking = false;
    }
}
