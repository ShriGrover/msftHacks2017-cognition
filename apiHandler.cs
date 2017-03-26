class Program {



    static async HttpResponseMessage detectFace(String binaryInput) {
        // String baseUrl = "https://westus.api.cognitive.microsoft.com/face/v1.0/detect[?returnFaceId][&returnFaceLandmarks][&returnFaceAttributes]";
        String baseUrl = "https://westus.api.cognitive.microsoft.com/face/v1.0/detect?returnFaceId=true&returnFaceLandmarks=true";
        HttpClient client = new HttpClient();

    }
}