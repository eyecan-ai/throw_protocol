using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemoteTransformController : MonoBehaviour
{

    public ThrowEndpointFloat throwProtocolManager;
    private Matrix4x4 new_matrix;
    private System.Object locker = new System.Object();
    public bool relative = true;

    // Start is called before the first frame update
    void Start()
    {
        new_matrix = ThrowEndpointFloat.RightHandCoordinateSystem.getLocalToWorldTransform(transform);
        throwProtocolManager.passive_callbacks.Add(handleNewMessage);
    }

    // Update is called once per frame
    void Update()
    {
        lock (locker)
        {
            if (relative)
            {
                Matrix4x4 current_matrix = ThrowEndpointFloat.RightHandCoordinateSystem.getLocalToWorldTransform(transform);
                current_matrix = current_matrix * new_matrix;
                ThrowEndpointFloat.RightHandCoordinateSystem.setLocalToWorldTransform(transform, current_matrix);
            }
            else
            {
                ThrowEndpointFloat.RightHandCoordinateSystem.setLocalToWorldTransform(transform, new_matrix);
            }
        }
    }

    void handleNewMessage(ThrowEndpointFloat.Message<float> message)
    {

        Debug.Log("Received: " + message);
        Matrix4x4 matrix = new Matrix4x4();
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                matrix[i, j] = message.data[i * 4 + j];
            }
        }
        lock (locker)
        {
            new_matrix = matrix;
        }
    }
}
