﻿using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class WorldSpawningSystem : SystemBase
{
    protected override void OnCreate()
    {
        // Create queries and static data only here.
    }
    protected override void OnUpdate()
    {
        Entities
            .WithName("TileSpawner")
            .WithStructuralChanges()
            .ForEach((Entity spawnerEntity, in InitializeWorld initializeWorld) =>
            {
                var random = new Random(1);

                var tileSpawner = GetSingleton<TileSpawner>();
                var tileDisplaySettings = GetSingleton<TileDisplaySettings>();
                var bucketSpawner = GetSingleton<BucketSpawner>();
                var bucketScale = EntityManager.GetComponentData<NonUniformScale>(bucketSpawner.Prefab);

                var numTiles = tileSpawner.XSize * tileSpawner.YSize;

                var numFires = tileSpawner.StartingFireCount > numTiles ? numTiles : tileSpawner.StartingFireCount;
                var fireTiles = new bool[numTiles];
                for (int i = 0; i < numFires;)
                {
                    var rndValue = random.NextInt(0, numTiles);
                    if (fireTiles[rndValue] == false)
                    {
                        fireTiles[rndValue] = true;
                        i++;
                    }
                }

                var bucketTiles = new bool[numTiles];
                for (int i = 0; i < bucketSpawner.TotalBuckets;)
                {
                    var rndValue = random.NextInt(0, numTiles);
                    if (bucketTiles[rndValue] == false)
                    {
                        bucketTiles[rndValue] = true;
                        i++;
                    }
                }
                var blankBuckets = EntityManager.Instantiate(bucketSpawner.Prefab, bucketSpawner.TotalBuckets, Allocator.Temp);
                var blankTiles = EntityManager.Instantiate(tileSpawner.Prefab, numTiles, Allocator.Temp);
                int bucketCount = 0;
                int tileCount = 0;
                for (int x = 0; x < tileSpawner.XSize; x++)
                {
                    for (int y = 0; y < tileSpawner.YSize; y++)
                    {
                        var yPosition = random.NextFloat(0, 0.05f) -0.5f; // Height vs. pivot of prefab. TODO: Read prefab data
                        var position = new Translation() { Value = new float3(x * tileSpawner.Scale, yPosition, y * tileSpawner.Scale) };
                        EntityManager.SetComponentData(blankTiles[tileCount], position);
                        var scale = new NonUniformScale() { Value = new float3(tileSpawner.Scale, tileDisplaySettings.FlameHeight, tileSpawner.Scale) };
                        EntityManager.SetComponentData(blankTiles[tileCount], scale);

                        var tileAuthor = new Tile();
                        tileAuthor.Id = y * tileSpawner.XSize + x;
                        EntityManager.SetComponentData(blankTiles[tileAuthor.Id], tileAuthor);

                        // Add Fire
                        // TODO: random.NextFloat(flashpoint, 1f) // Add flashpoint randomization
                        if (fireTiles[tileCount] == true)
                        {
                            var temperature = new Temperature();
                            temperature.Value = 1;
                            EntityManager.SetComponentData(blankTiles[tileCount], temperature);
                        }
                        // Add Bucket
                        if (bucketTiles[tileCount] == true)
                        {
                            var positionBucket = new Translation() { Value = new float3(x * tileSpawner.Scale, bucketScale.Value.y/2, y * tileSpawner.Scale) };
                            EntityManager.SetComponentData(blankBuckets[bucketCount], positionBucket);
                            bucketCount++;
                        }

                        tileCount++;
                    }
                }
                blankTiles.Dispose();
                blankBuckets.Dispose();
                
                EntityManager.DestroyEntity(spawnerEntity);
            }).Run();
    }
}
