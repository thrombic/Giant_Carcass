using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Giant Carcass/Loot/Drop Table")]
public sealed class DropTable : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public GameObject prefab;
        [Range(0f, 100f)] public float chancePercent;
        public Vector2 spawnOffset;
    }

    public Entry[] entries;

    public GameObject TrySpawnDrop(Vector3 position)
    {
        float roll = UnityEngine.Random.Range(0f, 100f);
        float cumulativeChance = 0f;

        if (entries == null)
            return null;

        foreach (Entry entry in entries)
        {
            if (entry == null || entry.prefab == null)
                continue;

            cumulativeChance += Mathf.Max(0f, entry.chancePercent);
            if (roll < cumulativeChance)
                return Instantiate(entry.prefab, position + (Vector3)entry.spawnOffset, Quaternion.identity);
        }

        return null;
    }
}
