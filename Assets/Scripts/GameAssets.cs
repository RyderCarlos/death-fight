// 创建 GameAssets.cs
using UnityEngine;

public class GameAssets : MonoBehaviour
{
    private static GameAssets _instance;
    public static GameAssets Instance {
        get {
            if (_instance == null) _instance = FindObjectOfType<GameAssets>();
            return _instance;
        }
    }
    
    [Header("预制体")]
    public GameObject damagePopupPrefab;
}