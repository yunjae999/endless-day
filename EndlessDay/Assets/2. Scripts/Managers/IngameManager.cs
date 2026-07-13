using UnityEngine;

public class IngameManager : MonoBehaviour
{
    public static IngameManager Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        TableDataManager._instance.TableAllLoad();
    }
    void Start()
    {
    }
}
