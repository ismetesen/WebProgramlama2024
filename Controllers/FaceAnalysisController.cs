using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using System.Text.Json;


public class FaceAnalysisController : Controller
{
    private readonly string _apiKey = "FuEU9RrXC5zUhUqmem2bG9rjRNa0381aEwFfIKV4BiUwBS0HaeY8JQQJ99ALAC5RqLJXJ3w3AAAKACOGtyGF";
    private readonly string _apiEndpoint = "https://barber-face-recognition.cognitiveservices.azure.com/";

    public IActionResult Analysis()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> AnalyzePhoto(IFormFile photo)
    {
        if (photo == null || photo.Length == 0)
            return BadRequest("Fotoğraf yüklemeniz gerekiyor");

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _apiKey);

        // Fotoğrafı byte array'e çevir
        using var ms = new MemoryStream();
        await photo.CopyToAsync(ms);
        var imageBytes = ms.ToArray();

        // API endpoint'ini ayarla
        var endpoint = $"{_apiEndpoint}face/v1.0/detect?returnFaceAttributes=headPose,faceLandmarks";

        // API'a gönder
        var content = new ByteArrayContent(imageBytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var response = await client.PostAsync(endpoint, content);
        var result = await response.Content.ReadAsStringAsync();

        // Yüz şeklini belirle ve öneri yap
        var faceShape = AnalyzeFaceShape(result);
        var recommendation = GetHairstyleRecommendation(faceShape);

        return Json(new
        {
            faceShape = faceShape,
            recommendation = recommendation
        });
    }

    private string AnalyzeFaceShape(string apiResponse)
    {
        // API yanıtını analiz et ve yüz şeklini belirle
        // Bu basit bir örnek - gerçek implementasyonda daha detaylı analiz yapılmalı
        try
        {
            var faceData = JsonDocument.Parse(apiResponse);
            // Burada API yanıtına göre yüz şekli belirleme algoritması yazılmalı
            return "oval"; // Örnek dönüş
        }
        catch
        {
            return "unknown";
        }
    }

    private string GetHairstyleRecommendation(string faceShape)
    {
        return faceShape switch
        {
            "oval" => "Oval yüz şekliniz çoğu saç modeliyle uyumludur. Öneriler:\n- Kısa bob kesim\n- Uzun katmanlı kesim\n- Pixie kesim",
            "round" => "Yuvarlak yüzünüz için öneriler:\n- Yüzü uzun gösteren uzun düz kesimler\n- Çene hizasında köşeli kesimler\n- Asimetrik kesimler",
            "square" => "Kare yüzünüz için öneriler:\n- Yumuşak dalgalar\n- Katmanlı kesimler\n- Yüzü çerçeveleyen uzun saçlar",
            "heart" => "Kalp şeklindeki yüzünüz için öneriler:\n- Orta boy katmanlı kesimler\n- Çene hizasında biten kesimler\n- Yan perçemler",
            "long" => "Uzun yüzünüz için öneriler:\n- Hacimli kesimler\n- Dalgalı stiller\n- Yanlarda katmanlar",
            _ => "Genel öneriler:\n- Yüz hatlarınızı dengeleyen kesimler\n- Profesyonel bir kuaförle konsültasyon yapmanızı öneririz"
        };
    }
}