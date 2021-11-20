using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Runtime.InteropServices;

public class ThrowProtocolManager : MonoBehaviour
{


    /**
    * The Header struct is a fixed size struct that is sent before every message.
    */
    public struct Header
    {
        public const int HEADER_SIZE = 52;
        public int crc;
        public int width;
        public int height;
        public int depth;
        public int byte_per_element;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public String command;

        /**
        * Constructor for the Header struct with data shape information.
        */
        public Header(int height, int width, int depth, int byte_per_element, String command)
        {
            this.crc = 111559174; // bytes (6,66,166,6)
            this.width = width;
            this.height = height;
            this.depth = depth;
            this.byte_per_element = byte_per_element;
            this.command = command;
        }

        /**
         * Convert the Header struct to a byte array.
         */
        public byte[] getBytes()
        {
            int size = Marshal.SizeOf(this);
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(this, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        /**
        * Convert the byte array to a Header struct.
        */
        public static Header fromBytes(byte[] arr)
        {
            Header h = new Header();
            int size = Marshal.SizeOf(h);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(arr, 0, ptr, size);
                h = (Header)Marshal.PtrToStructure(ptr, h.GetType());

            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return h;
        }

        /**
        * get the size of the expected payload bytes
        */
        public int getPayloadSize()
        {
            return width * height * depth * byte_per_element;
        }

    }

    /**
    * The message class holds the header and payload of a message.
    */
    public class Message<T>
    {

        public Message()
        {
            data = new T[0];
            header = new Header(0, 0, 0, 1, "void");
        }

        public Message(Header header, T[] data)
        {
            this.header = header;
            this.data = data;
        }

        public Header header;
        public T[] data;
    }

    /**
    * The MessageCallbackWithResponse is a callback function that is called when a message is received.
    * it produces also a response message.
    */
    public delegate Message<T> MessageCallbackWithResponse<T>(Message<T> message);

    /**
    * The MessageCallback is a callback function that is called when a message is received.
    * it produces no response message.  
    */
    public delegate void MessageCallback<T>(Message<T> message);


    /**
    * The ThrowNode class is responsible for sending and receiving messages.
    */
    public class ThrowNode
    {

        private const int TIMEOUT = 5000;
        private TcpClient socketConnection;
        private NetworkStream stream;
        private bool connected = false;

        /**
        * Constructor for the ThrowNode class used to connect to the server [CLIENT]
        */
        public ThrowNode(string host, int port)
        {
            socketConnection = new TcpClient("localhost", 8000);
            socketConnection.ReceiveTimeout = ThrowNode.TIMEOUT;
            stream = socketConnection.GetStream();
            connected = true;
        }

        /**
        * Constructor for the ThrowNode class used to connect given an Accepted Connection [SERVER]
        */
        public ThrowNode(TcpClient client)
        {
            socketConnection = client;
            socketConnection.ReceiveTimeout = ThrowNode.TIMEOUT;
            stream = socketConnection.GetStream();
            connected = true;
        }

        /**
        * send raw bytes to the server
        */
        public void sendBytes(byte[] bytes)
        {
            this.stream.Write(bytes, 0, bytes.Length);
        }

        /**
        * receive raw bytes from the server
        */
        public byte[] receiveBytes(int total)
        {
            byte[] bytes = new byte[total];
            int received_size = 0;
            int length;
            while (received_size < total)
            {
                length = this.stream.Read(bytes, received_size, total - received_size);
                if (length == 0)
                {
                    this.socketConnection.Close();
                }
                received_size += length;
            }
            return bytes;
        }

        /**
        * send an Header to the server
        */
        public void sendHeader(Header header)
        {
            this.sendBytes(header.getBytes());
        }

        /**
        * receive an Header from the server
        */
        public Header receiveHeader()
        {
            return Header.fromBytes(receiveBytes(Header.HEADER_SIZE));
        }

        /**
        * receive raw payloads bytes from the server given the Header
        */
        public byte[] receivePayloadBytes(Header header)
        {
            return receiveBytes(header.getPayloadSize());
        }

        /**
        * receive numeric data array from the server given the Header
        */
        public T[] receivePayloadData<T>(Header header)
        {
            byte[] raw_data = receivePayloadBytes(header);
            T[] data = new T[header.width * header.height * header.depth];
            if (header.byte_per_element == Marshal.SizeOf(typeof(T)))
            {
                Buffer.BlockCopy(raw_data, 0, data, 0, raw_data.Length);
            }
            else
            {
                throw new Exception("Byte per element mismatch");
            }
            return data;
        }

        /**
        * receive a Message from the server
        */
        public Message<T> receiveMessage<T>()
        {
            Header header = receiveHeader();
            return new Message<T>(header, receivePayloadData<T>(header));
        }

        /**
        * send a Message to the server
        */
        public void sendMessage<T>(Message<T> message)
        {
            sendData<T>(message.data, message.header);
        }

        /**
        * sends numerica data array to the server given the Header
        */
        public void sendPayloadData<T>(T[] data, Header header)
        {
            byte[] raw_data = new byte[data.Length * Marshal.SizeOf(typeof(T))];
            Buffer.BlockCopy(data, 0, raw_data, 0, raw_data.Length);
            sendBytes(raw_data);
        }

        /**
        * sends an Header and numeric data array to the server
        */
        public void sendData<T>(T[] data, Header header)
        {
            sendHeader(header);
            sendPayloadData(data, header);
        }

        /**
        * sends numeric data array to the server building automatically the Header given the data shape
        * and the command string
        */
        public void sendData<T>(T[] data, int height, int width, int depth, String command)
        {
            Header header = new Header(height, width, depth, Marshal.SizeOf(typeof(T)), command);
            sendData(data, header);
        }


    }




    /**
    * Attributes
    */
    public string address = "0.0.0.0";
    public int port = 8000;
    private Thread serverThread;
    private Thread connectionThread;
    private bool connected = false;
    float[] shared_data;
    protected Matrix4x4 T = Matrix4x4.identity;


    /**
    * Static callbacks handlers
    */
    public List<MessageCallback<float>> passive_callbacks = new List<MessageCallback<float>>();
    public MessageCallbackWithResponse<float> active_callback = null;

    /**
    * Starts an AcceptLoop thread to listen for incoming connections.
    */
    void Start()
    {

        // T = RightHandCoordinateSystem.getLocalToWorldTransform(transform);
        // active_callback = handleNewMessage;

        try
        {
            serverThread = new Thread(() => AcceptLoop());
            serverThread.IsBackground = true;
            serverThread.Start();
        }
        catch (Exception e)
        {
            Debug.Log("On client connect exception " + e);
        }
    }

    /**
    * Tears down the connections.
    */
    void OnDestroy()
    {
        Debug.Log("Destroy");
        serverThread.Abort();
        if (connected)
        {
            connectionThread.Abort();
        }
    }

    /**
    * Loops accepting a new connection and creates a new thread for the connection.
    * Only one connection is accepted at a time. AcceptLoop will wait for 1 second
    * before accepting another connection for scheduling purposes.
    */
    void AcceptLoop()
    {
        TcpListener server = new TcpListener(IPAddress.Parse(address), port);
        server.Start();


        while (true)
        {
            if (!connected)
            {
                Debug.Log("Waiting for a connection... ");
                ThrowNode client = new ThrowNode(server.AcceptTcpClient());
                connected = true;
                connectionThread = new Thread(() => ConnectionLoop(client));
                connectionThread.IsBackground = true;
                connectionThread.Start();
                Debug.Log("Connected!");
            }
            else
            {
                Debug.Log("Connection already present... ");
            }
            Thread.Sleep(1000);
        }
    }

    /**
    * Loops receiving data from the connection and sending it to the shared data.
    * The connection is closed when the connection is closed by the client.
    */
    void ConnectionLoop(ThrowNode client)
    {
        Debug.Log("Connection loop started");
        while (true)
        {
            try
            {
                // Receive message
                Message<float> message = client.receiveMessage<float>();

                // Produce Response message if active callback is set
                Message<float> response_message = new Message<float>();
                if (active_callback != null)
                {
                    response_message = active_callback(message);
                }

                // Propagate message to all passive callbacks
                foreach (MessageCallback<float> callback in passive_callbacks)
                {
                    callback(message);
                }

                // Send response message
                client.sendData(response_message.data, response_message.header);
            }
            catch (Exception e)
            {
                Debug.Log("Connection loop exception " + e);
                connected = false;
                break;
            }
        }
    }

    public class RightHandCoordinateSystem
    {
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

        public static Matrix4x4 left2right(Matrix4x4 T)
        {
            return right2left(T);
        }

        public static void setLocalToWorldTransform(Transform transform, Matrix4x4 right_hand_matrix)
        {
            Matrix4x4 current_matrix = right2left(right_hand_matrix);
            transform.position = current_matrix.GetColumn(3);
            transform.rotation = current_matrix.rotation;
        }

        public static Matrix4x4 getLocalToWorldTransform(Transform transform)
        {
            return left2right(transform.localToWorldMatrix);
        }
    }



    void Update()
    {
        // lock (threadLocker)
        // {
        //     Debug.Log("Setting: " + T);
        //     RightHandCoordinateSystem.setLocalToWorldTransform(transform, T);
        // }
    }

    // /**
    // //  ____    ___   _   _    ____   _       _____   _____    ___    _   _ 
    // // / ___|  |_ _| | \ | |  / ___| | |     | ____| |_   _|  / _ \  | \ | |
    // // \___ \   | |  |  \| | | |  _  | |     |  _|     | |   | | | | |  \| |
    // //  ___) |  | |  | |\  | | |_| | | |___  | |___    | |   | |_| | | |\  |
    // // |____/  |___| |_| \_|  \____| |_____| |_____|   |_|    \___/  |_| \_|
    // */
    // private static ThrowProtocolManager _instance;
    // public static ThrowProtocolManager Instance { get { return _instance; } }

    // /**
    // * Awake object
    // */
    // private void Awake()
    // {
    //     if (_instance != null && _instance != this)
    //     {
    //         Destroy(this.gameObject);
    //     }
    //     else
    //     {
    //         _instance = this;
    //     }
    // }
}
