using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectForce : MonoBehaviour
{
    public float force = 1.0f;
    public ThrowEndpointFloat manager;
    private System.Object locker = new System.Object();
    private bool connection = false;
    public ParticleSystem particle;
    Vector3 current_velocity;
    Vector3 current_position;

    // Start is called before the first frame update
    void Start()
    {
        manager.active_callback = handleNewMessage;
        PauseGame();
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Connection status: " + connection);
        if (connection)
        {
            ResumeGame();
        }
    }

    void PauseGame()
    {
        Time.timeScale = 0;
    }

    void ResumeGame()
    {
        Time.timeScale = 1;
    }


    void FixedUpdate()
    {
        var emission = particle.emission;
        emission.rateOverTime = force * 100;

        var rb = GetComponent<Rigidbody>();
        rb.AddForce(new Vector3(0, force, 0));

        lock (locker)
        {
            current_velocity = rb.velocity;
            current_position = rb.position;
        }
    }

    ThrowEndpointFloat.Message<float> handleNewMessage(ThrowEndpointFloat.Message<float> message)
    {
        force = message.data[0];

        float[] data = new float[3];
        lock (locker)
        {
            connection = true;
            data[0] = current_position.y;
            data[1] = current_velocity.y;
        }
        ThrowEndpointFloat.Header header = new ThrowEndpointFloat.Header(3, 1, 1, 4, "");
        ThrowEndpointFloat.Message<float> response = new ThrowEndpointFloat.Message<float>(header, data);
        return response;
    }
}
