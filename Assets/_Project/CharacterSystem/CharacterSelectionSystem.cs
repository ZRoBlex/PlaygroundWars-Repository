// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: CharacterSelectionSystem.cs                    ║
// ║  CARPETA: Assets/_Project/CharacterSystem/Selection/     ║
// ║                                                          ║
// ║  RESPONSABILIDAD:                                        ║
// ║    • Registrar personajes disponibles                    ║
// ║    • Select(playerID, charID) → emite evento             ║
// ║    • Persistencia opcional via PlayerPrefs               ║
// ║    • FindCharacterByID (static, para CharacterManager)   ║
// ╚══════════════════════════════════════════════════════════╝

using System.Collections.Generic;
using CharacterSystem.Data;
using Core.Debug;
using Core.Events;
using UnityEngine;

namespace CharacterSystem
{
    [DisallowMultipleComponent]
    public class CharacterSelectionSystem : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Personajes disponibles")]
        [SerializeField] private List<CharacterData> _characters = new();

        [Header("Configuración")]
        [SerializeField] private bool _persistSelection = true;
        [SerializeField] private string _defaultCharacterID = "";

        // ── Singleton de escena ───────────────────────────────

        public static CharacterSelectionSystem Instance { get; private set; }

        // Registro estático para acceso sin referencia directa
        private static readonly Dictionary<string, CharacterData> _registry = new();

        // ── Estado ────────────────────────────────────────────

        // playerID → characterID seleccionado
        private readonly Dictionary<int, string> _selections = new();

        public IReadOnlyList<CharacterData> AvailableCharacters => _characters;

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            RebuildRegistry();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Registro ──────────────────────────────────────────

        private void RebuildRegistry()
        {
            _registry.Clear();
            foreach (var c in _characters)
                if (c != null && !string.IsNullOrEmpty(c.CharacterID))
                    _registry[c.CharacterID] = c;

            CoreLogger.LogSystem("CharacterSelection",
                $"{_registry.Count} personaje(s) registrado(s).");
        }

        /// <summary>Acceso estático para CharacterManager y UI.</summary>
        public static CharacterData FindCharacterByID(string id)
        {
            _registry.TryGetValue(id, out var c);
            return c;
        }

        // ── Selección ─────────────────────────────────────────

        /// <summary>Selecciona un personaje para un jugador.</summary>
        public bool Select(int playerID, string characterID)
        {
            if (!_registry.ContainsKey(characterID))
            {
                CoreLogger.LogWarning(
                    $"[CharacterSelection] '{characterID}' no registrado.");
                return false;
            }

            string prevID = _selections.TryGetValue(playerID, out string prev) ? prev : "";

            if (prevID == characterID) return true; // ya seleccionado

            _selections[playerID] = characterID;

            if (_persistSelection)
                PlayerPrefs.SetString($"char_p{playerID}", characterID);

            CoreLogger.LogSystem("CharacterSelection",
                $"P{playerID}: '{prevID}' → '{characterID}'");

            EventBus<OnCharacterSelectedEvt>.Raise(new OnCharacterSelectedEvt
            {
                PlayerID              = playerID,
                CharacterID           = characterID,
                PreviousCharacterID   = prevID
            });

            return true;
        }

        /// <summary>Selecciona un personaje por índice en la lista.</summary>
        public bool SelectByIndex(int playerID, int index)
        {
            if (index < 0 || index >= _characters.Count) return false;
            var c = _characters[index];
            return c != null && Select(playerID, c.CharacterID);
        }

        /// <summary>Carga la selección persistida de un jugador.</summary>
        public void RestoreSelection(int playerID)
        {
            if (!_persistSelection) return;

            string saved = PlayerPrefs.GetString($"char_p{playerID}", "");
            if (!string.IsNullOrEmpty(saved) && _registry.ContainsKey(saved))
                Select(playerID, saved);
            else if (!string.IsNullOrEmpty(_defaultCharacterID))
                Select(playerID, _defaultCharacterID);
            else if (_characters.Count > 0 && _characters[0] != null)
                Select(playerID, _characters[0].CharacterID);
        }

        /// <summary>Obtiene el CharacterData actualmente seleccionado por un jugador.</summary>
        public CharacterData GetSelected(int playerID)
        {
            if (!_selections.TryGetValue(playerID, out string id)) return null;
            return FindCharacterByID(id);
        }

        public string GetSelectedID(int playerID)
            => _selections.TryGetValue(playerID, out string id) ? id : "";

        /// <summary>Añade un personaje al registro en runtime.</summary>
        public void Register(CharacterData data)
        {
            if (data == null) return;
            _characters.Add(data);
            _registry[data.CharacterID] = data;
        }
    }
}