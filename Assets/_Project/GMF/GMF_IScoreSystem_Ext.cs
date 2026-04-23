// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_IScoreSystem_Ext.cs                        ║
// ║  (añadir a GMF_Interfaces.cs o compilar como archivo     ║
// ║   separado — mismo namespace GMF)                        ║
// ║                                                          ║
// ║  Extiende IScoreSystem con métodos de kills.             ║
// ╚══════════════════════════════════════════════════════════╝

namespace GMF
{
    /// <summary>
    /// Extiende la interfaz de solo lectura con consulta de kills.
    /// IScoreSystem original en GMF_Interfaces.cs se mantiene.
    /// </summary>
    public interface IScoreSystemEx : IScoreSystem
    {
        int GetTeamKills(int teamID);
        int GetPlayerKills(int playerID);
        int GetLeadingTeamByKills(int teamCount);
    }
}