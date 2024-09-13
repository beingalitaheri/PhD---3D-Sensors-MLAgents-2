using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubePrefabGenerator : MonoBehaviour
{
    public GameObject[] prefabs;
    public int gridSize = 3;
    public float spacing = 1.5f;

    void Start()
    {
        GenerateCube();
    }
    void GenerateCube()
    {
        GameObject parentObject = new GameObject("PrefabCube");
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    int randomIndex = Random.Range(0, prefabs.Length);
                    GameObject selectedPrefab = prefabs[randomIndex];

                    Vector3 position = new Vector3(x * spacing, y * spacing, z * spacing);
                    GameObject newPrefab = Instantiate(selectedPrefab, position, Quaternion.identity);

                    newPrefab.transform.SetParent(parentObject.transform);
                }
            }
        }
    }
}
