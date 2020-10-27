using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using NLog;

namespace KLCWDownloader {
   class Program {
      static void Main(string[] args) {
         //Przydałoby się zrobić obsługę wielowątkowości
         int threads = Environment.ProcessorCount;
         ServicePointManager.DefaultConnectionLimit = threads;
         ServicePointManager.Expect100Continue = false;



         DownloadMangaPanda(750);
      }

      private static void DownloadMangaPanda(int maxChapter) {
         string basePath = @"D:\Naruto\";
         string baseUrl = "http://manga-panda.xyz/naruto-chapter-";
         //string baseUrl = "http://manga-panda.xyz/naruto-full-color-chapter-";

         for(int chapter = 637; chapter <= maxChapter; chapter++) {
            string chapterUrl = string.Format("{0}{1}", baseUrl, chapter);
            string chapterPath = string.Format(@"{0}{1:D3}\", basePath, chapter);
            _logger.Trace("Przetwarzam stronę {0}", chapterUrl);

            var request = (HttpWebRequest)WebRequest.Create(chapterUrl);
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:80.0) Gecko/20100101 Firefox/80.0";
            using(var response = (HttpWebResponse)request.GetResponse()) {
               string page = new StreamReader(response.GetResponseStream()).ReadToEnd();

               //<p id=arraydata style=display:none> ... </p>
               Regex pattern = new Regex(@"<p id=arraydata style=display:none>(?<array>[\w\d\s+-–:._%#&/]+)<\/p>");
               MatchCollection mp1 = pattern.Matches(page);
               if(mp1.Count > 0) {
                  DateTime dt = DateTime.MinValue;
                  string sArray = mp1[0].Groups["array"].Value?.Trim();
                  string[] imgs = sArray.Split(',');
                  _logger.Trace("\t Do pobrania {0} plików", imgs.Length);
                  for(int i = 0; i < imgs.Length; i++) {
                     DownloadImage(chapterPath, imgs[i], i.ToString("D3"));
                  }
               } else {
                  //Nie znaleziono rozdziału więc prawdopodobnie koniec
                  _logger.Trace("Koniec na rozdziale {0}", chapterUrl);
                  return;
               }
            }
         }
      }




      static bool DownloadImage(string path, string url, string name) {
         bool ret = false;
         string filePath = Path.Combine(path, CleanFileName(name?.Trim()));
         if(!Directory.Exists(path))
            Directory.CreateDirectory(path);

         if(filePath.Length >= 250)
            filePath = filePath.Substring(0, 249);

         filePath += ".jpeg";

         try {
            if(!File.Exists(filePath)) {
               WebClient wb = new WebClient();
               wb.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:80.0) Gecko/20100101 Firefox/80.0");
               wb.DownloadFile(new Uri(url), filePath);
               ret = true;
            } else
               _logger.Trace($"Pomijam plik -> {url}");
         } catch(Exception ex) {
            _logger.Error(ex, "Wystąpił błąd w trakcie pobierania pliku {0}", filePath);
            File.Delete(filePath);
         }
         return ret;
      }


      static string CleanFileName(string name) {
         foreach(char c in System.IO.Path.GetInvalidFileNameChars())
            name = name.Replace(c.ToString(), "");
         return name;
      }

      private static Logger _logger = LogManager.GetCurrentClassLogger();
   }
}

