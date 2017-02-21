using RecipeParser.RecipeJsonModel;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Script.Serialization;

namespace RecipeParser.Controllers
{
    public class RecipeController : ApiController
    {
        private const string MATLISTAN_API_URI_STRING = "http://api.test.matlistan.se/Recipes/Parse";

        [Route("api/recipe/raw")]
        [HttpPost]
        public Recipe ParseImage()
        {
            byte[] image = Request.Content.ReadAsByteArrayAsync().Result;
            if (image == null)
                image = File.ReadAllBytes(@"C:\test\new\IMG_0030.jpg");
            OCR.OCRSpaceMain ocrSpace = new OCR.OCRSpaceMain();
            string jsonObject = ocrSpace.ProcessPicture(image, "TestImageName.jpg");

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            Recipe recipe = serializer.Deserialize<Recipe>(jsonObject);

            if (recipe.OCRExitCode != 1) return null;

            TextOverlay overlay = recipe.ParsedResults[0].TextOverlay;
            overlay.ComputeExtraFields();

            return recipe;
        }

        [Route("api/recipe/ocr")]
        [HttpPost]
        public async Task<Recipe> OcrRecipe()
        {
            var multipartRequest = await Request.Content.ReadAsMultipartAsync();
            byte[] image = await multipartRequest.Contents[0].ReadAsByteArrayAsync();

            if (image == null)
                image = File.ReadAllBytes(@"C:\test\new\IMG_0030.jpg");
            OCR.OCRSpaceMain ocrSpace = new OCR.OCRSpaceMain();
            string jsonObject = ocrSpace.ProcessPicture(image, "TestImageName.jpg");
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            Recipe recipe = serializer.Deserialize<Recipe>(jsonObject);
            return recipe;
        }

        [Route("api/recipe/parse")]
        [HttpPost]
        public async Task<string> ProcessRecipe()
        {
            var jsonRequest = await Request.Content.ReadAsStringAsync();

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            Recipe recipe = serializer.Deserialize<Recipe>(jsonRequest);

            if (recipe.OCRExitCode != 1) return recipe.ErrorMessage;

            string htmlOutput = RecipeHTMLConverter.ConvertRecipeToHTML(recipe);

            return ProcessRecipe(htmlOutput);
        }

        private string ProcessRecipe(string htmlPage)
        {
            using (var client = new WebClient())
            {
                var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(htmlPage), "data", "recipe.html");
                return SendMultipartFormData(formData).Result;
            }
        }

        private async Task<string> SendMultipartFormData(MultipartFormDataContent formData)
        {
            var httpClient = new HttpClient();
            var response = httpClient.PostAsync(new Uri(MATLISTAN_API_URI_STRING), formData).Result;
            return await response.Content.ReadAsStringAsync();
        }
    }
}
