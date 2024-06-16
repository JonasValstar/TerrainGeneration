using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class TerrainScript : MonoBehaviour
{
    // other GameObjects / scripts
    GameObject player;
    MovementScript movementScript;

    public Transform treeArea;

    // terrain
    public Terrain startTerrain; //? will eventually support terrain generation
    public NavMeshSurface navSurface;
    
    // Trees
    [SerializeField] GameObject treeContainer; // empty holding the trees
    [SerializeField] GameObject treeObject; // empty holding the trees
    List<Vector2> trees = new List<Vector2>(); // stores the locations of all the trees
    List<Transform> treesGameObject = new List<Transform>(); // stores the gameObject's of the trees
    List<Transform> nonActiveTrees = new List<Transform>(); // stores the de-activated trees from buildings
    public float treeHeight = 5f; //? temp variable for the height of a tree

    // Structure info
    [SerializeField] float spawnDist = 20; // distance from the player that the structure will be spawned in

    // structures
    [SerializeField] GameObject structureContainer;
    private GameObject currentStructureContainer;
    [SerializeField] GameObject extraction;
    [SerializeField] GameObject extractionArea;
    [SerializeField] GameObject rabbit;
    [SerializeField] GameObject rabbitArea;

    // all the stuff that influences map generation
    [Header("Terrain Gen Modifiers")]
    [Tooltip("resolution and max amount of half-isions of the hill map. (make sure that X / 2^Y = even whole number)")]                 
    [SerializeField] Vector2Int hillRes = new Vector2Int(160, 5);
    [Tooltip("How much the normal terrain gen and hill gen respectively influence the final result (make sure that they add up to 1)")] 
    [SerializeField] Vector2 weights = new Vector2(.5f, .5f);
    [Tooltip("the min and max height of the hill map (doesn't really change anything)")] 
    [SerializeField] Vector2 Heights = new Vector2(0, 10);
    [Tooltip("how much randomness is implemented in the hill map naturally")]
    [SerializeField] float diffConst = 0.1f;

    [Header("Tree Gen Modifiers")]
     [Tooltip("how much the origin (left bottom) moves to make sure the trees are in correct place (usually half of the size)")]
    [SerializeField] Vector2 displacement = new Vector2(-50, -50);
     [Tooltip("size of the area in which trees will be spawned")] 
    [SerializeField] Vector2 treeAreaSize = new Vector2(100, 100);
    [Tooltip("how much distance is between the trees (a random number within these bounds is chosen for each tree individually)")]
    [SerializeField] Vector2 radiusRange = new Vector2(3, 9);
    [Tooltip("how many times the script tries to spawn a new tree from an already existing tree before giving up on that tree")]
    [SerializeField] int maxTries = 20;

    [Header("Structure Gen Modifiers")] // void GenerateStructures(int type, int amount, float radius, Vector2 areaSize, float spawnRad)
    [Tooltip("Type of structure placed (will be important later)")]
    [SerializeField] int structType = 1;
    [Tooltip("how many structures of chosen type are placed")]
    [SerializeField] int structAmount = 3;
    [Tooltip("minimum distance between structures")]
    [SerializeField] float structRadius = 30;
    [Tooltip("Size of the area in which structures can be placed")]
    [SerializeField] Vector2 structAreaSize = new Vector2(100, 100);
    [Tooltip("how far away from the center the structures have to be placed at minimum")]
    [SerializeField] float spawnRadius = 15;

    // start function
    void Start()
    {
        // gather data
        player = GameObject.Find("Player");
        movementScript = player.GetComponent<MovementScript>();
    }

    //! temporary until automatic generation
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T)) {
            TempTreeSpawner();
        }
        if (Input.GetKeyDown(KeyCode.Y)) {
            GenerateTerrain(startTerrain, weights, Heights);
        }
        if (Input.GetKeyDown(KeyCode.U)) {
            if (currentStructureContainer != null) { currentStructureContainer.name = "toBeDestroyed"; }
            Destroy(currentStructureContainer);
            GenerateStructures(structType, structAmount, structRadius, structAreaSize, spawnRadius);
        }
        if (Input.GetKeyDown(KeyCode.I)) {
            startScript();
        }
        if (Input.GetKeyDown(KeyCode.O)) {
            GenerateStructures(0, 1, structRadius, structAreaSize, spawnRadius, new Vector2(player.transform.position.x, player.transform.position.z));
        }
    }

    public void startScript()
    {
        if (currentStructureContainer != null) { currentStructureContainer.name = "toBeDestroyed"; }
            Destroy(currentStructureContainer);
            GenerateTerrain(startTerrain, weights, Heights);
            TempTreeSpawner();
            GenerateStructures(structType, structAmount, structRadius, structAreaSize, spawnRadius);
            navSurface.BuildNavMesh();
    }

    void TempTreeSpawner()
    {
        Destroy(GameObject.Find("TreeContainer"));
        trees.Clear();
        treesGameObject.Clear();
        SpawnTrees(displacement, GenerateTrees(treeAreaSize, radiusRange, maxTries));
    }
    //! until here

    // function that generates a heightmap for terrain and applies it
    void GenerateTerrain(Terrain terrain, Vector2 weights, Vector2 hillHeight) //? add something to link to other terrains
    {
        // variables
        int res = terrain.terrainData.heightmapResolution;
        float[,] heightMap = new float[res, res];

        // generating the heights
        for (int m = 0; m < res; m++) {
            for (int n = 0; n < res; n++) {

                // getting the average of surrounding vertices
                float average = 0;
                List<float> averageList = new List<float>(); // list to store the floats for averaging
                if (m > 0) {        if (heightMap[m-1, n] > 0) { averageList.Add(heightMap[m-1, n]);}} // getting the amount of the neighboring vertex
                if (m < res - 1) {  if (heightMap[m+1, n] > 0) { averageList.Add(heightMap[m+1, n]);}} // getting the amount of the neighboring vertex
                if (n > 0) {        if (heightMap[m, n-1] > 0) { averageList.Add(heightMap[m, n-1]);}} // getting the amount of the neighboring vertex
                if (n < res - 1) {  if (heightMap[m, n+1] > 0) { averageList.Add(heightMap[m, n+1]);}} // getting the amount of the neighboring vertex
                if (averageList.Count != 0) {average = averageList.Average();}

                // setting the amount for the current vertex
                if (average != 0) {
                    float diffToCenter = 0.5f - average; // getting the deviation from the center 
                    heightMap[m, n] = average + Random.Range((-0.4f + diffToCenter)/10, (0.4f + diffToCenter)/10) / 2;
                } else {
                    heightMap[m, n] = Random.Range(0.1f, 0.9f);
                }

                //Debug.Log("[" + m + ", " + n + "]: " + heightMap[m, n] + " | " + res);
            }
        }

        // adding a hill map for extra height variation
        float[,] hillMap = GenerateHillMap(hillRes, hillHeight, diffConst);
        float resFloat = res;
        for (int m = 0; m < res; m++) {
            for (int n = 0; n < res; n++) {
                heightMap[m, n] = (heightMap[m, n] * weights.x) + (hillMap[Mathf.RoundToInt(m/resFloat * hillRes.x), Mathf.RoundToInt(n/resFloat * hillRes.x)] * weights.y / hillHeight.y);
            }
        }

        // applying terrain
        terrain.terrainData.SetHeights(0, 0, heightMap);
    }

    // function to spawn random points a certain distance away from each other and inside an area using something similar to Poisson Disc Sampling
    List<Vector2> GenerateTrees(Vector2 area, Vector2 radiusRange, int maxTries = 20)
    {
        // variables
        List<Vector2> points = new List<Vector2>(); // list of all the generated points
        List<Vector2> spawnPoints = new List<Vector2>(); // list of all the points still able to spawn new ones
        float radius = radiusRange.x;

        // generate first point inside area
        points.Add(new Vector2(Random.Range(0 + radius, area.x - radius), Random.Range(0 + radius, area.y - radius)));
        spawnPoints.Add(points[0]);

        // main loop generating new points
        while (spawnPoints.Count > 0) {

            // selecting which point use
            int index = Random.Range(0, spawnPoints.Count - 1);

            // setting a random range
            radius = Random.Range(radiusRange.x, radiusRange.y);

            // making amount of points equal to maxTries
            for (int i = 0; i < maxTries; i++) {

                // getting the location of the new point
                float angle = Random.Range(0f, 2f*Mathf.PI);
                Vector2 currentPoint = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                currentPoint *= radius + Random.Range(0f,radius/10f); // moving the point along the angle with a distance of the radius plus 0-10% of the radius
                currentPoint.x += spawnPoints[index].x;
                currentPoint.y += spawnPoints[index].y;
                
                // checking valid point
                if (currentPoint.x > 0 && currentPoint.x < area.x && currentPoint.y > 0 && currentPoint.y < area.y) {
                    if (CheckValidPoint(points, currentPoint, radius)) {
                        points.Add(currentPoint);
                        spawnPoints.Add(currentPoint);
                    }
                }
            }

            // removing point from spawn-able points
            spawnPoints.RemoveAt(index);
        }

        // return the list of points
        return points;
    }

    // function to check validity of a point in the tree generation script
    bool CheckValidPoint(List<Vector2> points, Vector2 currentPoint, float radius)
    {
        // checking if point is valid
        for (int i = 0; i < points.Count; i++) { // loop through all generated points
            if (points[i].x > currentPoint.x - radius && points[i].x < currentPoint.x + radius) { // check if within x bounds
                if (points[i].y > currentPoint.y - radius && points[i].y < currentPoint.y + radius) { // check if within y bounds
                    if (Mathf.Sqrt(Mathf.Pow(currentPoint.x - points[i].x, 2) + Mathf.Pow(currentPoint.y - points[i].y, 2)) <= radius) { // check precise distance
                        return false;
                    }
                }
            }
        }
        
        // if no overlap was discovered
        return true;
    }
    

    // generate hill map
    float[,] GenerateHillMap(Vector2Int areaInfo, Vector2 heights, float diffConstant = 0.1f)
    {
        // setting up
        float maxHeight = heights.y;
        float minHeight = heights.x;
        int areaSize = areaInfo.x;
        int maxDiv = areaInfo.y;
        float[,] oldMap;
        float[,] newMap;
        int res = areaSize / Mathf.RoundToInt(Mathf.Pow(2, maxDiv));
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
                    //Debug.Log(m + ", " + n);
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

                    Debug.Log(newMap[m, n]);
                }
            }
        }

        // returning hill map
        return newMap;
    }

    // function to make the generated tree points into actual trees
    void SpawnTrees(Vector2 origin, List<Vector2> points)
    {

        //Debug.Log(origin);
        // making the tree container
        GameObject container = Instantiate(treeContainer, Vector3.zero, Quaternion.identity);
        container.name = "TreeContainer";

        // looping over every point
        for (int i = 0; i < points.Count; i++) {
            GameObject tree = Instantiate(treeObject, container.transform);
            tree.transform.position = new Vector3(origin.x + points[i].x, 0, origin.y + points[i].y);
            tree.name = "Tree";
            tree.transform.position = new Vector3(tree.transform.position.x, startTerrain.SampleHeight(tree.transform.position) + treeHeight/2, tree.transform.position.z);

            // adding to lists
            trees.Add(points[i] + origin);
            //Debug.Log(trees[trees.Count - 1]);
            treesGameObject.Add(tree.transform);
        }
    }

    // function that generates the objectives
    void GenerateStructures(int type, int amount, float radius, Vector2 areaSize, float spawnRad, Vector2 spawnLocation = new Vector2())
    {
        // variables
        List<Vector2> locations = new List<Vector2>();
        Vector2 location;
        bool valid = false;

        // looping until amount reached
        for (int i = 0; i < amount; i++) {
            
            // setting up check-boolean
            valid = true;

            // generating location
            location = new Vector2(Random.Range(-areaSize.x/2, areaSize.x/2), Random.Range(-areaSize.y/2, areaSize.y/2));

            // checking location for spawn area
            if (location.x > -spawnRad + spawnLocation.x && location.x < spawnRad + spawnLocation.x && location.y > -spawnRad + spawnLocation.y && location.y < spawnRad + spawnLocation.y) {
                valid = false;
            }

            // checking if too close to other structures
            foreach (Vector2 other in locations) {
                if (Mathf.Sqrt(Mathf.Pow(other.x - location.x, 2) + Mathf.Pow(other.y - location.y, 2)) <= radius) {
                    valid = false;
                }
            }

            // resolving checks
            if (valid == true) {
                locations.Add(location);
            } else {
                i--;
            }
        }

        // returning the list
        if (type == 0) { placeStructures(locations, extraction, extractionArea); } // extraction
        else if (type == 1) { placeStructures(locations, rabbit, rabbitArea); } // rabbit type
        else if (type == 2) {} //? will have more types eventually
        
    }

    // function to apply generated structures
    void placeStructures(List<Vector2> positions, GameObject Structure, GameObject Area) //? the GameObject Area can later be replaced by a vector2. It is just for visualization
    {
        // making the structure container
        GameObject container = Instantiate(structureContainer, Vector3.zero, Quaternion.identity);
        container.name = "StructureContainer";
        currentStructureContainer = container;

        // looping through all positions
        for (int i = 0; i < positions.Count; i++) {

            // clearing the area 
            GameObject area = Instantiate(Area, GameObject.Find("StructureContainer").transform);
            area.transform.position = new Vector3(positions[i].x, 5, positions[i].y);
            area.name = "Area";
            Deforest(area.transform);

            // placing the structure
            GameObject structure = Instantiate(Structure, GameObject.Find("StructureContainer").transform);
            structure.transform.position = new Vector3(positions[i].x, startTerrain.SampleHeight(structure.transform.position) + structure.transform.localScale.y/2, positions[i].y);
            structure.name = "Structure";
        }
    }

    // function to move area to new location
    public void RecalculateStructure(Transform area)
    {
        // calling functions
        ResetArea(area);
        CalcNewLocation(area);
        Deforest(area);
    }

    // function that resets trees to active in an area
    void ResetArea(Transform area)
    {
        // getting the area
        float maxX = area.position.x + area.localScale.x/2;
        float minX = area.position.x - area.localScale.x/2;
        float maxY = area.position.z + area.localScale.z/2;
        float minY = area.position.z - area.localScale.z/2;

        // setting the trees active
        for (int i = nonActiveTrees.Count - 1; i <= 0; i--) {
            if (nonActiveTrees[i].position.x > minX && nonActiveTrees[i].position.x < maxX && nonActiveTrees[i].position.y > minY && nonActiveTrees[i].position.y < maxY) { // check to see if tree in range
                nonActiveTrees[i].gameObject.SetActive(true); // setting tree active
                nonActiveTrees.RemoveAt(i); // removing tree from list of inactive trees
            }   
        }
    }

    // function to calculate a new location for the area to move in front of the player
    void CalcNewLocation(Transform area)
    {
        // variables
        Vector2 loc1;
        Vector2 loc2;
        Vector2 dir;

        // gather movement data from player
        loc1 = movementScript.prevLocations[0];
        loc2 = movementScript.prevLocations[movementScript.prevLocations.Count - 1];

        // getting the direction vector and scaling correctly
        dir = new Vector2(loc2.x - loc1.x, loc2.y - loc1.y);
        dir.Normalize();
        dir *= spawnDist;

        // getting and applying the new location
        area.position = player.transform.position + new Vector3(dir.x, 0, dir.y);
    }

    // function to remove the trees in an area
    void Deforest(Transform area)
    {   // variables
        float maxX;
        float minX;
        float maxY;
        float minY;

        // getting the area
        maxX = area.position.x + area.localScale.x/2;
        minX = area.position.x - area.localScale.x/2;
        maxY = area.position.z + area.localScale.z/2;
        minY = area.position.z - area.localScale.z/2;

        // checking all tree spawn points and deactivating them
        for (int i = 0; i < trees.Count; i++) {
            if (trees[i].x > minX && trees[i].x < maxX && trees[i].y > minY && trees[i].y < maxY) { //? inside the area     
                Debug.Log(i + " | " + trees.Count + " " + treesGameObject.Count);           
                treesGameObject[i].gameObject.SetActive(false);
                nonActiveTrees.Add(treesGameObject[i]);
            }
        }
    }
}
