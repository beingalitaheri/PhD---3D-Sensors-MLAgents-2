using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubePrefabGenerator : MonoBehaviour
{
    public GameObject[] prefabs; // Array of prefabs to instantiate
    public int gridSize = 3;     // Size of the grid
    public float spacing = 1.5f; // Spacing between prefabs

    // List to hold generated prefab instances
    [HideInInspector]
    public List<GameObject> generatedPrefabs = new List<GameObject>();

    void Start()
    {
        GenerateCube();
        //AssignTargetsToAgents();
    }

    void GenerateCube()
    {
        GameObject parentObject = new GameObject("PrefabCube"); // Parent object for organization

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    int randomIndex = Random.Range(0, prefabs.Length); // Select a random prefab
                    GameObject selectedPrefab = prefabs[randomIndex];

                    Vector3 position = new Vector3(x * spacing, y * spacing, z * spacing); // Calculate position
                    GameObject newPrefab = Instantiate(selectedPrefab, position, Quaternion.identity); // Instantiate prefab
                    newPrefab.transform.SetParent(parentObject.transform); // Set parent for organization

                    // Add generated prefab to the list
                    generatedPrefabs.Add(newPrefab);

                    // Ensure the prefab has the "Target" tag
                    if (!newPrefab.CompareTag("Target"))
                    {
                        newPrefab.tag = "Target";
                    }
                }
            }
        }
    }

    /*void AssignTargetsToAgents()
    {
        // Find all JetAgents in the scene
        JetAgent[] agents = FindObjectsOfType<JetAgent>();

        // Assign the list of generated targets to each agent
        foreach (JetAgent agent in agents)
        {
            agent.AssignTargets(generatedPrefabs);
        }
    }*/
}
