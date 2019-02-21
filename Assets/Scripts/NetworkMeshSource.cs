using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
#if !UNITY_EDITOR
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine.XR.WSA.WebCam;
#endif

public class NetworkMeshSource : MonoBehaviour
{

    private static NetworkMeshSource networkMeshSourceSingleton = null;
    public static NetworkMeshSource getSingleton() { return networkMeshSourceSingleton; }

    public int serverPort = 32123;
    public int targetPort = 32123;
    public string targetIP = "192.168.137.1";
    public volatile bool connected = false;
#if !UNITY_EDITOR
    //public DatagramSocket udpClient = null;
    public StreamSocket tcpClient = null;
    public Windows.Storage.Streams.IOutputStream outputStream = null;

    private PhotoCapture photoCaptureObject = null;
    private Texture2D targetTexture = null;
    private Vector3 textureLocation = Vector3.zero;
    private Quaternion textureRotation = Quaternion.identity;

#endif
    //public Mesh testMesh = null;
    // Start is called before the first frame update
    void Start()
    {
        if (networkMeshSourceSingleton != null)
        {
            Destroy(this);
            return;
        }
        networkMeshSourceSingleton = this;
        setupSocket();
    }

    public async void setupSocket()
    {

#if !UNITY_EDITOR
        //udpClient = new DatagramSocket();
        //udpClient.Control.DontFragment = true;
        tcpClient = new Windows.Networking.Sockets.StreamSocket();
        tcpClient.Control.OutboundBufferSizeInBytes = 128000;
        tcpClient.Control.NoDelay = false;
        try
        {
            //await udpClient.BindServiceNameAsync("" + targetPort);
            await tcpClient.ConnectAsync(new HostName(targetIP), "" + targetPort);
            
            outputStream = tcpClient.OutputStream;
            connected = true;
            //outputStream = await udpClient.GetOutputStreamAsync(new HostName(targetIP), "" + targetPort);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
            return;
        }
        #endif
    }
    /*
    public void captureImageData()
    {

        Resolution cameraResolution = UnityEngine.XR.WSA.WebCam.PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
        targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height);

        // Create a PhotoCapture object
        UnityEngine.XR.WSA.WebCam.PhotoCapture.CreateAsync(false, delegate (UnityEngine.XR.WSA.WebCam.PhotoCapture captureObject) {
            photoCaptureObject = captureObject;
            UnityEngine.XR.WSA.WebCam.CameraParameters cameraParameters = new UnityEngine.XR.WSA.WebCam.CameraParameters();
            cameraParameters.hologramOpacity = 0.0f;
            cameraParameters.cameraResolutionWidth = cameraResolution.width;
            cameraParameters.cameraResolutionHeight = cameraResolution.height;
            cameraParameters.pixelFormat = UnityEngine.XR.WSA.WebCam.CapturePixelFormat.BGRA32;

            // Activate the camera
            photoCaptureObject.StartPhotoModeAsync(cameraParameters, delegate (UnityEngine.XR.WSA.WebCam.PhotoCapture.PhotoCaptureResult result) {
                // Take a picture
                photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
            });
        });
    }

    void OnCapturedPhotoToMemory(UnityEngine.XR.WSA.WebCam.PhotoCapture.PhotoCaptureResult result, UnityEngine.XR.WSA.WebCam.PhotoCaptureFrame photoCaptureFrame)
    {
        // Copy the raw image data into the target texture
        photoCaptureFrame.UploadImageDataToTexture(targetTexture);

        // Create a GameObject to which the texture can be applied
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Renderer quadRenderer = quad.GetComponent<Renderer>() as Renderer;
        quadRenderer.material = new Material(Shader.Find("Custom/Unlit/UnlitTexture"));

        quad.transform.parent = this.transform;
        quad.transform.localPosition = new Vector3(0.0f, 0.0f, 3.0f);

        quadRenderer.material.SetTexture("_MainTex", targetTexture);

        // Deactivate the camera
        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
    }

    void OnStoppedPhotoMode(UnityEngine.XR.WSA.WebCam.PhotoCapture.PhotoCaptureResult result)
    {
        // Shutdown the photo capture resource
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
    }
    */
    public async void sendMesh(Mesh m, Vector3 location, Quaternion rotation)
    {
        if (!connected)
            return;
#if !UNITY_EDITOR
        try
        {
            List<Mesh> meshes = new List<Mesh>();
            meshes.Add(m);
            byte[] meshData = SimpleMeshSerializer.Serialize(meshes);
            //byte[] vectBytes = BitConverter.GetBytes(location);
            //byte[] quatBytes = BitConverter.GetBytes(rotation);
            
            byte[] bytes = new byte[4 + 12 + 16]; // 4 bytes per float
            System.Buffer.BlockCopy(BitConverter.GetBytes(32 + meshData.Length), 0, bytes, 0, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(location.x), 0, bytes, 4, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(location.y), 0, bytes, 8, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(location.z), 0, bytes, 12, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(rotation.x), 0, bytes, 16, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(rotation.y), 0, bytes, 20, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(rotation.z), 0, bytes, 24, 4);
            System.Buffer.BlockCopy(BitConverter.GetBytes(rotation.w), 0, bytes, 28, 4);


            //byte[] sendData = Compress(Combine(bytes, meshData));
            byte[] sendData = Combine(bytes, meshData);

            //byte[] sendData = Compress(Combine(bytes, SimpleMeshSerializer.Serialize(meshes)));
            //Byte[] sendData = Encoding.ASCII.GetBytes("WEEEEEE!");
            //udpClient.Send(sendData, sendData.Length);
            //safety catch for huge items

            //temp for testing
            //byte[] sendData = meshData;

            if (sendData.Length>200000)
            {
                Debug.Log("Packet of length " + sendData.Length + " waiting to go out... But can't.. Because it is probably too huge...");
                return;
            }
            DataWriter writer = new DataWriter(outputStream);
            writer.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
            writer.ByteOrder = Windows.Storage.Streams.ByteOrder.LittleEndian;
            //StreamWriter writer = new StreamWriter(outputStream);
            //writer.BaseStream.Write(sendData,0,sendData.Length);
            writer.WriteBytes(sendData);
            await writer.StoreAsync();
            await writer.FlushAsync();
            Debug.Log("Sent " + sendData.Length + " bytes.");
            writer.DetachStream();
            //writer.WriteBytes(sendData);
            //await writer.FlushAsync();
            //await writer.StoreAsync();
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
            return;
        }
        //Debug.Log("Sent: " + sendData.Length + " bytes");
#endif
    }
#if !UNITY_EDITOR
    public static byte[] Compress(byte[] raw)
    {
        using (MemoryStream memory = new MemoryStream())
        {
            using (GZipStream gzip = new GZipStream(memory, CompressionMode.Compress, true))
            {
                gzip.Write(raw, 0, raw.Length);
            }
            return memory.ToArray();
        }
    }

    static byte[] Decompress(byte[] gzip)
    {
        using (GZipStream stream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress))
        {
            const int size = 4096;
            byte[] buffer = new byte[size];
            using (MemoryStream memory = new MemoryStream())
            {
                int count = 0;
                do
                {
                    count = stream.Read(buffer, 0, size);
                    if (count > 0)
                    {
                        memory.Write(buffer, 0, count);
                    }
                }
                while (count > 0);
                return memory.ToArray();
            }
        }
    }
#endif
    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDestroy()
    {
#if !UNITY_EDITOR
        //if (tcpClient != null)
        //{
            //tcpClient.Close();
       //     tcpClient = null;
        //}

        //if (udpClient != null)
        //{
            //udpClient.Close();
        //    udpClient = null;
        //}
        #endif
    }

    //stolen useful code.
    public static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] ret = new byte[first.Length + second.Length];
        System.Buffer.BlockCopy(first, 0, ret, 0, first.Length);
        System.Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
        return ret;
    }
    public static byte[] Combine(byte[] first, byte[] second, byte[] third)
    {
        byte[] ret = new byte[first.Length + second.Length + third.Length];
        System.Buffer.BlockCopy(first, 0, ret, 0, first.Length);
        System.Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
        System.Buffer.BlockCopy(third, 0, ret, first.Length + second.Length,
                         third.Length);
        return ret;
    }


}
