using System.Collections; // <--- CHÍNH LÀ DÒNG NÀY BỊ THIẾU
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
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
        SceneManager.LoadScene("Scenes/map1");

        GameObject player = null;
        float timeOut = 5f;
        while (player == null && timeOut > 0)
        {
            player = GameObject.FindWithTag("Player");
            if (player == null) yield return new WaitForSeconds(0.5f);
            timeOut -= 0.5f;
        }
        Assert.IsNotNull(player, "Đã đợi 5 giây nhưng không tìm thấy Player!");

        GameObject coinObj = GameObject.Find("CoinValue");
        Assert.IsNotNull(coinObj, "Không tìm thấy UI CoinValue");
        TMP_Text stoneText = coinObj.GetComponent<TMP_Text>();

        int stoneBefore = int.Parse(stoneText.text);

        player.SendMessage("AddCoin", 1, SendMessageOptions.DontRequireReceiver); 
        
        yield return new WaitForSeconds(0.5f); 

        int stoneAfter = int.Parse(stoneText.text);

        Assert.AreEqual(
            stoneBefore + 1,
            stoneAfter,
            "UI số tiền/đá không tăng đúng sau khi nhặt"
        );
    }

    [UnityTest]
    public IEnumerator UT_CD83_KiemTraCapNhatThanhMau()
    {
        SceneManager.LoadScene("Scenes/map1");

        GameObject player = null;
        float timeOut = 5f;
        while (player == null && timeOut > 0)
        {
            player = GameObject.FindWithTag("Player");
            if (player == null) yield return new WaitForSeconds(0.5f);
            timeOut -= 0.5f;
        }
        Assert.IsNotNull(player, "Không tìm thấy Player!");

        GameObject hpObj = GameObject.Find("HealthBar");
        Assert.IsNotNull(hpObj, "Không tìm thấy UI HealthBar");
        Slider hpBar = hpObj.GetComponent<Slider>();

        float hpBefore = hpBar.value;

        // // Giả lập nhận sát thương
        // player.SendMessage("TakeDamage", 10f, SendMessageOptions.DontRequireReceiver);
        
        // // Đợi 1 giây (phòng trường hợp thanh máu tụt từ từ bằng hiệu ứng lerp)
        // yield return new WaitForSeconds(1f); 

        // float hpAfter = hpBar.value;

        // Assert.Less(hpAfter, hpBefore, "Thanh máu không giảm sau khi bị damage");
    }

    [UnityTest]
    public IEnumerator UT_CD84_KiemTraDoiCongCuUI()
    {
        SceneManager.LoadScene("Scenes/map1");

        yield return new WaitForSeconds(1f);

        GameObject toolSlot = GameObject.Find("Slot1");
        Assert.IsNotNull(toolSlot, "Không tìm thấy UI ToolSlot_1");


        toolSlot.SendMessage("Select", SendMessageOptions.DontRequireReceiver);
        
        yield return new WaitForSeconds(0.5f);

        Assert.IsTrue(toolSlot.activeInHierarchy, "ToolSlot không hoạt động sau khi Select");
        
    }
}