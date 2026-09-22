using RpgBattleWeb.Models;

namespace RpgBattleWeb.Services;

/// <summary>
/// Scoped per browser session (Blazor Server circuit). Holds all game state that used
/// to live as loose local variables in the console app's giant while(true) loop, and
/// raises <see cref="OnChange"/> so the UI component re-renders after every action.
/// </summary>
public class GameEngine
{
    private readonly Random _rng = new();

    private bool _enemyStunned;
    private bool _guardActiveNextEnemyTurn;
    private int _poisonTurnsRemaining;

    public Player Hero { get; } = new();
    public Enemy? Foe { get; private set; }
    public Difficulty SelectedDifficulty { get; private set; }
    public GamePhase Phase { get; private set; } = GamePhase.MainMenu;
    public List<LogEntry> Log { get; } = new();
    public int TotalDamageDealt { get; private set; }
    public string? ErrorMessage { get; private set; }

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    private void AddLog(string message, LogKind kind)
    {
        Log.Insert(0, new LogEntry(message, kind));
        if (Log.Count > 14) Log.RemoveAt(Log.Count - 1);
    }

    // ----- Menu navigation -----

    public void GoBuildCharacter()
    {
        Phase = GamePhase.BuildCharacter;
        Notify();
    }

    public void GoDifficultySelect()
    {
        Phase = GamePhase.DifficultySelect;
        Notify();
    }

    public void BackToMenu()
    {
        Phase = GamePhase.MainMenu;
        Notify();
    }

    public void ReturnToMenu()
    {
        Foe = null;
        Phase = GamePhase.MainMenu;
        Notify();
    }

    // ----- Build Character -----

    public void IncreaseDef()
    {
        if (Hero.Def < Player.MaxDef) Hero.Def++;
        Notify();
    }

    public void DecreaseDef()
    {
        if (Hero.Def > 0) Hero.Def--;
        Notify();
    }

    public void IncreaseAtk()
    {
        if (Hero.Atk < Player.MaxAtk) Hero.Atk++;
        Notify();
    }

    public void DecreaseAtk()
    {
        if (Hero.Atk > 0) Hero.Atk--;
        Notify();
    }

    // ----- Starting a battle -----

    public void SelectDifficulty(Difficulty difficulty)
    {
        SelectedDifficulty = difficulty;
        Foe = Enemy.Create(difficulty);
        Hero.Hp = Hero.MaxHp;
        Hero.Cost = 15;
        TotalDamageDealt = 0;
        ErrorMessage = null;
        _poisonTurnsRemaining = 0;
        _enemyStunned = false;
        _guardActiveNextEnemyTurn = false;
        Log.Clear();
        Phase = GamePhase.Intro;
        Notify();
    }

    public void ConfirmIntro()
    {
        if (Foe is null) return;
        Phase = GamePhase.Battle;
        AddLog($"{Foe.Name} เข้าสู่สนามรบด้วยพลังชีวิต {Foe.Hp} หน่วย", LogKind.System);
        Notify();
    }

    // ----- Battle actions -----

    public void PlayerAttack(int power)
    {
        if (Phase != GamePhase.Battle || Foe is null) return;

        if (power < 1 || power > 20)
        {
            ErrorMessage = "เลือกพลังโจมตีระหว่าง 1-20";
            Notify();
            return;
        }
        if (power > Hero.Cost)
        {
            ErrorMessage = "Cost ไม่พอสำหรับพลังโจมตีนี้";
            Notify();
            return;
        }

        Hero.Cost -= power;
        int bonus = _rng.Next(1, 5) + Hero.Atk; // 1-4 + Atk stat
        bool crit = _rng.Next(1, 101) <= 15;     // 15% crit chance
        if (crit) bonus *= 2;
        int total = power + bonus;

        Foe.Hp -= total;
        TotalDamageDealt += total;
        ErrorMessage = null;

        AddLog(
            crit ? $"คุณโจมตีคริติคอล! สร้างความเสียหาย {total}" : $"คุณโจมตี สร้างความเสียหาย {total}",
            crit ? LogKind.Critical : LogKind.PlayerAttack);

        ResolveAfterPlayerAction();
    }

    public void UseSkill(string skillId)
    {
        if (Phase != GamePhase.Battle || Foe is null) return;

        switch (skillId)
        {
            case "fireball":
                if (!SpendCost(10)) return;
                Foe.Hp -= 13;
                TotalDamageDealt += 13;
                AddLog("คุณร่าย Fireball สร้างความเสียหาย 13", LogKind.PlayerSkill);
                break;

            case "heal":
                if (!SpendCost(13)) return;
                int healed = Math.Min(9, Hero.MaxHp - Hero.Hp);
                Hero.Hp += healed;
                AddLog(healed > 0
                    ? $"คุณร่าย Heal ฟื้นฟู {healed} HP"
                    : "HP เต็มอยู่แล้ว เสีย Cost ไปเปล่า ๆ",
                    LogKind.PlayerHeal);
                break;

            case "magicball":
                if (!SpendCost(25)) return;
                Foe.Hp -= 25;
                TotalDamageDealt += 25;
                AddLog("คุณร่าย Magic Ball สร้างความเสียหาย 25", LogKind.PlayerSkill);
                break;

            case "ultimate":
                if (!SpendCost(40)) return;
                Foe.Hp -= 35;
                TotalDamageDealt += 35;
                _enemyStunned = true;
                AddLog("คุณร่าย Ultimate Fireball สร้างความเสียหาย 35 และสกัดตาศัตรูถัดไป!", LogKind.PlayerSkill);
                break;

            case "poison":
                if (!SpendCost(15)) return;
                Foe.Hp -= 5;
                TotalDamageDealt += 5;
                _poisonTurnsRemaining = 3;
                AddLog("คุณใช้ Poison Strike ศัตรูติดพิษ 3 เทิร์น", LogKind.PlayerSkill);
                break;

            case "unknownbook":
                if (!SpendCost(100)) return;
                int dealt = Math.Max(Foe.Hp, 0);
                Foe.Hp = 0;
                TotalDamageDealt += dealt;
                AddLog("คุณเปิดตำราต้องห้าม... แสงจ้าสลายศัตรูไปทั้งตัว", LogKind.Critical);
                break;

            default:
                return;
        }

        ErrorMessage = null;
        ResolveAfterPlayerAction();
    }

    private bool SpendCost(int amount)
    {
        if (Hero.Cost < amount)
        {
            ErrorMessage = $"Cost ไม่พอ (ต้องการ {amount})";
            Notify();
            return false;
        }
        Hero.Cost -= amount;
        return true;
    }

    public void Guard()
    {
        if (Phase != GamePhase.Battle) return;
        Hero.Cost += 7;
        _guardActiveNextEnemyTurn = true;
        ErrorMessage = null;
        AddLog("คุณตั้งการ์ด (+7 Cost)", LogKind.Guard);
        ResolveAfterPlayerAction(forceEnemyTurn: true);
    }

    public void Flee()
    {
        if (Phase != GamePhase.Battle) return;
        AddLog("คุณหลบหนีออกจากการต่อสู้...", LogKind.System);
        Phase = GamePhase.Fled;
        Notify();
    }

    /// <summary>
    /// Runs poison tick + the enemy's chance to act, then checks win/lose.
    /// Replaces the original console app's "trunEntity / skipTrun" bookkeeping,
    /// which had a bug where the Ultimate skill's turn-skip could bleed into
    /// the following round instead of reliably stunning the enemy once.
    /// </summary>
    private void ResolveAfterPlayerAction(bool forceEnemyTurn = false)
    {
        if (Foe is null) return;

        if (Foe.Hp <= 0)
        {
            WinBattle();
            return;
        }

        if (_poisonTurnsRemaining > 0)
        {
            Foe.Hp -= 5;
            _poisonTurnsRemaining--;
            AddLog($"{Foe.Name} โดนพิษกัดกิน -5 HP (เหลือพิษอีก {_poisonTurnsRemaining} เทิร์น)", LogKind.Poison);
            if (Foe.Hp <= 0)
            {
                WinBattle();
                return;
            }
        }

        // 1-in-3 chance the enemy gets a bonus strike before your next turn,
        // same tension as the original - unless it's stunned, or Guard forces it.
        bool enemyRolledIn = _rng.Next(1, 4) == 2;
        bool enemyActs = !_enemyStunned && (forceEnemyTurn || enemyRolledIn);

        if (enemyActs)
        {
            int roll = _rng.Next(Foe.BaseDamage, Foe.MaxDamage + 1);
            if (_guardActiveNextEnemyTurn) roll = Math.Max(0, roll - Hero.Def);
            Hero.Hp -= roll;
            Hero.Cost += 4;

            AddLog(_guardActiveNextEnemyTurn
                ? $"{Foe.Name} โจมตีขณะคุณตั้งการ์ด -{roll} HP (+4 Cost)"
                : $"{Foe.Name} โจมตีแทรกก่อนตาคุณ -{roll} HP (+4 Cost)",
                LogKind.EnemyAttack);

            if (Hero.Hp <= 0)
            {
                LoseBattle();
                return;
            }
        }
        else if (_enemyStunned)
        {
            AddLog($"{Foe.Name} ยังสั่นจากท่าไม้ตายก่อนหน้า ไม่สามารถโจมตีในเทิร์นนี้", LogKind.System);
        }

        _enemyStunned = false;
        _guardActiveNextEnemyTurn = false;
        Notify();
    }

    private void WinBattle()
    {
        Hero.Wins++;
        Phase = GamePhase.Victory;
        AddLog($"คุณกำจัด {Foe!.Name} ได้สำเร็จ!", LogKind.System);
        Notify();
    }

    private void LoseBattle()
    {
        Phase = GamePhase.Defeat;
        AddLog("คุณสิ้นสติลงกับพื้น...", LogKind.System);
        Notify();
    }
}
