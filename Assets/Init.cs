using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

// convenience class for objects that have an attached WebVideoPlayer
public class VideoObject : MonoBehaviour
{
    WebVideoPlayer webVideoPlayer;
    public GameObject videoCanvas;
    string url;
    public bool isActive = false;

    // store local position, to calculate distance to player 
    public Vector3 localPosition;
    public bool portrait = false;

    public void SetSpatial(Vector3 position, Vector3 scale, Quaternion rotation) {
        localPosition = position;
        videoCanvas.transform.position = position;
        videoCanvas.transform.localScale = scale;
        videoCanvas.transform.localRotation = rotation;
        webVideoPlayer.audioSource.transform.position = position;
    }

    public void Setup(string _url, bool _portrait) {
        url = _url;
        portrait = _portrait;
        videoCanvas = GameObject.CreatePrimitive(PrimitiveType.Cube);
        // audioSource = videoCanvas.AddComponent<AudioSource>();

        Material referenceMaterial = Resources.Load<Material>("Materials/videomat");
        videoCanvas.GetComponent<Renderer>().material.CopyPropertiesFromMaterial(referenceMaterial);
        initWebPlayer();
    }
    public void initWebPlayer() {
        webVideoPlayer = videoCanvas.AddComponent<WebVideoPlayer>();
        webVideoPlayer.audioSource = videoCanvas.AddComponent<AudioSource>();
        webVideoPlayer.audioSource.dopplerLevel = 0;
        webVideoPlayer.audioSource.spatialize = true;
        webVideoPlayer.audioSource.spatialBlend = 1;
        webVideoPlayer.audioSource.minDistance = 8;
        webVideoPlayer.audioSource.maxDistance = 12;
        webVideoPlayer.audioSource.rolloffMode = AudioRolloffMode.Linear;
        webVideoPlayer.url = url;
        if (portrait) {
            webVideoPlayer.portrait = true;
        }
        webVideoPlayer.material = videoCanvas.GetComponent<Renderer>().material;
        webVideoPlayer.Start();
    }

    public void Start()
    {
    }
    public void DistanceUpdate(float dist)
    {
        bool shouldBeActive = dist < 0;
        if (isActive) {
            if (!shouldBeActive) {
                webVideoPlayer.DestroyMe();
                webVideoPlayer.shouldBeRunning = false;
            } else {
                webVideoPlayer.Update();
                webVideoPlayer.shouldBeRunning = true;
            }
        }
        isActive = shouldBeActive;
    }
}

// we want to save settings in an external textfile
// so we can add more videos or change parameters without having to recompile  
[System.Serializable]
public class Settings {
    public float distanceThreshold;
    public string[] videos;
}

// function to get settings (as string).
// works both when running in Unity Editor, or compiled as webGL
// could be easily cannibalized for a generic textfile reader
// is actually a generic textfile reader, if only the filename wouldn't be hardcoded
public class GetSettings : MonoBehaviour {
    // first set the result to no_result, so we can check if result has been fetched.
    // fails, if the textfile contains "no_result", BUT in our case we want specifically
    // the settings, so ... that behaviour is actually fine for us.
    public string result = "no_result";
    public bool ready = false;
    void Start() {
        // coroutine runs asynchronously, we don't know beforehand when it finishes executing,
        // and the rest of the app just keeps on running in the meantime.
        StartCoroutine(GetText());
    }
 
    IEnumerator GetText() {
        string url = Application.streamingAssetsPath + "/settings.json";
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();
 
        if(www.isNetworkError || www.isHttpError) {
            Debug.Log(www.error);

            // oops, network error. We optimistically assume that we're running in the Unity Editor.
            // Why? because it doesn't serve a website when it just runs inside the editor. Ergo the
            // network error. in that case, read the file with a StreamReader
            // is it elegant? ..hm.. is it the most convenient method? ..phew.. the cleanest? ..ha..
            // does it work? apparently :) Will it backfire in case there is a real network error?
            // no. in that case we'd have a network error anyways
            string path = Path.Combine(Application.streamingAssetsPath, "settings.json");
            StreamReader reader = new StreamReader(path); 
            string returner = reader.ReadToEnd();
            reader.Close();
            result = returner;
            ready = true;
        }
        else {
            result = www.downloadHandler.text;
            ready = true;
        }
    }
}

public class Init : MonoBehaviour
{

    // list with all video objects, so we can iterate over them
    List<VideoObject> videos;
    // the first person player, we need it to be able to calculate distances to video objects
    GameObject player;

    // settings that we might want to adjust without recompiling
    Settings settings;

    // and the function to get the settings
    GetSettings getSettings;
    // we only want to read the settings once, so we can just
    // flip a variable to 'true' once we read it. Then we know
    // if it has been already read, and ignore it next time.
    bool settingsRead = false;

    void Start()
    {
        // actually get the settings
        getSettings = gameObject.AddComponent<GetSettings>();
        // actually get the player
        player = GameObject.Find("FirstPerson-AIO");
        // actually creating a list of videos
        // well, an empty list. this list is empty. BUT it is a list now,
        // so we can put things in it.
        videos = new List<VideoObject>();
    }

    // Update is called once per frame
    void Update()
    {
        // iterate through all videos
        for (int i = 0; i < videos.Count; i++) {
            // get the distance 
            float dist = Vector3.Distance(videos[i].localPosition, player.transform.position);
            // then pass it to the video
            // could actually be a bit cleaner
            // because here we don't send the distance, but the distance minus the threshold
            // it makes calculation a bit simpler (<=0), but we would have to call our function
            // not "DistanceUpdate" but "DistanceThresholdDifference" or something better
            videos[i].DistanceUpdate(dist - settings.distanceThreshold);
        }
        // in case that the settingstext is ready to be consumed by our settings object,
        // and we didn't do that yet.. do it
        if (getSettings.ready && !settingsRead) {
            // read them now into the settings object
            settings = JsonUtility.FromJson<Settings>(getSettings.result);
            // tell ourselves that we did it
            settingsRead = true;
            // shuffle the videos around
            System.Array.Sort(settings.videos, RandomSort);
            // and then finally populate all our beautiful video objects
            for (int i = 0; i < settings.videos.Length; i++) {
                videos.Add(gameObject.AddComponent<VideoObject>());

                string url = settings.videos[i%settings.videos.Length];
                bool portrait = url.Contains("_PORTAIT");
                // fun fact, with a simple modulo % we could populate any amount of video objects
                // independent of the amount of videos we actually have.
                videos[i].Setup(url,portrait);

                // generate the arbitrary semi random grid coordinates per video object
                float scaleFactor = 4f;
                float grid = 16;
                float x_random = Random.Range(-5.0f,5.0f);
                float z_random = Random.Range(-5.0f,5.0f);
                int gridLength = 6;
                float x = grid + (i%gridLength)*grid + x_random;
                float z = grid + Mathf.Round(i/gridLength)  * grid + z_random;
                float yShift = videos[i].portrait ? 1.4f : 0.8f;
                Vector3 position = new Vector3(x,scaleFactor * yShift,z);
                float w = videos[i].portrait ? scaleFactor * 1.08f : scaleFactor * 1.92f;
                float h = videos[i].portrait ? scaleFactor * 1.92f : scaleFactor * 1.08f;
                float d = 0.01f;
                Vector3 localScale = new Vector3(d,h,w);
                float randomRotation = Random.Range(-180f,180f);
                Quaternion localRotation = Quaternion.Euler(0f, randomRotation, 0f);

                // set the spatial transform for the video object
                // probably should've called the function rather "SetTransforms"
                // but now I'm too lazy to refactor and already wrote this comment
                // so please excuse
                videos[i].SetSpatial(position,localScale,localRotation);
            }
        }
    }

    // this function just returns a number in the range -1 to +1
    // and is used by Array.Sort to 'shuffle' the array
    int RandomSort(string a, string b)
    {
        return Random.Range(-1, 2);
    }
}
