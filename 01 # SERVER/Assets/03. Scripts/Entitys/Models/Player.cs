using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Player : Entity
{
    public Dictionary<int, int> questStateLib = new Dictionary<int, int>();
    public string questStates = string.Empty;

    public bool kickPlayer = false;
    private bool[] inputs;

    public System.DateTime lastUpdate = System.DateTime.Now;

    public void Initialize(int _id, string username)
    {
        Reflector.ObjectToObject(DataHandler.GetPlayerData(username), this);

        this.id = _id;
        this.name = username;
        this.entityType = EntityGroup.PLAYER;

        if(questStates != null)
        {
            this.questStateLib = JObject.Parse(questStates).ToObject<Dictionary<string, string>>().ToDictionary(s => int.Parse(s.Key), s => int.Parse(s.Value));

        }
        inputs = new bool[6];
    }

    #region Update 
    /// <summary>Processes player input and moves the player.</summary>
    public void FixedUpdate()
    {
        StartCoroutine("CheckPlayer");
        StartCoroutine("SendEntitysAroundPlayer");
    }


    IEnumerator SendEntitysAroundPlayer()
    {
        yield return new WaitForEndOfFrame();
        this.SendToPlayer(this.id);

        Collider[] hits = new Collider[100];
        Physics.OverlapSphereNonAlloc(this.transform.position, Constants.ENTITY_RENDER_RADIUS, hits);
        foreach (Collider hit in hits)
        {
            if(hit != null && hit.GetComponent<Entity>() != null)
            {
                hit.GetComponent<Entity>().SendToPlayer(this.id);
            }
        }
    }

    IEnumerator CheckPlayer()
    {
        yield return new WaitForEndOfFrame();

        if (kickPlayer)
        {
            Debug.Log($"Kicking {this.name} from server");
            SendMessage("002");
            Server.clients[id].Disconnect();
        }
        else
        {
            Vector2 _inputDirection = Vector2.zero;
            if (inputs.Length >= 4)
            {
                if (inputs[0]) // UP
                {

                    _inputDirection.y += 1;
                }
                if (inputs[1]) //DOWN
                {
                    _inputDirection.y -= 1;
                }
                if (inputs[2]) // LEFT
                {
                    _inputDirection.x -= 1;
                }
                if (inputs[3]) // RIGHT
                {
                    _inputDirection.x += 1;
                }
                Move(_inputDirection);
            }
            if (inputs.Length >= 6 && inputs[5]) Attack();
            if (inputs.Length >= 7 && inputs[6]) Interact();

        }
    }



    #endregion

    #region Behaviour
    /// <summary>Calculates the player's desired movement directi   on and moves him.</summary>
    /// <param name="_inputDirection"></param>
    private void Move(Vector2 _inputDirection)
    {
        Vector3 _moveDirection = transform.right * _inputDirection.x + transform.forward * _inputDirection.y;
        float moveSpeed = (inputs[4] ? 6.5f : 5f) / Constants.TICKS_PER_SEC;

        transform.position += _moveDirection * moveSpeed;

        if (transform.position.y < 0)
            this.transform.position = new Vector3(transform.position.x, 0f, transform.position.z);
    }

    private void CollissonCheck(Collision collision)
    {
        ObjectRequirements requirements = collision.transform.GetComponent<ObjectRequirements>();

        if (requirements != null)
        {
            Physics.IgnoreCollision(collision.transform.GetComponent<BoxCollider>(), this.GetComponent<BoxCollider>(), requirements.IgnoreColissionAllowed(this));
            Physics.IgnoreCollision(collision.transform.GetComponent<MeshCollider>(), this.GetComponent<BoxCollider>(), requirements.IgnoreColissionAllowed(this));
        } else
        {
            foreach(Vector3 direction in new[]{ Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
            {
                RaycastHit hit;
                if(Physics.Raycast(this.transform.position, transform.TransformDirection(direction), out hit, 0.1f))
                {
                    inputs = new bool[inputs.Length];
                }
            }
        }
    }

    public void OnCollisionEnter(Collision collision) => CollissonCheck(collision);

    public void OnCollisionStay(Collision collision) => CollissonCheck(collision);
    public void OnCollisionExit(Collision collision) => CollissonCheck(collision);


    private void Attack()
    {

        if (System.DateTime.Now > nextAttack)
        {
            RaycastHit hit;
            if (Physics.SphereCast(this.transform.position, 2f, this.transform.forward, out hit, 2f))
            {
                NPC targetNPC;
                if ((targetNPC = hit.transform.GetComponent<NPC>()) != null)
                {
                    if (hit.distance <= 0.5f)
                    {
                        targetNPC.AddDamage(20);
                    }
                }
                nextAttack = System.DateTime.Now.AddMilliseconds(attackDurationInMs);
            }
        }
    }

    private void Interact()
    {

        if (System.DateTime.Now > nextAttack)
        {
            Debug.Log("Interact attempt");
            RaycastHit hit;
            if (Physics.SphereCast(this.transform.position, 1f, this.transform.forward, out hit, 2f))
            {
                Entity targetEntity = hit.transform.GetComponent<Entity>();
                Debug.Log(targetEntity);
                if (targetEntity != null)
                {
                    Debug.Log("Entity found attempting to interract");
                    if (targetEntity.action_id >= 0)
                    {
                        Debug.Log("Actionid found");
                        ActionHandler.EntityAction(targetEntity.action_id)(this);
                    }
                }
                nextAttack = System.DateTime.Now.AddMilliseconds(attackDurationInMs);
            }
        }
    }
    #endregion

    #region handeling
    /// <summary>Updates the player input with newly received input.</summary>
    /// <param name="_inputs">The new key inputs.</param>
    /// <param name="_rotation">The new rotation.</param>
    public void SetInput(Dictionary<string, object> inputData)
    {
        if (new[] { "input", "rotation" }.Count(k => !inputData.ContainsKey(k)) == 0)
        {
            Debug.Log(inputData["input"].GetType());
            inputs = (bool[])inputData["input"];
            transform.rotation = (Quaternion)inputData["rotation"];

            lastUpdate = System.DateTime.Now;
        } else Debug.Log($"[SetInput::Player({id}]: Invalid input packet received, keys: {string.Join(",", inputData.Keys)}");
    }

    public void Destroy() => EntityHandler.DestroyEntity<Player>(this);
    #endregion
}
