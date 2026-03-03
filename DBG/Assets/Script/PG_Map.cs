using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PG_Map : MonoBehaviour
{
    [Header("Map Settings")]
    [SerializeField] private int numLevel = 15;
    [SerializeField] private int maxShop = 3;
    [SerializeField] private int maxWidth = 6;

    public List<List<Node>> map = new List<List<Node>>();
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

        public List<Node> nextNodes = new List<Node>();
        public List<Node> prevNodes = new List<Node>();

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

            bool isExpansion = level < numLevel / 2;

            // ---------- 最后一层 Boss ----------
            if (level == numLevel - 1)
            {
                Node boss = new Node(level, RoomType.Boss);
                newLayer.Add(boss);

                foreach (var node in prevLayer)
                {
                    node.nextNodes.Add(boss);
                    boss.prevNodes.Add(node);
                }

                map.Add(newLayer);
                continue;
            }

            // ---------- 扩张阶段 ----------
            if (isExpansion)
            {
                foreach (var node in prevLayer)
                {
                    // 第一层强制分叉
                    int branchCount = (level == 1) ? 2 :
                        (Random.value > 0.5f ? 2 : 1);

                    for (int i = 0; i < branchCount; i++)
                    {
                        if (newLayer.Count >= maxWidth)
                            break;

                        Node child = new Node(level, RollRoomType(level));

                        node.nextNodes.Add(child);
                        child.prevNodes.Add(node);

                        newLayer.Add(child);
                    }
                }
            }
            // ---------- 收缩阶段 ----------
            else
            {
                for (int i = 0; i < prevLayer.Count; i += 2)
                {
                    Node merged = new Node(level, RollRoomType(level));
                    newLayer.Add(merged);

                    // 当前节点连接
                    prevLayer[i].nextNodes.Add(merged);
                    merged.prevNodes.Add(prevLayer[i]);

                    // 相邻节点合并
                    if (i + 1 < prevLayer.Count)
                    {
                        prevLayer[i + 1].nextNodes.Add(merged);
                        merged.prevNodes.Add(prevLayer[i + 1]);
                    }
                }
            }

            map.Add(newLayer);
        }

        EnsureShopGuarantee();
    }

    RoomType RollRoomType(int level)
    {
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
                line += node.type + "(" + node.nextNodes.Count + ") ";
            }

            Debug.Log(line);
        }
    }
}