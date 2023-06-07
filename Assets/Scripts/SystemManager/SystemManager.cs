using System;
using System.Collections;
using System.ComponentModel.Design.Serialization;
using CCC;
using Level;
using Path;
using UnityEngine;



public  class SystemManager : MonoBehaviour
{
    public static SystemManager Instance;

    private void Awake() => Instance = this;

    private void Start()
    {
        StartCoroutine(EnableAllSystem());
        GameManager.Instance.TogglePauseEvent += ToggleSystems;
    }


    private IEnumerator EnableAllSystem()
    {
        yield return new WaitForSeconds(.75f);
        SysAgent.Instance.EnableSystem(true);
        SysGridMaker.Instance.EnableSystem(true);
    }



    public void ToggleSystems(bool active)
    {
        SysAgent.Instance.PauseSystem(!active);
        SysGridMaker.Instance.PauseSystem(!active);
        //Debug.Log(active);
    }
}