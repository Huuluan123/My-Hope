using UnityEngine;
using System.Collections.Generic;

public class RockSpawner : MonoBehaviour, IRoomElement
{
    [Header("Object Pooling")]
    [Tooltip("Kéo thả tất cả các viên đá bạn đã tạo sẵn (và tắt đi) trong Scene vào danh sách này.")]
    public List<GameObject> rockPool;

    [Header("Spawn Timing")]
    public float minSpawnDelay = 0.2f;
    public float maxSpawnDelay = 0.5f;
    private float spawnTimer;

    [Header("Spawn Area & Targeting")]
    public Transform playerTarget;
    [Tooltip("Chiều rộng của vùng trên trần nhà mà đá có thể rơi xuống")]
    public float spawnAreaWidth = 20f;

    [Range(0f, 1f)]
    [Tooltip("Tỉ lệ % ưu tiên thả đá ngay trên đầu người chơi (0.6 = 60%)")]
    public float prioritizePlayerChance = 0.6f;
    [Tooltip("Vùng sai số khi thả trên đầu người chơi (để họ còn có cơ hội né)")]
    public float playerAreaWidth = 4f;

    private void Start()
    {
        ResetTimer();
    }

    private void Update()
    {
        if (playerTarget == null) return;

        // Đếm ngược thời gian
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnRock();
            ResetTimer();
        }
    }

    private void SpawnRock()
    {
        // 1. LẤY ĐÁ TỪ POOL
        GameObject rock = GetInactiveRock();

        // Nếu tất cả đá trong Pool đều đang rơi (đang bật), ta bỏ qua lượt thả này để đảm bảo hiệu năng
        if (rock == null) return;

        // 2. TÍNH TOÁN TOẠ ĐỘ X
        float spawnX = transform.position.x; // Mặc định ở tâm Spawner

        // Tung xúc xắc xem có ưu tiên thả trúng đầu người chơi không
        if (Random.value < prioritizePlayerChance)
        {
            // Thả quanh toạ độ X của người chơi
            float randomOffset = Random.Range(-playerAreaWidth / 2f, playerAreaWidth / 2f);
            spawnX = playerTarget.position.x + randomOffset;
        }
        else
        {
            // Thả ngẫu nhiên trong toàn bộ căn phòng
            float randomOffset = Random.Range(-spawnAreaWidth / 2f, spawnAreaWidth / 2f);
            spawnX = transform.position.x + randomOffset;
        }

        // CHẶN RÌA: Ép toạ độ X không được văng ra khỏi khu vực quy định của Spawner
        float minX = transform.position.x - (spawnAreaWidth / 2f);
        float maxX = transform.position.x + (spawnAreaWidth / 2f);
        spawnX = Mathf.Clamp(spawnX, minX, maxX);

        // 3. KÍCH HOẠT VÀ DI CHUYỂN ĐÁ
        rock.transform.position = new Vector3(spawnX, transform.position.y, 0f);
        rock.SetActive(true);
    }

    // Hàm duyệt Pool để tìm viên đá đang tắt
    private GameObject GetInactiveRock()
    {
        foreach (GameObject rock in rockPool)
        {
            if (!rock.activeInHierarchy) return rock;
        }
        return null;
    }

    private void ResetTimer()
    {
        spawnTimer = Random.Range(minSpawnDelay, maxSpawnDelay);
    }

    // Hàm vẽ dải hiển thị màu đỏ trên Editor để Level Designer dễ dàng căn chỉnh vùng rơi của đá
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.4f); // Màu đỏ trong suốt
        Gizmos.DrawCube(transform.position, new Vector3(spawnAreaWidth, 0.5f, 0f));
    }

    public void ResetState() { }
}