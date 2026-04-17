// ============================================================
//  GameState.cs
//  Core/GameState/GameState.cs
//
//  Enum centralizado de estados del juego.
//  Agrega nuevos estados aquí sin tocar ningún otro sistema.
// ============================================================

namespace Core
{
    public enum GameState
    {
        None         = 0,   // Estado nulo / sin inicializar
        Initializing = 1,   // El Bootstrapper está cargando sistemas
        MainMenu     = 2,   // Menú principal activo
        Lobby        = 3,   // Sala de espera / selección de personaje
        Playing      = 4,   // Partida en curso
        Paused       = 5,   // Juego pausado (overlay de pausa)
        GameOver     = 6    // Fin de partida (pantalla de resultados)
    }
}
