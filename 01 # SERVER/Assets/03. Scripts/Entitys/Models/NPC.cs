using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NPC : Entity
{
    /* NPC Parameters. */
    public int default_hitpoints = 0;

    /* Death */
    public System.DateTime despawnTime = System.DateTime.Now;
    public bool deathSend = false;

    /* Area */
    private int walk_offset = 1;
    private List<Vector3> destinations;

    /* Movement */
    public Vector3 target;
    public bool walking = false;
    public bool running = false;

    /* Rotation */
    private bool rotate = false;
    private Vector3 relativePosition;
    private Quaternion targetRotation;
    private float movementSpeed = 0.1f;
    private float rotationTime = 0;

    private void Start()
    {
        StartCoroutine("Walk");
    }
        
 

    public void Initialize(int id)
    {
        var entityData = DataHandler.GetNPCData(id);
        Reflector.ObjectToObject(entityData, this);
        this.name = id.ToString();
        this.entityType = EntityGroup.NPC;

        this.CreateWalkDestinations();
        this.target = destinations[0];

    }

    private void CreateWalkDestinations()
    {
        destinations = new List<Vector3>();
        var origin = this.transform.position;

        foreach (var i in Enumerable.Range(0, 5))
            destinations.Add(new Vector3(Random.Range(origin.x - walk_offset, origin.x + walk_offset), origin.y, Random.Range(origin.z - walk_offset, origin.z + walk_offset)));
    }

    private void FixedUpdate()
    {
        StartCoroutine("Walk");
    }

    IEnumerator Walk()
    {
        walking = ((!rotate) & ((this.transform.position != target) & (Vector3.Distance(this.transform.position, target) >= 0.2f)));

        if (walking)
        {
            transform.position = Vector3.MoveTowards(this.transform.position, target, movementSpeed);

        } 
        
        else if (!rotate)
        {
            // Send target update so client rotates at same time to next destination.
            target = destinations[Random.Range(0, destinations.Count)];

            // Set rotation params.
            rotate = true;
            rotationTime = 0f;
            relativePosition = target - transform.position;
            targetRotation = Quaternion.LookRotation(relativePosition);
        } 

        else 
        {
            rotationTime += Time.deltaTime * movementSpeed*2;
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationTime);

            if (rotationTime > 1)
            {
                // Send target update so client has no movement delay.

                rotate = false;
                yield return new WaitForEndOfFrame();
            }
        }
    }

    public string GetState()
    {
        var states = new List<(string name, bool activated)>() { ("walking", walking), ("running", running), ("rotating", rotate) };

        return states.Where(i => i.activated).Select(i => i.name).FirstOrDefault();
    }


    public void AddDamage(int damage)
    {
        this.hitpoints -= damage;
        if (this.hitpoints <= 0) this.despawnTime = System.DateTime.Now.AddSeconds(12);
    }
}
