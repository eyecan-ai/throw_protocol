using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class CameraCaptureSystem : MonoBehaviour
{

    [Header("Image parameters")]
    public int output_width = 400;
    public int output_height = 400;

    [Header("What to export")]
    public bool rgb = true;
    public bool instances = false;
    public bool semantic = true;
    public bool depth = true;
    public bool normals = false;
    public bool flow = false;

    [Header("Depth")]
    public bool compressed_depth = false;

    [Header("Shaders & Material")]
    public Shader replacementShader;
    public Shader opticalFlowShader;
    private Material opticalFlowMaterial;
    public float opticalFlowSensitivity = 1.0f;

    // Buffers
    private System.Object locker = new System.Object();
    private Dictionary<string, byte[]> data_map = new Dictionary<string, byte[]>();
    private Dictionary<string, Texture2D> data_map2 = new Dictionary<string, Texture2D>();
    byte[] current_image_data;
    byte[] current_depth_data;

    /**
     * Capture passes: each pass produces a different image type
     */
    private CapturePass[] capturePasses = new CapturePass[] {
        new CapturePass() { name = "rgb", enabled=true },
        new CapturePass() { name = "instances", supportsAntialiasing = false, enabled = false },
        new CapturePass() { name = "semantics", supportsAntialiasing = false , enabled = true},
        new CapturePass() { name = "depth", enabled=true },
        new CapturePass() { name = "normals", enabled = false },
        new CapturePass() { name = "flow", supportsAntialiasing = false, needsRescale = true, enabled = false } // (see issue with Motion Vectors in @KNOWN ISSUES)
	};

    /**
     * Capture pass settings
     */
    struct CapturePass
    {
        // configuration
        public string name;
        public bool supportsAntialiasing;
        public bool needsRescale;
        public bool enabled;
        public CapturePass(string name_) { name = name_; supportsAntialiasing = true; needsRescale = false; camera = null; enabled = true; }

        // impl
        public Camera camera;
    };

    /**
     * Shaders replacement modes
     */
    enum ReplacelementModes
    {
        ObjectId = 0,
        CatergoryId = 1,
        DepthCompressed = 2,
        DepthMultichannel = 3,
        Normals = 4
    };

    void Start()
    {

        /**
         * auto find shaders if not set
         */
        if (!replacementShader)
            replacementShader = Shader.Find("CameraCaptureSystemShader");

        if (!opticalFlowShader)
            opticalFlowShader = Shader.Find("CameraCaptureSystemShaderFlow");

        /**
        * Uses Real camera for RGB, hidden cameras for other passes
        */
        capturePasses[0].camera = GetComponent<Camera>();
        for (int q = 1; q < capturePasses.Length; q++)
            capturePasses[q].camera = CreateHiddenCamera(capturePasses[q].name);

        OnCameraChange();
        OnSceneChange();
    }

    void LateUpdate()
    {
#if UNITY_EDITOR
        if (DetectPotentialSceneChangeInEditor())
            OnSceneChange();
#endif // UNITY_EDITOR
        // @TODO: detect if camera properties actually changed
        OnCameraChange();
    }


    public float deltaTime;
    public Text fpsText;

    void Update()
    {
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        fpsText.text = Mathf.Ceil(fps).ToString();
        StartBuffering(output_width, output_height);
    }

    /**
     * Creates an hidden camera with the given name, attached
     * to this game object.
     */
    private Camera CreateHiddenCamera(string name)
    {
        var go = new GameObject(name, typeof(Camera));
        go.hideFlags = HideFlags.HideAndDontSave;
        go.transform.parent = transform;
        var newCamera = go.GetComponent<Camera>();
        return newCamera;
    }


    static private void SetupCameraWithReplacementShader(Camera cam, Shader shader, ReplacelementModes mode)
    {
        SetupCameraWithReplacementShader(cam, shader, mode, Color.black);
    }

    static private void SetupCameraWithReplacementShader(Camera cam, Shader shader, ReplacelementModes mode, Color clearColor)
    {

        var cb = new CommandBuffer();
        cb.SetGlobalFloat("_OutputMode", (int)mode); // @TODO: CommandBuffer is missing SetGlobalInt() method
        // cb.SetGlobalInt("_OutputMode", (int)mode);
        cam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, cb);
        cam.AddCommandBuffer(CameraEvent.BeforeFinalPass, cb);
        cam.SetReplacementShader(shader, "");
        cam.backgroundColor = clearColor;
        cam.clearFlags = CameraClearFlags.SolidColor;
    }

    static private void SetupCameraWithPostShader(Camera cam, Material material, DepthTextureMode depthTextureMode = DepthTextureMode.None)
    {
        var cb = new CommandBuffer();
        cb.Blit(null, BuiltinRenderTextureType.CurrentActive, material);
        cam.AddCommandBuffer(CameraEvent.AfterEverything, cb);
        cam.depthTextureMode = depthTextureMode;
    }

    public void OnCameraChange()
    {
        int targetDisplay = 1;
        var mainCamera = GetComponent<Camera>();
        foreach (var pass in capturePasses)
        {
            if (pass.camera == mainCamera)
                continue;

            // cleanup capturing camera
            pass.camera.RemoveAllCommandBuffers();

            // copy all "main" camera parameters into capturing camera
            pass.camera.CopyFrom(mainCamera);

            // set targetDisplay here since it gets overriden by CopyFrom()
            pass.camera.targetDisplay = targetDisplay++;
        }

        // cache materials and setup material properties
        if (!opticalFlowMaterial || opticalFlowMaterial.shader != opticalFlowShader)
            opticalFlowMaterial = new Material(opticalFlowShader);
        opticalFlowMaterial.SetFloat("_Sensitivity", opticalFlowSensitivity);

        // setup command buffers and replacement shaders
        SetupCameraWithReplacementShader(capturePasses[1].camera, replacementShader, ReplacelementModes.ObjectId);
        SetupCameraWithReplacementShader(capturePasses[2].camera, replacementShader, ReplacelementModes.CatergoryId);
        SetupCameraWithReplacementShader(capturePasses[3].camera, replacementShader, compressed_depth ? ReplacelementModes.DepthCompressed : ReplacelementModes.DepthMultichannel, Color.white);
        SetupCameraWithReplacementShader(capturePasses[4].camera, replacementShader, ReplacelementModes.Normals);
        SetupCameraWithPostShader(capturePasses[5].camera, opticalFlowMaterial, DepthTextureMode.Depth | DepthTextureMode.MotionVectors);

        capturePasses[0].enabled = this.rgb;
        capturePasses[1].enabled = this.instances;
        capturePasses[2].enabled = this.semantic;
        capturePasses[3].enabled = this.depth;
        capturePasses[4].enabled = this.normals;
        capturePasses[5].enabled = this.flow;

    }

    public void OnSceneChange()
    {
        var renderers = Object.FindObjectsOfType<Renderer>();
        var mpb = new MaterialPropertyBlock();
        foreach (var r in renderers)
        {
            var id = r.gameObject.GetInstanceID();
            var layer = r.gameObject.layer;
            var tag = r.gameObject.tag;

            /**
             * Instance ID is converted as unique color [Instance Segmentaiton]
             */
            mpb.SetColor("_ObjectColor", CameraCaptureSystemColorEncoding.EncodeIDAsColor(id));

            /**
             * Layer is converted as unique color for [Semantic Segmentation]
             */
            mpb.SetColor("_CategoryColor", CameraCaptureSystemColorEncoding.EncodeLayerAsColor(layer));
            r.SetPropertyBlock(mpb);
        }
    }

    public void StartBuffering(int width = -1, int height = -1)
    {
        if (width <= 0 || height <= 0)
        {
            width = Screen.width;
            height = Screen.height;
        }

        StartCoroutine(
            WaitForEndOfFrameAndSave(width, height)
        );
    }

    private IEnumerator WaitForEndOfFrameAndSave(int width, int height)
    {
        yield return new WaitForEndOfFrame();
        PassesBuffering(width, height);
    }

    private void PassesBuffering(int width, int height)
    {
        foreach (var pass in capturePasses)
        {
            if (pass.enabled)
                BufferizeSinglePass(pass.camera, width, height, pass.supportsAntialiasing, pass.needsRescale, pass);
        }
    }

    private void BufferizeSinglePass(Camera cam, int width, int height, bool supportsAntialiasing, bool needsRescale, CapturePass pass)
    {
        var mainCamera = GetComponent<Camera>();
        var depth = 24;
        var format = RenderTextureFormat.Default;
        var readWrite = RenderTextureReadWrite.Default;
        var antiAliasing = (supportsAntialiasing) ? Mathf.Max(1, QualitySettings.antiAliasing) : 1;

        var finalRT = RenderTexture.GetTemporary(width, height, depth, format, readWrite, antiAliasing);
        var renderRT = (!needsRescale) ? finalRT : RenderTexture.GetTemporary(mainCamera.pixelWidth, mainCamera.pixelHeight, depth, format, readWrite, antiAliasing);
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);

        var prevActiveRT = RenderTexture.active;
        var prevCameraRT = cam.targetTexture;

        // render to offscreen texture (readonly from CPU side)
        RenderTexture.active = renderRT;
        cam.targetTexture = renderRT;

        cam.Render();

        if (needsRescale)
        {
            // blit to rescale (see issue with Motion Vectors in @KNOWN ISSUES)
            RenderTexture.active = finalRT;
            Graphics.Blit(renderRT, finalRT);
            RenderTexture.ReleaseTemporary(renderRT);
        }

        // read offsreen texture contents into the CPU readable texture
        tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
        tex.Apply();

        // encode texture into PNG
        var bytes = tex.EncodeToPNG();

        //File.WriteAllBytes(filename, bytes);
        lock (locker)
        {
            data_map[pass.name] = bytes;
            // data_map2[pass.name] = new Texture2D(width, height, TextureFormat.RGB24, false);
            // Graphics.CopyTexture(tex, data_map2[pass.name]);
        }

        // restore state and cleanup
        cam.targetTexture = prevCameraRT;
        RenderTexture.active = prevActiveRT;

        Object.Destroy(tex);
        RenderTexture.ReleaseTemporary(finalRT);
    }


    public Dictionary<string, byte[]> GetCurrentDataMap()
    {
        Dictionary<string, byte[]> buffered_data_map = new Dictionary<string, byte[]>();
        lock (locker)
        {
            foreach (var kv in data_map)
            {
                byte[] arr = kv.Value;
                buffered_data_map[kv.Key] = new byte[arr.Length];
                arr.CopyTo(buffered_data_map[kv.Key], 0);
            }
        }
        return buffered_data_map;
    }


#if UNITY_EDITOR
    private GameObject lastSelectedGO;
    private int lastSelectedGOLayer = -1;
    private string lastSelectedGOTag = "unknown";
    private bool DetectPotentialSceneChangeInEditor()
    {
        bool change = false;
        // there is no callback in Unity Editor to automatically detect changes in scene objects
        // as a workaround lets track selected objects and check, if properties that are 
        // interesting for us (layer or tag) did not change since the last frame
        if (UnityEditor.Selection.transforms.Length > 1)
        {
            // multiple objects are selected, all bets are off!
            // we have to assume these objects are being edited
            change = true;
            lastSelectedGO = null;
        }
        else if (UnityEditor.Selection.activeGameObject)
        {
            var go = UnityEditor.Selection.activeGameObject;
            // check if layer or tag of a selected object have changed since the last frame
            var potentialChangeHappened = lastSelectedGOLayer != go.layer || lastSelectedGOTag != go.tag;
            if (go == lastSelectedGO && potentialChangeHappened)
                change = true;

            lastSelectedGO = go;
            lastSelectedGOLayer = go.layer;
            lastSelectedGOTag = go.tag;
        }

        return change;
    }
#endif // UNITY_EDITOR
}