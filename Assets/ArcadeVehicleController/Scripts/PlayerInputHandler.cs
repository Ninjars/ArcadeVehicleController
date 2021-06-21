using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcadeVehicleController {
    public class PlayerInputHandler : InputSource {
        private Vector2 _moveXY;
        private bool _isAction;

        public void OnMove(InputAction.CallbackContext context) {
            _moveXY = context.ReadValue<Vector2>();
        }

        public void OnAction(InputAction.CallbackContext context) {
            _isAction = context.ReadValue<float>() == 1;
        }

        public override Vector2 moveXY() {
            return _moveXY;
        }

        public override bool isAction() {
            return _isAction;
        }
    }
}
