using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DisruptorUnity3d;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System;

public class NetworkToServer {

    private string ServerIP = "127.0.0.1";
    private int ServerPort = 5000;

    private string errorStatus = null;
    private string successStatus = null;
    private bool exchangeStopRequested = false;


    private RingBuffer<string> receivedQueue = new RingBuffer<string>(10000);

    System.Net.Sockets.TcpClient socket = new System.Net.Sockets.TcpClient();

    /*
     * Function: CheckForServerUpdate
     * ----------------------------
     *   Checks if there has been any messages from the server
     * 
     *   return: a string containing the message from the server, else returns empty string
     * 
     */
    public string CheckForServerUpdate()
    {
        //check if any message from server (updated Q matrix)
        //if so update known Qmatrix
        string message;
        bool successfulRec = receivedQueue.TryDequeue(out message);
        if (successfulRec)
        {
            return message;
        }

        if (errorStatus != null)
        {
            Debug.Log(errorStatus);
            errorStatus = null;
        }
        return "";
        //Do action based on Qmatrix
    }

    /*
     * Function: RecieveFromServer
     * ----------------------------
     *   Recieves data from the server, and pushes into a queue for it to be processed later
     * 
     */
    public void RecieveFromServer()
    {
        try
        {
            NetworkStream serverStream = socket.GetStream();
            while (!exchangeStopRequested)
            {
                byte[] inStream = new byte[(int)socket.ReceiveBufferSize];
                serverStream.Read(inStream, 0, (int)socket.ReceiveBufferSize);
                string dataFromServer = System.Text.Encoding.ASCII.GetString(inStream);
                foreach (string mess in dataFromServer.Split(new[] { "\r\n", "\r", "\n" },
    StringSplitOptions.None))
                {
                    if (!(mess.Length >= 65500)) receivedQueue.Enqueue(mess);
                }
                    //if (!(dataFromServer.Length >= 65520)) receivedQueue.Enqueue(dataFromServer);
                }
        }
        catch (Exception e)
        {
            errorStatus = e.ToString();
        }

    }

    /*
     * Function: ConnectToServer
     * ----------------------------
     *   Establishes a connection to the server, and starts recieving on a seperate thread
     * 
     */
    public void ConnectToServer()
    {
        System.Net.IPAddress ip = IPAddress.Parse(ServerIP);
        socket.Connect(ip, ServerPort);
        successStatus = "Connected!";
        Thread t = new Thread(() => RecieveFromServer());
        t.Start();
    }

    /*
     * Function: SendData
     * ----------------------------
     *   Transmits data to the server
     * 
     *   message: The message that will be transmitted to the server
     * 
     */
    public void SendData(string message)
    {
        NetworkStream serverStream = socket.GetStream();
        message += "\n";
        byte[] outStream = System.Text.Encoding.ASCII.GetBytes(message);
        serverStream.Write(outStream, 0, outStream.Length);
        serverStream.Flush();

    }

    /*
     * Function: OnApplicationQuit
     * ----------------------------
     *   Called when application finishes, then closes any established socket connection
     * 
     */
    public void OnApplicationQuit()
    {
        exchangeStopRequested = true;
        socket.Close();
        Debug.Log("Application ending after " + Time.time + " seconds");
    }

}
