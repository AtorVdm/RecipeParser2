using System;
using System.Text;
using System.Web.UI;

namespace RecipeParser
{
    public partial class _Default : Page
    {
        RecipeProcessor processor = new RecipeProcessor();
        private Encoding swedishEncoding = Encoding.GetEncoding(1252);

        protected void Page_Load(object sender, EventArgs e)
        {

        }
        
        protected void btnSubmitClick(object sender, EventArgs e)
        {
            if (!uploadedFile.HasFiles)
            {
                errorLabel.Text = "File wasn't set!";
                return;
            }
            StringBuilder sb = new StringBuilder();
            foreach (var file in uploadedFile.PostedFiles)
            {
                sb.Append(processor.ProcessUploadedFile(file));
            }

            Response.Clear();
            Response.ClearHeaders();
            Response.AddHeader("Content-Type", "text/plain");
            Response.Write(sb.ToString());
            Response.Flush();
            Response.End();
        }
    }
}