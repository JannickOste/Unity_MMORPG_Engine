using UnityEngine;

class WorldObject : Entity
{
    string requirements;

    public void Initialize(int id)
    {
        Reflector.ObjectToObject(DataHandler.GetObjectData(id), this);

        this.entityType = EntityGroup.WORLDOBJECT;
        this.name = id.ToString();
    }
}

