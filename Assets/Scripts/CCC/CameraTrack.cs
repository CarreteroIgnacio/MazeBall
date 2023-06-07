using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Inputs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;


namespace CCC
{
    public class CameraTrack : MonoBehaviour
    {

        
        public static CameraTrack Instance;
        public Transform speedTrail;

        public Transform leakContainer;

        public CinemachineVirtualCamera virtualCamera;
        public Camera camera;
        public float noiseAmount;
        public float noiseDuration;
        
        [Range(0,1)]
        public float lookSen = 1;
        
        Transform playerTransform;
        private void Awake()
        {
            Instance = this;
            playerTransform = virtualCamera.LookAt;
        }

        public float acumulativeMul = 1;
        public float acumulativeClamp = 1;

        private float acumulative;

        

        private void Update()
        {
            if (Mathf.Abs(InputManager.PlayerInputs.Wasd.x) < .1f) acumulative = 0;
            else acumulative = Mathf.Clamp(Time.deltaTime * acumulativeMul + acumulative,0, acumulativeClamp);

            //Debug.Log(acumulative);
            transform.eulerAngles += new Vector3(0, InputManager.PlayerInputs.Wasd.x, 0) * (acumulative * lookSen);

            if (InputManager.PlayerInputs.TapLeft)
            {
                transform.eulerAngles -= new Vector3(0, 90, 0);
                //DotForward();
            }
            if (InputManager.PlayerInputs.TapRight)
            {
                transform.eulerAngles += new Vector3(0, 90, 0);
                //DotForward();
            }
            
        }

        public void LooKEnemySpawn()
        {
            StartCoroutine(LooKSpawnCo());
        }

        private IEnumerator LooKSpawnCo()
        {
            var transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
            virtualCamera.LookAt = GameManager.Instance.transform;
            transposer.m_FollowOffset.z = -2;
            yield return new WaitForSeconds(3f);
            
            transposer.m_FollowOffset.z = -4;
            virtualCamera.LookAt = playerTransform;
        }


        private void DotForward()
        {
            var forward = transform.forward;
            var forw = Vector3.Dot(Vector3.forward, forward);
            var rgh = Vector3.Dot(Vector3.right, forward);
            if (forw >= .5f) 
                transform.eulerAngles = Vector3.zero;
            else if (forw <= -.5f)
                transform.eulerAngles = new Vector3(0, 180, 0);
            else if (rgh >= .5f)
                transform.eulerAngles = new Vector3(0, 90, 0);
            else
                transform.eulerAngles = new Vector3(0, 270, 0);
            
        }
        //----------------------------//
        public void SetPosition(float3 pos, quaternion rotation, Vector3 linearVelocity)
        {
            transform.position = pos;
            leakContainer.rotation = rotation;

        }

        public void SetSpeedTrailDirection(Vector3 dir)
        {
            speedTrail.forward = dir.normalized;
        }

        public void RunCameraNoise()
        {
            StartCoroutine(CameraNoise());
        }

        private IEnumerator CameraNoise()
        {
            var vShake = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

            vShake.m_AmplitudeGain = noiseAmount;
            vShake.m_FrequencyGain = noiseAmount;
            yield return new WaitForSeconds(noiseDuration);
            vShake.m_AmplitudeGain = 0;
            vShake.m_FrequencyGain = 0;
        }
    }

}