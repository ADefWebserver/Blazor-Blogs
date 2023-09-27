using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BlazorBlogsLibrary.Data.Models
{
    public class NewsletterParser
    {
        HttpClient client = new HttpClient();

        public NewsletterParser()
        {

        }

        #region public List<NewsletterContent> ParseHtmlImageLinks(string html)
        public List<NewsletterContent> ParseHtmlImageLinks(string html)
        {
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            var RawLinks = htmlDoc.DocumentNode.ChildNodes.ToList();

            List<NewsletterContent> Links = new List<NewsletterContent>();

            // Loop through array using iterator

            for (int i = 0; i < RawLinks.Count; i++)
            {
                var InnerHTML = RawLinks[i].InnerHtml;

                if (InnerHTML.Contains("img"))
                {
                    // Loop through each childNode
                    foreach (var childNode in RawLinks[i].ChildNodes)
                    {
                        // Call recursively
                        var ChildNodeHTML = "";
                        if (childNode.InnerHtml.Trim() != "")
                        {
                            ChildNodeHTML = childNode.InnerHtml;
                        }
                        else
                        {
                            ChildNodeHTML = childNode.OuterHtml;
                        }

                        ParseChildLinks(ref Links, ChildNodeHTML);
                    }

                    if (RawLinks[i].FirstChild.Attributes["src"] != null)
                    {
                        var Url = RawLinks[i].FirstChild.Attributes["src"].Value;

                        if (!DetectDuplcateLink(Links, Url))
                        {
                            Links.Add(new NewsletterContent
                            {
                                ContentType = "Url",
                                Content = Url
                            });
                        }
                    }
                }

                if (RawLinks[i].InnerText != "")
                {
                    var TextBlock = ScrubHtml(RawLinks[i].InnerText.ToLower());

                    // If we have li's we have to use the raw html instead of the inner text
                    // otherwise it will run all the text together
                    if (RawLinks[i].InnerHtml.ToString().ToLower().Contains("<li>"))
                    {
                        var PutSpacesAroundli = RawLinks[i].InnerHtml.ToString().ToLower().Replace("<li>", "<li>&nbsp; ");
                        TextBlock = ScrubHtml(PutSpacesAroundli);
                    }

                    if (TextBlock.Trim().Length > 0)
                        Links.Add(new NewsletterContent { ContentType = "Text", Content = TextBlock });
                }
            }

            return Links;
        }

        private void ParseChildLinks(ref List<NewsletterContent> links, string paramChildNodeHTML)
        {
            // Call recursively
            if (paramChildNodeHTML.Contains("img"))
            {
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(paramChildNodeHTML);

                var RawLinks = htmlDoc.DocumentNode.ChildNodes.ToList();

                // Loop through array using iterator
                for (int i = 0; i < RawLinks.Count; i++)
                {
                    var ChildNodeHTML = "";
                    if (RawLinks[i].InnerHtml.Trim() != "")
                    {
                        ChildNodeHTML = RawLinks[i].InnerHtml;
                    }
                    else
                    {
                        ChildNodeHTML = RawLinks[i].OuterHtml;
                    }

                    if (ChildNodeHTML.Contains("img"))
                    {
                        // Loop through each childNode
                        foreach (var childNode in RawLinks[i].ChildNodes)
                        {
                            // Call recursively
                            var InnerChildNode = "";
                            if (childNode.InnerHtml.Trim() != "")
                            {
                                InnerChildNode = childNode.InnerHtml;
                            }
                            else
                            {
                                InnerChildNode = childNode.OuterHtml;
                            }

                            ParseChildLinks(ref links, InnerChildNode);

                            if (childNode.Name == "img")
                            {
                                if (childNode.Attributes["src"] != null)
                                {
                                    var Url = childNode.Attributes["src"].Value;
                                    if (!DetectDuplcateLink(links, Url))
                                    {
                                        links.Add(new NewsletterContent { ContentType = "Url", Content = Url });
                                    }
                                }
                            }

                            if (childNode.InnerText != "")
                            {
                                var TextBlock = ScrubHtml(childNode.InnerText.ToLower());

                                // If we have li's we have to use the raw html instead of the inner text
                                // otherwise it will run all the text together
                                if (RawLinks[i].InnerHtml.ToString().ToLower().Contains("<li>"))
                                {
                                    var PutSpacesAroundli = RawLinks[i].InnerHtml.ToString().ToLower().Replace("<li>", "<li>&nbsp; ");
                                    TextBlock = ScrubHtml(PutSpacesAroundli);
                                }

                                if (TextBlock.Trim().Length > 0)
                                    links.Add(new NewsletterContent { ContentType = "Text", Content = TextBlock });
                            }
                        }

                        if (RawLinks[i].Attributes["src"] != null)
                        {
                            var Url = RawLinks[i].Attributes["src"].Value;
                            if (!DetectDuplcateLink(links, Url))
                            {
                                links.Add(new NewsletterContent { ContentType = "Url", Content = Url });
                            }
                        }

                        if (RawLinks[i].FirstChild != null)
                        {
                            if (RawLinks[i].FirstChild.Attributes["src"] != null)
                            {
                                var Url = RawLinks[i].FirstChild.Attributes["src"].Value;
                                if (!DetectDuplcateLink(links, Url))
                                {
                                    links.Add(new NewsletterContent { ContentType = "Url", Content = Url });
                                }
                            }
                        }
                    }

                    if (RawLinks[i].InnerText != "")
                    {
                        var TextBlock = ScrubHtml(RawLinks[i].InnerText.ToLower());

                        // If we have li's we have to use the raw html instead of the inner text
                        // otherwise it will run all the text together
                        if (RawLinks[i].InnerHtml.ToString().ToLower().Contains("<li>"))
                        {
                            var PutSpacesAroundli = RawLinks[i].InnerHtml.ToString().ToLower().Replace("<li>", "<li>&nbsp; ");
                            TextBlock = ScrubHtml(PutSpacesAroundli);
                        }

                        if (TextBlock.Trim().Length > 0)
                            links.Add(new NewsletterContent { ContentType = "Text", Content = TextBlock });
                    }
                }
            }
        }
        #endregion

        #region private List<string> ParseHtmlText(List<NewsletterContent> NewsletterContent)
        private List<string> ParseHtmlText(List<NewsletterContent> NewsletterContent)
        {
            List<string> TextBlocks = new List<string>();

            // Loop through array using iterator
            string CurrentTextBlock = "";

            // Skip the [Title] image
            foreach (var item in NewsletterContent.Where(x => x.Content != "[Title]"))
            {
                if (item.ContentType == "Text")
                {
                    // Filter to deal with periods at the end of sentences
                    CurrentTextBlock = CurrentTextBlock + item.Content.Replace(".", ". ");
                }
                else
                {
                    // Add and reset current text block
                    if (CurrentTextBlock != "")
                    {
                        TextBlocks.Add(CurrentTextBlock);
                        CurrentTextBlock = "";
                    }
                }
            }

            // If the last CurrentTextBlock was not added to the TextBlocks collection add it
            if (CurrentTextBlock != "")
            {
                // Get the last TextBlock
                var lastTextBlock = TextBlocks.LastOrDefault();

                if (lastTextBlock != null)
                {
                    // If the last TextBlock is not equal to the CurrentTextBlock add it
                    if (lastTextBlock != CurrentTextBlock)
                    {
                        TextBlocks.Add(CurrentTextBlock);
                    }
                }
                else
                {
                    // There was no last TextBlock so add the CurrentTextBlock
                    TextBlocks.Add(CurrentTextBlock);
                }
            }

            return TextBlocks;
        }
        #endregion

        #region public bool DetectDuplcateLink(List<NewsletterContent> links, string paramLinkToAdd)
        public bool DetectDuplcateLink(List<NewsletterContent> links, string paramLinkToAdd)
        {
            bool duplicateDetected = false;
            // loop through all links looking for paramLinkToAdd

            foreach (var link in links)
            {
                if (link.Content.ToLower() == paramLinkToAdd.ToLower())
                {
                    duplicateDetected = true;
                }
            }

            return duplicateDetected;
        }
        #endregion

        public AlternateView CreateEmailHTML(string EmailContent, Dictionary<string, Image> colImages)
        {
            AlternateView alternateView = null;

            try
            {
                List<LinkedResource> linkedResources = new List<LinkedResource>();

                // Loop through each image
                foreach (var item in colImages)
                {
                    string ImageName = item.Key;
                    Image ImageData = item.Value;

                    var objImage = PngImageToByteArray(ImageData);
                    LinkedResource LinkResource = new LinkedResource(new MemoryStream(objImage));
                    LinkResource.ContentId = Guid.NewGuid().ToString();
                    LinkResource.ContentType = new ContentType(MediaTypeNames.Image.Jpeg);

                    linkedResources.Add(LinkResource);

                    string URLTagToReplace = $"<img src=\"{ImageName}\"";

                    EmailContent = EmailContent.Replace(URLTagToReplace, "<img src='cid:" + LinkResource.ContentId + @"'");
                }

                alternateView = AlternateView.CreateAlternateViewFromString(EmailContent, Encoding.UTF8, MediaTypeNames.Text.Html);

                // Add the images to the email
                foreach (var item in linkedResources)
                {
                    alternateView.LinkedResources.Add(item);
                }                
            }
            catch
            {
                return null;
            }

            return alternateView;
        }

        // Utility

        #region public static string ScrubHtml(string value)
        public static string ScrubHtml(string value)
        {
            var step1 = Regex.Replace(value, @"<[^>]+>|&nbsp;", " ").Trim();
            var step2 = Regex.Replace(step1, @"\s{2,}", " ");
            return step2;
        }
        #endregion

        #region public System.Drawing.Image DownloadImageFromUrl(string imageUrl)
        public System.Drawing.Image DownloadImageFromUrl(string imageUrl)
        {
            System.Drawing.Image image = null;

            try
            {
                var response = client.GetAsync(new Uri(imageUrl)).Result;

                if (response != null && response.StatusCode == HttpStatusCode.OK)
                {
                    //download images to photos and save
                    try
                    {
                        using (MemoryStream stream = new MemoryStream())
                        {
                            var buffer = response.Content.ReadAsStreamAsync();
                            buffer.Result.CopyTo(stream);
                            image = Image.FromStream(stream, false, true);
                        }
                    }
                    catch
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }

            return image;
        }
        #endregion

        #region public byte[] PngImageToByteArray(Image imageIn)
        public byte[] PngImageToByteArray(Image imageIn)
        {
            MemoryStream ms = new MemoryStream();
            imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }
        #endregion

    }
}
