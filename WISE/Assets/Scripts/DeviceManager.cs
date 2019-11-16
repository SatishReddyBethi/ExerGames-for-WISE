using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.SceneManagement;

public class DeviceManager : MonoBehaviour
{
    public Dropdown Age;
    public Dropdown Gender;
    public Dropdown Difficulty;
    public Dropdown Dexterity;
    public Dropdown Activties;
    public Dropdown PatientMenu_Activities;
    public GameObject cam;
    public Dropdown CameraView;
    public Slider[] Sys;
    public Slider[] Acc;
    public Slider[] Gyro;
    public Slider[] Magneto;

    public InputField Name;
    public string PatientName;
    public string gender;
    public string dexterity;
    public string difficulty;
    public string age;
    public string CurrentActivity;
    private Dictionary<string, string> PatientDictionary;
    private string PatientData;
    private static string savedDataPath;
    private static string savedAngleDataPath;
    private string dictionaryFullName_P;
    private string PlayerInfo;
    public Text Msg;
    public GameObject UI;
    private Connection Conn;
    float StartTime;
    float CurrTime;
    string LocalAnglesCache;
    public bool ActivityChanged;
    public int SavingIteration;
    public bool pause;
    public Transform[] CameraTransforms;
    public GameObject[] PlayerTexts;
    public bool CameraChangeDone = false;
    public GameObject[] PlayerModels;
    public List<string> ActivitiesNames;
    public List<string> Rec_Activities;
    private RecordActivity RA;
    private Playback PB;
    public Toggle RecordedAct;
    public Quaternion TestRef;
    public Quaternion TestQuat;
    public string TestConfig;
    public Vector3 TestAngles;


    private void Start()
    {
        pause = true;
        RA = GetComponent<RecordActivity>();
        PB = GetComponent<Playback>();
        ActivitiesNames = new List<string>();
        Rec_Activities = new List<string>();
        foreach (var Option in Activties.options)
        {
            ActivitiesNames.Add(Option.text);
        }
        RefreshActivities();
        PatientDictionary = new Dictionary<string, string>();
        PatientData = "";
        savedDataPath = Application.persistentDataPath + "/savedData";
        savedAngleDataPath = Application.persistentDataPath + "/savedAngleDataPath";
        Conn = GetComponent<Connection>();
        PatientMenu_Activities.options = Activties.options;
        cam.transform.position = CameraTransforms[CameraView.value].position;
        cam.transform.rotation = CameraTransforms[CameraView.value].rotation;
        CurrentActivity = Activties.options[Activties.value].text;
    }

    public void RefreshActivities()
    {
        RA.GetActivities();
        Rec_Activities.Clear();
        Rec_Activities.AddRange(RA.ActivityFileNames);
        Activties.ClearOptions();
        Activties.AddOptions(Rec_Activities);
        SetActivities();
    }

    private void Update()
    {
        UpdateCalibration();
        TestEuler();
    }

    public void TestEuler()
    {
        TestAngles = Conn.Quat2Angle(TestQuat, TestConfig);
        //Debug.Log(TestRef);
        //TestAngles = Conn.GetAngles("LA", TestRef, TestQuat);
    }
    public void SetActivities()
    {
        if (RecordedAct.isOn)
        {
            Activties.ClearOptions();
            Activties.AddOptions(Rec_Activities);
        }
        else
        {
            Activties.ClearOptions();
            Activties.AddOptions(ActivitiesNames);
        }
        PatientMenu_Activities.options = Activties.options;
    }

    public void GameStart(bool Save)
    {
        if (Save)
        {
            pause = false;
            string Time_ = System.DateTime.Now.ToString("_dd_MM_yyyy_HH_mm_ss");

            PatientName = Name.text;
            gender = Gender.options[Gender.value].text;
            dexterity = Dexterity.options[Dexterity.value].text;
            difficulty = Difficulty.options[Difficulty.value].text;
            age = Age.options[Age.value].text;
            CurrentActivity = Activties.options[Activties.value].text;
            if (PatientName == "")
            {
                PatientName = "Unknown_Subject";
            }

            dictionaryFullName_P = savedDataPath + "/" + PatientName + "/" + CurrentActivity + "/" + PatientName + Time_ + ".txt";
            dictionaryFullName_PA = savedAngleDataPath + "/" + PatientName + "/" + CurrentActivity + "/" + PatientName + Time_ + ".txt";
            string Patient_Data = age + "," + gender + "," + dexterity + "," + difficulty;
            if (PatientDictionary.ContainsKey(PatientName) == false)
            {
                PatientDictionary.Add(PatientName, Patient_Data);
            }
            UI.SetActive(false);
            //Time.timeScale = 1.0f;
            Conn.Save_Statics();
            SaveEveryIteration(Patient_Data + "\n", true);

            StartTime = Time.realtimeSinceStartup;
            InvokeRepeating("Saving", 0f, 0.01f);
            ChangeCameraView();
        }
        else
        {
            pause = false;
            UI.SetActive(false);
            Conn.Save_Statics();
            PlayerModels[0].transform.localPosition = new Vector3(-1.3f, 0.5f, -1.7f);
            PlayerModels[1].transform.localPosition = new Vector3(-1.3f, 0.5f, -1.6f);
        }
    }


    public PlayerController PC;
    private string GlobalAngles;
    string dictionaryFullName_PA;
    public int SavingAngleIteration;
    void Saving()
    {
        if (ActivityChanged)
        {
            SaveEveryIteration(LocalAnglesCache, false);//If Replace is true, it replaces all the data
            SaveAnglesEveryIteration(GlobalAngles, false);
            LocalAnglesCache = "";
            GlobalAngles = "";
            CurrentActivity = Activties.options[Activties.value].text;
            string Time_ = System.DateTime.Now.ToString("_dd_MM_yyyy_HH_mm_ss");
            string Patient_Data = age + "," + gender + "," + dexterity + "," + difficulty;
            dictionaryFullName_P = savedDataPath + "/" + PatientName + "/" + CurrentActivity + "/" + PatientName + Time_ + ".txt";
            dictionaryFullName_PA = savedAngleDataPath + "/" + PatientName + "/" + CurrentActivity + "/" + PatientName + Time_ + ".txt";
            SaveEveryIteration(Patient_Data + "\n", true);
            SaveAnglesEveryIteration(Patient_Data + "\n", true);
            SavingIteration = 0;
            SavingAngleIteration = 0;
            StartTime = Time.realtimeSinceStartup;
            ActivityChanged = false;
        }
        CurrTime = Time.realtimeSinceStartup - StartTime;
        if (Conn.DeviceLocalAngles != "")
        {
            LocalAnglesCache += Conn.DeviceLocalAngles + "," + CurrTime.ToString("F3") + "\n";
            SavingIteration += 1;
        }

        /*if(PC.AngleText != "")//Kinect Angle System 
        {
            GlobalAngles += PC.AngleText + "\n";
            SavingAngleIteration += 1;
        }*/

        if (Conn.JCSAngles != "")
        {
            GlobalAngles += Conn.JCSAngles + "," + CurrTime.ToString("F3") + "\n";
            SavingAngleIteration += 1;
        }

        if (SavingIteration >= 10)
        {
            SavingIteration = 0;
            SaveEveryIteration(LocalAnglesCache, false);//If Replace is true, it replaces all the data
            LocalAnglesCache = "";
        }

        if (SavingAngleIteration >= 10)
        {
            SavingAngleIteration = 0;
            SaveAnglesEveryIteration(GlobalAngles, false);
            GlobalAngles = "";
        }

    }

    public void End()
    {
        Conn.Exit();
        Tab_Shift();
        //Time.timeScale = 0.0f;
        PatientData = Conn.DeviceLocalAngles;
        //SaveInfo_P();
        //StopAllCoroutines();
        CancelInvoke();
        pause = true;
        PlayerTexts[0].transform.rotation = Quaternion.Euler(0f, CameraView.value * 180.0f, 0f);
        PlayerTexts[1].transform.rotation = Quaternion.Euler(0f, CameraView.value * 180.0f, 0f);
        PlayerModels[0].transform.rotation = Quaternion.Euler(0f, CameraView.value * -20.0f, 0f);
        PlayerModels[1].transform.rotation = Quaternion.Euler(0f, CameraView.value * 20.0f, 0f);
        PlayerModels[0].transform.localPosition = new Vector3(0.7f, 0.5f, -1.7f);
        PlayerModels[1].transform.localPosition = new Vector3(-3.34f, 0.5f, -1.6f);
    }

    #region MainMenu UI

    public Dropdown TabShift;
    public GameObject[] Tabs;
    public int CameraViewValue;
    public void ChangeCamerawhileAnimation()
    {
        cam.transform.position = CameraTransforms[CameraViewValue].position;
        cam.transform.rotation = CameraTransforms[CameraViewValue].rotation;
        if (CameraViewValue == 0 || CameraViewValue == 1)//Back and Front  
        {
            PlayerTexts[0].transform.rotation = Quaternion.Euler(0f, CameraViewValue * 180.0f, 0f);
            PlayerTexts[1].transform.rotation = Quaternion.Euler(0f, CameraViewValue * 180.0f, 0f);
            //Rotating Models when the camera view changes
            PlayerModels[0].transform.rotation = Quaternion.Euler(0f, CameraViewValue * -20.0f, 0f);
            PlayerModels[1].transform.rotation = Quaternion.Euler(0f, CameraViewValue * 20.0f, 0f);
            PlayerModels[0].transform.localPosition = new Vector3(0.7f, 0.5f, -1.7f);
            PlayerModels[1].transform.localPosition = new Vector3(-3.34f, 0.5f, -1.6f);
        }
        else if (CameraViewValue == 2)//Right
        {
            PlayerTexts[0].transform.rotation = Quaternion.Euler(0f, 90.0f, 0f);
            PlayerTexts[1].transform.rotation = Quaternion.Euler(0f, 90.0f, 0f);
            PlayerModels[0].transform.localPosition = new Vector3(0.7f, 0.5f, -3.7f);
            PlayerModels[1].transform.localPosition = new Vector3(-3.34f, 0.5f, -1.6f);
            PlayerTexts[0].transform.rotation = Quaternion.Euler(0f, -90.0f, 0f);
            PlayerTexts[1].transform.rotation = Quaternion.Euler(0f, -90.0f, 0f);
        }
        else if (CameraViewValue == 3)//Left
        {
            PlayerTexts[0].transform.rotation = Quaternion.Euler(0f, -90.0f, 0f);
            PlayerTexts[1].transform.rotation = Quaternion.Euler(0f, -90.0f, 0f);
            PlayerModels[0].transform.localPosition = new Vector3(0.7f, 0.5f, -1.7f);
            PlayerModels[1].transform.localPosition = new Vector3(-3.34f, 0.5f, -3.6f);
            PlayerTexts[0].transform.rotation = Quaternion.Euler(0f, 90.0f, 0f);
            PlayerTexts[1].transform.rotation = Quaternion.Euler(0f, 90.0f, 0f);
        }
    }

    public Dropdown ConfigureView;
    public void ConfigureCameraView()
    {
        cam.transform.position = CameraTransforms[ConfigureView.value].position;
        cam.transform.rotation = CameraTransforms[ConfigureView.value].rotation;
    }

    public void ChangeCameraView()
    {
        cam.transform.position = CameraTransforms[CameraView.value].position;
        cam.transform.rotation = CameraTransforms[CameraView.value].rotation;
        if(CameraView.value == 0 || CameraView.value == 1)//Back and Front  
        {
            PlayerTexts[0].transform.rotation = Quaternion.Euler(0f, CameraView.value * 180.0f, 0f);
            PlayerTexts[1].transform.rotation = Quaternion.Euler(0f, CameraView.value * 180.0f, 0f);
            //Rotating Models when the camera view changes
            PlayerModels[0].transform.rotation = Quaternion.Euler(0f, CameraView.value * -20.0f, 0f);
            PlayerModels[1].transform.rotation = Quaternion.Euler(0f, CameraView.value * 20.0f, 0f);
            PlayerModels[0].transform.localPosition = new Vector3(0.7f, 0.5f, -1.7f);
            PlayerModels[1].transform.localPosition = new Vector3(-3.34f, 0.5f, -1.6f);
        }
        else if(CameraView.value == 2)//Right
        {
            PlayerTexts[0].transform.rotation = Quaternion.Euler(0f, 90.0f, 0f);
            PlayerTexts[1].transform.rotation = Quaternion.Euler(0f, 90.0f, 0f);
            PlayerModels[0].transform.localPosition = new Vector3(0.7f, 0.5f, -3.7f);
            PlayerModels[1].transform.localPosition = new Vector3(-3.34f, 0.5f, -1.6f);
            PlayerTexts[0].transform.rotation = Quaternion.Euler(0f, -90.0f, 0f);
            PlayerTexts[1].transform.rotation = Quaternion.Euler(0f, -90.0f, 0f);
        }
        else if(CameraView.value == 3)//Left
        {
            PlayerTexts[0].transform.rotation = Quaternion.Euler(0f, -90.0f, 0f);
            PlayerTexts[1].transform.rotation = Quaternion.Euler(0f, -90.0f, 0f);
            PlayerModels[0].transform.localPosition = new Vector3(0.7f, 0.5f, -1.7f);
            PlayerModels[1].transform.localPosition = new Vector3(-3.34f, 0.5f, -3.6f);
            PlayerTexts[0].transform.rotation = Quaternion.Euler(0f, 90.0f, 0f);
            PlayerTexts[1].transform.rotation = Quaternion.Euler(0f, 90.0f, 0f);
        }
        
    }
    public void OnActivityChange()
    {
        ActivityChanged = true;
        CameraChangeDone = false;
        PC.ActivityIteration = 0;
        if(pause)
        {
            Activties.value = PatientMenu_Activities.value;
        }
        else
        {
            PatientMenu_Activities.value = Activties.value;
        }
        //Loading Recorded Activity
        PC.Act_iteration = 0;
        PC.Act_Timer = 0;
        PC.percentage = 0;
        PB.LoadActivityFile(Activties.options[Activties.value].text);
    }

    public void NextActivity()
    {
        if (Activties.value != Activties.options.Count - 1)
        {
            Activties.value++;
            OnActivityChange();
        }
    }

    public void PreviousActivity()
    {
        if (Activties.value != 0)
        {
            Activties.value--;
            OnActivityChange();
        }
    }

    public void Tab_Shift()
    {
        DisableAllTabs();
        Tabs[TabShift.value].SetActive(true);
    }

    void DisableAllTabs()
    {
        for (int i = 0; i < Tabs.Length; i++)
        {
            Tabs[i].SetActive(false);
        }
    }

    public void UpdateCalibration()
    {
        for(int i = 0; i < 5; i++)
        {
            Sys[i].value = Conn.Calibrations[i][0] / 3.0f;
            Acc[i].value = Conn.Calibrations[i][1] / 3.0f;
            Gyro[i].value = Conn.Calibrations[i][2] / 3.0f;
            Magneto[i].value = Conn.Calibrations[i][3] / 3.0f;
        }
    
    //    Sys.value = Conn.Calibrations[IMU.value][0] / 3.0f;
    //    Acc.value = Conn.Calibrations[IMU.value][1] / 3.0f;
    //    Gyro.value = Conn.Calibrations[IMU.value][2] / 3.0f;
    //    Magneto.value = Conn.Calibrations[IMU.value][3] / 3.0f;
    }
    #endregion

    #region Save and Load
    /*void LoadInfo_P()
    {
        PatientDictionary = new Dictionary<string, string>();

        // CreateDirectory() checks for existence and 
        // automagically creates the directory if necessary
        Directory.CreateDirectory(savedDataPath + "/" + PatientName + "/" + CurrentActivity);

        string[] filePaths = Directory.GetFiles(savedDataPath + "/" + PatientName + "/" + CurrentActivity, "*.txt");

        // the file is a simple key,value list, one dictionary item per line
        if (File.Exists(dictionaryFullName_P))
        {
            string[] fileContent = File.ReadAllLines(dictionaryFullName_P);
            int i = 0;
            foreach (string line in fileContent)
            {
                if (i == 0)
                {
                    string[] buffer = line.Split(',');
                    if (buffer.Length == 5)
                    {
                        string playerinfo = buffer[1] + buffer[2] + buffer[3] + buffer[4];
                        PatientDictionary.Add(buffer[0], playerinfo);
                    }
                }
                else
                {

                }
                i++;
            }
        }
    }
    */
    //void SaveInfo_P()
    //{
    //    if (!File.Exists(dictionaryFullName_P))
    //    {
    //        Directory.CreateDirectory(savedDataPath + "/" + PatientName + "/" + CurrentActivity);
    //    }
    //        if (PatientDictionary != null)
    //    {
    //        string fileContent = "";

    //        /*foreach (var item in PatientDictionary)
    //        {
    //            fileContent += item.Key + "," + item.Value + "\n";
    //        }*/
    //        fileContent += PatientName + "," + age + "," + gender + "," + dexterity + "," + difficulty + "\n";//First line of file with patient data
    //        foreach (var item in PatientData)
    //        {
    //            fileContent += item + "\n";
    //        }

    //        File.WriteAllText(dictionaryFullName_P, fileContent);
    //    }
    //}

    void SaveEveryIteration(string Data, bool Replace)
    {
        if (!File.Exists(dictionaryFullName_P))
        {
            Directory.CreateDirectory(savedDataPath + "/" + PatientName + "/" + CurrentActivity);
        }

        if (Replace)
        {
            File.WriteAllText(dictionaryFullName_P, Data);
        }
        else
        {
            File.AppendAllText(dictionaryFullName_P, Data);
        }
    }

    void SaveAnglesEveryIteration(string Data, bool Replace)
    {
        if (!File.Exists(dictionaryFullName_PA))
        {
            Directory.CreateDirectory(savedAngleDataPath + "/" + PatientName + "/" + CurrentActivity);
        }
        if (Replace)
        {
            File.WriteAllText(dictionaryFullName_PA, Data);
        }
        else
        {
            File.AppendAllText(dictionaryFullName_PA, Data);
        }
    }

    //void LoadData_P()
    //{
    //    PatientDictionary = new List<int[]>();

    //    // CreateDirectory() checks for existence and 
    //    // automagically creates the directory if necessary
    //    Directory.CreateDirectory(savedDataPath);

    //    // the file is a simple key,value list, one dictionary item per line
    //    if (File.Exists(dictionaryFullName_P))
    //    {
    //        string[] fileContent = File.ReadAllLines(dictionaryFullName_P);

    //        foreach (string line in fileContent)
    //        {
    //            string[] buffer = line.Split(',');
    //            if (buffer.Length == 3)
    //            {
    //                int[] angles = new int[] { int.Parse(buffer[0]), int.Parse(buffer[1]), int.Parse(buffer[2]) };
    //                PatientDictionary.Add(angles);
    //            }
    //        }
    //    }
    //}

    //void SaveData_P()
    //{
    //    if (PatientDictionary != null)
    //    {
    //        string fileContent = "";

    //        foreach (var item in PatientDictionary)
    //        {
    //            fileContent += item[0] + "," + item[1] + "," + item[2] + "\n";
    //        }

    //        File.WriteAllText(dictionaryFullName_P, fileContent);
    //    }
    //}
    #endregion
    public void Quit()
    {
        Application.Quit();
    }
}
