using NUnit.Framework;
using UnityEngine;


public class PlayerEditModeTest
{
    // ========================
    // LOGIC CƠ BẢN
    // ========================


    // TC1: Điểm khởi đầu phải >= 0
    [Test]
    public void Score_InitialValue_NotNegative()
    {
        int score = 0;


        Assert.GreaterOrEqual(
            score,
            0,
            "Score ban đầu không được âm"
        );
    }


    // TC2: Cộng điểm hợp lệ
    [Test]
    public void Score_AddPoint_Correct()
    {
        int score = 10;
        score += 5;


        Assert.AreEqual(
            15,
            score,
            "Score phải được cộng đúng"
        );
    }


    // TC3: Không cho phép cộng điểm âm
    [Test]
    public void Score_AddNegative_NotAllowed()
    {
        int score = 10;
        int addPoint = -5;


        Assert.GreaterOrEqual(
            addPoint,
            0,
            "Không cho phép cộng điểm âm"
        );
    }


    // ========================
    // LOGIC NÂNG CAO
    // ========================


    // TC4: Tốc độ di chuyển > 0
    [Test]
    public void Player_MoveSpeed_GreaterThanZero()
    {
        float moveSpeed = 5f;


        Assert.Greater(
            moveSpeed,
            0,
            "Tốc độ di chuyển phải lớn hơn 0"
        );
    }


    // TC5: Máu nhân vật không âm
    [Test]
    public void Player_Health_NotNegative()
    {
        int health = 100;


        Assert.GreaterOrEqual(
            health,
            0,
            "Máu nhân vật không được âm"
        );
    }


    // TC6: Sát thương không được âm
    [Test]
    public void Damage_NotNegative()
    {
        int damage = 10;


        Assert.GreaterOrEqual(
            damage,
            0,
            "Sát thương không được là số âm"
        );
    }
}

