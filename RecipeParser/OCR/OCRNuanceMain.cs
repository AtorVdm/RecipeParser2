using RecipeParser.RecipeJsonModel;
using System;
using System.IO;
using Nuance.OmniPage.CSDK.ArgTypes;
using System.Security.Cryptography;
using System.Xml;
using System.Collections.Generic;

namespace RecipeParser.OCR
{
    public class OCRNuanceMain
    {
        private Recipe ProcessPicture(byte[] pictureBytes)
        {
            String imageName;
            using (var md5 = MD5.Create())
            {
                imageName = md5.ComputeHash(pictureBytes).ToString();
            }
            Recipe recipe = null;
            string imagePath = Server.MapPath("~/images/" + imageName + ".jpg");
            string xmlPath = Server.MapPath("~/images/" + imageName + ".xml");
            File.WriteAllBytes(imagePath, pictureBytes);
            Nuance.OmniPage.CSDK.Objects.Engine.Init(null, null, true, Server.MapPath("~/OPCaptureSDK19/Bin"));
            using (Nuance.OmniPage.CSDK.Objects.SettingCollection settings = new Nuance.OmniPage.CSDK.Objects.SettingCollection())
            {
                //Set the language
                settings.Languages.Manage(MANAGE_LANG.SET_LANG, LANGUAGES.LANG_SWE);
                //Use the most accurate OCR engine
                settings.DefaultRecognitionModule = RECOGNITIONMODULE.RM_OMNIFONT_PLUS3W;
                //Specify formatted text output
                settings.OutputFormats.Current = "Converters.Text.XML";
                //OCR document and generate output.
                settings.ProcessPages(xmlPath, new string[] { imagePath });
                recipe = GetRecipeFromNuanceXML(xmlPath);
            }
            return recipe;
        }

        private Recipe GetRecipeFromNuanceXML(string xmlPath)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlPath);

            Recipe recipe = SetupRecipe();
            TextOverlay textOverlay = recipe.ParsedResults[0].TextOverlay;

            XmlNode pageInfoNode = xmlDoc.SelectSingleNode("/document/page[1]/description/theoreticalPage");
            textOverlay.Width = Int32.Parse(pageInfoNode.Attributes["width"].Value);
            textOverlay.Height = Int32.Parse(pageInfoNode.Attributes["height"].Value);
            XmlNodeList nodeList = xmlDoc.SelectNodes("/document/page[1]/body//para/ln");

            foreach (XmlNode lnNode in nodeList)
            {
                TextLine line = new TextLine();
                line.Words = new List<TextWord>();
                line.MinTop = Int32.Parse(lnNode.Attributes["t"].Value);
                line.MaxHeight = Int32.Parse(lnNode.Attributes["b"].Value) - line.MinTop;
                foreach (XmlNode nodeInsideLn in lnNode.ChildNodes)
                {
                    if (nodeInsideLn.Name == "wd")
                    {
                        line.Words.Add(CreateWordFromWordNode(nodeInsideLn));
                    }
                    else if (nodeInsideLn.Name == "run")
                    {
                        foreach (XmlNode nodeInsideRun in nodeInsideLn.ChildNodes)
                        {
                            if (nodeInsideLn.Name == "wd")
                            {
                                TextWord word = CreateWordFromWordNode(nodeInsideRun);
                                // May be checking actual bold attribute is better than cheching
                                // deviance from the normal text, depends on the recipe
                                // use the code below in case you just want to find Bold text
                                // word.Bold = Boolean.Parse(nodeInsideLn.Attributes["bold"].Value);
                                word.Bold = true;
                                line.Words.Add(word);
                            }
                        }
                    }
                }
                textOverlay.Lines.Add(line);
            }
            return recipe;
        }

        private TextWord CreateWordFromWordNode(XmlNode nodeInsideLn)
        {
            TextWord word = new TextWord();
            word.WordText = nodeInsideLn.InnerText;
            word.Left = Int32.Parse(nodeInsideLn.Attributes["l"].Value);
            word.Top = Int32.Parse(nodeInsideLn.Attributes["t"].Value);
            word.Width = Int32.Parse(nodeInsideLn.Attributes["r"].Value) - word.Left;
            word.Height = Int32.Parse(nodeInsideLn.Attributes["b"].Value) - word.Top;
            return word;
        }

        private Recipe SetupRecipe()
        {
            Recipe recipe = new Recipe();
            recipe.IsErroredOnProcessing = false;
            recipe.OCRExitCode = 0;
            recipe.ProcessingTimeInMilliseconds = 1;
            recipe.ParsedResults = new List<ParsedResult>();
            ParsedResult parsedResult = new ParsedResult();
            parsedResult.FileParseExitCode = 0;
            parsedResult.TextOverlay = new TextOverlay();
            parsedResult.TextOverlay.Lines = new List<TextLine>();
            recipe.ParsedResults.Add(parsedResult);
            return recipe;
        }
    }
}