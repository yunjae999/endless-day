using UnityEngine;

public class IngameManager : MonoBehaviour
{
    public static IngameManager Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        TableDataManager._instance.TableAllLoad();
    }
}
