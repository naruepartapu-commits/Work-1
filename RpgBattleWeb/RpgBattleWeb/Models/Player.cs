namespace RpgBattleWeb.Models;

public class Player
{
    public const int BaseMaxHp = 100;
    public const int MaxDef = 10;
    public const int MaxAtk = 5;

    public int Hp { get; set; } = BaseMaxHp;
    public int MaxHp { get; set; } = BaseMaxHp;

    /// <summary>Reduces enemy damage while guarding. Adjustable in "Build Character".</summary>
    public int Def { get; set; } = 2;

    /// <summary>Adds bonus damage to every attack. Adjustable in "Build Character".</summary>
    public int Atk { get; set; }

    /// <summary>Battle resource spent on attacks/skills, regained by guarding or getting hit.</summary>
    public int Cost { get; set; }

    /// <summary>Persists for the whole browser session, like the console version's session counter.</summary>
    public int Wins { get; set; }
}
