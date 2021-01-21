using System.Collections.Generic;
using UnityEngine;

public class OpenChat : ActionPacket
{
    public OpenChat()
    {
        this.id = 1;
    }

    public override void Invoke(params object[] parameters)
    {
        Debug.Log("Opening chat");
        int playerId = (int)parameters[0];
        int dialogID = (int)parameters[1];

        string formated_dialog = string.Format(DataHandler.GetNPCDialog(dialogID), EntityHandler.GetEntity<Player>(playerId).name);
        Debug.Log("Sending chat request");
        PacketHandler.SendPacket(playerId, ServerPacket.OPEN_CHAT, new Dictionary<string, object>()
        {
            { "dialog", formated_dialog }
        });
    }
}