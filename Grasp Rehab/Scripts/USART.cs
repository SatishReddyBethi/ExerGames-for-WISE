using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO.Ports;

public class USART : MonoBehaviour
{
    private SerialPort sp;
    private bool SaveStatics;
    private int timeout = 0;
    public bool start;
    public Text DebugText;
    public bool debug = true;

    public List<int[]> Calibrations = new List<int[]>();
    // Use this for initialization
    void Start()
    {
        Scan();
    }

    #region Code Connecting With Arduino

    public List<string> portExists;
    public Dropdown ComPorts;

    public void Scan()
    {
        portExists = new List<string>();
        portExists.AddRange(SerialPort.GetPortNames());
        if (portExists.Count != 0)
        {
            ComPorts.ClearOptions();
            ComPorts.AddOptions(portExists);
        }
    }

    public void Initialize()
    {
        if (portExists.Count > 1)
        {
            sp = new SerialPort(portExists[ComPorts.value], 9600);
            sp.NewLine = "\n";
            sp.DtrEnable = true;
            sp.ReadTimeout = 5;//25 for query
            sp.WriteTimeout = 5;

            try
            {
                sp.Open();
            }
            catch (System.Exception)
            {
                Verbose_Logging("No Device Conneced to that COM Port");
            }
            Verbose_Logging("Initialized " + portExists[ComPorts.value]);

        }
    }

    private void OnDisable()
    {        
        if (sp != null)
        {
            sp.Close();
        }
        else
        {
            Debug.Log("COM Port Does Not Exist");
        }
    }

    private void OnApplicationQuit()
    {        
        if (sp != null)
        {
            sp.Close();
        }
    }
    #endregion

    #region Code for Data Streaming and Parsing

    public float Hts;//Highest Time Stamp
    void FixedUpdate()//SerialPort sp)
    {
        if (start)
        {
            if (sp != null)
            {
                float ts = 0;
                ts = Time.realtimeSinceStartup;
                string Line = ReadFromArduino();
                if (Line != "")
                {
                    ParseAngles(Line);
                }
                ts = Time.realtimeSinceStartup - ts;
                if (ts > Hts)
                {
                    Hts = ts;
                }
                Verbose_Logging("Got Data");
            }
            else
            {
                Verbose_Logging("No Device Connected");
            }
        }
    }

    public void WriteToArduino(string message)
    {
        sp.WriteLine(message);
        sp.BaseStream.Flush();
    }

    public string ReadFromArduino()
    {
        string Line = "";
        if (sp != null && sp.IsOpen)
        {
            try
            {
                Line = sp.ReadLine();
                DebugText.text = Line;
                timeout = 0;
                sp.BaseStream.Flush();
            }
            catch (System.TimeoutException)
            {
                Line = "";
                timeout = timeout + 1;
                Verbose_Logging("Receive Data Error. Read Timeout");
            }

            if (timeout > 50)
            {
                sp.Close();
                timeout = 0;
            }
        }
        else
        {
            Initialize();
        }
        return Line;
    }

    public void ParseAngles(string Line)
    {
        bool ReadStatus = true;
        string[] forces = Line.Split(',');
        if (forces.Length == 4)
        {
            for (int i = 0; i < forces.Length; i++)
            {
                if (forces[i] == "")
                {
                    ReadStatus = false;
                }
            }
            if (ReadStatus)
            {
                try
                {
                    // Assign each variable in the string
                    InputEu = new Vector3(float.Parse(forces[1]), float.Parse(forces[2]), float.Parse(forces[3]));
                    SpoonPressure = float.Parse(forces[0]);
                }
                catch (System.FormatException)
                {
                    Verbose_Logging("Format Error");
                }

            }
        }
        else
        {
            Debug.Log("Wrong Data: " + Line);
        }
    }
    #endregion

    #region Buttons
    public void Save_Statics()
    {
        start = true;
    }

    public void Exit()
    {
        start = false;
    }

    public void CloseSerialPort()
    {
        if (sp != null)
        {
            sp.Close();
        }
    }

    public void GetComPort()
    {
        if (sp != null)
        {
            sp.Close();
        }
        Initialize();
    }

    public void Verbose_Logging(string msg)
    {
        if (debug)
        {
            Debug.Log(msg);
            DebugText.text = msg;
        }
    }
    #endregion

}
