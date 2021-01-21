using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Entity : MonoBehaviour
{
    public EntityGroup entityType;

    public int id;
    public string model_name;
    public int hitpoints;
    public int action_id = -1;

    public bool targetable;
    public int attackDurationInMs = 500;
    public System.DateTime nextAttack = System.DateTime.Now;
        
    public void SendToPlayer(int id)
    {
        Dictionary<string, object> packetData = new Dictionary<string, object>();

        if (new[] { EntityGroup.PLAYER, EntityGroup.NPC }.Contains(entityType))
        {
            if (entityType == EntityGroup.PLAYER) 
                packetData.Add("username", this.name);

            packetData.Add("hitpoints", this.hitpoints);
        }

        packetData.Add("id", this.id);
        packetData.Add("model_name", this.model_name);
        packetData.Add("position", this.transform.position);
        packetData.Add("rotation", this.transform.rotation);
        
        packetData.Add("entity_type", this.entityType);

        PacketHandler.SendPacket(
            toClient: id, 
            type: ServerPacket.SPAWN_ENTITY, 
            packetData: packetData, 
            udp: true, 
            useEncrypt: false
        );
    }
}
