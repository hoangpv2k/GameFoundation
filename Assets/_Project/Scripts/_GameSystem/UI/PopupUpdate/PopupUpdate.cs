using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupUpdate : Popup
{
    [SerializeField] private UIButton btnUpdate;

    public void Initialize()
    {

        btnUpdate.onClick.RemoveListener(UpdateVersion);
        btnUpdate.onClick.AddListener(UpdateVersion);
    }

    public void UpdateVersion() 
    {
    
    }

}
