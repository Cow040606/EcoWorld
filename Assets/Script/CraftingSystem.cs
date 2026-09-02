using UnityEngine;

public class CraftingSystem : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource audioSource; // Kéo thả AudioSource vào đây
    public AudioClip craftSound;    // Kéo thả file âm thanh craft vào đây
    public AudioClip createSound;  // Kéo thả file âm thanh create vào đây

    // Hàm này được gọi khi người dùng bấm nút Craft
    public void CraftItem()
    {
        // 1. Logic kiểm tra nguyên liệu, tạo ra item mới của bạn ở đây...
        bool canCraft = true; // Giả sử đã đủ điều kiện craft

        if (canCraft)
        {
            Debug.Log("Đã craft thành công!");
            
            // 2. Phát âm thanh craft đồ
            PlayCraftSound();
            
            // 3. Trừ nguyên liệu, thêm đồ vào túi đồ...
        }
    }
    public void CreateItem()
    {
        // 1. Logic kiểm tra nguyên liệu, tạo ra item mới của bạn ở đây...
        bool canCreate = true; // Giả sử đã đủ điều kiện create

        if (canCreate)
        {
            Debug.Log("Đã tạo thành công!");
            
            // 2. Phát âm thanh create đồ
            PlayCreateSound();
            
            // 3. Trừ nguyên liệu, thêm đồ vào túi đồ...
        }
    }

    private void PlayCraftSound()
    {
        // Kiểm tra xem có gán âm thanh và AudioSource chưa để tránh lỗi
        if (audioSource != null && craftSound != null)
        {
            // Dùng PlayOneShot để âm thanh có thể đè lên nhau 
            // nếu người chơi craft liên tục nhiều món
            audioSource.PlayOneShot(craftSound);
        }
    }
    private void PlayCreateSound()
    {
        // Kiểm tra xem có gán âm thanh và AudioSource chưa để tránh lỗi
        if (audioSource != null && createSound != null)
        {
            // Dùng PlayOneShot để âm thanh có thể đè lên nhau 
            // nếu người chơi create liên tục nhiều món
            audioSource.PlayOneShot(createSound);
        }
    }
}