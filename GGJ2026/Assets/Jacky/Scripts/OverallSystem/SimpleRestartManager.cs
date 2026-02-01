using UnityEngine;
using UnityEngine.SceneManagement;
public class SimpleRestartManager : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SceneManager.LoadScene("Scene_Open");
        }
    }
}
