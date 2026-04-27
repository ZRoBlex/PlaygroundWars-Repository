// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: CharacterManager.cs                            ║
// ║  CARPETA: Assets/_Project/CharacterSystem/Runtime/       ║
// ║                                                          ║
// ║  RESPONSABILIDAD:                                        ║
// ║    Escucha OnCharacterSelectedEvt y aplica el personaje  ║
// ║    al jugador: stats, prefab visual, habilidades.        ║
// ║                                                          ║
// ║  AÑADIR: al prefab del jugador                           ║
// ╚══════════════════════════════════════════════════════════╝

using CharacterSystem.Data;
using Core.Debug;
using Core.Events;
using Player.Authority;
using Player.Health;
using Player.Movement;
using UnityEngine;

namespace CharacterSystem.Runtime
{
    [RequireComponent(typeof(PlayerAuthority))]
    [DisallowMultipleComponent]
    public class CharacterManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Personaje inicial (opcional)")]
        [Tooltip("Si se asigna, se aplica al iniciar sin esperar selección.")]
        [SerializeField] private CharacterData _startCharacter;

        [Header("Visual Attachment")]
        [Tooltip("Transform donde se instancia el prefab visual del personaje.")]
        [SerializeField] private Transform _visualRoot;

        // ── Referencias ───────────────────────────────────────

        private PlayerAuthority _authority;
        private PlayerMovement_Fixed _movement;
        private PlayerHealth         _health;

        // Referencia al AbilityManager si el sistema de habilidades está activo
        // Se obtiene dinámicamente para no crear dependencia directa
        private MonoBehaviour _abilityManager;

        // ── Estado ────────────────────────────────────────────

        public CharacterData     ActiveCharacter { get; private set; }
        public CharacterStats    Stats           { get; private set; }
        public CharacterSkinData ActiveSkin      { get; private set; }

        private GameObject _currentVisual;

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            _authority = GetComponent<PlayerAuthority>();
            _movement  = GetComponent<PlayerMovement_Fixed>();
            _health    = GetComponent<PlayerHealth>();

            // AbilityManager — referencia opcional
            _abilityManager = GetComponent("AbilityManager") as MonoBehaviour;

            Stats = new CharacterStats(_authority.PlayerID, this);

            if (_visualRoot == null) _visualRoot = transform;
        }

        private void Start()
        {
            if (_startCharacter != null)
                ApplyCharacter(_startCharacter);
        }

        private void OnEnable()
        {
            EventBus<OnCharacterSelectedEvt>.Subscribe(OnCharacterSelected);
            EventBus<OnSkinChangedEvt>.Subscribe(OnSkinChanged);
            EventBus<OnStatsChangedEvt>.Subscribe(OnStatsChanged);
        }

        private void OnDisable()
        {
            EventBus<OnCharacterSelectedEvt>.Unsubscribe(OnCharacterSelected);
            EventBus<OnSkinChangedEvt>.Unsubscribe(OnSkinChanged);
            EventBus<OnStatsChangedEvt>.Unsubscribe(OnStatsChanged);
        }

        // ── Callbacks ─────────────────────────────────────────

        private void OnCharacterSelected(OnCharacterSelectedEvt e)
        {
            if (e.PlayerID != _authority.PlayerID) return;

            var data = CharacterSelectionSystem.FindCharacterByID(e.CharacterID);
            if (data == null)
            {
                CoreLogger.LogWarning(
                    $"[CharacterManager] P{_authority.PlayerID}: " +
                    $"CharacterData '{e.CharacterID}' no encontrado.");
                return;
            }

            ApplyCharacter(data);
        }

        private void OnSkinChanged(OnSkinChangedEvt e)
        {
            if (e.PlayerID != _authority.PlayerID) return;
            if (ActiveCharacter == null) return;

            var skin = ActiveCharacter.AvailableSkins
                .Find(s => s != null && s.SkinID == e.SkinID);

            if (skin == null && ActiveCharacter.DefaultSkin?.SkinID == e.SkinID)
                skin = ActiveCharacter.DefaultSkin;

            if (skin != null)
                ApplySkin(skin);
        }

        private void OnStatsChanged(OnStatsChangedEvt e)
        {
            if (e.OwnerID != _authority.PlayerID) return;
            SyncStatsToSystems();
        }

        // ── Aplicar personaje ─────────────────────────────────

        public void ApplyCharacter(CharacterData data)
        {
            if (data == null) return;

            string prevID = ActiveCharacter?.CharacterID ?? "";
            ActiveCharacter = data;

            // 1. Inicializar stats
            Stats.Initialize(data);

            // 2. Sincronizar con sistemas del jugador
            SyncStatsToSystems();

            // 3. Aplicar visual
            ApplyVisual(data.DefaultSkin ?? CreateDefaultSkin(data));

            // 4. Aplicar habilidades
            ApplyAbilities(data);

            CoreLogger.LogSystem("CharacterManager",
                $"P{_authority.PlayerID}: personaje '{data.CharacterID}' aplicado.");

            EventBus<OnCharacterInitializedEvt>.Raise(new OnCharacterInitializedEvt
            {
                PlayerID    = _authority.PlayerID,
                CharacterID = data.CharacterID
            });
        }

        // ── Sincronizar stats → sistemas ──────────────────────

        private void SyncStatsToSystems()
        {
            if (ActiveCharacter == null) return;

            // HP máximo
            float hp = Stats.Get("hp");
            if (hp > 0f && _health != null)
                _health.SetMaxHealth(hp);

            // Velocidad
            float speed = Stats.Get("speed");
            if (speed > 0f && _movement != null)
            {
                // Multiplicador relativo al valor base del PlayerConfig
                // Aquí usamos SetSpeedMultiplier como factor
                float basePCSpeed = 5f; // valor base del PlayerConfig (ajustar)
                _movement.SetSpeedMultiplier(speed / basePCSpeed);
            }
        }

        // ── Visual ────────────────────────────────────────────

        private void ApplyVisual(CharacterSkinData skin)
        {
            if (_currentVisual != null)
                Destroy(_currentVisual);

            ActiveSkin = skin;
            if (skin == null) return;

            GameObject prefab = skin.SkinPrefab ?? ActiveCharacter?.CharacterPrefab;
            if (prefab == null) return;

            Vector3 offset = ActiveCharacter?.PrefabOffset ?? Vector3.zero;
            Quaternion rot = Quaternion.Euler(ActiveCharacter?.PrefabRotation ?? Vector3.zero);

            _currentVisual = Instantiate(prefab, _visualRoot);
            _currentVisual.transform.localPosition = offset;
            _currentVisual.transform.localRotation = rot;

            // Aplicar materiales de la skin si los tiene
            if (skin.Materials?.Length > 0)
            {
                var renderers = _currentVisual.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < Mathf.Min(skin.Materials.Length, mats.Length); i++)
                        if (skin.Materials[i] != null) mats[i] = skin.Materials[i];
                    r.sharedMaterials = mats;
                }
            }
        }

        private void ApplySkin(CharacterSkinData skin) => ApplyVisual(skin);

        private CharacterSkinData CreateDefaultSkin(CharacterData data)
        {
            if (data.CharacterPrefab == null) return null;
            var skin = ScriptableObject.CreateInstance<CharacterSkinData>();
            skin.SkinID     = "default";
            skin.SkinPrefab = data.CharacterPrefab;
            return skin;
        }

        // ── Habilidades ───────────────────────────────────────

        private void ApplyAbilities(CharacterData data)
        {
            if (_abilityManager == null) return;
            if (data.AbilityDefinitions == null || data.AbilityDefinitions.Length == 0) return;

            // Invocar AddAbility en el AbilityManager via reflexión
            // Esto evita dependencia directa entre CharacterSystem y ABF
            var method = _abilityManager.GetType().GetMethod("AddAbility");
            if (method == null)
            {
                CoreLogger.LogWarning("[CharacterManager] AbilityManager.AddAbility() no encontrado.");
                return;
            }

            foreach (var abilityDef in data.AbilityDefinitions)
                if (abilityDef != null)
                    method.Invoke(_abilityManager, new object[] { abilityDef });
        }

        // ── API Pública ───────────────────────────────────────

        /// <summary>Aplica un modificador de stat desde fuentes externas (habilidades, items).</summary>
        public void AddStatModifier(StatModifier mod) => Stats.AddModifier(mod);

        /// <summary>Remueve un modificador por ID.</summary>
        public void RemoveStatModifier(string modID) => Stats.RemoveModifier(modID);

        /// <summary>Obtiene el valor calculado de una stat.</summary>
        public float GetStat(string statID) => Stats.Get(statID);
    }
}