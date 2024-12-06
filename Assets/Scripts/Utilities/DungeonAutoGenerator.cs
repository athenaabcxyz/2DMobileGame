using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DungeonAutoGenerator : MonoBehaviour
{
    public static void GenerateMap(Dictionary<string, int> roomCounts, RoomNodeGraphSO roomNodeGraph, RoomNodeTypeListSO roomNodeTypeList)
    {
        
    }

    private static Vector2 GetRandomPosition(List<RoomNodeSO> nodes)
    {
        // Generate a random position avoiding overlap
        Vector2 position;
        bool valid;
        do
        {
            valid = true;
            position = new Vector2(Random.Range(-500, 500), Random.Range(-500, 500));
            foreach (var node in nodes)
            {
                if (Vector2.Distance(position, node.rect.position) < 200)
                {
                    valid = false;
                    break;
                }
            }
        } while (!valid);

        return position;
    }

    private static RoomNodeSO CreateRoomNode(Vector2 position, RoomNodeGraphSO graph, RoomNodeTypeSO nodeType)
    {
        RoomNodeSO roomNode = ScriptableObject.CreateInstance<RoomNodeSO>();
        graph.roomNodeList.Add(roomNode);
        roomNode.Initialise(new Rect(position, new Vector2(160f, 75f)), graph, nodeType);
        AssetDatabase.AddObjectToAsset(roomNode, graph);
        return roomNode;
    }

    private static void EnsureAllRoomsConnected(List<RoomNodeSO> nodes)
    {
        // Simple DFS or BFS to ensure all nodes are connected
        HashSet<RoomNodeSO> visited = new HashSet<RoomNodeSO>();
        Queue<RoomNodeSO> queue = new Queue<RoomNodeSO>();

        queue.Enqueue(nodes[0]);
        visited.Add(nodes[0]);

        while (queue.Count > 0)
        {
            RoomNodeSO node = queue.Dequeue();
            foreach (string childID in node.childRoomNodeIDList)
            {
                RoomNodeSO childNode = nodes.Find(n => n.id == childID);
                if (!visited.Contains(childNode))
                {
                    visited.Add(childNode);
                    queue.Enqueue(childNode);
                }
            }
        }

        foreach (RoomNodeSO node in nodes)
        {
            if (!visited.Contains(node))
            {
                // Connect unvisited nodes
                RoomNodeSO connectedNode = nodes[Random.Range(0, visited.Count)];
                connectedNode.AddChildRoomNodeIDToRoomNode(node.id);
                node.AddParentRoomNodeIDToRoomNode(connectedNode.id);
                visited.Add(node);
            }
        }
    }
}
