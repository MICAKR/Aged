using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[System.Serializable]
public class FoliageData
{
    public string foliageName = "Tree or Grass";

    [Header("Prefabs")]
    [Tooltip("ใส่ Prefab ต้นไม้หรือหญ้าหลายๆ แบบลงไปเพื่อสุ่มหน้าตาได้")]
    public GameObject[] prefabs;

    [Tooltip("ใส่แผ่น Tile ที่อนุญาตให้ Prefab นี้เกิดได้ (เช่น ลาก Tile หญ้ามาใส่)")]
    public List<TileBase> allowedTiles;

    [Header("Clustering (ความกระจุกตัว)")]
    [Tooltip("ขนาดของกลุ่มก้อน ยิ่งมากยิ่งเกาะกลุ่มเป็นป่าผืนใหญ่ (แนะนำ 15-30)")]
    public float clusterScale = 20f;
    [Tooltip("ความหนาแน่น ยิ่งค่าน้อย ป่ายิ่งกินพื้นที่กว้าง (แนะนำ 0.4 - 0.6)")]
    [Range(0f, 1f)] public float clusterThreshold = 0.5f;

    [Header("Spacing (ความห่าง)")]
    [Tooltip("ระยะห่างขั้นต่ำจากต้นอื่น (นับเป็นจำนวนบล็อก) เช่น ต้นไม้ควรห่าง 2 บล็อก หญ้า 1 บล็อก")]
    public int minDistance = 2;

    [Header("Density (โอกาสเกิดในจุดนั้น)")]
    [Tooltip("โอกาสที่จะเกิดต้นไม้ในพิกัดที่ผ่านเงื่อนไขกลุ่มแล้ว (0 = ไม่เกิดเลย, 1 = เกิดแน่นเต็มพื้นที่)")]
    [Range(0f, 1f)] public float spawnChance = 0.4f;
}

public class FoliageGenerator : MonoBehaviour
{
    public FoliageData[] foliageSettings;

    [Tooltip("Object ว่างๆ สำหรับเก็บต้นไม้ที่เสกมาไม่ให้รก Hierarchy")]
    public Transform foliageParent;

    // เก็บพิกัดที่เสกไปแล้ว เพื่อเอาไว้เช็คระยะห่าง (Spacing)
    private Dictionary<Vector2Int, GameObject> spawnedObjects = new Dictionary<Vector2Int, GameObject>();

    // ฟังก์ชันนี้จะถูกเรียกใช้จาก WorldGenerator ทันทีที่สร้าง Chunk ดินเสร็จ
    public void GenerateFoliageForChunk(Vector2Int chunkPos, int chunkSize, string seed, Tilemap grassTilemap, Tilemap groundTilemap, float seedOffsetX, float seedOffsetY)
    {
        // สร้างระบบสุ่มที่ล็อกผลลัพธ์ตาม Seed และพิกัด Chunk
        System.Random prng = new System.Random(seed.GetHashCode() + chunkPos.x * 1000 + chunkPos.y);

        for (int cx = 0; cx < chunkSize; cx++)
        {
            for (int cy = 0; cy < chunkSize; cy++)
            {
                int worldX = chunkPos.x * chunkSize + cx;
                int worldY = chunkPos.y * chunkSize + cy;
                Vector3Int tilePos = new Vector3Int(worldX, worldY, 0);

                // ตรวจสอบว่าพิกัดนี้มีบล็อกอะไรอยู่บ้าง (เช็คเลเยอร์หญ้าก่อน ถ้าไม่มีค่อยเช็คเลเยอร์ดิน)
                TileBase currentTile = grassTilemap.GetTile(tilePos);
                if (currentTile == null) currentTile = groundTilemap.GetTile(tilePos);

                if (currentTile == null) continue; // ถ้าไม่มีแผ่นดินเลยให้ข้าม

                Vector2Int worldPos2D = new Vector2Int(worldX, worldY);

                // เช็คว่าตรงกับเงื่อนไขพืชชนิดไหนบ้าง
                foreach (var foliage in foliageSettings)
                {
                    // 1. เช็คว่าบล็อกพื้นดินตรงกับที่อนุญาตหรือไม่
                    if (!foliage.allowedTiles.Contains(currentTile)) continue;

                    // 2. เช็ค Clustering (Perlin Noise) เพื่อสร้างเป็นกลุ่มก้อนป่า
                    float clusterNoise = Mathf.PerlinNoise(worldX / foliage.clusterScale + seedOffsetX, worldY / foliage.clusterScale + seedOffsetY);
                    if (clusterNoise < foliage.clusterThreshold) continue;

                    // 3. สุ่มโอกาสเกิด (Density)
                    if (prng.NextDouble() > foliage.spawnChance) continue;

                    // 4. เช็คระยะห่าง (Spacing) ว่าพิกัดนี้ใกล้กับต้นไม้อื่นเกินไปไหม
                    if (!IsPositionValid(worldPos2D, foliage.minDistance)) continue;

                    // 5. ถ้าผ่านทุกอย่าง -> เสก Prefab โลด!
                    GameObject prefabToSpawn = foliage.prefabs[prng.Next(0, foliage.prefabs.Length)];

                    // ปรับตำแหน่งให้โผล่ตรงกึ่งกลางของบล็อก (บวก 0.5f)
                    Vector3 spawnWorldPos = new Vector3(worldX + 0.5f, worldY + 0.5f, 0);

                    GameObject inst = Instantiate(prefabToSpawn, spawnWorldPos, Quaternion.identity, foliageParent);

                    // บันทึกพิกัดไว้เช็คระยะห่างต้นต่อไป
                    spawnedObjects.Add(worldPos2D, inst);

                    break; // เสกได้แค่ 1 อย่างต่อ 1 บล็อกเท่านั้น (กันต้นไม้เกิดทับหญ้า)
                }
            }
        }
    }

    // ฟังก์ชันเช็ครัศมีรอบๆ ว่ามี Object อื่นเกิดไปแล้วหรือยัง
    private bool IsPositionValid(Vector2Int pos, int minDistance)
    {
        if (minDistance <= 0) return true;

        for (int x = -minDistance; x <= minDistance; x++)
        {
            for (int y = -minDistance; y <= minDistance; y++)
            {
                Vector2Int checkPos = new Vector2Int(pos.x + x, pos.y + y);
                // เช็คเป็นแนวรัศมีวงกลม
                if (Vector2.Distance(pos, checkPos) <= minDistance)
                {
                    if (spawnedObjects.ContainsKey(checkPos)) return false;
                }
            }
        }
        return true;
    }
}