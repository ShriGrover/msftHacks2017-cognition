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


    void Start()
    {
        instance = this;

        if (string.IsNullOrEmpty(faceSubscriptionKey))
        {
            throw new Exception("Please set your face-subscription key.");
        }

        isInitialized = true;
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

        string requestUrl = string.Format("{0}/detect?returnFaceId={1}&returnFaceLandmarks={2}",
                                            ServiceHost, true, false);

        Dictionary<string, string> headers = new Dictionary<string, string>();
        headers.Add("ocp-apim-subscription-key", faceSubscriptionKey);

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
            //var json = JsonUtility.ToJson(retrived);

            //throw new Exception(json);
            string response = cleanResponse(www.text);
            //response = response.Substring(1);
            //response = response.Substring(0, response.Length - 1);
            aFace face = JsonUtility.FromJson<aFace>(response);
            //throw new Exception(face.faceId);
            matchingFace(face.faceId);
        }
        else
        {
            ProcessFaceError(www);
        }
    }

    private string cleanResponse(string response)
    {
        string temp = response.Substring(1);
        temp = temp.Substring(0, temp.Length - 1);

        return temp;
    }

    public bool matchingFace(string faceId)
    {
        string requestUrl = string.Format("{0}/verify", ServiceHost);
        Dictionary<string, string> header = new Dictionary<string, string>();
        header.Add("ocp-apim-subscription-key", faceSubscriptionKey);
        header.Add("faceId1", faceId);
        header.Add("faceId2", "0b2ebd57-7316-4e44-aa9b-1fb702015a41");

        WWW www = new WWW(requestUrl, new Byte[0], header);

        while(!www.isDone) {}

        string response = cleanResponse(www.text);

        aMatching match = JsonUtility.FromJson<aMatching>(response);

        if(match.confidence  >= 0.7) {
            throw new Exception(match.isIdentical.ToString());
            return match.isIdentical;
        } else
        {
            throw new Exception("False: No confidence");
        }

        return false;
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
