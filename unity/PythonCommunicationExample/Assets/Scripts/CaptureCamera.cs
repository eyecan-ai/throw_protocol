using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CaptureCamera : MonoBehaviour
{

    public ThrowEndpointByte manager;
    private System.Object locker = new System.Object();
    byte[] current_image_data;
    byte[] current_depth_data;


    public int captureWidth = 400;
    public int captureHeight = 400;

    // Start is called before the first frame update
    void Start()
    {
        manager.active_callback = handleNewMessage;
    }

    // Update is called once per frame
    void Update()
    {
        lock (locker)
        {
            current_depth_data = this.CaptureDepth();
            current_image_data = this.Capture();
        }

    }

    public byte[] Capture()
    {

        RenderTexture renderTexture = null;
        Texture2D screenShot = null;
        Rect rect = new Rect(0, 0, captureWidth, captureHeight);

        if (renderTexture == null)
        {

            // creates off-screen render texture that can rendered into
            renderTexture = new RenderTexture(captureWidth, captureHeight, 24);
            screenShot = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
        }

        Camera _camera = GetComponent<Camera>();
        _camera.targetTexture = renderTexture;
        _camera.Render();

        // reset active camera texture and render texture
        _camera.targetTexture = null;
        RenderTexture.active = null;

        // read pixels will read from the currently active render texture so make our offscreen 
        // render texture active and then read the pixels
        RenderTexture.active = renderTexture;
        screenShot.ReadPixels(rect, 0, 0);
        screenShot.Apply();

        byte[] imageBytes = screenShot.EncodeToPNG();
        //Object.Destroy(screenShot);

        //File.WriteAllBytes(Application.dataPath + "/../"+ imagePath + "/img{counter}.png", bytes);
        //counter = counter + 1;
        return imageBytes;
    }


    byte[] CaptureDepth()
    {

        Rect rect = new Rect(0, 0, captureWidth, captureHeight);
        Texture2D depthScreenShot = new Texture2D(captureWidth, captureHeight, TextureFormat.ARGB32, false);
        // RenderTexture renderTexture = new RenderTexture(captureWidth, captureHeight, 0);
        RenderTexture depthTexture = new RenderTexture(captureWidth, captureHeight, 24, RenderTextureFormat.Depth);

        RenderTexture.active = depthTexture;
        Camera camera = GetComponent<Camera>();
        camera.depthTextureMode = DepthTextureMode.Depth;
        camera.SetTargetBuffers(depthTexture.colorBuffer, depthTexture.depthBuffer);
        camera.targetTexture = depthTexture;
        camera.Render();

        // RenderTexture.active = depthTexture;
        depthScreenShot.ReadPixels(rect, 0, 0);
        depthScreenShot.Apply();

        // //Encode the texture data into .png formatted bytes
        // byte[] data = depthScreenShot.EncodeToPNG();
        // Debug.Log("Depth size: " + data.Length);
        // camera.targetTexture = null;
        // RenderTexture.active = null;
        // Destroy(depthScreenShot);
        return new byte[0];
    }

    ThrowEndpointByte.Message<byte> handleNewMessage(ThrowEndpointByte.Message<byte> message)
    {

        ThrowEndpointByte.Message<byte> response_message = new ThrowEndpointByte.Message<byte>();

        lock (locker)
        {
            response_message.data = current_depth_data;
        }
        response_message.header = new ThrowEndpoint<byte>.Header(1, 1, response_message.data.Length, 1, "image");
        return response_message;
    }


}
