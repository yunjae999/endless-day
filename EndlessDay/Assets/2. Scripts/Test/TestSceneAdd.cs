using UnityEngine;
using UnityEngine.SceneManagement;

public class TestSceneAdd : MonoBehaviour
{
    public void OnClickOpenInventory()
    {
        SceneManager.LoadScene("InventoryScene", LoadSceneMode.Additive);
    }
}
