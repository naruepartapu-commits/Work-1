namespace RpgBattleWeb.Models;

public class Enemy
{
    public required string Name { get; init; }
    public required string Intro { get; init; }
    public int MaxHp { get; init; }
    public int Hp { get; set; }

    /// <summary>Floor of the enemy's attack roll.</summary>
    public int BaseDamage { get; init; }

    /// <summary>Ceiling of the enemy's attack roll (was an unused variable in the original code - now wired in).</summary>
    public int MaxDamage { get; init; }

    public static Enemy Create(Difficulty difficulty) => difficulty switch
    {
        Difficulty.Easy => new Enemy
        {
            Name = "สไลม์",
            Intro = "ก้อนวุ้นสีเขียวกระเพื่อมเข้ามาช้า ๆ ดูไม่มีพิษภัย... น่าจะนะ",
            MaxHp = 100,
            Hp = 100,
            BaseDamage = 7,
            MaxDamage = 16
        },
        Difficulty.Normal => new Enemy
        {
            Name = "นักรบออร์ค",
            Intro = "ออร์คกำแขนขวานแน่น พร้อมยิ้มอย่างมั่นใจว่าคุณจะแพ้",
            MaxHp = 500,
            Hp = 500,
            BaseDamage = 13,
            MaxDamage = 32
        },
        Difficulty.Hard => new Enemy
        {
            Name = "มังกรโบราณ",
            Intro = "พื้นดินสั่นสะเทือนเมื่อมังกรโบราณร่อนลงมา ขอให้โชคดี",
            MaxHp = 1000,
            Hp = 1000,
            BaseDamage = 18,
            MaxDamage = 55
        },
        _ => throw new ArgumentOutOfRangeException(nameof(difficulty))
    };
}
