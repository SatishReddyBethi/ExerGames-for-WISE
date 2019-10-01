using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class RecordActivity : MonoBehaviour
{
    public Dictionary<string, List<string>> Activity;
    public InputField ActivityName;
    public List<string> ActivityKeys;
    public Quaternion[] Angles;
    private Connection Conn;
    public float TimeTaken;
    public int KeyNo;
    public Transform[] CameraTransforms;
    public GameObject cam;
    public Dropdown CameraView;
    public List<GameObject> ClonedObjects = new List<GameObject>();
    private Playback PB;
    public InputField KeyDiffTime;
    private static string savedActivityPath;
    private static string savedDataPath;
    public Text Keys;

    public bool ver_log;
    // Start is called before the first frame update
    private void Awake()
    {
        PB = GetComponent<Playback>();
        savedActivityPath = Application.persistentDataPath + "/ActivityData";
        savedDataPath = Application.persistentDataPath + "/savedData";
        Conn = GetComponent<Connection>();
        Angles = new Quaternion[Conn.no_devices];
        ActivityKeys = new List<string>();
        Activity = new Dictionary<string, List<string>>();
        KeyNo = 0;
        //setting initial camera position
        cam.transform.position = CameraTransforms[0].position;
        cam.transform.rotation = CameraTransforms[0].rotation;
        GetAllRecordedData();
        //Invoke("LoadActivities", 0.5f);
        Keys.text = "0";
    }

    #region Recording Activities
    

    public void AddActivity()
    {
        if(ActivityName.text == "")
        {
            ActivityName.text = "Untitled_Activity";
        }
        
        string Path = savedActivityPath + "/" + ActivityName.text;
        Activity.Add(ActivityName.text, ActivityKeys);
        string Data = "";
        foreach(var Key in ActivityKeys)
        {
            Data += Key + "\n";
        }
        Keys.text = "0";
        SaveActivity(Path,Data);
        PrintAllActivities();
        KeyNo = 0;
    }

    public void ChangeCameraView()
    {
        cam.transform.position = CameraTransforms[CameraView.value].position;
        cam.transform.rotation = CameraTransforms[CameraView.value].rotation;
    }

    public void AddKey()
    {
        float KeyDiff_Time; ;
        try
        {
            KeyDiff_Time = float.Parse(KeyDiffTime.text);
        }
        catch (System.FormatException)
        {
            KeyDiff_Time = 5.0f;
        }

        ActivityKeys.Add(Conn.DeviceLocalAngles + "," + KeyDiff_Time.ToString("F2"));
        KeyNo = KeyNo + 1;
        Keys.text = KeyNo.ToString();
        PrintAllActivityElements(ActivityKeys);
    }

    public void RemoveKey()
    {
        ActivityKeys.RemoveAt(KeyNo-1);
        KeyNo = KeyNo - 1;
        Keys.text = KeyNo.ToString();
    }

    public void PrintAllActivities()
    {
        foreach(var act in Activity)
        {
            Debug.Log(act.Key);
            PrintAllActivityElements(act.Value);
        }
    }

    public void PrintAllActivityElements(List<string> AKeys)
    {
        foreach(var item in AKeys)
        {
            //foreach(var Vec in item)
            //{
                Debug.Log(item);
            //}
        }
    }

    #endregion

    public void PlayActiviy()
    {

    }

    #region Saving Activity
    void SaveActivity(string Path, string Data)
    {
        string DictionaryPath = Path + ".txt";
        if (!File.Exists(DictionaryPath))
        {
            Directory.CreateDirectory(savedActivityPath);
        }
        File.WriteAllText(DictionaryPath, Data);
    }
    #endregion

    #region Displaying Subject Recordings and Activities

    public RectTransform Content;
    public GameObject Item_Prefab;
    public Dropdown Activities;
    public Dropdown Subjects;
    public List<Vector2> ContentSize = new List<Vector2>();
    public List<GameObject> SubjectsUI = new List<GameObject>();

    public string[] ActivityFileNames;//Names of Activities
    public string[] SubjectIDs;//Names of Directories

    public void DeleteActivity()
    {
        if (Directory.Exists(savedActivityPath))
        {
            File.Delete(savedActivityPath + "/" + ActivityFileNames[Activities.value] + ".txt");
        }
    }

    public void GetActivities()
    {
        if (Directory.Exists(savedActivityPath))
        {
            string[] fileInfo = Directory.GetFiles(savedActivityPath, "*.txt");

            for (int i = 0; i < fileInfo.Length; i++)
            {
                fileInfo[i] = Path.GetFileName(fileInfo[i]);
                if (fileInfo[i] != "")
                {
                    string[] SplitNames = fileInfo[i].Split('.');
                    fileInfo[i] = SplitNames[0];
                }

                if (ver_log)
                {
                    Debug.Log(Path.GetFileName(fileInfo[i]));
                }
                
            }
            ActivityFileNames = fileInfo;
        }
    }

    void LoadActivities()
    {
        foreach (string ActivityName in ActivityFileNames)
        {
            PB.LoadActivityFile(ActivityName);
        }
    }

    void GetSubjects()
    {
        if (Directory.Exists(savedDataPath))
        {
            string[] DirectoryInfo = Directory.GetDirectories(savedDataPath);
            for (int i = 0; i < DirectoryInfo.Length; i++)
            {
                DirectoryInfo[i] = Path.GetFileName(DirectoryInfo[i]);
                if (ver_log)
                {
                    Debug.Log(DirectoryInfo[i]);
                }
            }
            SubjectIDs = DirectoryInfo;
        }
    }

    string[] GetSubjectActivities(string SubjectID)
    {
        
        string[] DirectoryInfo = new string[1];
        if (Directory.Exists(savedDataPath + "/" + SubjectID))
        {
            DirectoryInfo = Directory.GetDirectories(savedDataPath + "/" + SubjectID);
            for (int i = 0; i < DirectoryInfo.Length; i++)
            {
                DirectoryInfo[i] = Path.GetFileName(DirectoryInfo[i]);
                if (ver_log)
                {
                    Debug.Log(DirectoryInfo[i]);
                }
            }
        }
        return DirectoryInfo;
    }

    string[] GetSubjectRecordingTimeStamps(string ActivityName, string SubjectID)
    {
        string[] fileInfo = new string[1];
        
        if (Directory.Exists(savedDataPath + "/" + SubjectID + "/" + ActivityName))
        {
            fileInfo = Directory.GetFiles(savedDataPath + "/" + SubjectID + "/" + ActivityName, "*.txt");
            string[] filePaths = new string[fileInfo.Length*2];
            for (int i = 0; i < fileInfo.Length; i++)
            {
                filePaths[i + fileInfo.Length] = fileInfo[i];
                fileInfo[i] = Path.GetFileName(fileInfo[i]);
                if (fileInfo[i] != "")
                {
                    string[] SplitNames = fileInfo[i].Split('_');
                    string[] TimeStamp = new string[6];

                    for(int j = SplitNames.Length - 6; j < SplitNames.Length; j++)
                    {
                       // Debug.Log(i + SplitNames[i] + SplitNames.Length);
                        TimeStamp[j + 6 - SplitNames.Length] = SplitNames[j];
                        if((j + 6 - SplitNames.Length) == 5)
                        {
                            string[] SplitText = TimeStamp[j + 6 - SplitNames.Length].Split('.');
                            TimeStamp[j + 6 - SplitNames.Length] = SplitText[0];
                        }
                    }
                    
                    fileInfo[i] = TimeStamp[1] + "/" + TimeStamp[0] + "/" + TimeStamp[2] + "|" + TimeStamp[3] + ":" + TimeStamp[4] + ":" + TimeStamp[5];
                    //Debug.Log("Done");
                }

                if (ver_log)
                {
                    Debug.Log(fileInfo[i]);
                }
                //Debug.Log(i + "," + fileInfo.Length + "," + filePaths.Length);
                filePaths[i] = fileInfo[i];
                //Debug.Log(filePaths[i + fileInfo.Length]);
            }
            return filePaths;
        }
        return fileInfo;
    }

    List<string> UnsortedPaths;
    public GameObject EmptyObj;
    void GetAllRecordedData()//Should be called only once at the start of the program
    {
        Activities.ClearOptions();
        Subjects.ClearOptions();
        ContentSize.Clear();
        GetActivities();
        GetSubjects();
        SubjectsUI.Clear();
        List<string> Sub_Act = new List<string>();
        Sub_Act.AddRange(SubjectIDs);
        Subjects.AddOptions(Sub_Act);
        Sub_Act.Clear();

        Sub_Act.AddRange(ActivityFileNames);
        Activities.AddOptions(Sub_Act);
        //Subjects.AddOptions(Sub_Act);

        foreach (string SubjectID in SubjectIDs)
        {
            GameObject EmptySubject = Instantiate(EmptyObj, Content);
            EmptySubject.name = SubjectID;
            ClonedObjects.Add(EmptySubject);
            string[] SubjectActivities;//Names of Directories of Activitiesz    
            SubjectActivities = GetSubjectActivities(SubjectID);
            float TempSize = 0f;            
            foreach (string Activity in SubjectActivities)
            {/*                
                string[] SubjectRecordingTimeStamps;//Names of Recording Files Time Stamps of Subject Activities
                SubjectRecordingTimeStamps = GetSubjectRecordingTimeStamps(Activity, SubjectID);
                for (int i = 0; i < SubjectRecordingTimeStamps.Length / 2; i++)
                {
                    //Debug.Log("Done");


                    GameObject Clone = Instantiate(Item_Prefab, EmptySubject.transform);
                    ClonedObjects.Add(Clone);
                    RectTransform CloneTransform = Clone.GetComponent<RectTransform>();
                    CloneTransform.localPosition = new Vector3(-6.1f, (-42.0f - (110.0f * i)) - TempSize, 0f);
                    SetupAndLoadData CloneSLD = Clone.GetComponent<SetupAndLoadData>();
                    CloneSLD.TimeStamp = SubjectRecordingTimeStamps[i];
                    CloneSLD.ActivityName = Activity;
                    CloneSLD.Path = SubjectRecordingTimeStamps[i + (SubjectRecordingTimeStamps.Length / 2)];
                    CloneSLD.LoadingTextReferenceDataDone();
                }
                TempSize += (SubjectRecordingTimeStamps.Length / 2) * 110.0f;
                */
                //-------------------------------------------------------------------------------------------------------------------------//
                List<int[]> SortedTimeStamps = new List<int[]>(); //Each element has time(h,m,s), date, month and year
                List<int[]> UnsortedTimeStamps = new List<int[]>();
                List<string> SortedPaths = new List<string>();
                List<string> SubjectRecordingTimeStamps = new List<string>(GetSubjectRecordingTimeStamps(Activity, SubjectID));
                
                // Parsing and Sorthing Time Stamps and Paths
                UnsortedTimeStamps = ParseTimeStamp(SubjectRecordingTimeStamps.GetRange(0, SubjectRecordingTimeStamps.Count / 2));
                UnsortedPaths = SubjectRecordingTimeStamps.GetRange(SubjectRecordingTimeStamps.Count / 2, SubjectRecordingTimeStamps.Count / 2);

                SortedTimeStamps = SortTimeStmap(UnsortedTimeStamps);

                for (int i = 0; i < UnsortedTimeStamps.Count; i++)
                {
                    GameObject Clone = Instantiate(Item_Prefab, EmptySubject.transform);
                    ClonedObjects.Add(Clone);
                    RectTransform CloneTransform = Clone.GetComponent<RectTransform>();
                    CloneTransform.localPosition = new Vector3(-6.1f, (-42.0f - (110.0f * i)) - TempSize, 0f);
                    SetupAndLoadData CloneSLD = Clone.GetComponent<SetupAndLoadData>();
                    CloneSLD.TimeStamp = SortedTimeStamps[i][0].ToString("D2") + "/" + SortedTimeStamps[i][1].ToString("D2") + "/" + SortedTimeStamps[i][2].ToString("D4") + " " + SortedTimeStamps[i][3].ToString("D2") + ":" + SortedTimeStamps[i][4].ToString("D2") + ":" + SortedTimeStamps[i][5].ToString("D2");
                    CloneSLD.ActivityName = Activity;
                    CloneSLD.Path = UnsortedPaths[i];
                    CloneSLD.LoadingTextReferenceDataDone();
                }
                TempSize += (UnsortedTimeStamps.Count) * 110.0f;
                //-------------------------------------------------------------------------------------------------------------------------//
            }
            if (TempSize > 580.0f)
            {
                ContentSize.Add(new Vector2(Content.sizeDelta.x, TempSize));
            }
            else
            {
                ContentSize.Add(new Vector2(Content.sizeDelta.x, 580.0f));
            }
            SubjectsUI.Add(EmptySubject);
            DisableAllContent();
        }
        PB.ClearCache();
       // LoadActivities();
    }

    bool once = true;
    bool twice = true;
    List<int[]> ParseTimeStamp(List<string> Data)// String Format: "mm/dd/yyyy hh:mm:ss"
    {
        List<int[]> ParsedTimeStamp = new List<int[]>();
        foreach(string StringTimeStamp in Data)
        {
            string[] SplitData = StringTimeStamp.Split('|');
            string[] TimeData = SplitData[1].Split(':');
            string[] DateData = SplitData[0].Split('/');
            //Debug.Log(StringTimeStamp);
            //Debug.Log(TimeData[0] + " " + TimeData[1] + " " + TimeData[2]);
            //Debug.Log(DateData[0] + " " + DateData[1] + " " + DateData[2]);
            int[] TimeStampData = { int.Parse(DateData[0]), int.Parse(DateData[1]), int.Parse(DateData[2]), int.Parse(TimeData[0]), int.Parse(TimeData[1]), int.Parse(TimeData[2]) };
            ParsedTimeStamp.Add(TimeStampData);

            /*if (once)
            {
                string newData = "";
                foreach(int data in TimeStampData)
                {
                    newData += data.ToString() + " ";
                }
                Debug.Log(newData);
            }*/
        }
        /*Debug.Log("---");
        once = false;*/
        return ParsedTimeStamp;
    }

    List<int[]> SortTimeStmap(List<int[]> UnsortedData)
    {
        List<int[]> SortedTimeStamp = new List<int[]>();
        List<string> SortedPath = new List<string>();
        for (int i = 0; i < UnsortedData.Count; i++)
        {
            int SortedIndex = 0;
            for(int j = SortedTimeStamp.Count-1; j > -1; j--)
            {
                if (UnsortedData[i][2] > SortedTimeStamp[j][2])// Year
                {
                    SortedIndex = j;//Comes before the current sorted TimeStamp
                }
                else
                {
                    if (UnsortedData[i][0] > SortedTimeStamp[j][0])// Month
                    {
                        SortedIndex = j;//Comes before the current sorted TimeStamp
                    }
                    else
                    {
                        if (UnsortedData[i][1] > SortedTimeStamp[j][1])// Day
                        {
                            SortedIndex = j;//Comes before the current sorted TimeStamp
                        }
                        else
                        {
                            if (UnsortedData[i][3] > SortedTimeStamp[j][3])// Hour
                            {
                                SortedIndex = j;//Comes before the current sorted TimeStamp
                            }
                            else
                            {
                                if (UnsortedData[i][4] > SortedTimeStamp[j][4])//Minute
                                {
                                    SortedIndex = j;//Comes before the current sorted TimeStamp
                                }
                                else
                                {
                                    if (UnsortedData[i][5] > SortedTimeStamp[j][5])//Second
                                    {
                                        SortedIndex = j;//Comes before the current sorted TimeStamp
                                    }
                                }
                            }
                        }
                    }
                }
            }            
            SortedTimeStamp.Insert(SortedIndex,UnsortedData[i]);
            SortedPath.Insert(SortedIndex, UnsortedPaths[i]);
            /*
            if (twice)
            {
                Debug.Log(SortedTimeStamp.Count);
                foreach (int[] Item in SortedTimeStamp)
                {
                    string newData = "";
                    foreach (int data in Item)
                    {
                        newData += data.ToString() + " ";
                    }
                    Debug.Log(newData);
                }

                Debug.Log("---" + i.ToString());
            }*/
        }
        /*
        if (twice)
        {
            foreach(int[] Item in SortedTimeStamp)
            {
                string newData = "";
                foreach (int data in Item)
                {
                    newData += data.ToString() + " ";
                }
                Debug.Log(newData);
            }

            Debug.Log("---");
        }
        twice = false;*/
        
        return SortedTimeStamp;
    }

    public void RefreshFiles()
    {
        DeleteAllClonedObjects();
        GetAllRecordedData();
    }

    private void DeleteAllClonedObjects()
    {
        foreach(GameObject Clone in ClonedObjects)
        {
            Destroy(Clone);
        }
        ClonedObjects.Clear();
    }

    public void SetContentSize()
    {
        DisableAllContent();
        Content.sizeDelta = ContentSize[Subjects.value];
        SubjectsUI[Subjects.value].SetActive(true);
    }

    public void DisableAllContent()
    {
        foreach(GameObject Subject in SubjectsUI)
        {
            Subject.SetActive(false);
        }
    }
    #endregion
}
