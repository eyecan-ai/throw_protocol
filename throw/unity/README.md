# Unity Examples

## PythonCommunicationExample

!!Important!!
Unity has a left-hand coordinates system. Common practice in robotics/cv is right-hand cooordinates system. In the examples the Python side will produce Right-Hand end C# Throw client will convert them
into Left-Hand coordinates. Conversion is made with:

```c#
public static Matrix4x4 right2left(Matrix4x4 T)
{
    Matrix4x4 left = Matrix4x4.identity;
    left.SetTRS(
        new Vector3(T[0, 3], T[2, 3], T[1, 3]),
        new Quaternion(-T.rotation.x, -T.rotation.z, -T.rotation.y, T.rotation.w),
        new Vector3(1, 1, 1)
    );
    return left;
}
```

This conversion will swap (conceptually) the Y and Z axis in unity. It is recommended to also change the colors of these axes, using Green for Z and Blue for Y, so visually they will look like they belong to a Right-Hand coordinate system, even if the name will be different.

Each Scene is a single example. Launching the scene a Throw server will be activated.
Launch the in-scene-folder python script *python_client.py* to activate a custom client.

### Scene: RemoteTransformController

Is a scene where python client control camera Pose with Throw.

### Scene: CameraStream

Is a scene streaming camera images with Throw.

### Scene: PIDCOntroller

Is a scene with Python PID controller moving Unity3D dynamics.
