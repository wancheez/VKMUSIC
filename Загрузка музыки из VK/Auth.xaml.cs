using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.IO;

namespace Загрузка_музыки_из_VK
{
    /// <summary>
    /// Логика взаимодействия для Auth.xaml
    /// </summary>
    public partial class Auth : Window
    {
        private int appId;
        //Список разрешений (для удобства используем числовые значения)
        private enum VkontakteScopeList
        {
            /// <summary>
            /// Пользователь разрешил отправлять ему уведомления. 
            /// </summary>
            notify = 1,
            /// <summary>
            /// Доступ к друзьям.
            /// </summary>
            friends = 2,
            /// <summary>
            /// Доступ к фотографиям. 
            /// </summary>
            photos = 4,
            /// <summary>
            /// Доступ к аудиозаписям. 
            /// </summary>
            audio = 8,
            /// <summary>
            /// Доступ к видеозаписям. 
            /// </summary>
            video = 16,
            /// <summary>
            /// Доступ к предложениям (устаревшие методы). 
            /// </summary>
            offers = 32,
            /// <summary>
            /// Доступ к вопросам (устаревшие методы). 
            /// </summary>
            questions = 64,
            /// <summary>
            /// Доступ к wiki-страницам. 
            /// </summary>
            pages = 128,
            /// <summary>
            /// Добавление ссылки на приложение в меню слева.
            /// </summary>
            link = 256,
            /// <summary>
            /// Доступ заметкам пользователя. 
            /// </summary>
            notes = 2048,
            /// <summary>
            /// (для Standalone-приложений) Доступ к расширенным методам работы с сообщениями. 
            /// </summary>
            messages = 4096,
            /// <summary>
            /// Доступ к обычным и расширенным методам работы со стеной. 
            /// </summary>
            wall = 8192,
            /// <summary>
            /// Доступ к документам пользователя.
            /// </summary>
            docs = 131072
        }
        //Выбранные разрешения для приложения
        private int scope = (int)(VkontakteScopeList.audio);
        bool log;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="applicationVkId"></param>
        /// <param name="logIn">Если true, то залогиниться, иначе - выйти из акк</param>
        public Auth(int applicationVkId, bool logIn)
        {
            InitializeComponent();
            appId = applicationVkId;
            log = logIn;            
        }



        private void webBrowser_Auth_Loaded(object sender, RoutedEventArgs e)
        {
            if(log)
            webBrowser_Auth.Navigate(String.Format("http://api.vkontakte.ru/oauth/authorize?client_id={0}&scope={1}&display=popup&response_type=token", appId, scope));
            else
            {
               // webBrowser_Auth.Navigate(String.Format("http://api.vkontakte.ru/oauth/authorize?client_id={0}&scope={1}&display=popup&response_type=token", appId, scope));
                //webBrowser_Auth.Navigate("javascript:void((function(){var a,b,c,e,f;f=0;a=document.cookie.split('; ');for(e=0;e<a.length&&a[e];e++){f++;for(b='.'+location.host;b;b=b.replace(/^(?:%5C.|[^%5C.]+)/,'')){for(c=location.pathname;c;c=c.replace(/.$/,'')){document.cookie=(a[e]+'; domain='+b+'; path='+c+'; expires='+new Date((new Date()).getTime()-1e11).toGMTString());}}}})())");
                //Пытаемся разлогиниться
                File.Delete("userID.txt");
                //File.Create("userID.txt");
                
                MainWindow main = this.Owner as MainWindow;
                main.isAuthorised = false;
                this.Close();
            }
        
        }

        private void webBrowser_Auth_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            if (e.Uri.ToString().IndexOf("access_token") != -1)
            {
                string accessToken = "";
                int userId = 0;
                Regex myReg = new Regex(@"(?<name>[\w\d\x5f]+)=(?<value>[^\x26\s]+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                foreach (Match m in myReg.Matches(e.Uri.ToString()))
                {
                    if (m.Groups["name"].Value == "access_token")
                    {
                        accessToken = m.Groups["value"].Value;
                    }
                    else if (m.Groups["name"].Value == "user_id")
                    {
                        userId = Convert.ToInt32(m.Groups["value"].Value);
                    }
                    // еще можно запомнить срок жизни access_token - expires_in,
                    // если нужно
                }

                //MessageBox.Show(String.Format("Ключ доступа: {0}\nUserID: {1}", accessToken, userId));
                MainWindow main = this.Owner as MainWindow;
                if (main != null)
                {

                    main.userId = userId;
                    main.accessToken = accessToken;
                    main.isAuthorised = true;//Авторизация прошла успешна. Метка для основной формы
                    /*
                    if (!Directory.Exists(Environment.GetEnvironmentVariable("appdata") + "\\VKPLAYER"))
                        Directory.CreateDirectory(Environment.GetEnvironmentVariable("appdata") + "\\VKPLAYER");
                    StreamWriter write_text = new StreamWriter(Environment.GetEnvironmentVariable("appdata") + "\\VKPLAYER\\userID.txt", false);
                     */
                    if (!File.Exists("userID.txt"))
                        File.Create("userID.txt");
                    StreamWriter write_text = new StreamWriter("userID.txt", false);
                    //write_text = main.file.AppendText();
                    write_text.WriteLine(accessToken.ToString());
                    write_text.WriteLine(userId.ToString());
                    write_text.Close();
                    this.Close();
                }
                else
                    MessageBox.Show("Возникли проблемы...");
                this.Close();
            }
        }
    }
}
