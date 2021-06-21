# ArcadeVehicleController
Simple Unity vehicle controller with an arcade-y approach to physics.

Inspired and based upon work by Kenney (www.kenney.nl), and uploaded here for ease of reference for when I need similar functionality again and to share the knowledge in a hopefully accessible way. The code is namespaced and contained within its own asset folder for ease of import.

The principle of this controller is that the physics object driving the vehicle is a simple sphere, with the vehicle model merely a visual element following it.

This requires a bit of a specific object hierarchy but proves to be very effective way of creating a flexible, drifty vehicle controller. Well-suited to creating a Wipeout-style drifty hover-racer.

Also included is DustEmitter and example prefabs for setting these up, effectively a particle system controller that ray-casts down to position a particle effect on the ground plane, which can also be set to only emit over a speed threshold and up to a max distance to the ground. This is used in the example to create effects from below the wings that dynamically start and stop as the vehicle banks into turns.

To set up the vehicle you need a structure such as this:
```
Empty GameObject - provides vehicle's name and position
    -> Sphere with Rigidbody and Sphere Collider on "Ignore Raycast" layer
    -> GameObject with VehicleController and VehicleCameraController
        -> GameObject containing model (referenced by "container" variable in code), which is tilted into corners if tiltFactor is set
            -> (optional) GameObject named "body", to be tilted by forces on turns and acceleration
            -> (optional) GameObject name "wheelFrontLeft" and "wheelFrontRight", to turn when steering
    -> Empty GameObject to be the camera rig referenced by the VehicleCameraController
        -> GameObject with camera component
```
There are further comments and property tooltips in VehicleController to hopefully make it accessible and modifiable.

https://user-images.githubusercontent.com/5053926/122815507-72bc3e00-d2cd-11eb-9974-5caa388f7b62.mp4
