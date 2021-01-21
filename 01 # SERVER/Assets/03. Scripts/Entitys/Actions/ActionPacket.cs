using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class ActionPacket : MonoBehaviour
 {
    public int id;

    public virtual void Invoke(params object[] parameters)
    {
        throw new NotImplementedException();
    }
 }

