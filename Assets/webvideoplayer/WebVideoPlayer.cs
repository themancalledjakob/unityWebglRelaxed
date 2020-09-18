using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
public class WebVideoPlayer : MonoBehaviour
{
    public string url;
    public Material material;
    public AudioSource audioSource;
    private RenderTexture renderTexture;
    private UnityEngine.Video.VideoPlayer videoPlayer;
    public bool userInputReceived = false;
    private bool videoStarted = false;
    public bool shouldBeRunning = false;
    public void Start()
    {

#if UNITY_EDITOR
        if (url == "") {
            EditorUtility.DisplayDialog("Video not specified", "please tell WebVideoPlayer the name of the video file at 'url'", ".. ok");
        }
        if (!File.Exists(Path.Combine(Application.streamingAssetsPath, url))) {
            EditorUtility.DisplayDialog("Video not found", "Please put your video '" + url + "' in the StreamingAssets directory at '" + Application.streamingAssetsPath + "'. If the directory does not exist, create it. Otherwise the video will not be played.", "alright, thank you.");
        }
        if (!audioSource) {
            EditorUtility.DisplayDialog("AudioSource not found", "drag an drop an AudioSource on the WebVideoPlayer, or you won't hear anything.", "OK");
        }
        if (!material) {
            EditorUtility.DisplayDialog("Material not found", "drag an drop a material on the WebVideoPlayer, or you won't see anything. This material you can put on objects to make them play the video.", "OK");
        }
#endif
        renderTexture = new RenderTexture(1280, 720, 16, RenderTextureFormat.ARGB32);
        
        // somehow setting material properties like this doesn't work for webGL, or at least gives issues.
        // therefore we are rather getting a material from the resources,
        // where everything is prepared already. this happens externally
        //
        // material.EnableKeyword ("_EMISSION");
        // material.color = Color.white;
        // material.SetColor("_EmissionColor",Color.white);
        // material.SetFloat("_Metallic", 1.0f);
        // material.SetFloat("_Glossiness", 1.0f);
        material.SetTexture("_EmissionMap", renderTexture);

        // set the audiosource properties to get ourselves truly 3d spatial sound
        // which of course doesn't work in webGL due to a bug with html5 videos
        // the sound will always be full volume 
        // :-/
        // leave the code though, in case it gets fixed
        audioSource.dopplerLevel = 0;
        audioSource.spatialize = true;
        audioSource.spatialBlend = 1;
        audioSource.minDistance = 8;
        audioSource.maxDistance = 12;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
    }

    public void Update()
    {
        if (!userInputReceived) {
            foreach(Touch touch in Input.touches)
            {
                if (touch.phase == TouchPhase.Ended)
                {
                    userInputReceived = true;
                }
            }
            if (Input.GetMouseButton(0)
                || Input.GetMouseButton(1)
                || Input.GetMouseButton(2)) {
                    userInputReceived = true;
            }
            if (Input.anyKey)
            {
                userInputReceived = true;
            }
        }
        if (userInputReceived) {
            if (!videoStarted && shouldBeRunning) {
                videoPlayer = gameObject.AddComponent<UnityEngine.Video.VideoPlayer>();
                videoPlayer.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture;
                videoPlayer.targetTexture = renderTexture;
                videoPlayer.playOnAwake = false;
                videoPlayer.waitForFirstFrame = false;
                videoPlayer.isLooping = true;
                videoPlayer.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.AudioSource;
                videoPlayer.SetTargetAudioSource(0, audioSource);
                videoPlayer.url = Path.Combine(Application.streamingAssetsPath, url);
                videoPlayer.Play();
                videoStarted = true;
            }
        }
    }
    public void DestroyMe() {
        videoPlayer.Pause();
        Destroy(gameObject.GetComponent<UnityEngine.Video.VideoPlayer>());
        videoStarted = false;
        shouldBeRunning = false;
    }
}