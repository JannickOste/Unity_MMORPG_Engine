using UnityEngine;

public class Teleport : ActionPacket
{
    public Teleport()
    {
        this.id = 0;
    }

    public Vector3 position;
    public Quaternion rotation;
    public int scene_id = 0;

    public void OnCollisionEnter(Collision collision)
    {
        collision.gameObject.transform.position = position;
        if (rotation != null) collision.gameObject.transform.rotation = rotation;
    }
}
