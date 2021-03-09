﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SegmentSpawner : MonoBehaviour
{
    [SerializeField] Vector3 startSpawnPosition;

    [SerializeField] float spawnAheadDistance = 200;
    [SerializeField] float segmentLength = 75;
    [SerializeField] float archHeight = 3;
    [SerializeField] float archSpacing = 3;
    [SerializeField] float coinSpacing = 4;

    private int numSegmentsSpawned = 0;
    private bool isSpawning;

    private GameManager gameManager;
    private ResourceManager resourceManager;
    private VehiclePhysicsController vehiclePhysicsController;

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            throw new System.Exception($"Unable to find object of type {nameof(GameManager)}");
        }

        resourceManager = FindObjectOfType<ResourceManager>();
        if (resourceManager == null)
        {
            throw new System.Exception($"Unable to find object of type {nameof(ResourceManager)}");
        }

        vehiclePhysicsController = FindObjectOfType<VehiclePhysicsController>();
        if (vehiclePhysicsController == null)
        {
            throw new System.Exception($"Unable to find object of type {nameof(VehiclePhysicsController)}");
        }

        isSpawning = true;
    }

    void Update()
    {
        if (vehiclePhysicsController == null)
        {
            return;
        }

        // TODO: This shouldn't be here
        // The spawner should be notified when and what and where to spawn by the game session, maybe?
        Vector3 playerCurrentPosition = vehiclePhysicsController.transform.position;

        // TODO: This shouldn't be done here, but it's the most convenient place for now
        // because it is where segment length is defined
        int numSegmentsCleared = (int)((playerCurrentPosition.z - startSpawnPosition.z) / segmentLength);
        gameManager.SetTowers(numSegmentsCleared);

        if (isSpawning)
        {
            Vector3 nextSegmentPosition = startSpawnPosition + Vector3.forward * numSegmentsSpawned * segmentLength;

            if (nextSegmentPosition.z - spawnAheadDistance < playerCurrentPosition.z)
            {
                SpawnSegment(nextSegmentPosition);
            }
        }
    }

    void SpawnSegment(Vector3 segmentPosition)
    {
        // Difficulty progression (for example)
        // - level 1, towers are simple, with single depth
        // - level 2, towers are simple, with multiple depth
        // - level 3, towers can be extra tall, but must have a gap on the bottom layer
        // - level 4, towers can have gaps anywhere, with single depth
        // - level 5, towers can have gaps anywhere, with multiple depth
        // - level 6, lighting changes to make it harder to see
        // - level 7, speed increases (might need to affect other things like segment length, gap height, etc.)

        int difficultyLevel = GetDifficultyLevel();

        SpawnRoad(difficultyLevel, segmentPosition);
        SpawnBuildings(difficultyLevel, segmentPosition);

        if (numSegmentsSpawned > 0)
        {
            SpawnTowers(difficultyLevel, segmentPosition);
            SpawnCoins(segmentPosition);
        }

        numSegmentsSpawned++;
    }

    void SpawnRoad(int difficultyLevel, Vector3 segmentPosition)
    {
        GameObject road = resourceManager.GetOrCreateRoad();
        road.transform.position = segmentPosition;
    }

    void SpawnBuildings(int difficultyLevel, Vector3 segmentPosition)
    {
        const int numBuildings = 5;
        const float buildingWidth = 8;
        const float buildingDepth = 8;
        const float buildingGap = 2;
        const float buildingSetback = 10;

        for (int i = 0; i < numBuildings; i++)
        {
            float buildingHeight = Random.Range(8, 20);

            GameObject building = resourceManager.GetOrCreateBuilding();
            building.transform.position = new Vector3(buildingSetback, buildingHeight / 2, segmentPosition.z + (buildingDepth + buildingGap) * i);
            building.transform.localScale = new Vector3(buildingWidth, buildingHeight, buildingDepth);
        }
    }

    void SpawnTowers(int difficultyLevel, Vector3 segmentPosition)
    {
        int towerType = 0;

        if (difficultyLevel < 3)
        {
            // First three towers are predictable
            towerType = difficultyLevel;
        }
        else
        {
            int numTowerTypes = 6;

            int lowerTowerType = 1;
            int upperTowerType = Mathf.Min(difficultyLevel + 1, numTowerTypes);

            // Use power of 2 to try and weight the harder towers in the higher difficulty levels
            towerType = (int)Mathf.Sqrt(Random.Range(lowerTowerType, Mathf.Pow(upperTowerType, 2)));
        }

        //Debug.Log("Difficulty: " + difficultyLevel + " Tower: " + towerType);

        if (towerType < 3)
        {
            // Simple towers of various height
            // Must jump over them

            int numArchesUp;
            if (towerType == 0)
            {
                numArchesUp = 1;
            }
            else if (towerType == 1)
            {
                numArchesUp = 2;
            }
            else if (towerType == 2)
            {
                numArchesUp = 3;
            }
            else
            {
                numArchesUp = (int)(Random.value * 4);
            }

            for (int y = 0; y < numArchesUp; y++)
            {
                GameObject arch = resourceManager.GetOrCreateArch3();
                arch.transform.position = segmentPosition + (Vector3.up * y * archHeight);
            }
        }
        else if (towerType == 3)
        {
            // Simple towers of various height, multiple towers deep
            // Must jump over them

            int numArchesUp = Random.Range(1, 4);

            // Maybe impossible to make this jump otherwise
            int maxArchesDeep = 4;
            if (numArchesUp == 3)
            {
                maxArchesDeep = 3;
            }

            int numArchesDeep = Random.Range(1, maxArchesDeep + 1);

            for (int z = 0; z < numArchesDeep; z++)
            {
                for (int y = 0; y < numArchesUp; y++)
                {
                    GameObject arch = resourceManager.GetOrCreateArch3();
                    arch.transform.position = segmentPosition + (Vector3.up * y * archHeight) + (Vector3.forward * z * archSpacing);
                }
            }
        }
        else if (towerType == 4)
        {
            // Possibly taller towers, with gap on the bottom layer
            // Must drive through them

            int numArchesUp = Random.Range(3, 6);

            for (int y = 0; y < numArchesUp; y++)
            {
                GameObject arch = null;
                if (y == 0)
                {
                    // Can drive through
                    arch = resourceManager.GetOrCreateArch2();
                }
                else
                {
                    // Must fly over
                    arch = resourceManager.GetOrCreateArch3();
                }

                arch.transform.position = segmentPosition + (Vector3.up * y * archHeight);
            }
        }
        else if (towerType >= 5)
        {
            // Possibly taller towers, with gap on layers above the ground
            // Must fly through these

            bool isArch1Open = false;
            bool hasOpening = false;

            int numArchesUp = Random.Range(2, 6);

            for (int y = 0; y < numArchesUp; y++)
            {
                GameObject arch = null;
                if (y == 0)
                {
                    // No driving through
                    arch = resourceManager.GetOrCreateArch3();
                }
                else
                {
                    if (isArch1Open)
                    {
                        // Always spawn Arch1 and Arch2 together
                        arch = resourceManager.GetOrCreateArch2();
                        isArch1Open = false;
                    }
                    else
                    {
                        int numArchesRemaining = numArchesUp - y;

                        // Ensure the tower has an opening
                        bool openArch1 = (numArchesRemaining > 2 && Random.value > 0.5f) || (false == hasOpening && numArchesUp > 3);
                        if (openArch1)
                        {
                            arch = resourceManager.GetOrCreateArch1();
                            isArch1Open = true;
                            hasOpening = true;
                        }
                        else
                        {
                            arch = resourceManager.GetOrCreateArch3();
                        }
                    }
                }

                arch.transform.position = segmentPosition + (Vector3.up * y * archHeight);
            }
        }
    }

    void SpawnCoins(Vector3 segmentPosition)
    {
        float firstCoinZ = segmentPosition.z + segmentLength / 4;
        float lastCoinZ = segmentPosition.z + segmentLength * 3 / 4;

        for (float z = firstCoinZ; z <= lastCoinZ; z += coinSpacing)
        {
            GameObject coin = resourceManager.GetOrCreateCoin();
            coin.transform.position = new Vector3(segmentPosition.x, segmentPosition.y, z);

            // Ensure coin animations remain synchronized
            coin.transform.rotation = Quaternion.identity;
        }
    }

    int GetDifficultyLevel()
    {
        if (numSegmentsSpawned < 2)
        {
            return 0;
        }
        else if (numSegmentsSpawned < 3)
        {
            return 1;
        }
        else if (numSegmentsSpawned < 4)
        {
            return 2;
        }
        else if (numSegmentsSpawned < 7)
        {
            return 3;
        }
        else if (numSegmentsSpawned < 10)
        {
            return 4;
        }
        else if (numSegmentsSpawned < 15)
        {
            return 5;
        }
        else if (numSegmentsSpawned < 20)
        {
            return 6;
        }
        else
        {
            return 7;
        }
    }
}
