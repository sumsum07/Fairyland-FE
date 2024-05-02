using UnityEngine;
using UnityEngine.SceneManagement;

public class GoToRoleScene : MonoBehaviour
{
    public void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Update() 메서드가 호출되었습니다.");
            SceneManager.LoadScene("role_playing_scene");
        }

    }
}
