using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PG_Map : MonoBehaviour
{
    [Header("Map Settings")]
    [SerializeField] private int numLevel = 15;
    [SerializeField] private int maxWidth = 6;

    [Header("Spacing")]
    [SerializeField] private float xSpacing = 2.5f;
    [SerializeField] private float ySpacing = 2.5f;

    [Header("Room Probabilities")]
    [SerializeField][Range(0, 1)] private float shopChance = 0.2f;
    [SerializeField][Range(0, 1)] private float specialChance = 0.4f;

    [Header("Expansion Settings")]
    [SerializeField][Range(0, 1)] private float branchChance = 0.3f;

    [Header("Shrink Settings")]
    [SerializeField] private float shrinkExponent = 2f;

    [Header("Node Prefabs")]
    [SerializeField] private GameObject combatPrefab;
    [SerializeField] private GameObject shopPrefab;
    [SerializeField] private GameObject specialPrefab;
    [SerializeField] private GameObject bossPrefab;

    private List<List<Node>> map = new List<List<Node>>();

    public enum RoomType { Combat, Shop, Special, Boss }

    public class Node
    {
        public int layer;
        public RoomType type;
        public List<Node> nextNodes = new List<Node>();
        public Vector3 worldPos;

        public Node(int l, RoomType t)
        {
            layer = l;
            type = t;
        }
    }

    void Start()
    {
        GenerateMap();
        DrawMap();
    }

void GenerateMap()
{
    map.Clear();

    map.Add(new List<Node>());
    map[0].Add(new Node(0, RoomType.Combat));

    bool shrinkStarted = false;
    int shrinkStartLevel = -1;

    for (int level = 1; level < numLevel; level++)
    {
        List<Node> prevLayer = map[level - 1];
        List<Node> newLayer = new List<Node>();

        bool isLast = level == numLevel - 1;

        // ===== 最后一层强制Boss =====
        if (isLast)
        {
            Node boss = new Node(level, RoomType.Boss);
            newLayer.Add(boss);

            foreach (var node in prevLayer)
                node.nextNodes.Add(boss);

            map.Add(newLayer);
            continue;
        }

        int prevCount = prevLayer.Count;

        // ===== 判断是否开始收缩 =====
        if (!shrinkStarted && prevCount >= maxWidth)
        {
            shrinkStarted = true;
            shrinkStartLevel = level;
        }

        float shrinkChance = 0f;

        if (shrinkStarted)
        {
            float progress = (float)(level - shrinkStartLevel)
                             / (numLevel - 1 - shrinkStartLevel);

            progress = Mathf.Clamp01(progress);
            shrinkChance = Mathf.Pow(progress, shrinkExponent) + branchChance;
        }

        // =============================
        // ===== 不收缩（扩张阶段） =====
        // =============================
        if (!shrinkStarted)
        {
            int prevNodeCount = prevLayer.Count;
            int remainingWidth = maxWidth;

            // 生成下一层节点（正常扩张逻辑）
            for (int i = 0; i < prevNodeCount && remainingWidth > 0; i++)
            {
                int branchCount = (level == 1) ? 2 :
                    (Random.value < branchChance ? 2 : 1);

                branchCount = Mathf.Min(branchCount, remainingWidth);
                remainingWidth -= branchCount;

                List<Node> children = new List<Node>();

                for (int j = 0; j < branchCount; j++)
                {
                    Node child = new Node(level, RandomRoom());
                    newLayer.Add(child);
                    children.Add(child);

                    prevLayer[i].nextNodes.Add(child);
                }
            }

            // 仅当上一层节点数 = 下一层节点数时，才额外连接左右相邻节点
            if (prevLayer.Count == newLayer.Count)
            {
                for (int i = 0; i < prevLayer.Count; i++)
                {
                    if (i - 1 >= 0 && Random.value < branchChance)
                        prevLayer[i].nextNodes.Add(newLayer[i - 1]);
                    if (i + 1 < newLayer.Count && Random.value < branchChance)
                        prevLayer[i].nextNodes.Add(newLayer[i + 1]);
                }
            }
        }
        // =============================
        // ===== 收缩阶段（指数） =====
        // =============================
        else
        {
            for (int i = 0; i < prevCount; i++)
            {
                bool shouldMerge =
                    i < prevCount - 1 &&
                    Random.value < shrinkChance;

                if (shouldMerge)
                {
                    Node merged = new Node(level, RandomRoom());
                    newLayer.Add(merged);

                    prevLayer[i].nextNodes.Add(merged);
                    prevLayer[i + 1].nextNodes.Add(merged);

                    i++;
                }
                else
                {
                    Node child = new Node(level, RandomRoom());
                    newLayer.Add(child);
                    prevLayer[i].nextNodes.Add(child);
                }
            }
        }

        map.Add(newLayer);
    }
}

    RoomType RandomRoom()
    {
        float roll = Random.value;

        if (roll < shopChance)
            return RoomType.Shop;

        if (roll < shopChance + specialChance)
            return RoomType.Special;

        return RoomType.Combat;
    }

    void DrawMap()
    {
        foreach (var layer in map)
        {
            float offset = (layer.Count - 1) * xSpacing / 2f;

            for (int i = 0; i < layer.Count; i++)
            {
                Vector3 pos = new Vector3(
                    i * xSpacing - offset,
                    -layer[0].layer * ySpacing,
                    0);

                layer[i].worldPos = pos;
                Instantiate(GetPrefab(layer[i].type), pos, Quaternion.identity, transform);
            }
        }

        DrawConnections();
    }

    GameObject GetPrefab(RoomType type)
    {
        switch (type)
        {
            case RoomType.Combat: return combatPrefab;
            case RoomType.Shop: return shopPrefab;
            case RoomType.Special: return specialPrefab;
            case RoomType.Boss: return bossPrefab;
        }
        return combatPrefab;
    }

    void DrawConnections()
    {
        foreach (var layer in map)
        {
            foreach (var node in layer)
            {
                foreach (var next in node.nextNodes)
                {
                    CreateLine(node.worldPos, next.worldPos);
                }
            }
        }
    }

    void CreateLine(Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject("Line");
        lineObj.transform.parent = transform;

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
    }
}