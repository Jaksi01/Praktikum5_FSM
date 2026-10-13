using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyHealthUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text statusText;

    private EnemyHealth enemyHealth;

    private void Awake()
    {
        enemyHealth = GetComponentInParent<EnemyHealth>();
    }

    private void Start()
    {
        if (enemyHealth == null)
        {
            Debug.LogError(
                "EnemyHealth tidak ditemukan!", this);
            enabled = false;
            return;
        }

        if (healthBar == null ||
            healthText == null ||
            statusText == null)
        {
            Debug.LogError(
                "Referensi UI belum lengkap!", this);
            enabled = false;
            return;
        }

        healthBar.minValue = 0;
        healthBar.maxValue = enemyHealth.MaxHealth;
    }

    private void Update()
    {
        float hp = enemyHealth.CurrentHealth;
        float maxHp = enemyHealth.MaxHealth;

        healthBar.value = hp;
        healthText.text = $"{hp:0} / {maxHp:0}";

        if (enemyHealth.IsDead)
        {
            statusText.text = "DEAD";
        }
        else
        {
            statusText.text = "ALIVE";
        }
    }
}

