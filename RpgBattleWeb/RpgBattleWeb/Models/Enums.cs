namespace RpgBattleWeb.Models;

public enum Difficulty
{
    Easy,
    Normal,
    Hard
}

public enum GamePhase
{
    MainMenu,
    BuildCharacter,
    DifficultySelect,
    Intro,
    Battle,
    Victory,
    Defeat,
    Fled
}

public enum LogKind
{
    Info,
    PlayerAttack,
    PlayerHeal,
    PlayerSkill,
    EnemyAttack,
    Poison,
    Guard,
    Critical,
    System
}

public record LogEntry(string Message, LogKind Kind);
