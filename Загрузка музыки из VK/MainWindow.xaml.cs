﻿using System;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.IO;
using System.Net;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Windows.Media.Animation;

namespace Загрузка_музыки_из_VK
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class audioItems
        {
            public string title { get; set; }
            public string artist { get; set; }
            public string duration { get; set; }
            public string source { get; set; }
            public bool audioChecked { get; set; }
            public DataTemplate buttonPlay { get; set; }
        public audioItems()
        {
            //Страшные вещи для создание кнопки в таблице. ПОСМОТРЕВNМ CХОДRТ С УМА
            string xaml = "<DataTemplate><Button Content=\"кнопко\" /></DataTemplate>";
            var sr = new MemoryStream(Encoding.UTF8.GetBytes(xaml));
            var pc = new ParserContext();
            pc.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
            pc.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");
            buttonPlay = (DataTemplate)XamlReader.Load(sr, pc);
            
        }
        }
        static private int appId = 4201412;
        Auth auth;
        VKAPI vkApiClass;
        public string accessToken = "";
        public int userId = 0;
        public bool isAuthorised = false;
        bool searchUserAudio = true; //Искать по записям пользователя. Если false, ищем глобально по сети
        XmlDocument profileXml;
        int currentOffset = 0;
        const int N = 20;
        List<audioItems> currentAudioList = new List<audioItems>(); //Список записей, которые сейчас отображены
        List<audioItems> audioToDownload = new List<audioItems>();//Список аудиозаписей, которые нужно скачать
        WebClient client;
        bool isPaused = true; //Стоит ли музыка на паузе
        int num = 0; //Переменная используется, чтобы при окончании трека запускался следующий. В переменной хранится смещение от первой композиции
        DispatcherTimer timer = new DispatcherTimer(); //Таймер для работы слайдера и вывода времени проигрывания. Пока проще выхода не придумал
        TimeSpan TotalTime;
        byte currentCatalog = 0; //Мои записи - 0/Популярное - 1/Рекомендации - 2/Поиск - 3

        public MainWindow()
        {
            InitializeComponent();
            client = new WebClient();
            dataGridView1.ItemsSource = currentAudioList;
            //Инициализация таймера
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;
           
            //Привязываем события к клиенту
            client.DownloadFileCompleted +=client_DownloadFileCompleted;
            client.DownloadProgressChanged += client_DownloadProgressChanged;
            //Авторизация
            if (File.Exists(Environment.GetEnvironmentVariable("appdata") + "\\VKPLAYER\\userID.txt"))
            {
                StreamReader streamReader = new StreamReader(Environment.GetEnvironmentVariable("appdata") + "\\VKPLAYER\\userID.txt");
                string str1 = "";
                string str2 = "";

                str1 += streamReader.ReadLine();
                str2 += streamReader.ReadLine();

                //Если уже авторизовывались ранее, читаем access token и user Id из файла.
                if (str1.Length != 0 && str2.Length != 0)
                {
                    accessToken = str1;
                    userId = Convert.ToInt32(str2);
                    vkApiClass = new VKAPI(appId, accessToken);
                    isAuthorised = true;
                    XmlDocument profileXml;
                    string userName = "";
                    profileXml = vkApiClass.getProfile(userId);
                    userName = GetDataFromXmlNode(profileXml.SelectSingleNode("response/user/first_name")) + " " + GetDataFromXmlNode(profileXml.SelectSingleNode("response/user/last_name"));

                    if (!(userName == "нет данных нет данных"))
                    {
                        vkApiClass = new VKAPI(appId, accessToken);

                        //Разукрашиваем кнопку авторизации
                        var bc = new BrushConverter();
                        LinearGradientBrush myLinearGradientBrush =
                        new LinearGradientBrush();
                        myLinearGradientBrush.StartPoint = new Point(0.5,0);
                        myLinearGradientBrush.EndPoint = new Point(0.5,1);
                        myLinearGradientBrush.GradientStops.Add(
                            new GradientStop((Color)ColorConverter.ConvertFromString("#FF5C7EA3"), 0.0));
                        myLinearGradientBrush.GradientStops.Add(
                            new GradientStop((Color)ColorConverter.ConvertFromString("#FF455D83"), 1));
                        button_auth.Background = myLinearGradientBrush;
                        
                        StackPanel myStackPanel = new StackPanel();
                        myStackPanel.Orientation = Orientation.Horizontal;
                        Image img = new Image();
                        img.Source = new BitmapImage(new Uri("Resources/Login-01.png", UriKind.Relative));
                        
                        myStackPanel.Children.Add(img);
                        TextBlock txt = new TextBlock();
                        txt.Text = "Authorised";
                        txt.Foreground = Brushes.White;
                        txt.Background = Brushes.Transparent;
                        txt.FontSize = 14;
                        myStackPanel.Children.Add(txt);
                        button_auth.Content = myStackPanel;
                        profileXml = vkApiClass.getProfile(userId);
                        
                        getAudioFunc();
                        try
                        {
                            // pictureBox1.Load(GetDataFromXmlNode(profileXml.SelectSingleNode("response/user/photo_50")));
                        }
                        catch (System.Net.WebException)
                        {

                        }
                    }
                    else
                    {
                        MessageBox.Show("Для работы необходимо подключение к сети", "Авторизация", MessageBoxButton.OK, MessageBoxImage.Information);
                        isAuthorised = false;
                    }
                }
                streamReader.Close();
            }
        }
       
        /// <summary>
        /// Кнопка авторизации
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_auth_Click(object sender, RoutedEventArgs e)
        {
            if (!isAuthorised)
            {
                auth = new Auth(appId,true);
            }
            else
            {
                auth = new Auth(appId, false);
            }
                auth.Owner = this;
                auth.Closed += auth_Closed;
                auth.Show();                     
        }

        /// <summary>
        /// Закрыли окно авторизации
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void auth_Closed(object sender, EventArgs e)
        {
            
            if (isAuthorised)
            {
                vkApiClass = new VKAPI(appId, accessToken);
                var bc = new BrushConverter();
                button_auth.Background = (Brush)bc.ConvertFrom("#FF36638E");
                button_auth.Content = "Logout";                
                profileXml = vkApiClass.getProfile(userId);                
                getAudioFunc(N);
            }
            else
            {
                if(currentAudioList!=null)
                currentAudioList.Clear();
                button_auth.Background = Brushes.Tomato;
               // button_auth.Content = "Login";                
            }
        }

        /// <summary>
        /// Получить и вывести в таблицу аудиозаписи пользователя
        /// </summary>
        /// <param name="count"></param>
        /// <param name="offset"></param>
        private void getAudioFunc(int count = 20, int offset = 0)
        {
            XmlNodeList nodeList;
            //item.Clear();
            List<audioItems> currentAudioList1 = new List<audioItems>(); //Временная переменная, чтобы обновлять содержимое таблицы
            //currentAudioList1 = currentAudioList;
            if (isAuthorised)
            {
                XmlDocument audioXml;
                switch (currentCatalog)
                {
                    case 0: audioXml = vkApiClass.getAudio(userId, count, offset); break;
                    case 1: audioXml = vkApiClass.audioGetPopular(userId, count, offset); break;
                    default: audioXml = vkApiClass.getAudio(userId, count, offset); break;
                }
               
                nodeList = audioXml.SelectNodes("response/audio");
                for(int i=0; i<nodeList.Count; i++){
                    audioItems audioIte = new audioItems();
                    audioIte.title = ProcessSpecialSymbols(GetDataFromXmlNode(nodeList.Item(i).SelectSingleNode("title")));
                    audioIte.artist = ProcessSpecialSymbols(GetDataFromXmlNode(nodeList.Item(i).SelectSingleNode("artist")));
                    String t_dur = GetDataFromXmlNode(nodeList.Item(i).SelectSingleNode("duration"));
                    int t_min = Convert.ToInt32(t_dur) / 60;
                    int t_sec = Convert.ToInt32(t_dur) - t_min*60;
                    audioIte.duration = Convert.ToString(t_min) + ":" + Convert.ToString(t_sec).PadLeft(2,'0');
                   
                    audioIte.source = GetDataFromXmlNode(nodeList.Item(i).SelectSingleNode("url"));
                    
                    currentAudioList.Add(audioIte);
                }
                double curTableHeight = dataGridView1.ActualHeight;//Сохраняем текущую высоту таблицы, чтоб после обновления она не растягивалась
                dataGridView1.ItemsSource = null;                
                dataGridView1.ItemsSource = currentAudioList;
                if(curTableHeight!=0.0)
                dataGridView1.Height = curTableHeight;              
            }
            else MessageBox.Show("Сначала нужно авторизоваться", "Ошибка", MessageBoxButton.OK,MessageBoxImage.Exclamation);
        }

        /// <summary>
        /// Исправляет специальные символы из xml на их обычный эквивалент
        /// </summary>
        /// <param name="sourceString">Исходная строка</param>
        /// <returns></returns>
        private string ProcessSpecialSymbols(string sourceString)
        {           
            sourceString = sourceString.Replace("&lt;", "<");
            sourceString = sourceString.Replace("&gt;", ">");
            sourceString = sourceString.Replace("&amp;", "&");
            sourceString = sourceString.Replace("&quot;", "\"");
            return sourceString;

        }

        //Функция позволяет избежать ошибок, если запрашиваемое поле пусто. Это для XML
        public string GetDataFromXmlNode(XmlNode input)
        {
            if (input == null || String.IsNullOrEmpty(input.InnerText))
            {
                return "нет данных";
            }
            else
            {
                return input.InnerText;
            }
        }

        private void button_find_Click(object sender, RoutedEventArgs e)
        {
            if (currentCatalog != 3)
            {
                currentCatalog = 3;
                currentAudioList.Clear();
                if (textBox_searchGlobalAudio.Text != "")
                {
                    getGlobalAudioFunc(N, 0, textBox_searchGlobalAudio.Text);
                }
            }
        }


        /// <summary>
        /// Получить и вывести в таблицу аудиозаписи из всей социальной сети
        /// </summary>
        /// <param name="count"></param>
        /// <param name="offset"></param>
        /// <param name="count">Имя искомой записи</param>
        private void getGlobalAudioFunc(int count = 20, int offset = 0, string query="")
        {
            searchUserAudio = false;
            XmlNodeList nodeList;
            //item.Clear();
            //currentAudioList = new List<audioItems>();
            if (isAuthorised)
            {
                XmlDocument audioXml;
                audioXml = vkApiClass.getAudioGlobal(userId, count, offset,query);
                nodeList = audioXml.SelectNodes("response/audio");
                for (int i = 0; i < nodeList.Count; i++)
                {
                    audioItems ite = new audioItems();
                    ite.title = GetDataFromXmlNode(nodeList.Item(i).SelectSingleNode("title"));
                    ite.artist = GetDataFromXmlNode(nodeList.Item(i).SelectSingleNode("artist"));
                    String t_dur = GetDataFromXmlNode(nodeList.Item(i).SelectSingleNode("duration"));
                    int t_min = Convert.ToInt32(t_dur) / 60;
                    int t_sec = Convert.ToInt32(t_dur) - t_min * 60;
                    ite.duration = Convert.ToString(t_min) + ":" + Convert.ToString(t_sec).PadLeft(2, '0');
                    ite.source = GetDataFromXmlNode(nodeList.Item(i).SelectSingleNode("url"));
                    currentAudioList.Add(ite);
                }
                dataGridView1.ItemsSource = null;
                dataGridView1.ItemsSource = currentAudioList;//Обновление таблицы

            }
            else MessageBox.Show("Сначала нужно авторизоваться", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        /// <summary>
        /// Кнопка Запросить следущую страницу с записями
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_moreOffset_Click(object sender, EventArgs e)
        {
            currentOffset+=N;
            if (searchUserAudio)
            {
                getAudioFunc(N, currentOffset);
            }
            else
            {
                getGlobalAudioFunc(N,currentOffset,textBox_searchGlobalAudio.Text);
            }
        }

       /// <summary>
       /// Кнопка запросить предыдущую страницу с записями
       /// </summary>
       /// <param name="sender"></param>
       /// <param name="e"></param>
        private void button_lessOffset_Click(object sender, EventArgs e)
        {
            if(currentOffset>10)
            currentOffset -= N;
            if (searchUserAudio)
            {
                getAudioFunc(N, currentOffset);
            }
            else
            {
                getGlobalAudioFunc(N, currentOffset, textBox_searchGlobalAudio.Text);
            }
        }

        string downloadAdress;
        bool downloading = false;
        private void button_downloadAudio_Click(object sender, RoutedEventArgs e)
        { 
            if (isAuthorised)
            {
            //audioToDownload.Clear();           
            System.Windows.Forms.FolderBrowserDialog fb = new System.Windows.Forms.FolderBrowserDialog();
            if (fb.ShowDialog() == System.Windows.Forms.DialogResult.OK) //Если пользователь не выберет папку, загрузка не начнется
            {
                downloadAdress = fb.SelectedPath;
                
            
            for (int i = audioToDownload.Count; i < currentAudioList.Count; i++)
            {
                if (currentAudioList[i].audioChecked)
                {
                    audioItems a = currentAudioList[i];
                    audioToDownload.Add(currentAudioList[i]);//Составляем очередь загрузки
                    currentAudioList[i].audioChecked = false;
                }
            }
            dataGridView1.ItemsSource = null;
            dataGridView1.ItemsSource = currentAudioList;

            if (audioToDownload.Count != 0&&!downloading)
            {
                progressBar_download.Visibility = System.Windows.Visibility.Visible;
                downloading = true;
                label_downloadingFileName.Content = audioToDownload[0].artist + " - " + audioToDownload[0].title;
                client.DownloadFileAsync(new Uri(audioToDownload[0].source), downloadAdress + "\\" + audioToDownload[0].artist + " - " + audioToDownload[0].title + ".mp3");
            }
            
            }
            }
            else MessageBox.Show("Сначала нужно авторизоваться", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        /// <summary>
        /// Событие происходит при завершении загрузки музыки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            audioToDownload.RemoveAt(0);
            if (audioToDownload.Count != 0)
            {
                label_downloadingFileName.Content = audioToDownload[0].artist + " - " + audioToDownload[0].title;
                client.DownloadFileAsync(new Uri(audioToDownload[0].source), downloadAdress + "\\" + audioToDownload[0].artist + " - " + audioToDownload[0].title + ".mp3");

            }
            else
            {
                progressBar_download.Value = 0;
                progressBar_download.Visibility = System.Windows.Visibility.Hidden;
                label_downloadingFileName.Content = "";
                downloading = false;
            }
        }
        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBar_download.Value = e.ProgressPercentage;
        }

        private void ButtonPlay_Click(object sender, RoutedEventArgs e)
            //Если запись на паузе, начинаем ее проигрывать, если играет, ставим на паузу. Если не на паузе и ничего не выбрано, то играем первую
        {
            if (isPaused)
            {
                
                StackPanel myStackPanel = new StackPanel();
                myStackPanel.Orientation = Orientation.Horizontal;
                Image img = new Image();
                img.Source = new BitmapImage(new Uri("Resources/Media Pause.png", UriKind.Relative));

                img.Height = 22;
                img.Width = 37;
                myStackPanel.Children.Add(img);

                button_playPause.Content = myStackPanel;

                num = dataGridView1.SelectedIndex;
                if (num == -1)
                {
                    num = 0;
                    playSelectedAudio(num);
                }
                else
                    mediaPlayer.Play();
                isPaused = false;
                
                
            }
            else
            {
                mediaPlayer.Pause();
                isPaused = true;
                StackPanel myStackPanel = new StackPanel();
                myStackPanel.Orientation = Orientation.Horizontal;
                Image img = new Image();
                img.Source = new BitmapImage(new Uri("Resources/Media-Play1.png", UriKind.Relative));
                img.Height = 22;
                img.Width = 37;
                myStackPanel.Children.Add(img);

                button_playPause.Content = myStackPanel;
            }

        }

        /// <summary>
        /// Воспроизводит выбранную аудио-запись, либо запись, смещнную от первой 
        /// </summary>
        /// <param name="i">Показывает, на сколько смещена запись от первой</param>
        private void playSelectedAudio(int i=0)
        {

            if (dataGridView1.SelectedIndex != -1 || i!=0)
            {
                dataGridView1.SelectedIndex = i;
                mediaPlayer.Source = new Uri((String)(currentAudioList[i].source + ".mp3").Replace("https", "http"));
                // текущий трек dataGridView1.SelectedIndex;
                songNameLabel.Text = currentAudioList[dataGridView1.SelectedIndex ].artist + " - " + currentAudioList[dataGridView1.SelectedIndex].title;
                String mins = currentAudioList[dataGridView1.SelectedIndex].duration.Substring(0, currentAudioList[dataGridView1.SelectedIndex].duration.IndexOf(":"));
                String secs = currentAudioList[dataGridView1.SelectedIndex].duration.Substring(currentAudioList[dataGridView1.SelectedIndex].duration.IndexOf(":")+1);
                TimeSlider.Maximum = Convert.ToInt32(mins)*60 + Convert.ToInt32(secs);
                mediaPlayer.Play();
            }
                //Если ничего не выбрано и нажали плей, то играем первую в списке и наводим фокус на нее
            else
            {
                dataGridView1.SelectedIndex = 0;
                mediaPlayer.Source = new Uri((String)(currentAudioList[dataGridView1.SelectedIndex].source + ".mp3").Replace("https", "http"));
                // текущий трек dataGridView1.SelectedIndex;
                songNameLabel.Text = currentAudioList[dataGridView1.SelectedIndex].artist + " - " + currentAudioList[dataGridView1.SelectedIndex].title;
                String mins = currentAudioList[dataGridView1.SelectedIndex].duration.Substring(0, currentAudioList[dataGridView1.SelectedIndex].duration.IndexOf(":"));
                String secs = currentAudioList[dataGridView1.SelectedIndex].duration.Substring(currentAudioList[dataGridView1.SelectedIndex].duration.IndexOf(":") + 1);
                TimeSlider.Maximum = Convert.ToInt32(mins) * 60 + Convert.ToInt32(secs);
                mediaPlayer.Play();
            }
        }
       
        private void mediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            num++;
            if (num < dataGridView1.Items.Count)
                playSelectedAudio(num);
            else
            {
                currentOffset += N;
                if (searchUserAudio)
                {
                    getAudioFunc(N, currentOffset);
                }
                else
                {
                    getGlobalAudioFunc(N, currentOffset, textBox_searchGlobalAudio.Text);
                }
                playSelectedAudio(num);
            }

        }

        private void mediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            TotalTime = mediaPlayer.NaturalDuration.TimeSpan;
            timer.Start();
            

        }

        /// <summary>
        /// Событие тика таймера
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void timer_Tick(object sender, EventArgs e)
        {          
                    TimeSlider.Value = mediaPlayer.Position.TotalSeconds;
                    TimeLabel.Content = TimeSpan.FromSeconds(mediaPlayer.Position.TotalSeconds).ToString("mm':'ss");
        }

        /// <summary>
        /// Перетащили слайдер
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimeSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isPaused)
            {
                StackPanel myStackPanel = new StackPanel();
                myStackPanel.Orientation = Orientation.Horizontal;
                Image img = new Image();
                img.Source = new BitmapImage(new Uri("Resources/Media Pause.png", UriKind.Relative));

                img.Height = 22;
                img.Width = 37;
                myStackPanel.Children.Add(img);

                button_playPause.Content = myStackPanel;
                isPaused = false;
            }
            mediaPlayer.Pause();
            mediaPlayer.Position = TimeSpan.FromSeconds(TimeSlider.Value);
            mediaPlayer.Play();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mediaPlayer.Volume = Volume_Slider.Value / 100;
            //VolumeLabel.Content = mediaPlayer.Volume.ToString();
        }

        private void Button_Reccomend_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Popular_Click(object sender, RoutedEventArgs e)
        {
            if (currentCatalog != 1)
            {
                currentCatalog = 1;
                currentAudioList.Clear();
                currentOffset = 0;
                searchUserAudio = true;
                textBox_searchGlobalAudio.Text = "";
                getAudioFunc();
            }
        }

        private void Button_MyAudio_Click(object sender, RoutedEventArgs e)
        {
            if (currentCatalog != 0)
            {
                currentCatalog = 0;
                currentAudioList.Clear();
                currentOffset = 0;
                searchUserAudio = true;
                textBox_searchGlobalAudio.Text = "";
                getAudioFunc();
            }
        }

        private void Button_NextTrack_Click(object sender, RoutedEventArgs e)
        {
            if (isPaused)
            {

                StackPanel myStackPanel = new StackPanel();
                myStackPanel.Orientation = Orientation.Horizontal;
                Image img = new Image();
                img.Source = new BitmapImage(new Uri("Resources/Media Pause.png", UriKind.Relative));

                img.Height = 22;
                img.Width = 37;
                myStackPanel.Children.Add(img);

                button_playPause.Content = myStackPanel;
                
                num++;
                playSelectedAudio(num);
                isPaused = false;


            }
            else
            {
                num++;

                playSelectedAudio(num);
            }
           
        }

        private void Button_PreviousTrack_Click(object sender, RoutedEventArgs e)
        {
            if (isPaused)
            {

                StackPanel myStackPanel = new StackPanel();
                myStackPanel.Orientation = Orientation.Horizontal;
                Image img = new Image();
                img.Source = new BitmapImage(new Uri("Resources/Media Pause.png", UriKind.Relative));

                img.Height = 22;
                img.Width = 37;
                myStackPanel.Children.Add(img);

                button_playPause.Content = myStackPanel;

                if (num >= 1)
                {
                    num--;
                }
                playSelectedAudio(num);
                isPaused = false;


            }
            else
            {
                if (num >= 1)
                {
                    num--;
                }

                playSelectedAudio(num);
            }
           
        }

        private void ButtonTablePlay_Click(object sender, RoutedEventArgs e)
        {
            if (isPaused)
            {

                StackPanel myStackPanel = new StackPanel();
                myStackPanel.Orientation = Orientation.Horizontal;
                Image img = new Image();
                img.Source = new BitmapImage(new Uri("Resources/Media Pause.png", UriKind.Relative));

                img.Height = 22;
                img.Width = 37;
                myStackPanel.Children.Add(img);

                button_playPause.Content = myStackPanel;

                num = dataGridView1.SelectedIndex;
               
                    playSelectedAudio(num);
                isPaused = false;


            }
            else
            {
                num = dataGridView1.SelectedIndex;

                playSelectedAudio(num);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            vkApiClass.wallPost(userId, userId, "Тест VK api wall post");
            XmlDocument test = vkApiClass.getWallUploadServer("");
            XmlNodeList xmlnode = test.SelectNodes("response/upload_url");
            string UploadServer = GetDataFromXmlNode(xmlnode.Item(0));
            WebRequest request = WebRequest.Create(UploadServer);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            Stream dataStream = request.GetRequestStream();
            byte[] myFile = File.ReadAllBytes(@"C:\Users\Иван\Pictures\Служебные\testf.png");
            dataStream.Write(myFile,0,myFile.Length);
            dataStream.Close();
            WebResponse response = request.GetResponse();
            Stream data = response.GetResponseStream();
            int x;
        }

        private void textBox_searchGlobalAudio_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                button_find_Click(sender, null);
            }
        }

  
    }
}
