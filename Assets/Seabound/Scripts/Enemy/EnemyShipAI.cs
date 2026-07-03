using Unity.Netcode;
using UnityEngine;

public class EnemyShipAI : NetworkBehaviour
{
    [Header("Gemi Hareket Ayarları")]
    public float moveSpeed = 8f;        // Geminin hızı
    public float rotationSpeed = 2f;    // Dönüş yumuşaklığı
    public float detectionRange = 200f; // Çok uzaktan oyuncuyu fark etme menzili
    public float stopDistance = 20f;    // Ateş etmek/bordalamak için duracağı mesafe

    private Transform targetPlayer;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        if (targetPlayer == null)
        {
            FindTarget();
            return;
        }

        float distance = Vector3.Distance(transform.position, targetPlayer.position);

        // Eğer oyuncu menzildeyse VE hala çok yaklaşmadıysak ÜSTÜNE SÜR!
        if (distance <= detectionRange && distance > stopDistance)
        {
            // Oyuncuya doğru burnunu çevir
            Vector3 direction = (targetPlayer.position - transform.position).normalized;
            direction.y = 0; // Geminin havaya kalkmasını / suya batmasını engeller

            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, lookRotation, Time.fixedDeltaTime * rotationSpeed));
            }

            // İleri (oyuncuya) doğru fiziksel olarak ilerle
            rb.MovePosition(rb.position + transform.forward * moveSpeed * Time.fixedDeltaTime);
        }
        else if (distance > detectionRange)
        {
            // Oyuncu çok uzaklaştıysa takibi bırak
            targetPlayer = null;
        }
    }

    private void FindTarget()
    {
        // "Player" etiketine sahip en yakın oyuncuyu bulur
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float closestDistance = Mathf.Infinity;

        foreach (GameObject player in players)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);

            if (distance <= detectionRange && distance < closestDistance)
            {
                closestDistance = distance;
                targetPlayer = player.transform;
            }
        }
    }
}