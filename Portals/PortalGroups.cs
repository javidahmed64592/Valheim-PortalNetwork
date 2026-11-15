using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace PortalNetwork.Portals;

/// <summary>
/// Splits the portal list into sections: by biome, by distance (portals near each other), or both, so that, say, the
/// Mistlands portals in the east and in the west show up apart.
/// </summary>
internal static class PortalGroups
{
    /// <summary>One section of the list.</summary>
    /// <typeparam name="T">What each row of the list holds.</typeparam>
    internal sealed class Group<T>
    {
        /// <summary>The biome of every portal in it, or None when not grouping by biome.</summary>
        public Heightmap.Biome Biome;

        /// <summary>The middle of its portals, for its direction and distance from the player.</summary>
        public Vector3 Centre;

        public readonly List<T> Items = [];
    }

    /// <summary>Biomes in the order the game unlocks them, so their sections are always in the same place.</summary>
    private static readonly Heightmap.Biome[] BiomeOrder =
    [
        Heightmap.Biome.Meadows,
        Heightmap.Biome.BlackForest,
        Heightmap.Biome.Swamp,
        Heightmap.Biome.Mountain,
        Heightmap.Biome.Plains,
        Heightmap.Biome.Mistlands,
        Heightmap.Biome.AshLands,
        Heightmap.Biome.DeepNorth,
        Heightmap.Biome.Ocean,
    ];

    /// <summary>
    /// Groups <paramref name="items"/>. By biome, sections follow the game's biome order. By distance, portals within
    /// <paramref name="linkDistance"/> of each other share a section, and so do portals linked through others (a chain
    /// of portals around an island stays together, while the sea between islands usually splits them); the sections
    /// nearest <paramref name="origin"/> come first. With both, each biome's portals are split by distance.
    /// </summary>
    /// <typeparam name="T">What each row of the list holds.</typeparam>
    internal static List<Group<T>> Build<T>(IEnumerable<T> items, Func<T, Vector3> position, Func<Vector3, Heightmap.Biome> biomeOf,
        bool byBiome, bool byDistance, Vector3 origin, float linkDistance)
    {
        List<T> all = [.. items];
        List<Group<T>> result = [];
        if (!byBiome)
        {
            AddGroups(result, all, Heightmap.Biome.None, position, byDistance, origin, linkDistance);
            return result;
        }

        ILookup<Heightmap.Biome, T> byBiomeLookup = all.ToLookup(item => biomeOf(position(item)));
        foreach (Heightmap.Biome biome in byBiomeLookup.Select(g => g.Key).OrderBy(BiomeRank))
        {
            AddGroups(result, [.. byBiomeLookup[biome]], biome, position, byDistance, origin, linkDistance);
        }
        return result;
    }

    private static int BiomeRank(Heightmap.Biome biome)
    {
        int index = Array.IndexOf(BiomeOrder, biome);
        return index >= 0 ? index : BiomeOrder.Length;
    }

    private static void AddGroups<T>(List<Group<T>> result, List<T> items, Heightmap.Biome biome, Func<T, Vector3> position,
        bool byDistance, Vector3 origin, float linkDistance)
    {
        if (items.Count == 0)
        {
            return;
        }

        if (!byDistance)
        {
            Group<T> group = new() { Biome = biome };
            group.Items.AddRange(items);
            group.Centre = Centre(group.Items, position);
            result.Add(group);
            return;
        }

        List<Vector3> points = new(items.Count);
        foreach (T item in items)
        {
            points.Add(position(item));
        }

        List<Group<T>> clusters = [];
        foreach (List<int> cluster in Clusters(points, linkDistance))
        {
            Group<T> group = new() { Biome = biome };
            group.Items.AddRange(cluster.Select(i => items[i]));
            group.Centre = Centre(group.Items, position);
            clusters.Add(group);
        }
        result.AddRange(clusters.OrderBy(g => Utils.DistanceXZ(g.Centre, origin)));
    }

    /// <summary>
    /// Indices of points linked by steps of at most <paramref name="linkDistance"/> (measured flat), one list per
    /// cluster. Every pair is compared, which is quick for the few hundred portals a world has.
    /// </summary>
    internal static List<List<int>> Clusters(IReadOnlyList<Vector3> points, float linkDistance)
    {
        int[] parent = [.. Enumerable.Range(0, points.Count)];
        float limit = linkDistance * linkDistance;
        for (int i = 0; i < points.Count; i++)
        {
            for (int j = i + 1; j < points.Count; j++)
            {
                float dx = points[i].x - points[j].x;
                float dz = points[i].z - points[j].z;
                if ((dx * dx) + (dz * dz) <= limit)
                {
                    parent[Find(parent, i)] = Find(parent, j);
                }
            }
        }

        Dictionary<int, List<int>> clusters = [];
        for (int i = 0; i < points.Count; i++)
        {
            int root = Find(parent, i);
            if (!clusters.TryGetValue(root, out List<int> members))
            {
                members = [];
                clusters[root] = members;
            }
            members.Add(i);
        }
        return [.. clusters.Values];
    }

    private static int Find(int[] parent, int i)
    {
        while (parent[i] != i)
        {
            parent[i] = parent[parent[i]];
            i = parent[i];
        }
        return i;
    }

    private static Vector3 Centre<T>(List<T> items, Func<T, Vector3> position)
    {
        Vector3 sum = Vector3.zero;
        foreach (T item in items)
        {
            sum += position(item);
        }
        return sum / items.Count;
    }

    /// <summary>The compass direction from <paramref name="from"/> to <paramref name="to"/> as a translation token.</summary>
    internal static string DirectionToken(Vector3 from, Vector3 to)
    {
        string[] tokens =
        [
            "$portalnetwork_dir_n", "$portalnetwork_dir_ne", "$portalnetwork_dir_e", "$portalnetwork_dir_se",
            "$portalnetwork_dir_s", "$portalnetwork_dir_sw", "$portalnetwork_dir_w", "$portalnetwork_dir_nw",
        ];
        float angle = Mathf.Atan2(to.x - from.x, to.z - from.z) * Mathf.Rad2Deg; // 0 = north (+z), 90 = east (+x)
        int index = Mathf.RoundToInt(Mathf.Repeat(angle, 360f) / 45f) % 8;
        return tokens[index];
    }
}
