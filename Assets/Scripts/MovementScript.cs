using System.Collections.Generic;
using UnityEngine;

public class MovementScript : MonoBehaviour
{
    [SerializeField] float movementSpeed = 10;
    [SerializeField] TerrainScript terrainScript;

    // for storing movement data
    public List<Vector2> prevLocations = new List<Vector2>(); // list containing a set amount of previous locations
    public int maxAmountOfSaves = 5; // amount of items allowed in list
    int delayBetweenSaves = 5; // amount of times FixedUpdate has to run to save current position
    int saveIndex = 0;

    //! debugging
    bool test = false;

    // Update is called once per frame
    void Update()
    {
        // movement
        if (Input.GetKey(KeyCode.D)) {
            transform.Translate(new Vector3(movementSpeed * Time.deltaTime,0,0));
        } else if (Input.GetKey(KeyCode.A)) {
            transform.Translate(new Vector3(-movementSpeed * Time.deltaTime,0,0));
        }
        if (Input.GetKey(KeyCode.W)) {
            transform.Translate(new Vector3(0, 0, movementSpeed * Time.deltaTime));
        } else if (Input.GetKey(KeyCode.S)) {
            transform.Translate(new Vector3(0, 0, -movementSpeed * Time.deltaTime));
        }
    }

    //? runs every 0.02 seconds (50 tps)
    void FixedUpdate()
    {
        // saving movement data to use in prediction script
        if (saveIndex >= delayBetweenSaves) { // save point reached
            saveIndex = 0; // resetting saveIndex
            prevLocations.Add(new Vector2(transform.position.x, transform.position.z));
            if (prevLocations.Count > maxAmountOfSaves) { // deleting the first element if going over the max
                prevLocations.RemoveAt(0);
            }
        } else {
            saveIndex++;
        }
    }
}
