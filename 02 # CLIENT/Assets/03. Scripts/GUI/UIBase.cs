using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

class UIBase : MonoBehaviour
{
    /* Template UI */
    protected UnityEngine.GameObject _template;
    protected bool NoneDestroyable = false;
    protected System.DateTime nextAction;


    private List<System.Type> typeList;
    private Dictionary<System.Type, dynamic> windowComponents;

    /* Internal getters */
    private dynamic GetTemplateComponents<T>() => this._template.GetComponentsInChildren<T>(true);

    /* Child getters */
    protected Canvas GetCanvas() => this.GetTemplateComponents<Canvas>().FirstOrDefault();
    protected Button[] GetButtons() => this.GetTemplateComponents<Button>();
    protected RawImage[] GetRawImages() => this.GetTemplateComponents<RawImage>();
    protected InputField[] GetInputFields() => this.GetTemplateComponents<InputField>();
    protected TMPro.TextMeshProUGUI[] GetTextMeshProUGUIs() => this.GetTemplateComponents<TMPro.TextMeshProUGUI>();

    private Dictionary<(System.Type type, string key), object> componentLib = new Dictionary<(System.Type, string), object>();


    public void SetUIValue<T>(string name, object input)
    {
        componentLib.Add((typeof(T), name), input);
    }

    public T GetUIValue<T>(string name)
    {
        object libval = componentLib.Keys.Where(keyset => (keyset.type == typeof(T) & keyset.key == name)).FirstOrDefault();
        return (T)System.Convert.ChangeType(libval, typeof(T));
    }

    public virtual void Load()
    {
        UnityEngine.Object _rawTemplate = Resources.Load($"Interfaces/{this.GetType().Name}");
        if (_rawTemplate == null) throw new System.ArgumentNullException($"# Load attempt for the UITemplate '{this.GetType().Name}' failed...");
        else
        {
            _template = Instantiate(_rawTemplate, Vector3.zero, Quaternion.identity) as GameObject;
            _template.name = this.GetType().Name;

            if(NoneDestroyable)
            {
                DontDestroyOnLoad(_template);
                DontDestroyOnLoad(this);
            }

            nextAction = System.DateTime.Now;
        }
    }

    public void Destroy()
    {
        Destroy(this._template);
        Destroy(this);
    }
}

