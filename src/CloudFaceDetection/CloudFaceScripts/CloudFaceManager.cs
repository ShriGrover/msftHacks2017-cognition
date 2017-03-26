using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;


public class CloudFaceManager : MonoBehaviour
{
    [Tooltip("Subscription key for Face API. Go to the Microsoft Face API Page and push the 'Try for free'-button to get new subscripiption keys.")]
    public string faceSubscriptionKey = "8b06ad0b9148481c9cc3d60e042425e4";

    [HideInInspector]
    public Face[] faces;  // the detected faces

    private const string ServiceHost = "https://westus.api.cognitive.microsoft.com/face/v1.0";
    private static CloudFaceManager instance = null;
    private bool isInitialized = false;

    public bool matching;


    void Start()
    {
        instance = this;

        if (string.IsNullOrEmpty(faceSubscriptionKey))
        {
            throw new Exception("Please set your face-subscription key.");
        }

        isInitialized = true;
        matching = false;
    }

    public static CloudFaceManager Instance
    {
        get
        {
            return instance;
        }
    }


    public bool IsInitialized()
    {
        return isInitialized;
    }


    public IEnumerator DetectFaces(Texture2D texImage)
    {
        if (texImage != null)
        {
            byte[] imageBytes = texImage.EncodeToJPG();
            yield return DetectFaces(imageBytes);
        }
        else
        {
            yield return null;
        }
    }


    public IEnumerator DetectFaces(byte[] imageBytes)
    {

        faces = null;

        if (string.IsNullOrEmpty(faceSubscriptionKey))
        {
            throw new Exception("The face-subscription key is not set.");
        }

        string requestUrl = string.Format("{0}/detect?returnFaceId={1}&returnFaceLandmarks={2}&returnFaceAttributes={3}",
                                            ServiceHost, true, false, "age,gender,smile,facialHair,glasses");

        Dictionary<string, string> headers = new Dictionary<string, string>();
        headers.Add("ocp-apim-subscription-key", "8b06ad0b9148481c9cc3d60e042425e4");

        headers.Add("Content-Type", "application/octet-stream");
        headers.Add("Content-Length", imageBytes.Length.ToString());
        WWW www = new WWW(requestUrl, imageBytes, headers);
        yield return www;

        if (!CloudWebTools.IsErrorStatus(www))
        {
            string newJson = "{ \"faces\": " + www.text + "}";
            FacesCollection facesCollection = JsonUtility.FromJson<FacesCollection>(newJson);
            faces = facesCollection.faces;
            string retrived = www.text;
            string response = cleanResponse(www.text);
            aFace face = JsonUtility.FromJson<aFace>(response);
            // Debug.Log("Calling with FaceID: " + face.faceId);
            // www.Dispose();
            // GC.Collect();
            matchingFace(face.faceId);
        }
        else
        {
            ProcessFaceError(www);
        }
    }

    private string cleanResponse(string response)
    {
        if (response.Length < 1)
        {
            return "";
        }
        string temp = response.Substring(1);
        temp = temp.Substring(0, temp.Length - 1);
        temp = temp.Replace("\r", "");
        temp = temp.Replace("\n", "");
        temp = temp.Replace(" ", "");

        return temp;
    }

    private bool isTrue(string response)
    {
        if(response.Length < 1)
        {
            return false;
        }
        string temp = response.Replace("{", "");
         temp = temp.Replace("{", "");
         temp = temp.Replace("\r", "");
         temp = temp.Replace("\n", "");
         temp = temp.Replace("\"", "");
        temp = temp.Replace(":", "");
        // Debug.Log(temp);
        return (temp.Substring(11, 12) == "t");
    }

    public IEnumerator matchingFace(string faceId)
    {
        // Debug.Log("Starting with FaceID" + faceId);
        string theFace;
        //if(faceId == "") {
        //    theFace = "9887a4a0-fd3c-4685-8e61-d600184c395e";
        //} else {
        //    theFace = faceId;
        //}
        theFace = faceId;
        string requestUrl = string.Format("{0}/verify", ServiceHost);
        Dictionary<string, string> header2 = new Dictionary<string, string>();
        header2.Add("ocp-apim-subscription-key", "8b06ad0b9148481c9cc3d60e042425e4");
        //header2.Add("faceId1", "19ce87ee-ad25-4ebc-b166-b58dad20c48c");
        // faceId2 is the static target
        header2.Add("faceId2", "0b2ebd57-7316-4e44-aa9b-1fb702015a41");
        header2.Add("Host", "westus.api.cognitive.microsoft.com");
        header2.Add("Content-Type", "application/json");
        string body = "{ \"faceId1\": \"" + faceId + "\", \"faceId2\": \"0b2ebd57-7316-4e44-aa9b-1fb702015a41\" }";
        //string body = "{ \"faceId1\": \"19ce87ee-ad25-4ebc-b166-b58dad20c48c\", \"faceId2\": \"0b2ebd57-7316-4e44-aa9b-1fb702015a41\" }";
        // Debug.Log("Starting Webclient");
        byte[] bodyValue = Encoding.ASCII.GetBytes(body);
        //AsyncOperation www2 = new WWW(requestUrl, new byte[0], header);
        // Debug.Log(header2);
        WWW www2 = new WWW(requestUrl, bodyValue, header2);
        // Debug.Log("Waiting...");
        yield return www2;
        while(!www2.isDone) { }
        if(www2.isDone) {
            // atempted to yield thread
            // yield return new WaitForSeconds(0.75f);
            //yield WaitForSeconds(5);
            // Debug.Log(faceId);
            //yield return www2;
            // Debug.Log("Returned: " + www2.text);
            if (!CloudWebTools.IsErrorStatus(www2))
            {
                string response = cleanResponse(www2.text);
                bool temp = isTrue(response);
                // Debug.Log(temp);
                // Debug.Log(response.Substring(15, 16));
                char[] theResponse = response.ToCharArray();
                if (theResponse[14] == 't')
                {
                    matching = true;
                    yield return null;
                }
                else
                {
                    matching = false;
                    yield return null;
                }
            }
        }
    }


    private void ProcessFaceError(WWW www)
    {
        ClientError ex = JsonUtility.FromJson<ClientError>(www.text);

        if (ex.error != null && ex.error.code != null)
        {
            string sErrorMsg = !string.IsNullOrEmpty(ex.error.code) && ex.error.code != "Unspecified" ?
                ex.error.code + " - " + ex.error.message : ex.error.message;
            throw new System.Exception(sErrorMsg);
        }
        else
        {
            ServiceError serviceEx = JsonUtility.FromJson<ServiceError>(www.text);

            if (serviceEx != null && serviceEx.statusCode != null)
            {
                string sErrorMsg = !string.IsNullOrEmpty(serviceEx.statusCode) && serviceEx.statusCode != "Unspecified" ?
                    serviceEx.statusCode + " - " + serviceEx.message : serviceEx.message;
                throw new System.Exception(sErrorMsg);
            }
            else
            {
                throw new System.Exception("Error " + CloudWebTools.GetStatusCode(www) + ": " + CloudWebTools.GetStatusMessage(www) + "; Url: " + www.url);
            }
        }
    }


    public void DrawFaceRects(Texture2D tex, Face[] faces, Color[] faceColors, bool drawHeadPoseArrow)
    {
        for (int i = 0; i < faces.Length; i++)
        {
            Face face = faces[i];
            Color faceColor = faceColors[i % faceColors.Length];

            FaceRectangle rect = face.faceRectangle;
            CloudTexTools.DrawRect(tex, rect.left, rect.top, rect.width, rect.height, faceColor);

            if (drawHeadPoseArrow)
            {
                HeadPose headPose = face.faceAttributes.headPose;

                int cx = rect.width / 2;
                int cy = rect.height / 4;
                int arrowX = rect.left + cx;
                int arrowY = rect.top + (3 * cy);
                int radius = Math.Min(cx, cy);

                float x = arrowX + radius * Mathf.Sin(headPose.yaw * Mathf.Deg2Rad);
                float y = arrowY + radius * Mathf.Cos(headPose.yaw * Mathf.Deg2Rad);

                int arrowHead = radius / 4;
                if (arrowHead > 15) arrowHead = 15;
                if (arrowHead < 8) arrowHead = 8;

                CloudTexTools.DrawArrow(tex, arrowX, arrowY, (int)x, (int)y, faceColor, arrowHead, 30);
            }
        }

        tex.Apply();
    }


}
