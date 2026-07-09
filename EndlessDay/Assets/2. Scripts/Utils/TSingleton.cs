using UnityEngine;

public class TSingleton<T> : MonoBehaviour where T : TSingleton<T>
{
    // 멀티 스레드 동기화를 위해 volatile 키워드 사용.
    static volatile GameObject _uniqueObject = null;
    static volatile T _uniqueInstance = null;
    // 상속전용 클래스기 때문에 protected 생성자
    protected TSingleton()
    {

    }
    protected virtual void Init()
    {
        DontDestroyOnLoad(gameObject);
    }
    public static T _instance
    {
        get
        {
            if (_uniqueObject == null)
            {
                lock (typeof(T))
                {
                    if (_uniqueInstance == null && _uniqueObject == null)
                    {
                        _uniqueObject = new GameObject(typeof(T).Name, typeof(T));
                        _uniqueInstance = _uniqueObject.GetComponent<T>();

                        // 해당 스크립트가 붙은 오브젝트가 파괴되지 않도록 하기 위함이다.
                        _uniqueInstance.Init();
                    }
                }
            }
            return _uniqueInstance;
        }
    }
}
