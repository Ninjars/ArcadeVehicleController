using UnityEngine;

namespace ArcadeVehicleController {
    /**
        This simple class allows for different input setups to easily be provided to the VehicleController,
        to avoid a hard dependency on any particular input system.
    **/
    public abstract class InputSource : MonoBehaviour {
        public abstract Vector2 moveXY();
        public abstract bool isAction();
    }
}
