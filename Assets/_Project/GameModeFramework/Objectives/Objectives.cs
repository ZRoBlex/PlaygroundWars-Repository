// ============================================================
//  Objectives.cs
//  GameModeFramework/Objectives/Objectives.cs
//
//  OBJETIVOS REUTILIZABLES DEL FRAMEWORK.
//  No conocen las reglas. Solo emiten eventos de interacción.
//
//  CONTENIDO:
//  • ObjectiveBase  — base MonoBehaviour para todos los objetivos
//  • Flag           — bandera que se puede recoger/soltar/capturar
//  • CaptureZone    — zona de captura (trigger)
//  • ControlPoint   — punto de control (KOTH) — estructura base
// ============================================================

using GameMode.Framework.Config;
using GameMode.Framework.Events;
using Core.Events;
using Player.Authority;
using UnityEngine;

namespace GameMode.Framework.Objectives
{
    // ════════════════════════════════════════════════════════
    //  OBJECTIVE BASE
    // ════════════════════════════════════════════════════════

    // public abstract class ObjectiveBase : MonoBehaviour, IObjective
    // {
    //     [Header("Configuración")]
    //     [SerializeField] protected string _objectiveID  = "objective";
    //     [SerializeField] protected int    _ownerTeamID  = -1;
    //     [SerializeField] protected bool   _startsActive = true;

    //     public string ObjectiveID  => _objectiveID;
    //     public int    OwnerTeamID  => _ownerTeamID;
    //     public bool   IsActive     { get; protected set; }
    //     public string CurrentState { get; protected set; } = "Idle";

    //     protected virtual void Start()
    //     {
    //         IsActive = _startsActive;

    //         // Auto-registrar en GameModeBase si existe en escena
    //         var gm = FindFirstObjectByType<GameModeBase>();
    //         gm?.RegisterObjective(this);
    //     }

    //     public virtual void Initialize(ObjectiveConfig cfg)
    //     {
    //         if (cfg == null) return;
    //         _objectiveID = cfg.ObjectiveID;
    //         _ownerTeamID = cfg.OwnerTeamID;
    //         IsActive     = cfg.StartsActive;
    //     }

    //     public abstract void Reset();

    //     public void SetActive(bool active) => IsActive = active;

    //     protected void EmitInteraction(string type, int playerID, int playerTeamID)
    //     {
    //         EventBus<ObjectiveInteractedEvent>.Raise(new ObjectiveInteractedEvent 
    //         {
    //             ObjectiveID         = _objectiveID,
    //             InteractionType     = type,
    //             PlayerID            = playerID,
    //             PlayerTeamID        = playerTeamID,
    //             ObjectiveOwnerTeamID = _ownerTeamID,
    //             Position             = transform.position
    //         });
    //     }
    // }

    // ════════════════════════════════════════════════════════
    //  FLAG
    // ════════════════════════════════════════════════════════

    // public class Flag : ObjectiveBase
    // {
    //     [Header("Flag Settings")]
    //     [SerializeField] private float _autoReturnTime = 15f;

    //     [Header("Visuals")]
    //     [SerializeField] private GameObject _mesh;
    //     [SerializeField] private GameObject _baseIndicator;

    //     // Estado
    //     private Vector3    _basePos;
    //     private Quaternion _baseRot;
    //     private Transform  _carrier;
    //     private int        _carrierID   = -1;
    //     private float      _dropTimer;
    //     private bool       _isDropped;

    //     public int  CarrierID => _carrierID;
    //     public bool IsCarried => _carrier != null;

    //     protected override void Start()
    //     {
    //         _basePos = transform.position;
    //         _baseRot = transform.rotation;
    //         base.Start();
    //     }

    //     private void Update()
    //     {
    //         if (IsCarried && _carrier != null)
    //             transform.position = _carrier.position + Vector3.up * 1.6f;

    //         if (_isDropped)
    //         {
    //             _dropTimer += Time.deltaTime;
    //             if (_dropTimer >= _autoReturnTime)
    //                 ReturnToBase(-1);
    //         }
    //     }

    //     // ── Trigger ───────────────────────────────────────────

    //     private void OnTriggerEnter(Collider other)
    //     {
    //         if (!IsActive) return;

    //         var auth = other.GetComponentInParent<PlayerAuthority>();
    //         if (auth == null) return;

    //         // Bandera caída + es el equipo dueño → devolver
    //         // if (_isDropped && auth.GetComponent<CTF.Teams.TeamManager>() is var tm
    //         //                && tm?.GetTeamOf(auth.PlayerID) == _ownerTeamID)
    //         {
    //             EmitInteraction("Return", auth.PlayerID, _ownerTeamID);
    //             ReturnToBase(auth.PlayerID);
    //             return;
    //         }

    //         // Bandera en base + es equipo enemigo → recoger
    //         // if (!IsCarried && !_isDropped)
    //         // {
    //         //     int playerTeam = GetPlayerTeam(auth.PlayerID);
    //         //     if (playerTeam != _ownerTeamID && playerTeam >= 0)
    //         //         PickUp(auth.PlayerID, auth.transform, playerTeam);
    //         // }
    //     }

    //     // ── Acciones ──────────────────────────────────────────

    //     public void PickUp(int playerID, Transform carrier, int playerTeamID)
    //     {
    //         _carrier   = carrier;
    //         _carrierID = playerID;
    //         _isDropped = false;
    //         _dropTimer = 0f;
    //         CurrentState = "Carried";

    //         EmitInteraction("Pickup", playerID, playerTeamID);
    //     }

    //     public void Drop(int playerID)
    //     {
    //         _carrier   = null;
    //         _carrierID = -1;
    //         _isDropped = true;
    //         _dropTimer = 0f;
    //         CurrentState = "Dropped";

    //         // EmitInteraction("Drop", playerID, GetPlayerTeam(playerID));
    //     }

    //     public void Capture(int playerID, int playerTeamID)
    //     {
    //         _carrier   = null;
    //         _carrierID = -1;
    //         CurrentState = "Captured";

    //         EmitInteraction("Capture", playerID, playerTeamID);
    //         ReturnToBase(-1);
    //     }

    //     public void ReturnToBase(int returnedByID)
    //     {
    //         _carrier   = null;
    //         _carrierID = -1;
    //         _isDropped = false;
    //         _dropTimer = 0f;
    //         transform.SetPositionAndRotation(_basePos, _baseRot);
    //         CurrentState = "Idle";

    //         if (returnedByID >= 0)
    //             EmitInteraction("Return", returnedByID, _ownerTeamID);

    //         UpdateVisuals();
    //     }

    //     public override void Reset() => ReturnToBase(-1);

    //     private void UpdateVisuals()
    //     {
    //         if (_mesh != null)           _mesh.SetActive(!IsCarried);
    //         if (_baseIndicator != null)  _baseIndicator.SetActive(CurrentState == "Idle");
    //     }

    //     // private int GetPlayerTeam(int pid)
    //     // {
    //     //     var tm = FindFirstObjectByType<CTF.Teams.TeamManager>();
    //     //     return tm?.GetTeamOf(pid) ?? -1;
    //     // }
    // }

    // ════════════════════════════════════════════════════════
    //  CAPTURE ZONE
    // ════════════════════════════════════════════════════════

    // [RequireComponent(typeof(Collider))]
    // public class CaptureZone : ObjectiveBase
    // {
    //     protected override void Start()
    //     {
    //         GetComponent<Collider>().isTrigger = true;
    //         base.Start();
    //     }

    //     private void OnTriggerEnter(Collider other)
    //     {
    //         if (!IsActive) return;

    //         var auth = other.GetComponentInParent<PlayerAuthority>();
    //         if (auth == null) return;

    //         // Publicar interacción — las reglas deciden si es captura válida
    //         // (ej: FlagCaptureRule valida que el jugador lleve bandera enemiga
    //         //  y está en su propia zona)
    //         // var carrier = other.GetComponentInParent<CTF.Flag.FlagCarrierComponent>();

    //         // EmitInteraction(
    //         //     carrier?.IsCarrying == true ? "Capture" : "Enter",
    //         //     auth.PlayerID,
    //         //     GetPlayerTeam(auth.PlayerID)
    //         // );
    //     }

    //     public override void Reset() { CurrentState = "Idle"; }

    //     // private int GetPlayerTeam(int pid)
    //     // {
    //     //     var tm = FindFirstObjectByType<CTF.Teams.TeamManager>();
    //     //     return tm?.GetTeamOf(pid) ?? -1;
    //     // }

    //     private void OnDrawGizmosSelected()
    //     {
    //         Gizmos.color = _ownerTeamID == 0
    //             ? new Color(1f, 0.2f, 0.2f, 0.3f)
    //             : new Color(0.2f, 0.2f, 1f, 0.3f);
    //         Gizmos.DrawSphere(transform.position, 2f);
    //     }
    // }

    // ════════════════════════════════════════════════════════
    //  CONTROL POINT (estructura base para KOTH)
    // ════════════════════════════════════════════════════════

    // [RequireComponent(typeof(Collider))]
    // public class ControlPoint : ObjectiveBase
    // {
    //     [Header("Control Point")]
    //     [SerializeField] private float _captureTime   = 5f;   // Segundos para capturar
    //     [SerializeField] private float _scoreInterval = 1f;   // Puntos cada N segundos

    //     private int   _contestingTeam  = -1;
    //     private float _captureProgress = 0f;
    //     private float _scoreAccumulator = 0f;
    //     private int   _occupantCount   = 0;

    //     private readonly System.Collections.Generic.HashSet<int> _occupants = new();

    //     protected override void Start()
    //     {
    //         GetComponent<Collider>().isTrigger = true;
    //         base.Start();
    //     }

    //     private void OnTriggerEnter(Collider other)
    //     {
    //         var auth = other.GetComponentInParent<PlayerAuthority>();
    //         if (auth == null || !_occupants.Add(auth.PlayerID)) return;

    //         _occupantCount = _occupants.Count;
    //         // EmitInteraction("Enter", auth.PlayerID, GetPlayerTeam(auth.PlayerID));
    //     }

    //     private void OnTriggerExit(Collider other)
    //     {
    //         var auth = other.GetComponentInParent<PlayerAuthority>();
    //         if (auth == null || !_occupants.Remove(auth.PlayerID)) return;

    //         _occupantCount = _occupants.Count;
    //         // EmitInteraction("Exit", auth.PlayerID, GetPlayerTeam(auth.PlayerID));
    //     }

    //     private void Update()
    //     {
    //         if (_occupantCount == 0) return;

    //         // Determinar si hay un equipo controlando (y no está contested)
    //         // En producción: verificar que TODOS los ocupantes son del mismo equipo

    //         _scoreAccumulator += Time.deltaTime;
    //         if (_scoreAccumulator >= _scoreInterval)
    //         {
    //             _scoreAccumulator = 0f;
    //             if (_ownerTeamID >= 0)
    //                 EmitInteraction("Tick", -1, _ownerTeamID);
    //         }
    //     }

    //     public override void Reset()
    //     {
    //         _captureProgress  = 0f;
    //         _scoreAccumulator = 0f;
    //         _ownerTeamID      = -1;
    //         _occupants.Clear();
    //         CurrentState = "Idle";
    //     }

    //     // private int GetPlayerTeam(int pid)
    //     // {
    //     //     var tm = FindFirstObjectByType<CTF.Teams.TeamManager>();
    //     //     return tm?.GetTeamOf(pid) ?? -1;
    //     // }
    // }
}
