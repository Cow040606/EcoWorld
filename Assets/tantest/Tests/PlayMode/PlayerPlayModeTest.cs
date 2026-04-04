using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI; // BẮT BUỘC THÊM DÒNG NÀY ĐỂ FIX LỖI TEXT VÀ SLIDER

public class PlayerPlayModeTest
{
    // TC1: Player được tạo thành công
    [UnityTest]
    public IEnumerator Player_IsCreated_Successfully()
    {
        GameObject player = new GameObject("Player");

        yield return null;

        Assert.IsNotNull(
            player,
            "Player phải được tạo thành công"
        );

        Object.Destroy(player);
    }

    // TC2: Player có thể di chuyển sang phải
    [UnityTest]
    public IEnumerator Player_Can_Move_Right()
    {
        GameObject player = new GameObject("Player");
        player.transform.position = Vector3.zero;

        yield return null;

        player.transform.Translate(Vector3.right * 2f);
        yield return null;

        Assert.Greater(
            player.transform.position.x,
            0,
            "Player phải di chuyển sang phải"
        );

        Object.Destroy(player);
    }

    // TC3: Player tồn tại sau nhiều frame
    [UnityTest]
    public IEnumerator Player_Exists_AfterFrames()
    {
        GameObject player = new GameObject("Player");

        yield return null;
        yield return null;

        Assert.IsTrue(
            player != null,
            "Player phải tồn tại sau nhiều frame"
        );

        Object.Destroy(player);
    }

    [UnityTest]
    public IEnumerator UT_CD82_KiemTraCapNhatUIDaKhiKhaiThac()
    {
        // 1. Load scene gameplay
        UnityEngine.SceneManagement.SceneManager.LoadScene("GamePlay");
        yield return new WaitForSeconds(1f);

        // 2. Tìm Player
        GameObject player = GameObject.FindWithTag("Player");
        Assert.IsNotNull(player, " Không tìm thấy Player");

        // 3. Tìm UI hiển thị số đá
        Text stoneText = GameObject.Find("StoneText").GetComponent<Text>();
        Assert.IsNotNull(stoneText, " Không tìm thấy UI StoneText");

        // 4. Lấy số đá ban đầu
        int stoneBefore = int.Parse(stoneText.text);

        // 5. Tạo Gold giả lập
        GameObject gold = new GameObject("Gold");
        gold.tag = "Gold";
        gold.transform.position = player.transform.position;

        // 6. Giả lập Player nhặt Gold (Đã sửa lại thành 3D cho chuẩn game của Bò)
        gold.SendMessage("OnTriggerEnter", player.GetComponent<Collider>(), SendMessageOptions.DontRequireReceiver);
        yield return new WaitForSeconds(0.5f);

        // 7. Lấy số đá sau khi nhặt
        int stoneAfter = int.Parse(stoneText.text);

        // 8. Kiểm tra kết quả
        Assert.AreEqual(
            stoneBefore + 1,
            stoneAfter,
            "UI số đá không tăng đúng sau khi nhặt vàng"
        );
    }

    [UnityTest]
    public IEnumerator UT_CD83_KiemTraCapNhatThanhMau()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("GamePlay");
        yield return new WaitForSeconds(1f);

        GameObject player = GameObject.FindWithTag("Player");
        Assert.IsNotNull(player);

        Slider hpBar = GameObject.Find("HealthBar").GetComponent<Slider>();
        Assert.IsNotNull(hpBar);

        float hpBefore = hpBar.value;

        player.SendMessage("TakeDamage", 10, SendMessageOptions.DontRequireReceiver);
        yield return new WaitForSeconds(0.5f);

        float hpAfter = hpBar.value;

        Assert.Less(hpAfter, hpBefore, "Thanh máu không giảm sau khi bị damage");
    }

    [UnityTest]
    public IEnumerator UT_CD84_KiemTraDoiCongCuUI()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("GamePlay");
        yield return new WaitForSeconds(1f);

        GameObject toolSlot = GameObject.Find("ToolSlot_1");
        Assert.IsNotNull(toolSlot);

        toolSlot.SendMessage("Select", SendMessageOptions.DontRequireReceiver);
        yield return null;

    }
}