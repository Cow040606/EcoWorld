using UnityEngine;
using UnityEngine.Video;
using System.Collections;
using System.Collections.Generic;

public class QuestVideoTrigger : MonoBehaviour
{
    public VideoPlayer myVideoPlayer;
    public GameObject videoScreenUI; 
    
    [Header("Kéo các Model muốn ẨN LUÔN sau khi Video xong vào đây:")]
    public List<GameObject> danhSachModelCanAn = new List<GameObject>();

    public void TriggerPlay()
    {
        if (videoScreenUI != null) videoScreenUI.SetActive(true);

        if (myVideoPlayer != null)
        {
            myVideoPlayer.gameObject.SetActive(true);
            myVideoPlayer.isLooping = false; 
            
            StopAllCoroutines(); 
            StartCoroutine(ForceStopVideo());
        }
    }

    IEnumerator ForceStopVideo()
    {
        myVideoPlayer.Play();

        while (!myVideoPlayer.isPrepared)
        {
            yield return null;
        }

        float videoDuration = (float)myVideoPlayer.length;
        yield return new WaitForSeconds(videoDuration);

        // --- ĐẾM NGƯỢC XONG -> ÉP TẮT ---
        myVideoPlayer.Stop();
        
        if (videoScreenUI != null) 
            videoScreenUI.SetActive(false); 
            
        myVideoPlayer.gameObject.SetActive(false); 

        // --- TẮT LUÔN MODEL NPC KHI XONG VIDEO ---
        if (danhSachModelCanAn != null && danhSachModelCanAn.Count > 0)
        {
            foreach (var model in danhSachModelCanAn)
            {
                if (model != null) model.SetActive(false);
            }
        }
    }
}