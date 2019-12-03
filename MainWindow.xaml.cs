using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace VKParser
{
    public partial class MainWindow : Window
    {
        Thread mainThread = new Thread(Parser.StartSelenium);
        public MainWindow()
        {
            InitializeComponent();
        }
        public void Button_Launch(object sender, RoutedEventArgs e)
        {
            if (Parser.login != null && Parser.login != null)
            {
                Console.WriteLine($"Login: {Parser.login} \nPassword: {Parser.password} \nStarting parsing...");
                mainThread.Start();
            }
        }
        private void Button_Shutdown(object sender, RoutedEventArgs e)
        {
            //Parser.StopSelenium();
            //mainThread.Join();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //TextBox textBox = 
        }
        public void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox loginBox = sender as TextBox;
            Parser.login = loginBox.Text;
        }
        public void PasswordBox_TextChanged(object sender, RoutedEventArgs e)
        {
            PasswordBox passwordBox = sender as PasswordBox;
            Parser.password = passwordBox.Password;
        }
        internal static class Parser
        {
            public static string login, password;
            private static ChromeDriver driver;

            public enum byType {selector, className, name}
            public static void StartSelenium()
            {
                driver = new ChromeDriver();
                goTo("https://vk.com/");

                //authentification
                find(byType.selector, "#index_email").SendKeys(login);
                find(byType.selector, "#index_pass").SendKeys(password);
                find(byType.selector, "#index_login_button").Click();

                //new WebDriverWait(driver, TimeSpan.FromSeconds(10)).Until(ExpectedConditions.ElementToBeClickable(By.ClassName("feed_row")));
                //new WebDriverWait(driver, TimeSpan.FromSeconds(5));
                Thread.Sleep(5000);

                List<IWebElement> news = new List<IWebElement>();
                news = driver.FindElementsByClassName("feed_row").ToList();

                List<Post> postList = new List<Post>();

                for (int i = 0; i < news.Count(); i++)
                {
                    postList.Add(new Post(news[i], i));
                    if (postList[i].id != null) { 
                        Console.WriteLine(postList[i].ToString());
                    }
                }

                //Thread textWriteThread = new Thread(new ThreadStart(toJSON.SerializeText));
                //Thread imagesWriteThread = new Thread(toJSON.SerializeImages(postList, "images.json"));
                //Thread linksWriteThread = new Thread(toJSON.SerializeLinks(postList, "links.json"));

                toJSON.SerializeImages(postList, "images.json"); 
                toJSON.SerializeText(postList, "texts.json");
                toJSON.SerializeLinks(postList, "links.json");

                //StopSelenium();

                void Serialization(object settings)
                {

                }
            }

            public static IWebElement find(byType type, string target)
            {
                switch (type)
                {
                    case byType.className:
                        return(driver.FindElementByClassName(target));
                    case byType.selector:
                        return (driver.FindElementByCssSelector(target));
                    case byType.name:
                        return(driver.FindElementByName(target));
                    default:
                        return null;
                }
            }
            public static void goTo(string target)
            {
                driver.Navigate().GoToUrl(target);
            }
            public static void Checker()
            {
                while (true) Console.WriteLine("Obama");
            }
            public static void StopSelenium()
            {
                driver.Quit();
            }
        }
        internal static class toJSON
        {
            [DataContract]
            public class postImage
            {

                [DataMember]
                public string id { get; set; }

                [DataMember]
                public string[] images { get; set; }

                public postImage(Post post)
                {
                    id = post.id;
                    images = (post.imageURLs).ToArray();
                }
            }

            [DataContract]
            public class postLink
            {

                [DataMember]
                public string id { get; set; }

                [DataMember]
                public string[] links { get; set; }

                public postLink(Post post)
                {
                    id = post.id;
                    links = (post.links).ToArray();
                }
            }

            [DataContract]
            public class postText
            {

                [DataMember]
                public string id { get; set; }

                [DataMember]
                public string text { get; set; }

                public postText(Post post)
                {
                    id = post.id;
                    text = post.text;
                }
            }

            public static void SerializeImages(List<Post> postList, string filename)
            {
                List<postImage> postImages = new List<postImage>();
                for (int i = 0; i < postList.Count(); i++)
                {
                    if ((postList[i].id != null) && (postList[i].imageURLs != null))
                    {
                        postImages.Add(new postImage(postList[i]));
                    }
                }

                JavaScriptSerializer serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(postImages);

                using (StreamWriter sw = new StreamWriter(filename, true, Encoding.Default))
                {
                    sw.WriteLine(json);
                }
            }
            public static void SerializeLinks(List<Post> postList, string filename)
            {
                List<postLink> postLinks = new List<postLink>();
                for (int i = 0; i < postList.Count(); i++)
                {
                    if ((postList[i].id != null) && (postList[i].links != null))
                    {
                        postLinks.Add(new postLink(postList[i]));
                    }
                }

                JavaScriptSerializer serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(postLinks);

                using (StreamWriter sw = new StreamWriter(filename, true, Encoding.Default))
                {
                    sw.WriteLine(json);
                }
            }
            public static void SerializeText(List<Post> postList, string filename)
            {
                List<postText> postTexts = new List<postText>();
                for (int i = 0; i < postList.Count(); i++)
                {
                    if ((postList[i].id != null) && (postList[i].text != null)) {
                        postTexts.Add(new postText(postList[i]));
                    }
                }

                JavaScriptSerializer serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(postTexts);

                using (StreamWriter sw = new StreamWriter(filename, true, Encoding.Default))
                {
                    sw.WriteLine(json);
                }
            }
            public static void Deserialize(string filename)
            {

            }
        }
        internal class Post
        {
            int counter;
            public string id, text, date;
            public List<string> imageURLs;
            public List<string> links;

            public Post(IWebElement post, int counter)
            {
                this.counter = counter;
                //id новости
                List<IWebElement> linkElements = (from item in post.FindElements(By.XPath(@".//div")) where item.Displayed select item).ToList();
                if (!linkElements.Any())
                {
                    id = null;
                }
                else id = linkElements[0].GetAttribute("id");

                //картинки 
                List<IWebElement> imageElements = (from item in post.FindElements(By.XPath(@".//div[@class='page_post_sized_thumbs  clear_fix']//a")) where item.Displayed select item).ToList();
                if (!imageElements.Any())
                {
                    imageURLs = null;
                }
                else
                {
                    imageURLs = new List<string>();
                    foreach (var img in imageElements)
                    {
                        imageURLs.Add(img.GetCssValue("background-image").Replace("url", "").Replace("(", "").Replace(")", "").Replace("\"", ""));
                    }
                }

                //текст новости
                date = "0";
                List<IWebElement> textElements = (from item in post.FindElements(By.XPath(@".//div[@class='wall_post_text']")) where item.Displayed select item).ToList();
                if (!textElements.Any())
                {
                    text = null;
                } else text = textElements[0].Text.Replace("\n", " ");

                //ссылки в новости
                List<IWebElement> linksElements = (from item in post.FindElements(By.XPath(@".//div[@class='wall_post_text']//a")) where item.Displayed select item).ToList();
                if (!linksElements.Any())
                {
                    links = null;
                }
                else
                {
                    links = new List<string>();
                    foreach (var link in linksElements)
                    {
                        links.Add(link.GetAttribute("href"));
                    }
                }
            }
            public override string ToString()
            {
                string imageLinks = "";
                if (imageURLs != null) {
                    foreach (string link in imageURLs)
                    {
                        imageLinks += link + "\n ";
                    }
                }

                string hrefLinks = "";
                if (links != null)
                {
                    foreach (string link in links)
                    {
                        hrefLinks += link + "\n ";
                    }
                }

                string line = string.Format("Post #{0}: \n Id: {1} \n Text: {2} \n Images:\n {3}Links:\n {4} \n", counter, id, text, imageLinks, hrefLinks);
                return line;
            }

            private void toJSON() { }
        }
        internal static class settings
        {

        }
    }
}
