using System.Collections;
using UnityEngine;
using TMPro; 
using UnityEngine.UI; 
using UnityEngine.SceneManagement;

public class OnlineOpeningCutscene : MonoBehaviour
{
    [Header("UI Elements")]
    public Image blackScreen;
    public TextMeshProUGUI openingText;

    [Header("Nội dung hội thoại")]
    [TextArea(2, 5)]
    public string[] danhSachCauThoai; 

    [Header("Cài đặt thời gian")]
    public float tocDoMo = 1.5f;     
    public float thoiGianDoc = 3f;  

    [Header("Chuyển Scene")]
    public string sceneToLoad;

    public void StartCutscene()
    {
        gameObject.SetActive(true); 
        StartCoroutine(ChayCutsceneOnline());
    }

    void Start()
    {
        
    }

    IEnumerator ChayCutsceneOnline()
    {
        blackScreen.color = new Color(0, 0, 0, 0f);
        openingText.color = new Color(1, 1, 1, 0f);

        while (blackScreen.color.a < 1f)
        {
            blackScreen.color = new Color(0, 0, 0, blackScreen.color.a + (Time.deltaTime * tocDoMo));
            yield return null;
        }
        
        yield return new WaitForSeconds(0.5f);
        
        foreach (string cauThoai in danhSachCauThoai)
        {
            openingText.text = cauThoai;

            while (openingText.color.a < 1f)
            {
                openingText.color = new Color(1, 1, 1, openingText.color.a + (Time.deltaTime * tocDoMo));
                yield return null; 
            }

            yield return new WaitForSeconds(thoiGianDoc);

            while (openingText.color.a > 0f)
            {
                openingText.color = new Color(1, 1, 1, openingText.color.a - (Time.deltaTime * tocDoMo));
                yield return null;
            }
            
            yield return new WaitForSeconds(0.5f); 
        }

        while (blackScreen.color.a > 0f)
        {
            blackScreen.color = new Color(0, 0, 0, blackScreen.color.a - (Time.deltaTime * tocDoMo));
            yield return null;
        }
        
        EndCutscene();
    }

    public void EndCutscene()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
