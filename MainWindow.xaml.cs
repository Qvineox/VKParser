using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.ServiceProcess;
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
        public MainWindow()
        {
            InitializeComponent();
        }
        public void Button_Launch(object sender, RoutedEventArgs e)
        {
            Thread mainThread = new Thread(Parser.StartSelenium);
            if (Parser.login != null && Parser.login != null)
            {
                Console.WriteLine($"Login: {Parser.login} \nPassword: {Parser.password} \nStarting parsing...");
                mainThread.Start();
            }
        }
        private void Button_Shutdown(object sender, RoutedEventArgs e)
        {
            Parser.loop = false;
            MessageBox.Show("Ожидайте завершения итерации...");
        }
        private void Button_Login(object sender, RoutedEventArgs e)
        {
             
        }
        private void Button_Loop(object sender, RoutedEventArgs e)
        {
            Thread loopThread = new Thread(Parser.LoopSelenium);
            if (Parser.login != null && Parser.login != null)
            {
                Console.WriteLine($"Login: {Parser.login} \nPassword: {Parser.password} \nStarting parsing...");
                Parser.loop = true;
                loopThread.Start();
            }
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
            public static List<Post> postList;
            public static bool loop;
            public enum byType {selector, className, name}
            public enum jsonManager { image, text, link }
            public static void StartSelenium()
            {
                driver = new ChromeDriver();
                goTo("https://vk.com/");

                find(byType.selector, "#index_email").SendKeys(login);
                find(byType.selector, "#index_pass").SendKeys(password);
                find(byType.selector, "#index_login_button").Click();

                new WebDriverWait(driver, TimeSpan.FromSeconds(10)).Until(ExpectedConditions.ElementToBeClickable(By.ClassName("feed_row")));
                //Thread.Sleep(5000);

                getNews();

                settings writerSettings = new settings("imagesT.json", "linksT.json", "textsT.json", postList);

                Thread imagesThread = new Thread(new ThreadStart(writerSettings.imageThread));
                Thread textThread = new Thread(new ThreadStart(writerSettings.textThread));
                Thread linksThread = new Thread(new ThreadStart(writerSettings.linkThread));

                imagesThread.Start();
                textThread.Start();
                linksThread.Start();
                imagesThread.Join();
                textThread.Join();
                linksThread.Join();

                Console.WriteLine("Одиночная запись завершена!");

                serviceController handler = new serviceController();
                handler.startService();

                StopSelenium();
            }
            public static void getNews()
            {
                postList = new List<Post>();
                List<IWebElement> news = new List<IWebElement>();
                news = driver.FindElementsByClassName("feed_row").ToList();

                for (int i = 0; i < news.Count(); i++)
                {
                    postList.Add(new Post(news[i], i));
                    if (postList[i].postId != null)
                    {
                        Console.WriteLine(postList[i].ToString());
                    }
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
            public static void LoopSelenium()
            {
                Console.WriteLine($"Login: {login} \nPassword: {password} \nStarting parsing...");

                driver = new ChromeDriver();
                goTo("https://vk.com/");

                find(byType.selector, "#index_email").SendKeys(login);
                find(byType.selector, "#index_pass").SendKeys(password);
                find(byType.selector, "#index_login_button").Click();

                new WebDriverWait(driver, TimeSpan.FromSeconds(10)).Until(ExpectedConditions.ElementToBeClickable(By.ClassName("feed_row")));
                //Thread.Sleep(5000);

                getNews();

                settings writerSettings = new settings("imagesT.json", "linksT.json", "textsT.json", postList);

                Thread imagesThread = new Thread(new ThreadStart(writerSettings.imageThread));
                Thread textThread = new Thread(new ThreadStart(writerSettings.textThread));
                Thread linksThread = new Thread(new ThreadStart(writerSettings.linkThread));

                imagesThread.Start();
                textThread.Start();
                linksThread.Start();
                imagesThread.Join();
                textThread.Join();
                linksThread.Join();

                Console.WriteLine("Первоначальная запись завершена!");

                serviceController serviceHandler = new serviceController();

                jsonManager switcher = jsonManager.image;

                while (loop)
                {
                    driver.Navigate().Refresh();
                    getNews();

                    Thread imagesWriteThread = new Thread(new ThreadStart(writerSettings.imageThread));
                    Thread textWriteThread = new Thread(new ThreadStart(writerSettings.textThread));
                    Thread linksWriteThread = new Thread(new ThreadStart(writerSettings.linkThread));

                    switch (switcher)
                    {
                        case (jsonManager.image):
                            {
                                Console.WriteLine("ВАЖНО: Начата итерация картинок...");
                                Thread deserializeThread = new Thread(() => toJSON.Deserialize(switcher));
                                deserializeThread.Start();
                                textWriteThread.Start();
                                linksWriteThread.Start();

                                deserializeThread.Join();
                                textWriteThread.Join();
                                linksWriteThread.Join();

                                serviceHandler.operate();
                                switcher = jsonManager.text;
                                Console.WriteLine("ВАЖНО: Окончена итерация картинок...");
                                break;
                            }
                        case (jsonManager.text):
                            {
                                Console.WriteLine("ВАЖНО: Начата итерация текста...");
                                imagesWriteThread.Start();
                                Thread deserializeThread = new Thread(() => toJSON.Deserialize(switcher));
                                deserializeThread.Start();
                                linksWriteThread.Start();

                                imagesWriteThread.Join();
                                deserializeThread.Join();
                                linksWriteThread.Join();

                                serviceHandler.operate();
                                switcher = jsonManager.link;
                                Console.WriteLine("ВАЖНО: Окончена итерация текста...");
                                break;
                            }
                        case (jsonManager.link):
                            {
                                Console.WriteLine("ВАЖНО: Начата итерация текста...");
                                imagesWriteThread.Start();
                                textWriteThread.Start();
                                Thread deserializeThread = new Thread(() => toJSON.Deserialize(switcher));
                                deserializeThread.Start();

                                imagesWriteThread.Join();
                                textWriteThread.Join();
                                deserializeThread.Join();

                                serviceHandler.operate();
                                switcher = jsonManager.image;
                                Console.WriteLine("ВАЖНО: Окончена итерация текста...");
                                break;
                            }
                    }
                }

                StopSelenium();
            }
            public static void StopSelenium()
            {
                if (driver != null)
                {
                    driver.Quit();
                    Thread.CurrentThread.Abort();
                }
            }
        }
        internal static class toJSON
        {
            [DataContract]
            public class postImage
            {

                [DataMember]
                public string postId { get; set; }

                [DataMember]
                public string[] images { get; set; }

                public postImage(Post post)
                {
                    postId = post.postId;
                    images = (post.imageURLs).ToArray();
                }

                public postImage()
                {

                }
            }

            [DataContract]
            public class postLink
            {

                [DataMember]
                public string postId { get; set; }

                [DataMember]
                public string[] links { get; set; }

                public postLink(Post post)
                {
                    postId = post.postId;
                    links = (post.links).ToArray();
                }
            }

            [DataContract]
            public class postText
            {

                [DataMember]
                public string postId { get; set; }

                [DataMember]
                public string text { get; set; }

                public postText(Post post)
                {
                    postId = post.postId;
                    text = post.text;
                }
            }

            public static void SerializeImages(List<Post> postList, string filename)
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                List<postImage> postImages = new List<postImage>();

                if (File.Exists("imagesT.json"))
                {
                    postImages.AddRange(Deserialize(Parser.jsonManager.image));
                }

                
                for (int i = 0; i < postList.Count(); i++)
                {
                    if ((postList[i].postId != null) && (postList[i].imageURLs != null))
                    {
                        if () { 
                            postImages.Add(new postImage(postList[i]));
                        }
                    }
                }

                string json = serializer.Serialize(postImages);

                using (StreamWriter sw = new StreamWriter(filename, false, Encoding.UTF8))
                {
                    sw.WriteLine(json);
                }
            }
            public static void SerializeLinks(List<Post> postList, string filename)
            {
                List<postLink> postLinks = new List<postLink>();
                for (int i = 0; i < postList.Count(); i++)
                {
                    if ((postList[i].postId != null) && (postList[i].links != null))
                    {
                        postLinks.Add(new postLink(postList[i]));
                    }
                }

                JavaScriptSerializer serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(postLinks);

                using (StreamWriter sw = new StreamWriter(filename, false, Encoding.UTF8))
                {
                    sw.WriteLine(json);
                }
            }
            public static void SerializeText(List<Post> postList, string filename)
            {
                List<postText> postTexts = new List<postText>();
                for (int i = 0; i < postList.Count(); i++)
                {
                    if ((postList[i].postId != null) && (postList[i].text != null)) {
                        postTexts.Add(new postText(postList[i]));
                    }
                }

                JavaScriptSerializer serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(postTexts);

                using (StreamWriter sw = new StreamWriter(filename, false, Encoding.UTF8))
                {
                    sw.WriteLine(json);
                }
            }
            public static List<postImage> Deserialize(Parser.jsonManager switcher)
            {
                switch (switcher)
                {
                    case (Parser.jsonManager.image):
                        {
                            string json;
                            using (StreamReader sr = new StreamReader("imagesT.json"))
                            {
                                json = sr.ReadToEnd();
                            }

                            JavaScriptSerializer serializer = new JavaScriptSerializer();
                            List<postImage> images = serializer.Deserialize<List<postImage>>(json);
                            return images;
                        }
                    case (Parser.jsonManager.text):
                        {
                            string json;
                            using (StreamReader sr = new StreamReader("textsT.json"))
                            {
                                json = sr.ReadToEnd();
                            }

                            JavaScriptSerializer serializer = new JavaScriptSerializer();
                            List<postImage> texts = serializer.Deserialize<List<postImage>>(json);
                            return texts;
                        }
                    case (Parser.jsonManager.link):
                        {
                            string json;
                            using (StreamReader sr = new StreamReader("linksT.json"))
                            {
                                json = sr.ReadToEnd();
                            }

                            JavaScriptSerializer serializer = new JavaScriptSerializer();
                            List<postImage> links = serializer.Deserialize<List<postImage>>(json);
                            return links;
                        }
                    default:
                        {
                            return null;
                        }
                }
                
            }
        }
        internal class Post
        {
            int counter;
            public string postId, text, date;
            public List<string> imageURLs;
            public List<string> links;

            public Post(IWebElement post, int counter)
            {
                this.counter = counter;
                //id новости
                List<IWebElement> linkElements = (from item in post.FindElements(By.XPath(@".//div")) where item.Displayed select item).ToList();
                if (!linkElements.Any())
                {
                    postId = null;
                }
                else postId = linkElements[0].GetAttribute("id");

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

                string line = string.Format("Post #{0}: \n Id: {1} \n Text: {2} \n Images:\n {3}Links:\n {4} \n", counter, postId, text, imageLinks, hrefLinks);
                return line;
            }
            private void toJSON() { }
        }
        internal class settings
        {
            string imagesFileName, linksFileName, textFileName;
            List<Post> postList;
            public settings(string imagesFileName, string linksFileName, string textFileName, List<Post> postList)
            {
                this.imagesFileName = imagesFileName;
                this.linksFileName = linksFileName;
                this.textFileName = textFileName;
                this.postList = postList;
            }

            public void imageThread()
            {
                Console.WriteLine("Начата запись картинок!");
                toJSON.SerializeImages(postList, imagesFileName);
                Console.WriteLine("Закончена запись картинок!");
            }
            public void linkThread()
            {
                Console.WriteLine("Начата запись ссылок!");
                toJSON.SerializeLinks(postList, linksFileName);
                Console.WriteLine("Закончена запись ссылок!");
            }
            public void textThread()
            {
                Console.WriteLine("Начата запись текстов!");
                toJSON.SerializeText(postList, textFileName);
                Console.WriteLine("Закончена запись текстов!");
            }
            public void imageReadThread()
            {
                Console.WriteLine("Начато чтение картинок...");
                Console.WriteLine("Завершено чтение картинок...");
            }
            public void linkReadThread()
            {
                Console.WriteLine("Начато чтение ссылок...");
                Console.WriteLine("Завершено чтение ссылок...");
            }
            public void textReadThread()
            {
                Console.WriteLine("Начато чтение текстов...");
                Console.WriteLine("Завершено чтение текстов...");
            }
        } 
        internal class serviceController
        {
            public void startService()
            {
                Console.WriteLine("Попытка запуска службы...");
                ServiceController service = new ServiceController("VKDBService");
                if (service.Status != ServiceControllerStatus.Running)
                {
                    service.Start();
                } else
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMinutes(1));
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMinutes(1));
                    Console.WriteLine("Служба была успешно перезапущена!");
                }
            }
            public void stopService()
            {
                Console.WriteLine("Попытка остановки службы...");
                ServiceController service = new ServiceController("VKDBService");
                if (service.Status != ServiceControllerStatus.Stopped)
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMinutes(1));
                    Console.WriteLine("Служба была успешно остановлена!");
                }
                else
                {
                    Console.WriteLine("Служба остановлена!");
                }
            }
            public void operate()
            {
                startService();
                ServiceController service = new ServiceController("VKDBService");
                Console.WriteLine("Ожидание завершения работы службы...");
                service.WaitForStatus(ServiceControllerStatus.Stopped);
                stopService();
            }
        }
    }
}
