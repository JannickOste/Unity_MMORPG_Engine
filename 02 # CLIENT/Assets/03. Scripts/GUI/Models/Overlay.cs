using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

class Overlay : UIBase
{
    private Slider _hpSlider;

    private RawImage imageOut;
    private Camera minimapCamera;

    public override void Load()
    {
        this.NoneDestroyable = true;
        base.Load();

        imageOut = this.GetRawImages().FirstOrDefault();
        if(imageOut != null)
        {
            minimapCamera = this._template.GetComponentInChildren<Camera>();
            RenderTexture renderImage = Resources.Load("UIImages/Overlay/MinimapRenderTexture") as RenderTexture;
            minimapCamera.targetTexture = renderImage;
            minimapCamera.forceIntoRenderTexture = true;
        }

        _hpSlider = this._template.GetComponentInChildren<Slider>();
    }

    public void FixedUpdate()
    {
        StartCoroutine("UpdateMinimap");
    }

    private IEnumerator UpdateMinimap()
    {
        Character player = EntityHandler.GetEntity<Character>(EntityGroup.PLAYER, Client.instance.myId);
        if(player != null)
        {
            Vector3 ppos = player.transform.position;

            if (minimapCamera != null)
            {
                if (imageOut != null)
                {
                    minimapCamera.transform.position = new Vector3(ppos.x, minimapCamera.transform.position.y, ppos.z);
                }
                else Debug.Log("[Interface::Overlay]: Unable to find rendering image for minimap");
            }
            else Debug.Log("[Interface::Overlay]: Unable to find minimap camera out");

        }


        yield return new WaitForFixedUpdate();
    }
}

