using Unity.Mathematics;
using UnityEngine;

namespace Inputs
{
    public class InputManager : MonoBehaviour
    {
        public static PlayerInputStruct PlayerInputs;
    
        private InputMaster _controls;
        private void Awake()
        {
            _controls = new InputMaster();
            PlayerInputs = new PlayerInputStruct();
        }

        private void OnEnable()
        {
            _controls.Player.Enable();
            _controls.Menu.Enable();
        }

        private void OnDisable()
        {
            _controls.Player.Disable();
            _controls.Menu.Disable();
        }


        private void Update()
        {
            PlayerInputs.Wasd =  _controls.Player.WASD.ReadValue<Vector2>();
            PlayerInputs.Mouse =  _controls.Player.Mouse.ReadValue<Vector2>();
            PlayerInputs.Jump =  _controls.Player.Jump.triggered;
            PlayerInputs.Dash =  _controls.Player.Dash.triggered;
            PlayerInputs.TapRight = _controls.Player.QuickRotRight.triggered;
            PlayerInputs.TapLeft = _controls.Player.QuickRotLeft.triggered;
            
            if(_controls.Menu.Esc.triggered)
                GameManager.Instance.TogglePauseGame();
        }//

        public struct PlayerInputStruct
        {
            public float2 Wasd;
            public float2 Mouse;
            public bool Jump;
            public bool Dash;
            public bool TapLeft;
            public bool TapRight;
        }
    }
    
}

