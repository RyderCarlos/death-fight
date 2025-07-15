using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    public static DamagePopup Create(Vector3 position, int damageAmount) {
        if (GameAssets.Instance == null) {
            Debug.LogError("GameAssets instance not found!");
            return;
        }
        
        if (GameAssets.Instance.damagePopupPrefab == null) {
            Debug.LogError("DamagePopup prefab not assigned in GameAssets!");
            return;
        }
        
        // 实例化伤害弹出
        GameObject popupGO = Instantiate(
            GameAssets.Instance.damagePopupPrefab, 
            position, 
            Quaternion.identity
        );
        
        DamagePopup popup = popupGO.GetComponent<DamagePopup>();
        if (popup != null) {
            popup.Setup(damageAmount);
        }
            GameObject popupGO = Instantiate(GameAssets.i.damagePopupPrefab, position, Quaternion.identity);
        DamagePopup popup = popupGO.GetComponent<DamagePopup>();
        popup.Setup(damageAmount);
        return popup;
    }
    
    private TextMeshPro textMesh;
    private float disappearTimer;
    private Color textColor;
    
    private void Awake() {
        textMesh = GetComponent<TextMeshPro>();
    }
    
    public void Setup(int damageAmount) {
        textMesh.SetText(damageAmount.ToString());
        textColor = damageAmount > 30 ? Color.red : Color.yellow;
        textMesh.color = textColor;
        disappearTimer = 1f;
        
        // 随机方向弹出
        float x = Random.Range(-1f, 1f);
        float y = Random.Range(2f, 4f);
        GetComponent<Rigidbody2D>().velocity = new Vector2(x, y);
    }
    
    private void Update() {
        disappearTimer -= Time.deltaTime;
        if (disappearTimer < 0) {
            float disappearSpeed = 3f;
            textColor.a -= disappearSpeed * Time.deltaTime;
            textMesh.color = textColor;
            if (textColor.a < 0) {
                Destroy(gameObject);
            }
        }
    }
}