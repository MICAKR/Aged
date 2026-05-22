using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public enum Temperature { Hot, Temperate, Cold }

[System.Serializable]
public class BiomeSettings
{
    public string biomeName;
    [Range(0f, 1f)] public float noiseThreshold;

    [Header("Base Tiles (ชั้นดิน)")]
    public TileBase mainTile;
    public TileBase transitionTile;
    public TileBase variationTile;

    [Header("Overlay Tiles (ชั้นหญ้า)")]
    [Tooltip("ไทล์ที่จะวางทับบนเลเยอร์หญ้า (เช่น Rule Tile หญ้า) ปล่อยว่างไว้ถ้าไบโอมนี้ไม่มีหญ้า")]
    public TileBase overlayTile;

    [Header("Inner Variation Setup")]
    [Range(0f, 1f)] public float variationThreshold = 0.7f;
}

[System.Serializable]
public class TemperatureZoneSetup
{
    public Temperature temperatureZone;
    [Range(0f, 1f)]
    public float maxLatitudeThreshold;
    public BiomeSettings[] biomes;
}

public class WorldGenerator : MonoBehaviour
{
    [Header("Tilemap Reference")]
    public Tilemap groundTilemap;   // เลเยอร์ชั้นล่างสุด (ดิน, ทราย, ทะเล)
    public Tilemap grassTilemap;    // เลเยอร์ชั้นบน (หญ้า) สำหรับทำขอบเหลือบทับดิน
    public Tilemap riverTilemap;

    [Header("Generator References")]
    public RiverGenerator riverGen;
    public FoliageGenerator foliageGen; // เพิ่มระบบเสกต้นไม้

    [Header("Ocean & Beach Setup")]
    public TileBase oceanTile;
    public TileBase beachTile;
    [Range(0.1f, 1.5f)] public float landRadius = 0.8f;
    [Range(0.01f, 0.5f)] public float beachThickness = 0.05f;

    [Header("Island Shape Settings")]
    [Tooltip("ค่าการบีบแกน X ยิ่งน้อยเกาะยิ่งรียาวในแนวตั้ง (แนะนำ 0.5 - 0.7) ถ้าปรับเป็น 1 จะกลับเป็นทรงกลม")]
    [Range(0.2f, 1.5f)] public float widthStretchFactor = 0.6f;

    [Header("Peninsula (แหลมผืนดินยื่น) Settings")]
    [Tooltip("ขนาดของแหลม/อ่าว ยิ่งมากแหลมยิ่งใหญ่หนาข้ามชังก์")]
    public float peninsulaScale = 350f;
    [Tooltip("ความยาวหรือความแรงในการพุ่งยื่นของแหลมออกไปในทะเล (แนะนำ 0.1 - 0.3)")]
    [Range(0f, 0.5f)] public float peninsulaStrength = 0.18f;

    [Header("Dynamic Generation Settings")]
    public Transform player;
    public int chunkSize = 16;
    public int renderDistance = 3;

    [Header("World Limits")]
    public int maxWorldWidth = 1000;
    public int maxWorldHeight = 1000;
    public string seedString = "StardewValley2026";

    [Header("Temperature Setup")]
    public float temperatureWiggleScale = 30f;
    [Range(0f, 0.5f)] public float temperatureWiggleStrength = 0.05f;
    public TemperatureZoneSetup[] temperatureZones;

    [Header("Biome Scale Settings")]
    public float biomeNoiseScale = 40f;
    public float detailNoiseScale = 10f;
    [Range(0f, 0.2f)] public float transitionBlendZone = 0.05f;

    private HashSet<Vector2Int> generatedChunks = new HashSet<Vector2Int>();
    private Vector2Int currentChunkPosition;
    private bool isInitialized = false;
    private float seedOffsetX, seedOffsetY;
    private Coroutine chunkGenerationCoroutine;

    void Start()
    {
        CalculateSeedOffsets();
        groundTilemap.ClearAllTiles();
        if (grassTilemap != null) grassTilemap.ClearAllTiles();
        riverTilemap.ClearAllTiles();

        StartCoroutine(UpdateChunksAsync(player != null ? (Vector2)player.position : Vector2.zero));
    }

    void Update()
    {
        if (player == null) return;
        Vector2Int playerChunk = new Vector2Int(Mathf.FloorToInt(player.position.x / chunkSize), Mathf.FloorToInt(player.position.y / chunkSize));

        if (!isInitialized || playerChunk != currentChunkPosition)
        {
            currentChunkPosition = playerChunk;
            isInitialized = true;
            if (chunkGenerationCoroutine != null) StopCoroutine(chunkGenerationCoroutine);
            chunkGenerationCoroutine = StartCoroutine(UpdateChunksAsync(player.position));
        }
    }

    private void CalculateSeedOffsets()
    {
        Random.InitState(seedString.GetHashCode());
        seedOffsetX = Random.Range(50000.123f, 90000.456f);
        seedOffsetY = Random.Range(50000.123f, 90000.456f);
    }

    private float GetFractalNoise(float x, float y, int octaves, float persistence)
    {
        float total = 0f;
        float frequency = 1f;
        float amplitude = 1f;
        float maxValue = 0f;
        for (int i = 0; i < octaves; i++)
        {
            total += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= 2f;
        }
        return total / maxValue;
    }

    private IEnumerator UpdateChunksAsync(Vector2 centerPosition)
    {
        int centerChunkX = Mathf.FloorToInt(centerPosition.x / chunkSize);
        int centerChunkY = Mathf.FloorToInt(centerPosition.y / chunkSize);
        int processed = 0;

        for (int radius = 0; radius <= renderDistance; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (Mathf.Abs(x) == radius || Mathf.Abs(y) == radius)
                    {
                        Vector2Int chunkPos = new Vector2Int(centerChunkX + x, centerChunkY + y);
                        if (!generatedChunks.Contains(chunkPos))
                        {
                            GenerateChunk(chunkPos);
                            generatedChunks.Add(chunkPos);
                            processed++;
                            if (processed >= 3) { yield return null; processed = 0; }
                        }
                    }
                }
            }
        }
    }

    private void GenerateChunk(Vector2Int chunkPos)
    {
        for (int cx = 0; cx < chunkSize; cx++)
        {
            for (int cy = 0; cy < chunkSize; cy++)
            {
                int worldX = chunkPos.x * chunkSize + cx;
                int worldY = chunkPos.y * chunkSize + cy;

                Vector3Int pos = new Vector3Int(worldX, worldY, 0);

                float islandGradient = GetIslandGradientAt(worldX, worldY);
                int riverStatus = (riverGen != null) ? riverGen.GetRiverStatus(worldX, worldY, seedOffsetX, seedOffsetY, false) : 0;

                if (islandGradient >= 1f) // โซนทะเลลึกภายนอกเกาะ
                {
                    if (riverStatus == 1)
                    {
                        if (riverGen.riverTile != null) riverTilemap.SetTile(pos, riverGen.riverTile);
                    }
                    else
                    {
                        if (oceanTile != null) groundTilemap.SetTile(pos, oceanTile);
                    }
                }
                else if (islandGradient > 0f) // โซนชายหาด
                {
                    if (riverStatus == 1)
                    {
                        if (riverGen.riverTile != null) riverTilemap.SetTile(pos, riverGen.riverTile);
                    }
                    else if (riverStatus == 2)
                    {
                        if (islandGradient > 0.5f)
                        {
                            if (beachTile != null) groundTilemap.SetTile(pos, beachTile);
                        }
                        else
                        {
                            if (riverGen.bankTile != null) riverTilemap.SetTile(pos, riverGen.bankTile);
                        }
                    }
                    else
                    {
                        if (beachTile != null) groundTilemap.SetTile(pos, beachTile);
                    }
                }
                else // โซนแผ่นดินหลักด้านในเกาะ
                {
                    if (riverStatus == 1)
                    {
                        if (riverGen.riverTile != null) riverTilemap.SetTile(pos, riverGen.riverTile);
                    }
                    else if (riverStatus == 2)
                    {
                        if (riverGen.bankTile != null) riverTilemap.SetTile(pos, riverGen.bankTile);
                    }
                    else
                    {
                        BiomeData data = CalculateBiomeDataAt(worldX, worldY);

                        // ไทล์ชั้นล่างสุด (ดิน)
                        TileBase groundTile = DetermineTileType(data.biome, data.detailNoise, data.latitude);

                        if (groundTile != null)
                        {
                            // 1. ปูบล็อกดินลงไปที่เลเยอร์ Ground (ชั้นล่าง)
                            groundTilemap.SetTile(pos, groundTile);

                            // 2. ตรวจสอบว่าไบโอมนี้มีหญ้าวางทับหรือไม่ ถ้ามีให้ปูทับไปบนเลเยอร์ Grass (ชั้นบน)
                            if (grassTilemap != null && data.biome.overlayTile != null)
                            {
                                grassTilemap.SetTile(pos, data.biome.overlayTile);
                            }
                        }
                    }
                }
            }
        }

        // 💡 จุดที่แก้ไข: เลื่อนคำสั่งเสกต้นไม้มาอยู่ล่างสุด 
        // ทำงานหลังจากปูดินปูหญ้าเสร็จครบทุกช่องใน Chunk นี้เรียบร้อยแล้ว
        if (foliageGen != null)
        {
            foliageGen.GenerateFoliageForChunk(chunkPos, chunkSize, seedString, grassTilemap, groundTilemap, seedOffsetX, seedOffsetY);
        }
    }

    private float GetIslandGradientAt(int x, int y)
    {
        float shiftedX = x + seedOffsetX;
        float shiftedY = y + seedOffsetY;

        float warpX = GetFractalNoise(shiftedX / 250f, shiftedY / 250f, 2, 0.5f) * 40f;
        float warpY = GetFractalNoise(shiftedX / 250f + 15.5f, shiftedY / 250f + 22.2f, 2, 0.5f) * 40f;

        float nx = (2f * (x + warpX)) / maxWorldWidth;
        float ny = (2f * (y + warpY)) / maxWorldHeight;

        float distance = Mathf.Sqrt((nx * nx) / (widthStretchFactor * widthStretchFactor) + (ny * ny));

        float peninsulaNoise = GetFractalNoise(shiftedX / peninsulaScale, shiftedY / peninsulaScale, 3, 0.5f);
        float peninsulaModulator = (peninsulaNoise - 0.5f) * peninsulaStrength;
        distance += peninsulaModulator;

        float beachStart = landRadius - beachThickness;

        if (distance < beachStart)
        {
            return 0f;
        }
        else if (distance > landRadius)
        {
            return 1f;
        }
        else
        {
            float t = (distance - beachStart) / beachThickness;
            return Mathf.SmoothStep(0f, 1f, t);
        }
    }

    public struct BiomeData
    {
        public BiomeSettings biome;
        public float detailNoise;
        public float latitude;
    }

    public BiomeData CalculateBiomeDataAt(int worldX, int worldY)
    {
        float shiftedX = worldX + seedOffsetX;
        float shiftedY = worldY + seedOffsetY;

        float warpX = GetFractalNoise(shiftedX / 100f, shiftedY / 100f, 2, 0.5f) * 20f;
        float warpY = GetFractalNoise(shiftedX / 100f + 5.2f, shiftedY / 100f + 1.3f, 2, 0.5f) * 20f;

        float nx = (shiftedX + warpX) / biomeNoiseScale;
        float ny = (shiftedY + warpY) / biomeNoiseScale;
        float biomeNoise = GetFractalNoise(nx, ny, 4, 0.6f);

        float wx = (shiftedX + warpX) / temperatureWiggleScale;
        float wy = (shiftedY + warpY) / temperatureWiggleScale;
        float wiggle = (GetFractalNoise(wx, wy, 2, 0.5f) - 0.5f) * temperatureWiggleStrength;

        float latitude = Mathf.Clamp01(((worldY + (maxWorldHeight / 2f)) / maxWorldHeight) + wiggle);

        TemperatureZoneSetup currentTempZone = temperatureZones[temperatureZones.Length - 1];
        foreach (var zone in temperatureZones)
        {
            if (latitude <= zone.maxLatitudeThreshold) { currentTempZone = zone; break; }
        }

        BiomeSettings finalBiome = GetBiomeFromZone(currentTempZone, biomeNoise);

        float dnx = shiftedX / detailNoiseScale + 5000f;
        float dny = shiftedY / detailNoiseScale + 5000f;
        float detailNoise = GetFractalNoise(dnx, dny, 2, 0.5f);

        return new BiomeData { biome = finalBiome, detailNoise = detailNoise, latitude = latitude };
    }

    private BiomeSettings GetBiomeFromZone(TemperatureZoneSetup zone, float noiseValue)
    {
        if (zone.biomes == null || zone.biomes.Length == 0) return null;
        for (int i = 0; i < zone.biomes.Length; i++)
        {
            if (noiseValue <= zone.biomes[i].noiseThreshold) return zone.biomes[i];
        }
        return zone.biomes[zone.biomes.Length - 1];
    }

    private TileBase DetermineTileType(BiomeSettings biome, float detailNoise, float latitude)
    {
        if (biome == null) return null;
        foreach (var zone in temperatureZones)
        {
            float distToEdge = Mathf.Abs(latitude - zone.maxLatitudeThreshold);
            if (distToEdge < transitionBlendZone && biome.transitionTile != null)
                return biome.transitionTile;
        }
        if (detailNoise > biome.variationThreshold && biome.variationTile != null)
            return biome.variationTile;
        return biome.mainTile;
    }
}