using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 참고 링크
/// https://itmining.tistory.com/66
/// http://greenday96.blogspot.com/2018/10/c-4-aa-star-simple-4-way-algorithm.html
/// </summary>
/// 
[System.Serializable]
public class Node
{
    public Node Parent;
    public bool Moveable;
    public int F; // F = H + G
    public int G; // 도착노드까지의 예상비용
    public int H; // 새로운 노드까지의 거리

    public int X;
    public int Y;

    public Vector2 Position
    {
        get
        {
            return new Vector2(X, Y);
        }
    }

    public Node(float x, float y)
    {
        X = (int)x;
        Y = (int)y;
        Moveable = true;
    }

    public void CalcCost(Node destination, int g)
    {
        GetH(destination);
        G = g;
        F = G + H;
    }

    void GetH(Node destination)
    {
        int diffX = Mathf.Abs(destination.X - X);
        int diffY = Mathf.Abs(destination.Y - Y);
        H = (diffX + diffY) * 10;
    }
}

public class AStar
{
    public List<Node> OpenList;
    public List<Node> ClosedList;

    Node[,] Map;

    Node StartNode, Destination;
    int newG;
    bool success;

    /// <summary>
    /// 각 노드의 정보 ex) 건물 여부
    /// </summary>
    public void LoadMapData(Node[,] map)
    {
        Map = map;
    }

    public List<Node> FindPath(Node startNode, Node destination)
    {
        StartNode = startNode;
        Destination = destination;

        Init();
        return AStar_Dir4();
    }

    void Init()
    {
        newG = 0;
        success = false;

        OpenList = new List<Node>();
        ClosedList = new List<Node>();
    }

    /// <summary>
    /// 탐색한 경로 전달
    /// </summary>
    List<Node> AStar_Dir4()
    {
        OpenList.Add(StartNode);
        Node CurrentNode = StartNode;
        StartNode.Parent = null;

        while (OpenList.Count > 0)
        {
            //if (CurrentNode.X == Destination.X && CurrentNode.Y == Destination.Y)
            if (CurrentNode == Destination)
            {
                success = true; break;
            }

            var adjacents = FindAdjacentNodes(CurrentNode);
            newG = CurrentNode.G + 10;


            foreach (var adj in adjacents)
            {
                //if (ClosedList.Contains(adj))
                //    continue;

                if (OpenList.Contains(adj))
                {
                    // better case
                    if (newG < adj.G)
                    {
                        adj.CalcCost(Destination, newG);
                    }
                }
                else
                { 
                    adj.CalcCost(Destination, newG);
                    adj.Parent = CurrentNode;

                    OpenList.Insert(0, adj); // 우선체크
                }
            }

            var best = OpenList.Min(n => n.F);
            CurrentNode = OpenList.First(n => n.F == best);

            ClosedList.Add(CurrentNode);
            OpenList.Remove(CurrentNode);
        }

        Stack<Node> bestPath = new Stack<Node>();
        while (CurrentNode != null)
        {
            bestPath .Push(CurrentNode);
            CurrentNode = CurrentNode.Parent;
        }

        return success ? bestPath : null;
    }

    List<Node> FindAdjacentNodes(Node currentNode)
    {
        int x = currentNode.X;
        int y = currentNode.Y;

        Node[] adjArray = new Node[4];
        adjArray[0] = AddAdjacent(x+1, y);
        adjArray[1] = AddAdjacent(x, y-1);
        adjArray[2] = AddAdjacent(x-1, y);
        adjArray[3] = AddAdjacent(x, y+1);

        List<Node> results = new List<Node>();
        for (int i = 0; i < adjArray.Length; i++)
        {
            if (adjArray[i] != null)
                results.Add(adjArray[i]);
        }

        return results;
    }

    Node AddAdjacent(int x, int y)
    {
        var adjacent = Map[x, y];
        if (adjacent == null || ClosedList.Contains(adjacent) || adjacent.Moveable == false)
            return null;

        return adjacent;
    }
}

