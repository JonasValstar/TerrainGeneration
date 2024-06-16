using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BiomeScript : MonoBehaviour
{
    // variables
    int areaSize = 160;
    int maxDiv = 5;

    float maxHeight = 10;
    float minHeight = 0;

    public float diffConstant = 0.75f;

    public GameObject cube;
    public GameObject container;
    public Material[] colors = new Material[14];

    int res; // resolution of the current map
    float[,] oldMap;
    float[,] newMap;

    // Start is called before the first frame update
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G)) {
            GenerateMap();
        }
    }

    // Update is called once per frame
    void GenerateMap()
    {
        // setting up
        int res = 160 / Mathf.RoundToInt(Mathf.Pow(2, maxDiv));
        newMap = new float[res, res];
        
        // making first map
        for (int m = 0; m < res; m++) {
            for (int n = 0; n < res; n++) {
                newMap[m,n] = Random.Range(minHeight, maxHeight);
            }
        }

        // loop through all maps
        for (int i = 0; i < maxDiv; i++) {

            // setting maps up
            res *= 2;
            oldMap = newMap;
            newMap = new float[res,res];

            // looping all spaces
            for (int m = 0; m < res; m++) {
                for (int n = 0; n < res; n++) {
                    Debug.Log(m + ", " + n);
                    if (m % 2 == 0) {
                        if (n % 2 == 0) { // found position we already know
                            newMap[m,n] = oldMap[m/2, n/2];
                        } else { // compare to space above and below
                            if (n != res - 1) { // last space protection
                                float average = (oldMap[m/2, Mathf.FloorToInt(n/2f)] + oldMap[m/2, Mathf.CeilToInt(n/2f)]) / 2;
                                float diff = Mathf.Abs(oldMap[m/2, Mathf.FloorToInt(n/2f)] - oldMap[m/2, Mathf.CeilToInt(n/2f)]);
                                average = average + Random.Range(-diff * diffConstant, diff * diffConstant);
                                newMap[m, n] = average;
                            } else {
                                newMap[m, n] = oldMap[m/2, Mathf.FloorToInt(n/2f)] + Random.Range(minHeight - oldMap[m/2, Mathf.FloorToInt(n/2f)], maxHeight - oldMap[m/2, Mathf.FloorToInt(n/2)]) / 10;
                            }
                        }
                    } else {
                        if (n % 2 == 0) { // compare space to left and right
                            if (m != res - 1) { // last space protection
                                float average = (oldMap[Mathf.FloorToInt(m/2f), n/2] + oldMap[Mathf.CeilToInt(m/2f), n/2]) / 2;
                                float diff = Mathf.Abs(oldMap[Mathf.FloorToInt(m/2f), n/2] - oldMap[Mathf.CeilToInt(m/2f), n/2]);
                                average = average + Random.Range(-diff * diffConstant, diff * diffConstant);
                                newMap[m, n] = average;
                            } else {
                                newMap[m, n] = oldMap[Mathf.FloorToInt(n/2f), n/2] + Random.Range(minHeight - oldMap[Mathf.FloorToInt(n/2f), n/2], maxHeight - oldMap[Mathf.FloorToInt(n/2), n/2]) / 10;
                            }
                        } else { // cross section found
                            List<float> averageList = new List<float>();
                            averageList.Add(oldMap[Mathf.FloorToInt(m/2f), Mathf.FloorToInt(n/2f)]);
                            if (m != res - 1) { averageList.Add(oldMap[Mathf.CeilToInt(m/2f), Mathf.FloorToInt(n/2f)]); }
                            if (n != res - 1) { averageList.Add(oldMap[Mathf.FloorToInt(m/2f), Mathf.CeilToInt(n/2f)]); }
                            if (m != res - 1 && n != res - 1) { averageList.Add(oldMap[Mathf.CeilToInt(m/2f), Mathf.CeilToInt(n/2f)]); }

                            if (averageList.Count > 1) {
                                float diff = Mathf.Max(averageList.ToArray());
                                newMap[m, n] = averageList.Average() + Random.Range(-diff * diffConstant, diff * diffConstant);
                            } else {
                                newMap[m, n] = averageList[0] + Random.Range(minHeight - averageList[0], maxHeight - averageList[0]) / 10;
                            }
                        }
                    }

                    // check if within limits
                    if (newMap[m, n] < minHeight) { newMap[m, n] = minHeight; }
                    else if (newMap[m, n] > maxHeight) { newMap[m, n] = maxHeight; }
                }
            }
        }

        // placing the points
        PlaceMap();
    }

    void PlaceMap()
    {
        Destroy(GameObject.Find("container"));
        GameObject cubeContainer = Instantiate(container, Vector3.zero, Quaternion.identity);
        cubeContainer.name = "container";

        for (int m = 0; m < areaSize; m++) {
            for (int n = 0; n < areaSize; n++) {
                GameObject point = Instantiate(cube, cubeContainer.transform);
                point.transform.position = new Vector3(m, newMap[m,n], n);
                Debug.Log(Mathf.RoundToInt(newMap[m,n] / (maxHeight-minHeight) * colors.Length) - 1);
                point.GetComponent<MeshRenderer>().material = colors[Mathf.RoundToInt(newMap[m,n] / (maxHeight-minHeight) * (colors.Length - 1))];
            }
        }
    }
}
