using UnityEngine;
using UnityEngine.Tilemaps;

public class RiverGenerator : MonoBehaviour
{
    [Header("River Tiles")]
    public TileBase riverTile;
    public TileBase bankTile;

    [Header("River Logic")]
    public float noiseScale = 200f;
    public float warpStrength = 50f;

    [Header("Thresholds")]
    [Range(0f, 0.1f)] public float riverThreshold = 0.02f;
    [Range(0f, 0.1f)] public float bankThreshold = 0.04f;

    // เพิ่มพารามิเตอร์ bool isOcean เพื่อเช็คว่าพิกัดนี้เป็นทะเลล้อมรอบหรือไม่
    public int GetRiverStatus(int x, int y, float offsetX, float offsetY, bool isOcean)
    {
        // ถ้าบริเวณนี้เป็นทะเลไปแล้ว ไม่ต้องวาดแม่น้ำหรือตลิ่งทรายทับ
        if (isOcean) return 0;

        float shiftedX = x + offsetX;
        float shiftedY = y + offsetY;

        float warpX = GetNoise(shiftedX * 0.005f, shiftedY * 0.005f) * warpStrength;
        float warpY = GetNoise(shiftedX * 0.005f + 12.34f, shiftedY * 0.005f + 56.78f) * warpStrength;

        float n = Mathf.PerlinNoise((shiftedX + warpX) / noiseScale, (shiftedY + warpY) / noiseScale);
        float distToCenter = Mathf.Abs(n - 0.5f);

        if (distToCenter < riverThreshold) return 1;
        if (distToCenter < bankThreshold) return 2;
        return 0;
    }

    private float GetNoise(float x, float y)
    {
        return Mathf.PerlinNoise(x, y);
    }
}