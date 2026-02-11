using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PG_Map : MonoBehaviour
{
    
    [Header("Map Settings")]
    [SerializeField] private int numLevel = 15;
    [SerializeField] private int maxShop = 3;

    private List<List<Node>> map = new List<List<Node>>();
    private int shopCount = 0;

    public enum RoomType
    {
        Combat,
        Shop,
        Special,
        Boss
    }

    public class Node
    {
        public int layer;
        public RoomType type;

        public Node(int l, RoomType t)
        {
            layer = l;
            type = t;
        }
    }

    void Start()
    {
        GenerateMap();
        PrintMap();
    }

    void GenerateMap()
    {
        map.Clear();
        shopCount = 0;

        // 第0层
        map.Add(new List<Node>());
        map[0].Add(new Node(0, RoomType.Combat));

        for (int level = 1; level < numLevel; level++)
        {
            List<Node> prevLayer = map[level - 1];
            List<Node> newLayer = new List<Node>();

            bool isExpansionPhase = level < numLevel / 2;

            int targetCount = prevLayer.Count;

            // -------- 扩张阶段 --------
            if (isExpansionPhase)
            {
                if (level == 1)
                {
                    targetCount = 2; // 强制分叉
                }
                else
                {
                    if (Random.value > 0.5f)
                        targetCount += 1;

                    if (targetCount < 2)
                        targetCount = 2;
                }
            }
            // -------- 收缩阶段 --------
            else
            {
                float shrinkChance = (float)(level - numLevel / 2) / (numLevel / 2);

                if (Random.value < shrinkChance)
                    targetCount -= 1;

                if (level == numLevel - 1)
                    targetCount = 1; // 最后一层Boss
            }

            targetCount = Mathf.Clamp(targetCount, 1, 6);

            // 生成节点
            for (int i = 0; i < targetCount; i++)
            {
                RoomType type = RollRoomType(level);
                newLayer.Add(new Node(level, type));
            }

            map.Add(newLayer);
        }

        EnsureShopGuarantee();
    }

    RoomType RollRoomType(int level)
    {
        if (level == numLevel - 1)
            return RoomType.Boss;

        float shopChance = 0.1f;
        float specialChance = 0.15f;

        if (level >= numLevel / 2 && shopCount == 0)
            shopChance += 0.2f;

        if (shopCount >= maxShop)
            shopChance = 0;

        float roll = Random.value;

        if (roll < shopChance)
        {
            shopCount++;
            return RoomType.Shop;
        }

        if (roll < shopChance + specialChance)
            return RoomType.Special;

        return RoomType.Combat;
    }

    void EnsureShopGuarantee()
    {
        if (shopCount > 0)
            return;

        int level = numLevel - 2;

        var layer = map[level];

        if (layer.Count > 0)
        {
            layer[0].type = RoomType.Shop;
            shopCount++;
        }
    }

    void PrintMap()
    {
        for (int i = 0; i < map.Count; i++)
        {
            string line = "Layer " + i + ": ";

            foreach (var node in map[i])
            {
                line += node.type.ToString() + " ";
            }

            Debug.Log(line);
        }
    }
}
