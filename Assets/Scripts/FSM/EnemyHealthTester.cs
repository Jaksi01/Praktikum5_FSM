using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyHealthTester : MonoBehaviour
{
    [SerializeField] private EnemyHealth enemyHealth;

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            Debug.Log("TOMBOL H TERDETEKSI!");

            if (enemyHealth == null)
            {
                Debug.LogError("Enemy Health belum terhubung!");
                return;
            }

            Debug.Log("HP sebelum damage: " + enemyHealth.CurrentHealth);

            enemyHealth.TakeDamage(10f);

            Debug.Log("HP sesudah damage: " + enemyHealth.CurrentHealth);
        }
    }
}
