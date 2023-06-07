using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeakPool : MonoBehaviour
{

    public static LeakPool Instance;
    public List<GameObject> _disabledLeaks;
    public List<GameObject> _activeLeaks;

    public float TimeBetweenLeak;
    private float currentTime;
    public int MaxLeaks = 6;
    public GameObject prefab;
    private void Awake()
    {
        Instance = this;
        _activeLeaks = new List<GameObject>();
        _disabledLeaks = new List<GameObject>();
    }


    public bool CreateLeak(Vector3 playerPos, Vector3 forward)
    {
        if(Time.timeSinceLevelLoad - currentTime < TimeBetweenLeak) return false;
        if(_activeLeaks.Count == MaxLeaks) return false;
        GameObject leak;
        
        
        if (_disabledLeaks.Count == 0)
            leak = Instantiate(prefab);
        else
        {
            leak = _disabledLeaks[0];
            _disabledLeaks.Remove(leak);
        }
        leak.transform.position = playerPos + forward * .3f;
        leak.transform.forward = forward;
        leak.transform.parent = transform;
        leak.SetActive(true);
        //leak.transform.localPosition = forward * .5f;
        _activeLeaks.Add(leak);

        currentTime = Time.timeSinceLevelLoad;
        return true;
    }

    public void RepairLeaks(float integrityRatio)
    {
        var amountToRemove = _activeLeaks.Count;
        if (integrityRatio < 1)
        {
            amountToRemove = (int)(_activeLeaks.Count * integrityRatio);
            if (_activeLeaks.Count - amountToRemove == 0) 
                amountToRemove--;
        }
        for (var i = 0; i < amountToRemove; i++)
        {
            var leak = _activeLeaks[0];
            leak.SetActive(false);
            _activeLeaks.Remove(leak);
            _disabledLeaks.Add(leak);
        }
        
    }

}
