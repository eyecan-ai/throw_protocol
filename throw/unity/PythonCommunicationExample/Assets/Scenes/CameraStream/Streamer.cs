using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Streamer : MonoBehaviour
{

    public ThrowEndpointByte throwEndpoint = null;
    public CameraCaptureSystem cameraCaptureSystem = null;
    public System.Object locker = new System.Object();
    private Dictionary<string, byte[]> buffered_data = new Dictionary<string, byte[]>();

    // Start is called before the first frame update
    void Start()
    {
        if (throwEndpoint != null)
        {
            throwEndpoint.active_callback = handleNewMessage;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (cameraCaptureSystem != null)
        {
            lock (locker)
            {
                buffered_data = cameraCaptureSystem.GetCurrentDataMap();
            }
        }
    }


    ThrowEndpointByte.Message<byte> handleNewMessage(ThrowEndpointByte.Message<byte> message)
    {

        ThrowEndpointByte.Message<byte> response_message = new ThrowEndpointByte.Message<byte>();
        string command = message.header.command.Trim();
        Debug.Log("Received command" + command);
        string[] chunks = command.Split(':');
        if (chunks.Length == 2)
        {

            Debug.Log("Chunks " + chunks);
            string action = chunks[0];
            string key = chunks[1];

            if (action.CompareTo("get") == 0)
            {
                lock (locker)
                {
                    if (buffered_data.ContainsKey(key))
                    {
                        response_message.data = buffered_data[key];
                        response_message.header = new ThrowEndpoint<byte>.Header(1, 1, response_message.data.Length, 1, "ok");
                    }
                    else
                    {
                        response_message.header = new ThrowEndpoint<byte>.Header(0, 0, 0, 0, "key_not_found");
                    }
                }

            }
            else
            {
                response_message.header = new ThrowEndpoint<byte>.Header(0, 0, 0, 0, "unknown_command");
            }
        }
        else
        {
            response_message.header = new ThrowEndpoint<byte>.Header(0, 0, 0, 0, "invalid_command");
        }
        return response_message;
    }
}
