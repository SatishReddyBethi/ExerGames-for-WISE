using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using UnityEngine.UI;

public class Connection : MonoBehaviour
{
    /* How to create a custom class in unity
    [System.Serializable]
    public class Movement
    {
        public float moveV;
        public float moveH;

        public bool isRunning;
        public bool isLooking;
    }
    public Movement movement;
 
    ...
 
    movement.moveV = 1.0f;
    */
    public bool Test = false;
    public Vector3 TestVector;
    public SerialPort sp;
    public string Line;
    public string DeviceLocalAngles;
    public string JCSAngles;

    [Header("Data being recieved from COM port")]
    public float[] x;
    public float[] y;
    public float[] z;
    public float[] w;
    [Space(10)]

    [Range(-180f, 180f)]
    public float[] angle_x;
    [Range(-180f, 180f)]
    public float[] angle_y;
    [Range(-180f, 180f)]
    public float[] angle_z;

    public Text[] A;
    public Text[] B;
    public Text[] C;
    public Text[] D;

    Quaternion L_Arm = Quaternion.identity;
    Quaternion L_Forearm = Quaternion.identity;
    Quaternion R_Arm = Quaternion.identity;
    Quaternion R_Forearm = Quaternion.identity;
    Quaternion _Back = Quaternion.identity;

    public Quaternion LeftForearm = Quaternion.identity;
    public Quaternion rightForearm = Quaternion.identity;
    public Quaternion LeftArm = Quaternion.identity;
    public Quaternion RightArm = Quaternion.identity;
    public Quaternion Back = Quaternion.identity;

    public Vector3 RightArm_Angles;
    public Vector3 LeftArm_Angles;
    public Vector3 RightForeArm_Angles;
    public Vector3 LeftForeArm_Angles;

    public float[] LeftAngles = new float[5];
    public float[] RightAngles = new float[5];

    public int no_devices = 5;
    bool[] _device;
    private bool SaveStatics;
    private int timeout = 0;
    public bool start;
    public bool onlyX;
    public bool onlyY;
    public bool onlyZ;
    public string ComPort;
    public Text CurrentComText;
    public Text DebugText;
    static float DataSendRate = 100.0f;
    static float TimeTakenforOneMsg = 1.0f / DataSendRate;
    public bool debug = true;
    public bool LeftAbbAdd;
    public bool RightAbbAdd;
    Vector3 tempaxis;
    float tempangle;
    public float Hts;//Highest Time Stamp
    public Image[] DataImages;

    public List<int[]> Calibrations = new List<int[]>();
    // Use this for initialization
    void Start()
    {
        x = new float[no_devices];
        y = new float[no_devices];
        z = new float[no_devices];
        w = new float[no_devices];

        angle_x = new float[no_devices];
        angle_y = new float[no_devices];
        angle_z = new float[no_devices];

        _device = new bool[no_devices];
        Initialize();
        for (int i = 0; i < no_devices; i++) {
            Calibrations.Add(new int[] { 0, 0, 0, 0, 0 });
        }
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
        else
        {
            ComPorts.ClearOptions();
            ComPorts.AddOptions(new List<string> { "No Ports" });
        }
    }

    public void Initialize()
    {
        Scan();

        if (portExists.Count != 0)
        {
            sp = new SerialPort(portExists[ComPorts.value], 115200);
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
            CurrentComText.text = ComPort;
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

    void Invokeup_date()
    {
        if (!Test)
        {
            if (start)
            {
                Quaternion _Left_Forearm_ = new Quaternion(x[0], y[0], z[0], w[0]);           //   A IMU
                Quaternion _right_Forearm_ = new Quaternion(x[1], y[1], z[1], w[1]);          //   B IMU
                Quaternion _Left_Arm_ = new Quaternion(x[2], y[2], z[2], w[2]);               //   C IMU
                Quaternion _Right_Arm_ = new Quaternion(x[3], y[3], z[3], w[3]);              //   D IMU
                Quaternion _Back_ = new Quaternion(x[4], y[4], z[4], w[4]);              //   E IMU

                _Left_Forearm_ = Box_Transf('a', _Left_Forearm_);
                _right_Forearm_ = Box_Transf('b', _right_Forearm_);
                _Left_Arm_ = Box_Transf('c', _Left_Arm_);
                _Right_Arm_ = Box_Transf('d', _Right_Arm_);
                _Back_ = Box_Transf('e', _Back_);

                RightArm_Angles = GetAngles("RA", _Back_, _Right_Arm_);
                LeftArm_Angles = GetAngles("LA", _Back_, _Left_Arm_);
                LeftArm_Angles.z = LeftArm_Angles.x + LeftArm_Angles.z;
                RightArm_Angles.z = RightArm_Angles.x + RightArm_Angles.z;
                RightForeArm_Angles = GetAngles("RF", _Right_Arm_, _right_Forearm_);
                LeftForeArm_Angles = GetAngles("LF", _Left_Arm_, _Left_Forearm_);

                LeftAbbAdd = (Mathf.Abs(180.0f - Mathf.Abs(LeftArm_Angles.x)) < 45.0f) || (Mathf.Abs(LeftArm_Angles.x) < 45.0f);
                RightAbbAdd = (Mathf.Abs(180.0f - Mathf.Abs(RightArm_Angles.x)) < 45.0f) || (Mathf.Abs(RightArm_Angles.x) < 45.0f);

                if (LeftAbbAdd)//Left Abduction Adduction 
                {
                    LeftAngles[2] = LeftArm_Angles.y;
                }
                else// Left Flex-Ext
                {
                    LeftAngles[0] = LeftArm_Angles.y;
                }

                if (RightAbbAdd)//Right Abduction Adduction 
                {
                    RightAngles[2] = RightArm_Angles.y;
                }
                else// Right Flex-Ext
                {
                    RightAngles[0] = RightArm_Angles.y;
                }

                if (LeftArm_Angles.z > 180.0f)
                {
                    LeftArm_Angles.z = LeftArm_Angles.z - 360.0f;
                }
                else if (LeftArm_Angles.z < -180.0f)
                {
                    LeftArm_Angles.z = LeftArm_Angles.z + 360.0f;
                }
                if (RightArm_Angles.z > 180.0f)
                {
                    RightArm_Angles.z = RightArm_Angles.z - 360.0f;
                }
                else if (RightArm_Angles.z < -180.0f)
                {
                    RightArm_Angles.z = RightArm_Angles.z + 360.0f;
                }

                LeftAngles[1] = LeftArm_Angles.z;// Left Internal External Rotation
                RightAngles[1] = RightArm_Angles.z;// Right Internal External Rotation
                LeftAngles[3] = LeftForeArm_Angles.y;// Left Extension Flexsion
                LeftAngles[4] = LeftForeArm_Angles.z;// Left Pronation Supination
                RightAngles[3] = RightForeArm_Angles.y;// Right Extension Flexsion
                RightAngles[4] = RightForeArm_Angles.z;// Right Pronation Supination

                Quaternion Left_Arm = Quaternion.Inverse(RotateLeftArm(_Back_)) * _Left_Arm_;
                Quaternion Left_Forearm = Quaternion.Inverse(RotateLeftForeArm(_Left_Arm_)) * _Left_Forearm_;
                Quaternion Right_Arm = Quaternion.Inverse(_Back_) * _Right_Arm_;
                Quaternion right_Forearm = Quaternion.Inverse(RotateRightForeArm(_Right_Arm_)) * _right_Forearm_;

                Vector3 axis;
                float angle;

                Left_Forearm.ToAngleAxis(out angle, out axis);
                LeftForearm = Quaternion.AngleAxis(-angle, new Vector3(-axis.y, axis.z, axis.x));//New Design

                Left_Arm.ToAngleAxis(out angle, out axis);
                LeftArm = Quaternion.AngleAxis(-angle, new Vector3(-axis.y, axis.x, -axis.z));//New Design

                Right_Arm.ToAngleAxis(out angle, out axis);
                RightArm = Quaternion.AngleAxis(-angle, new Vector3(axis.y, -axis.x, -axis.z));//New Design

                right_Forearm.ToAngleAxis(out angle, out axis);
                rightForearm = Quaternion.AngleAxis(-angle, new Vector3(axis.y, axis.z, -axis.x));//New Design

                Back = BackAdjust(_Back_);
                Back.ToAngleAxis(out angle, out axis);
                Back = Quaternion.AngleAxis(-angle, new Vector3(axis.y, -axis.x, -axis.z));

                if (onlyX)
                {
                    angle_x = new float[] { quat2eul(LeftForearm, 0).x, quat2eul(rightForearm, 1).x, quat2eul(LeftArm, 2).x, quat2eul(RightArm, 3).x, quat2eul(Back, 4).x };
                    for (int i = 0; i < angle_x.Length; i++)
                    {
                        if (angle_x[i] > 180.0f)
                        {
                            angle_x[i] = angle_x[i] - 360.0f;
                        }
                    }
                    A[0].text = angle_x[0].ToString("F2");
                    B[0].text = angle_x[1].ToString("F2");
                    C[0].text = angle_x[2].ToString("F2");
                    D[0].text = angle_x[3].ToString("F2");
                }
                if (onlyY)
                {
                    angle_y = new float[] { quat2eul(LeftForearm, 0).y, quat2eul(rightForearm, 1).y, quat2eul(LeftArm, 2).y, quat2eul(RightArm, 3).y, quat2eul(Back, 4).y };
                    for (int i = 0; i < angle_y.Length; i++)
                    {
                        if (angle_y[i] > 180.0f)
                        {
                            angle_y[i] = angle_y[i] - 360.0f;
                        }
                    }
                    A[1].text = angle_y[0].ToString("F2");
                    B[1].text = angle_y[1].ToString("F2");
                    C[1].text = angle_y[2].ToString("F2");
                    D[1].text = angle_y[3].ToString("F2");
                }
                if (onlyZ)
                {
                    angle_z = new float[] { quat2eul(LeftForearm, 0).z, quat2eul(rightForearm, 1).z, quat2eul(LeftArm, 2).z, quat2eul(RightArm, 3).z, quat2eul(Back, 4).z };
                    for (int i = 0; i < angle_z.Length; i++)
                    {
                        if (angle_z[i] > 180.0f)
                        {
                            angle_z[i] = angle_z[i] - 360.0f;
                        }
                    }
                    A[2].text = angle_z[0].ToString("F2");
                    B[2].text = angle_z[1].ToString("F2");
                    C[2].text = angle_z[2].ToString("F2");
                    D[2].text = angle_z[3].ToString("F2");
                }
                DeviceLocalAngles = "a" + "," + LeftForearm.w.ToString("F2") + "," + LeftForearm.x.ToString("F2") + "," + LeftForearm.y.ToString("F2") + "," + LeftForearm.z.ToString("F2") + "," + "b" + "," + rightForearm.w.ToString("F2") + "," + rightForearm.x.ToString("F2") + "," + rightForearm.y.ToString("F2") + "," + rightForearm.z.ToString("F2") + "," + "c" + "," + LeftArm.w.ToString("F2") + "," + LeftArm.x.ToString("F2") + "," + LeftArm.y.ToString("F2") + "," + LeftArm.z.ToString("F2") + "," + "d" + "," + RightArm.w.ToString("F2") + "," + RightArm.x.ToString("F2") + "," + RightArm.y.ToString("F2") + "," + RightArm.z.ToString("F2") + "," + "e" + "," + Back.w.ToString("F2") + "," + Back.x.ToString("F2") + "," + Back.y.ToString("F2") + "," + Back.z.ToString("F2");
                JCSAngles = RightArm_Angles.x.ToString("F2") + "," + RightArm_Angles.y.ToString("F2") + "," + RightArm_Angles.z.ToString("F2") + "," + RightForeArm_Angles.x.ToString("F2") + "," + RightForeArm_Angles.y.ToString("F2") + "," + RightForeArm_Angles.z.ToString("F2") + "," + LeftArm_Angles.x.ToString("F2") + "," + LeftArm_Angles.y.ToString("F2") + "," + LeftArm_Angles.z.ToString("F2") + "," + LeftForeArm_Angles.x.ToString("F2") + "," + LeftForeArm_Angles.y.ToString("F2") + "," + LeftForeArm_Angles.z.ToString("F2");
            }
        }
        else
        {
            LeftForearm = Quaternion.Euler(TestVector);
        }
    }

    Quaternion NegativeQuat(Quaternion q)
    {
        q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
        return q;
    }

    Quaternion Box_Transf(char DeviceName, Quaternion Q)
    {
        Quaternion q = Quaternion.identity;
        Quaternion Qt = Quaternion.identity;
        switch (DeviceName)
        {
            case 'a':
                Qt = new Quaternion(0.0050f, 0.0050f, -0.0090f, 0.9999f);
                break;
            case 'b':
                Qt = new Quaternion(-0.0370f, 0.0190f, 0.0190f, -0.9990f);
                break;
            case 'c':
                Qt = new Quaternion(-0.0090f, -0.0050f, 0.0110f, -0.9999f);
                break;
            case 'd':
                Qt = new Quaternion(0.0931f, -0.0531f, -0.0090f, 0.9942f);
                break;
            case 'e':
                Qt = new Quaternion(-0.0690f, -0.0010f, 0.0290f, -0.9972f);
                break;
        }

        Qt = Q * Qt * Quaternion.Inverse(Q);
        q = Qt * Q;
        return q;
    }

    Vector3 quat2eul(Quaternion Q, int Device)
    {
        float sinr_cosp = 2.0f * (Q.w * Q.x + Q.y * Q.z);
        float cosr_cosp = 1.0f - (2.0f * (Q.x * Q.x + Q.y * Q.y));
        Vector3 euler = new Vector3();
        euler.x = Mathf.Rad2Deg * Mathf.Atan2(sinr_cosp, cosr_cosp);

        float sinp = 2.0f * (Q.w * Q.y - Q.z * Q.x);
        euler.y = -Mathf.Rad2Deg * Mathf.Asin(sinp);

        float siny_cosp = 2.0f * (Q.w * Q.z + Q.y * Q.x);
        float cosy_cosp = 1.0f - (2.0f * (Q.y * Q.y + Q.z * Q.z));
        euler.z = Mathf.Rad2Deg * Mathf.Atan2(siny_cosp, cosy_cosp);
        if (float.IsNaN(euler.x) || float.IsNaN(euler.y) || float.IsNaN(euler.z))
        {
            euler.x = angle_x[Device];
            euler.y = angle_y[Device];
            euler.z = angle_z[Device];
        }
        return euler;
    }  

    public Vector3 GetAngles(string DeviceName, Quaternion qRef, Quaternion q)
    {
        Vector3 Ang = new Vector3(0, 0, 0);
        Quaternion qK = new Quaternion(0, 0, 1, 0);
        Quaternion qJ = new Quaternion(0, 1, 0, 0);
        Vector3 Angles = new Vector3(0f, 0f, 0f);
        Quaternion Z = Quaternion.identity;
        Quaternion qRel = Quaternion.identity;
        switch (DeviceName)
        {
            case "LA":
                //Debug.Log(qRef);
                Z = qRef * NegativeQuat(qK) * Quaternion.Inverse(qRef);
                //Debug.Log(Z);
                Z = new Quaternion(Z.x * Mathf.Sin(Mathf.PI / 4), Z.y * Mathf.Sin(Mathf.PI / 4), Z.z * Mathf.Sin(Mathf.PI / 4), Mathf.Cos(Mathf.PI / 4));
                qRef = Z * qRef;
                qRef.ToAngleAxis(out tempangle, out tempaxis);
                qRef = Quaternion.AngleAxis(tempangle, new Vector3(-tempaxis.x, tempaxis.y, -tempaxis.z));
                q.ToAngleAxis(out tempangle, out tempaxis);
                q = Quaternion.AngleAxis(tempangle, new Vector3(-tempaxis.x, tempaxis.y, -tempaxis.z));
                qRel = Quaternion.Inverse(qRef) * q;
                //Debug.Log(qRel);
                Angles = Quat2Angle(qRel, "YZY");
                break;
            case "RA":
                Z = qRef * Quaternion.Inverse(qK) * Quaternion.Inverse(qRef);
                Z = new Quaternion(Z.x * Mathf.Sin(Mathf.PI / 4), Z.y * Mathf.Sin(Mathf.PI / 4), Z.z * Mathf.Sin(Mathf.PI / 4), Mathf.Cos(Mathf.PI / 4));
                qRef = Z * qRef;
                qRef.ToAngleAxis(out tempangle, out tempaxis);
                qRef = Quaternion.AngleAxis(tempangle, new Vector3(-tempaxis.x, -tempaxis.y, tempaxis.z));
                q.ToAngleAxis(out tempangle, out tempaxis);
                q = Quaternion.AngleAxis(tempangle, new Vector3(-tempaxis.x, -tempaxis.y, tempaxis.z));
                qRel = Quaternion.Inverse(qRef) * q;
                Angles = Quat2Angle(qRel, "YZY");
                break;
            case "LF":
                Z = qRef * qJ * Quaternion.Inverse(qRef);
                Z = new Quaternion(Z.x * Mathf.Sin(Mathf.PI / 4), Z.y * Mathf.Sin(Mathf.PI / 4), Z.z * Mathf.Sin(Mathf.PI / 4), Mathf.Cos(Mathf.PI / 4));
                qRef = Z * qRef;
                qRef.ToAngleAxis(out tempangle, out tempaxis);
                qRef = Quaternion.AngleAxis(tempangle, new Vector3(-tempaxis.x, tempaxis.y, -tempaxis.z));
                q.ToAngleAxis(out tempangle, out tempaxis);
                q = Quaternion.AngleAxis(tempangle, new Vector3(-tempaxis.x, tempaxis.y, -tempaxis.z));
                qRel = Quaternion.Inverse(qRef) * q;
                Angles = Quat2Angle(qRel, "ZXY");
                break;
            case "RF":
                Z = qRef * Quaternion.Inverse(qJ) * Quaternion.Inverse(qRef);
                Z = new Quaternion(Z.x * Mathf.Sin(Mathf.PI / 4), Z.y * Mathf.Sin(Mathf.PI / 4), Z.z * Mathf.Sin(Mathf.PI / 4), Mathf.Cos(Mathf.PI / 4));
                qRef = Z * qRef;
                qRef.ToAngleAxis(out tempangle, out tempaxis);
                qRef = Quaternion.AngleAxis(tempangle, new Vector3(-tempaxis.x, -tempaxis.y, tempaxis.z));
                q.ToAngleAxis(out tempangle, out tempaxis);
                q = Quaternion.AngleAxis(tempangle, new Vector3(-tempaxis.x, -tempaxis.y, tempaxis.z));
                qRel = Quaternion.Inverse(qRef) * q;
                Angles = Quat2Angle(qRel, "ZXY");
                break;
        }
        return Angles;

    }

    public Vector3 Quat2Angle(Quaternion q, string type)
    {
        Vector3 R = new Vector3(0f, 0f, 0f);
        float r11;
        float r12;
        float r21;
        float r31;
        float r32;
        q = q.normalized;
        switch (type)
        {
            case "YZY":
                r11 = 2 * (q.y * q.z + q.w * q.x);
                r12 = -2 * (q.x * q.y - q.w * q.z);
                r21 = (q.w * q.w) - (q.x * q.x) + (q.y * q.y) - (q.z * q.z);
                r31 = 2 * (q.y * q.z - q.w * q.x);
                r32 = 2 * (q.x * q.y + q.w * q.z);
                R = twoaxisrot(r11, r12, r21, r31, r32, type, q);
                break;
            case "ZXY":
                r21 = 2 * (q.y * q.z + q.w * q.x);
                r11 = -2 * (q.x * q.y - q.w * q.z);
                r12 = (q.w * q.w) - (q.x * q.x) + (q.y * q.y) - (q.z * q.z);
                r31 = -2 * (q.x * q.z - q.w * q.y);
                r32 = (q.w * q.w) - (q.x * q.x) - (q.y * q.y) + (q.z * q.z);
                R = threeaxisrot(r11, r12, r21, r31, r32, type, q);
                break;
        }

        return R;
    }

    Vector3 twoaxisrot(float r11, float r12, float r21, float r31, float r32, string type, Quaternion q)
    {
        Vector3 R = new Vector3(0f, 0f, 0f);
        // Check for singularities
        bool r1comp = (r11 == 0) && (r12 == 0);
        bool r3comp = (r31 == 0) && (r32 == 0);
        bool OrR1R3 = r1comp || r3comp;
        bool NorR1R3 = !OrR1R3;
        if (OrR1R3)
        {
            R = dcm2angle(quat2dcm(q), type, "zeror3");
        }
        if (NorR1R3)
        {
            R.x = Mathf.Atan2(r11, r12) * Mathf.Rad2Deg;
            r21 = (r21 > -1) ? r21 : -1;
            r21 = (r21 < 1) ? r21 : 1;
            R.y = Mathf.Acos(r21) * Mathf.Rad2Deg; 
            R.z = Mathf.Atan2(r31, r32) * Mathf.Rad2Deg; 
        }
        return R;
    }

    Vector3 threeaxisrot(float r11, float r12, float r21, float r31, float r32, string type, Quaternion q)
    {
        Vector3 R = new Vector3(0f, 0f, 0f);
        R.x = Mathf.Atan2(r11, r12) * Mathf.Rad2Deg;
        r21 = (r21 > -1) ? r21 : -1;
        r21 = (r21 < 1) ? r21 : 1;
        //Debug.Log(r21);
        R.y = Mathf.Asin(r21) * Mathf.Rad2Deg;
        R.z = Mathf.Atan2(r31, r32) * Mathf.Rad2Deg;
        return R;
    }

    Vector3 dcm2angle(List<float[]> DCM, string type, string Limitation)
    {
        Vector3 R = new Vector3(0f, 0f, 0f);
        switch (type)
        {
            case "YZY":
                //     [cy2 * cz * cy - sy2 * sy, cy2 * sz, -cy2 * cz * sy - sy2 * cy]
                //    [-cy * sz, cz, sy * sz]
                //     [sy2 * cz * cy + cy2 * sy, sy2 * sz, -sy2 * cz * sy + cy2 * cy]
                float r11 = DCM[1][2];
                float r12 = -DCM[1][0];
                float r21 = DCM[1][1];
                float r31 = DCM[2][1];
                float r32 = DCM[0][1];
                float r11a = DCM[2][0];
                float r12a = DCM[2][2];
                R.x = Mathf.Atan2(r11, r12) * Mathf.Rad2Deg; 
                r21 = (r21 < -1) ? -1 : r21;
                r21 = (r21 > 1) ? 1 : r21;
                R.y = Mathf.Acos(r21) * Mathf.Rad2Deg; 
                R.z = Mathf.Atan2(r31, r32) * Mathf.Rad2Deg; 
                if (Limitation == "zeror3")
                {
                    R.x = Mathf.Atan2(r11a, r12a) * Mathf.Rad2Deg; 
                    R.z = 0;
                }
                break;
        }
        return R;
    }

    List<float[]> quat2dcm(Quaternion q)
    {
        List<float[]> DCM = new List<float[]>();
        q = q.normalized;
        float[] F1 = new float[3];
        float[] F2 = new float[3];
        float[] F3 = new float[3];
        F1[0] = q.w * q.w + q.x * q.x - q.y * q.y - q.z * q.z;
        F1[1] = 2 * (q.x * q.y + q.w * q.z);
        F1[2] = 2 * (q.x * q.z - q.w * q.y);
        F2[0] = 2 * (q.x* q.y - q.w * q.z);
        F2[1] = q.w * q.w - q.x * q.x + q.y * q.y - q.z * q.z;
        F2[2] = 2 * (q.y * q.z + q.w * q.x);
        F3[0] = 2 * (q.x * q.z + q.w * q.y);
        F3[1] = 2 * (q.y * q.z - q.w * q.x);
        F3[2] = q.w * q.w - q.x * q.x - q.y * q.y + q.z * q.z;
        DCM.Add(F1);
        DCM.Add(F2);
        DCM.Add(F3);
        return DCM;
    }

    float[] getleftarm(Quaternion back, Quaternion arm, Quaternion wrist)
    {
        float[] lefthand = new float[5];
        Quaternion Qi = new Quaternion(1, 0, 0, 0);
        Quaternion Qj = new Quaternion(0, 1, 0, 0);
        Quaternion Qk = new Quaternion(0, 0, 1, 0);

        Quaternion Vxb = back * (Qi * Quaternion.Inverse(back));
        Quaternion Vyb_ = Quaternion.Inverse(back * (Qj * Quaternion.Inverse(back)));
        Quaternion Vzb_ = Quaternion.Inverse(back * (Qk * Quaternion.Inverse(back)));

        Quaternion Vxa = arm * (Qi * Quaternion.Inverse(arm));
        Quaternion Vya = arm * (Qj * Quaternion.Inverse(arm));
        Quaternion Vza = arm * (Qk * Quaternion.Inverse(arm));

        Quaternion Vxw = wrist * (Qi * Quaternion.Inverse(wrist));
        Quaternion Vyw = wrist * (Qj * Quaternion.Inverse(wrist));
        Quaternion Vzw = wrist * (Qk * Quaternion.Inverse(wrist));

        Vector3 JA = new Vector3(Vya.x, Vya.y, Vya.z);
        Vector3 IE = new Vector3(Vxb.x, Vxb.y, Vxb.z);//Vxb
        Vector3 JE = new Vector3(Vyb_.x, Vyb_.y, Vyb_.z);//Vyb_
        Vector3 KE = new Vector3(Vzb_.x, Vzb_.y, Vzb_.z);//Vzb_

        float[] V = { Vector3.Dot(JA, IE), Vector3.Dot(JA, JE), Vector3.Dot(JA, KE) };

        // shoulder extension flexion
        lefthand[0] = Mathf.Atan2(V[2], V[0]) * Mathf.Rad2Deg;
        if(-180 <= lefthand[0] && lefthand[0] < -90.0f)
        {
            lefthand[0] = 360 + lefthand[0];
        }

        // shoulder abduction adduction
        lefthand[2] = Mathf.Atan2(V[1], V[0]) * Mathf.Rad2Deg;
        if (-180 <= lefthand[2] && lefthand[2] < -90.0f)
        {
            lefthand[2] = 360 + lefthand[2];
        }

        // elbow extension flexion
        Vector3 VxaV = new Vector3(Vxa.x, Vxa.y, Vxa.z);
        Vector3 VyaV = new Vector3(Vya.x, Vya.y, Vya.z);
        Vector3 VxwV = new Vector3(Vxw.x, Vxw.y, Vxw.z);
        Vector3 VywV = new Vector3(Vyw.x, Vyw.y, Vyw.z);
        Vector3 VzwV = new Vector3(Vzw.x, Vzw.y, Vzw.z);
        Vector3 YW = VywV - Vector3.Dot(VywV, VxaV) * VxaV;
        float Temp = Mathf.Acos(Vector3.Dot(VyaV, YW) / YW.magnitude) * Mathf.Rad2Deg;
        if(!float.IsNaN(Temp))
        {
            lefthand[3] = Temp;
        }
        else
        {
            lefthand[3] = LeftAngles[3];
        }
        // elbow pronation supination
        Vector3 Ref = Vector3.Cross(VxaV, VywV);
        Ref = new Vector3(Vector3.Dot(Ref, VxwV), Vector3.Dot(Ref, VywV), Vector3.Dot(Ref, VzwV));
        lefthand[4] = Mathf.Atan2(-Ref.z,-Ref.x) * Mathf.Rad2Deg;

        // shoulder internal external rotation 
        if (lefthand[3] >= 30)
        {
            Vector3 Zref = KE - Vector3.Dot(KE, VyaV) * VyaV;
            Zref = Zref / Zref.magnitude;
            Vector3 Yref = JE - Vector3.Dot(JE, VyaV) * VyaV;
            Yref = Yref / Yref.magnitude;
            VywV = VywV - Vector3.Dot(VywV, Yref) * VyaV;
            lefthand[1] = Mathf.Atan2(Vector3.Dot(VywV, Yref), Vector3.Dot(VywV, Zref)) * Mathf.Rad2Deg;

        }

        return lefthand;
    }

    float[] getrightarm(Quaternion back, Quaternion arm, Quaternion wrist)
    {
        float[] lefthand = new float[5];
        Quaternion Qi = new Quaternion(1, 0, 0, 0);
        Quaternion Qj = new Quaternion(0, 1, 0, 0);
        Quaternion Qk = new Quaternion(0, 0, 1, 0);

        Quaternion Vxb = back * (Qi * Quaternion.Inverse(back));
        Quaternion Vyb_ = back * (Qj * Quaternion.Inverse(back));
        Quaternion Vzb_ = Quaternion.Inverse(back * (Qk * Quaternion.Inverse(back)));

        Quaternion Vxa = arm * (Qi * Quaternion.Inverse(arm));
        Quaternion Vya = arm * (Qj * Quaternion.Inverse(arm));
        Quaternion Vza = arm * (Qk * Quaternion.Inverse(arm));

        Quaternion Vxw = wrist * (Qi * Quaternion.Inverse(wrist));
        Quaternion Vyw = wrist * (Qj * Quaternion.Inverse(wrist));
        Quaternion Vzw = wrist * (Qk * Quaternion.Inverse(wrist));

        Vector3 JC = new Vector3(Vya.x, Vya.y, Vya.z);
        Vector3 IE = new Vector3(Vxb.x, Vxb.y, Vxb.z);//Vxb
        Vector3 JE = new Vector3(Vyb_.x, Vyb_.y, Vyb_.z);//Vyb_
        Vector3 KE = new Vector3(Vzb_.x, Vzb_.y, Vzb_.z);//Vzb_


        float[] V = { Vector3.Dot(JC, IE), Vector3.Dot(JC, JE), Vector3.Dot(JC, KE) };


        // shoulder extension flexion
        lefthand[0] = Mathf.Atan2(V[2], V[0]) * Mathf.Rad2Deg;
        if (-180 <= lefthand[0] && lefthand[0] < -90.0f)
        {
            lefthand[0] = 360 + lefthand[0];
        }

        // shoulder abduction adduction
        lefthand[2] = Mathf.Atan2(V[1], V[0]) * Mathf.Rad2Deg;
        if (-180 <= lefthand[2] && lefthand[2] < -90.0f)
        {
            lefthand[2] = 360 + lefthand[2];
        }

        
        // elbow extension flexion
        Vector3 VxaV = new Vector3(Vxa.x, Vxa.y, Vxa.z);
        Vector3 VyaV = new Vector3(Vya.x, Vya.y, Vya.z);
        Vector3 VxwV = new Vector3(Vxw.x, Vxw.y, Vxw.z);
        Vector3 VywV = new Vector3(Vyw.x, Vyw.y, Vyw.z);
        Vector3 VzwV = new Vector3(Vzw.x, Vzw.y, Vzw.z);
        Vector3 YW = VywV - Vector3.Dot(VywV, VxaV) * VxaV;
        float Temp = Mathf.Acos(Vector3.Dot(VyaV, YW) / YW.magnitude) * Mathf.Rad2Deg;
        if(!float.IsNaN(Temp))
        {
            lefthand[3] = Temp;
        }
        else
        {
            lefthand[3] = RightAngles[3];
        }
        // elbow pronation supination
        Vector3 Ref = Vector3.Cross(VxaV, VywV);
        Ref = new Vector3(Vector3.Dot(Ref, VxwV), Vector3.Dot(Ref, VywV), Vector3.Dot(Ref, VzwV));
        lefthand[4] = Mathf.Atan2(-Ref.z, -Ref.x) * Mathf.Rad2Deg;

        // shoulder internal external rotation 
        if (lefthand[3] >= 30)
        {
            Vector3 Zref = KE - Vector3.Dot(KE, VyaV) * VyaV;
            Zref = Zref / Zref.magnitude;
            Vector3 Yref = JE - Vector3.Dot(JE, VyaV) * VyaV;
            Yref = Yref / Yref.magnitude;
            VywV = VywV - Vector3.Dot(VywV, Yref) * VyaV;
            lefthand[1] = Mathf.Atan2(Vector3.Dot(VywV, Yref), Vector3.Dot(VywV, Zref)) * Mathf.Rad2Deg;

        }
        return lefthand;
    }

    Quaternion Rotatequat(Quaternion Q)
    {
        Quaternion Qk = new Quaternion(0, 0, 1, 0);
        Quaternion Qe = Quaternion.identity;
        Quaternion Qz = Q * Qk * Quaternion.Inverse(Q);
        Qz = new Quaternion(Qz.x * Mathf.Sin(Mathf.PI / 4), Qz.y * Mathf.Sin(Mathf.PI / 4), Qz.z * Mathf.Sin(Mathf.PI / 4), Mathf.Cos(Mathf.PI / 4));
        Qe = Qz * Q;
        Quaternion Qi = new Quaternion(1, 0, 0, 0);
        Quaternion Qx = Qe * Qi * Quaternion.Inverse(Qe);
        Qx = new Quaternion(Qx.x * Mathf.Sin(-Mathf.PI / 4), Qx.y * Mathf.Sin(-Mathf.PI / 4), Qx.z * Mathf.Sin(-Mathf.PI / 4), Mathf.Cos(-Mathf.PI / 4));
        Qe = Qx * Qe;
        return Qe;
    }

    Quaternion BackAdjust(Quaternion Q)
    {
        Quaternion Qe = Quaternion.identity;

        Quaternion Qj = new Quaternion(0, 1, 0, 0);
        Quaternion Qy = Q * Qj * Quaternion.Inverse(Q);
        Vector3 Y = new Vector3(Qy.x, Qy.y, Qy.z);
        float th = -Mathf.Atan2(Y.x,Y.y);
        Quaternion Qref = new Quaternion(0, 0, Mathf.Sin(th / 2), Mathf.Cos(th / 2));

        Qj = new Quaternion(0, 1, 0, 0);
        Qy = Qref * Qj * Quaternion.Inverse(Qref);
        Qy = new Quaternion(Qy.x * Mathf.Sin(Mathf.PI / 4), Qy.y * Mathf.Sin(Mathf.PI / 4), Qy.z * Mathf.Sin(Mathf.PI / 4), Mathf.Cos(Mathf.PI / 4));
        Qref = Qy * Qref;
        Qe = Quaternion.Inverse(Qref) * Q;
        return Qe;
    }

    Quaternion RotateLeftArm(Quaternion Q)
    {
        Quaternion Qk = new Quaternion(0, 0, 1, 0);
        Quaternion Qe = Quaternion.identity;
        Quaternion Qz = Q * Qk * Quaternion.Inverse(Q);
        Qz = new Quaternion(Qz.x * Mathf.Sin(Mathf.PI / 2), Qz.y * Mathf.Sin(Mathf.PI / 2), Qz.z * Mathf.Sin(Mathf.PI / 2), Mathf.Cos(Mathf.PI / 2));
        Qe = Qz * Q;
        return Qe;
    }

    Quaternion RotateLeftForeArm(Quaternion Q)
    {
        Quaternion Qj = new Quaternion(0, 1, 0, 0);
        Quaternion Qe = Quaternion.identity;
        Quaternion Qy = Q * Qj * Quaternion.Inverse(Q);
        Qy = new Quaternion(Qy.x * Mathf.Sin(Mathf.PI / 4), Qy.y * Mathf.Sin(Mathf.PI / 4), Qy.z * Mathf.Sin(Mathf.PI / 4), Mathf.Cos(Mathf.PI / 4));
        Qe = Qy * Q;
        return Qe;
    }

    Quaternion RotateRightForeArm(Quaternion Q)
    {
        Quaternion Qj = new Quaternion(0, -1, 0, 0);
        Quaternion Qe = Quaternion.identity;
        Quaternion Qy = Q * Qj * Quaternion.Inverse(Q);
        Qy = new Quaternion(Qy.x * Mathf.Sin(Mathf.PI / 4), Qy.y * Mathf.Sin(Mathf.PI / 4), Qy.z * Mathf.Sin(Mathf.PI / 4), Mathf.Cos(Mathf.PI / 4));
        Qe = Qy * Q;
        return Qe;
    }
    
    void InvokeMultipleStream()
    {
        if (sp != null)
        {
            float ts = 0;
            ts = Time.realtimeSinceStartup;
            Line = "";
            if (ReadFromArduino())
            {
                ParseAngles();
            }
            ts = Time.realtimeSinceStartup - ts;
            if (ts > Hts)
            {
                Hts = ts;
            }
        }
        else
        {
            Verbose_Logging("No Device Connected");
        }
    }

    public void WriteToArduino(string message)
    {
        sp.WriteLine(message);
        sp.BaseStream.Flush();
    }

    public bool ReadFromArduino()
    {
        bool ReadStatus = true;
        if (sp!= null && sp.IsOpen)
        {
            try
            {
                Line = sp.ReadLine();
                DebugText.text = Line;
                ReadStatus = true;
                timeout = 0;
                sp.BaseStream.Flush();
            }
            catch (System.TimeoutException)
            {
                timeout = timeout + 1;
                Verbose_Logging("Receive Data Error. Read Timeout");
                ReadStatus = false;
            }

            if (timeout > 50)
            {
                sp.Close();
                Invoke("ResetComPort", 1.0f);
            }
        }
        return ReadStatus;
    }

    void ResetComPort()
    {
        timeout = 0;
        Initialize();
    }

    private void NoData()
    {
        foreach(Image Img in DataImages)
        {
            Img.color = new Color32(255, 0, 0, 255);
        }
    }

    public void ParseAngles()
    {
        NoData();
        bool ReadStatus = true;
        string[] forces = Line.Split(',');
        if (forces.Length == 5 || forces.Length == 10 || forces.Length == 15 || forces.Length == 20)
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
                    for (int i = 0; i < forces.Length/no_devices; i++)
                    {
                        Verbose_Logging("Got Data");
                        switch (forces[i * 5])//5 i the number of elements in a data set. a,w,x,y,z
                        {
                            case "a":
                                w[0] = (float.Parse(forces[(5 * i) + 1]) * 2.0f / 999.0f) - 1.0f;
                                x[0] = (float.Parse(forces[(5 * i) + 2]) * 2.0f / 999.0f) - 1.0f;
                                y[0] = (float.Parse(forces[(5 * i) + 3]) * 2.0f / 999.0f) - 1.0f;
                                z[0] = (float.Parse(forces[(5 * i) + 4]) * 2.0f / 999.0f) - 1.0f;
                                if (!_device[0])
                                {
                                    L_Forearm = new Quaternion(x[0], y[0], z[0], w[0]);
                                    L_Forearm = Quaternion.Inverse(L_Forearm);
                                    Verbose_Logging(L_Forearm + " A");// Device A
                                    _device[0] = true;
                                }
                                Calibrations[0] = new int[] { 3, 3, 3, 3 };
                                DataImages[0].color = new Color32(0, 200, 0, 255);
                                break;
                            case "b":
                                w[1] = (float.Parse(forces[(5 * i) + 1]) * 2.0f / 999.0f) - 1.0f;
                                x[1] = (float.Parse(forces[(5 * i) + 2]) * 2.0f / 999.0f) - 1.0f;
                                y[1] = (float.Parse(forces[(5 * i) + 3]) * 2.0f / 999.0f) - 1.0f;
                                z[1] = (float.Parse(forces[(5 * i) + 4]) * 2.0f / 999.0f) - 1.0f;
                                if (!_device[1])
                                {
                                    R_Forearm = new Quaternion(x[1], y[1], z[1], w[1]);
                                    R_Forearm = Quaternion.Inverse(R_Forearm);
                                    Verbose_Logging(R_Forearm + " B");// Device B
                                    _device[1] = true;
                                }
                                Calibrations[1] = new int[] { 3, 3, 3, 3 };
                                DataImages[1].color = new Color32(0, 200, 0, 255);
                                break;
                            case "c":
                                w[2] = (float.Parse(forces[(5 * i) + 1]) * 2.0f / 999.0f) - 1.0f;
                                x[2] = (float.Parse(forces[(5 * i) + 2]) * 2.0f / 999.0f) - 1.0f;
                                y[2] = (float.Parse(forces[(5 * i) + 3]) * 2.0f / 999.0f) - 1.0f;
                                z[2] = (float.Parse(forces[(5 * i) + 4]) * 2.0f / 999.0f) - 1.0f;
                                if (!_device[2])
                                {
                                    L_Arm = new Quaternion(x[2], y[2], z[2], w[2]);
                                    L_Arm = Quaternion.Inverse(L_Arm);
                                    Verbose_Logging(L_Arm + " C");// Device C
                                    _device[2] = true;
                                }
                                Calibrations[2] = new int[] { 3, 3, 3, 3 };
                                DataImages[2].color = new Color32(0, 200, 0, 255);
                                break;
                            case "d":
                                w[3] = (float.Parse(forces[(5 * i) + 1]) * 2.0f / 999.0f) - 1.0f;
                                x[3] = (float.Parse(forces[(5 * i) + 2]) * 2.0f / 999.0f) - 1.0f;
                                y[3] = (float.Parse(forces[(5 * i) + 3]) * 2.0f / 999.0f) - 1.0f;
                                z[3] = (float.Parse(forces[(5 * i) + 4]) * 2.0f / 999.0f) - 1.0f;
                                if (!_device[3])
                                {
                                    R_Arm = new Quaternion(x[3], y[3], z[3], w[3]);
                                    R_Arm = Quaternion.Inverse(R_Arm);
                                    Verbose_Logging(R_Arm + " D");// Device D
                                    _device[3] = true;
                                }
                                Calibrations[3] = new int[] { 3, 3, 3, 3 };
                                DataImages[3].color = new Color32(0, 200, 0, 255);
                                break;
                            case "e":
                                w[4] = (float.Parse(forces[(5 * i) + 1]) * 2.0f / 999.0f) - 1.0f;
                                x[4] = (float.Parse(forces[(5 * i) + 2]) * 2.0f / 999.0f) - 1.0f;
                                y[4] = (float.Parse(forces[(5 * i) + 3]) * 2.0f / 999.0f) - 1.0f;
                                z[4] = (float.Parse(forces[(5 * i) + 4]) * 2.0f / 999.0f) - 1.0f;
                                if (!_device[3])
                                {
                                    _Back = new Quaternion(x[4], y[4], z[4], w[4]);
                                    _Back = Quaternion.Inverse(_Back);
                                    Verbose_Logging(R_Arm + " E");// Device E
                                    _device[4] = true;
                                }
                                Calibrations[4] = new int[] { 3, 3, 3, 3 };
                                DataImages[4].color = new Color32(0, 200, 0, 255);
                                break;
                            default:
                                break;
                        }
                    }
                }
                catch (System.FormatException)
                {
                    Verbose_Logging("Format Error");
                }

            }
        }
        else if (forces.Length == 6)
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
                    if(forces[0] == "cal")
                      Verbose_Logging(" Recieved Calibratoin Data");
                        switch (forces[1])//5 i the number of elements in a data set. a,w,x,y,z
                        {
                            case "a":
                            Calibrations[0] = new int[] { int.Parse(forces[2]), int.Parse(forces[3]), int.Parse(forces[4]), int.Parse(forces[5]) };
                                break;
                            case "b":
                            Calibrations[1] = new int[] { int.Parse(forces[2]), int.Parse(forces[3]), int.Parse(forces[4]), int.Parse(forces[5]) };
                            break;
                            case "c":
                            Calibrations[2] = new int[] { int.Parse(forces[2]), int.Parse(forces[3]), int.Parse(forces[4]), int.Parse(forces[5]) };
                            break;
                            case "d":
                            Calibrations[3] = new int[] { int.Parse(forces[2]), int.Parse(forces[3]), int.Parse(forces[4]), int.Parse(forces[5]) };
                            break;
                            case "e":
                            Calibrations[4] = new int[] { int.Parse(forces[2]), int.Parse(forces[3]), int.Parse(forces[4]), int.Parse(forces[5]) };
                            break;
                            default:
                                break;
                        }
                        //Save_Statics();
                    
                }
                catch (System.FormatException)
                {
                    Verbose_Logging("Format Error");
                }
            }
        }
        else
        {
            Debug.Log("Wrong Data: "+ Line);
        }
    }
    #endregion

    #region Buttons
    public void Save_Statics()
    {
        CancelInvoke();
        start = true;
        SaveStatics = true;
        InvokeRepeating("InvokeMultipleStream", 0f, TimeTakenforOneMsg);//Remember Invoke doesnt happen when time.timescale is 0
        InvokeRepeating("Invokeup_date", 0f, 0.01f);
        //Remove all the calibration settings
        for (int i = 0; i < no_devices; i++)
        {
            _device[i] = false;
        }
    }

    public void Exit()
    {
        L_Arm = Quaternion.identity;
        L_Forearm = Quaternion.identity;
        R_Arm = Quaternion.identity;
        R_Forearm = Quaternion.identity;
        start = false;
        StopAllCoroutines();
        CancelInvoke();
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
