using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using ConsoleAppWikipedia.DAL;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ConsoleAppWikipedia
{
    class Program
    {
        //[JsonObject]
        //public class RootObject
        //{
        //    [JsonProperty("pages")]
        //    public Results Results { get; set; }
        //}

        [JsonObject]
        class Results
        {
            [JsonProperty("pages")]
            public Dictionary<string, Page> Pages { get; set; }
        }


        public class Test
        {
            public Dictionary<string, string> MessageCodes { get; set; }
        }

        [JsonObject]
        public class Page
        {
            [JsonProperty("pageid")]
            public int pageid { get; set; }

            [JsonProperty("ns")]
            public int ns { get; set; }

            [JsonProperty("title")]
            public string title { get; set; }

            [JsonProperty("extract")]
            public string extract { get; set; }

            [JsonProperty("images")]
            public List<Images> images { get; set; }
        }
        [JsonObject]
        public class Images
        {
            [JsonProperty("ns")]
            public int ns { get; set; }

            [JsonProperty("title")]
            public string title { get; set; }
        }

        static void Main(string[] args)
        {
            WebClient wc = new WebClient();

            List<string> stringList = new List<string>();

            var uniqueList = Helper.AddToList(stringList.Distinct().ToList());


            // Do something with the formatted string.. Parse it into pieces and build the wiki url
            // then you can query, serialize and add to db.

            //int tempIdToDb = 35000;


            var controllerText = "using System;" +
                                 "using System.Collections.Generic;" +
                                 "using System.Linq;" +
                                 "using System.Web;" +
                                 "using System.Web.Mvc;" +
                                 "using YourMVC.DAL;" +
                                 "namespace YourMVC.Controllers" +
                                 "{" +
                                 "public class LibraryController : Controller" +
                                 "{" +
                                 "public ActionResult Index()" +
                                 "{" +
                                 "var library = new YourMVCEntities();" +
                                 "var libraryResults = from m in library.Libraries" +
                                 "select m;" +
                                 "libraryResults = libraryResults.Where(s => !string.IsNullOrEmpty(s.Term));" +
                                 "return View(libraryResults);" +
                                 "}";




            foreach (var name in uniqueList)
            {





                var tempName = name.Replace(" ", "%20");

                var x = "https://en.wikipedia.org/w/api.php?titles=";
                var y = tempName;
                var z = "&action=query&prop=extracts%7Cpageimages%7Cimages&redirects=&format=json&formatversion=2";

                var url = x + y + z;

                var json = (JObject)JsonConvert.DeserializeObject(wc.DownloadString(url));

                if (json == null)
                    continue;


                // This gets you to the ROOT node. Pages I think.

                var results = json.Last ?? null;

                var desirializedJsonObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(wc.DownloadString(url));
                var tempDicObj = desirializedJsonObject["query"];

                if (results == null)
                    continue;

                var dictionary1 = JsonConvert.DeserializeObject<JObject>(tempDicObj.ToString()).GetValue("pages").First.Children();

                var title = string.Empty;
                var pageExtract = string.Empty;
                var id = string.Empty;

                foreach (var item in dictionary1)
                {

                    if (item.ToString().Contains("pageid"))
                    {
                        id = item.First.ToString();
                    }

                    if (item.ToString().Contains("extract"))
                    {
                        pageExtract = item.First.ToString();
                    }

                    if (item.ToString().Contains("title"))
                    {
                        title = item.ToString();
                    }
                }

                string ourUrl = "/Library/" + name.Replace(" ", "").Replace("(", "").Replace("-", "").Replace("  ", "").Replace(")", "");

                YourMVCEntities context = new YourMVCEntities();


                if (string.IsNullOrEmpty(pageExtract))
                    continue;



                var formattedName = name.Replace(" ", "").Replace("(", "").Replace("-", "").Replace("  ", "").Replace(")", ""); ;
                string viewPath = @"C:\Source\YourMVC\YourMVC\Views\Library\" + formattedName + ".cshtml";


                var extractText = "<!-- Breadcrumbs -->" +
                                    "<section class=\"g-bg-gray-light-v5 g-py-80\">" +
                                    "<div class=\"container text-center\">" +
                                    "<h2 class=\"h2 g-color-black g-font-weight-600\">" + name + "</h2>" +
                                    "<ul class=\"u-list-inline\">" +
                                    "<li class=\"list-inline-item g-mr-5\">" +
                                    "<a class=\"u-link-v5 g -color-gray-dark-v5 g-color-primary--hover\" href=\"/Library\">Home</a>" +
                                    "<i class=\"g-color-gray-light-v2 g-ml-5\">/</i>" +
                                    "</li>" +
                                    "<li class=\"list-inline-item g-color-primary\">" +
                                    "<span>" + name + "</span>" +
                                    "</li>" +
                                    "</ul>" +
                                    "</div>" +
                                    //"<button title=\"Back to serch results\" class=\"btn btn-link\" href=\"~/Library\">« Back</button>" +
                                    //"<p class=\"read-more text-info pull-right\"><a ng-href=\"https://en.wikipedia.org/wiki/" + ourUrl + " target=\"_blank\" href=\"https://en.wikipedia.org/wiki/" + ourUrl + ">Read on Wikipedia &gt;&gt; </a></p>" +
                                    "</section>" +
                                    "<!-- End Breadcrumbs -->" +
                                    "<div class=\"container\">" +
                                    //"<button title=\"Back to serch results\" class=\"btn btn-link\" href=\"/Library\">« Back</button>" +
                                    pageExtract.Replace(@"\", @"").Replace("/n", "").Replace("/n/n", "") +
                                    "</div>";


                var extractEdited = extractText.Replace("\\", "");


                var pageContent = extractEdited;

                // Add page method to controller
                controllerText += "public ActionResult " + formattedName + "()" +
                                  "{" +
                                  "return View();" +
                                  "}";

                // Create view file for term
                File.WriteAllText(viewPath, pageContent);



                // Setup the variable to add to the database
                var def = new Library()
                {
                    Id = int.Parse(id),
                    Term = name,
                    URLLink = ourUrl,
                    Definition = pageExtract
                };

                if (context.Libraries.Any(k => k.Definition == def.Definition))
                {
                    continue;
                }
                if (context.Libraries.Any(k => k.Id == def.Id))
                {
                    continue;
                }

                // Text inside controller file

                context.Libraries.Add(def);
                context.SaveChanges();

                Console.WriteLine(def.Definition);
            }

            controllerText += "}" +
                              "}";

            string libraryPath = @"C:\Source\YourMVC\YourMVC\Controllers\LibraryController.cs";

            // Create controller file with page method for each term
            // Comment out my code and just run your method from main. -Bill

            File.WriteAllText(libraryPath, controllerText);
        }
    }
}
