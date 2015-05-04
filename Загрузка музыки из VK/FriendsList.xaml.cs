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
using System.Xml;

namespace Загрузка_музыки_из_VK
{
    /// <summary>
    /// Логика взаимодействия для FriendsList.xaml
    /// </summary>
    public partial class FriendsList : Window
    {
        static public int appId = 4201412;
        public class FriendItem
        {
            public int id { get; set; }
            public string first_name { get; set; }
            public string last_name { get; set; }
            public string photo50Source { get; set; }
            
        }

        //Это свойство используется для возврата значения, а именно ID выбранного друга
        public int ReturnValueID { get; set; } 

        //Глобальные переменные 
        bool loading = false;
        VKAPI vkapi;
        MainWindow main;
        int currentOffset;

        List<FriendItem> listFriendItems;
        public FriendsList(Window owner)
        {
            
            main = owner as MainWindow; //Костыли-костылики
            InitializeComponent();
            listFriendItems = new List<FriendItem>();
            vkapi = new VKAPI(appId, main.accessToken);
            GetFriendsToList();
            
        }

        private void GetFriendsToList(int count = 10, int offset = 0)
        {
            loading = true;

            XmlDocument friendsXML = vkapi.getFriends(main.userId, count, offset, "hints", "photo_50, can_see_audio");
            XmlNodeList nodeList = friendsXML.SelectNodes("response/user");
           

            for (int i = 0; i < nodeList.Count; i++)
            {
                if (MainWindow.GetDataFromXmlNode(nodeList.Item(i).SelectSingleNode("can_see_audio")) == "1")
                {
                FriendItem friend = new FriendItem();
                friend.first_name = MainWindow.ProcessSpecialSymbols(MainWindow.GetDataFromXmlNode(nodeList.Item(i).SelectSingleNode("first_name")));
                friend.last_name = MainWindow.ProcessSpecialSymbols(MainWindow.GetDataFromXmlNode(nodeList.Item(i).SelectSingleNode("last_name")));
                int tempINT;
                int.TryParse(MainWindow.ProcessSpecialSymbols(MainWindow.GetDataFromXmlNode(nodeList.Item(i).SelectSingleNode("uid"))), out tempINT);
                friend.id = tempINT;

                friend.photo50Source = MainWindow.ProcessSpecialSymbols(MainWindow.GetDataFromXmlNode(nodeList.Item(i).SelectSingleNode("photo_50")));

               listFriendItems.Add(friend);
                }
            }

            DataGridFriends.ItemsSource = null;
            DataGridFriends.ItemsSource = listFriendItems;
            loading = false;
        }

        private void DataGridFriends_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            
            if (DataGridFriends.Items.Count > 0 && !loading)
            {
                var border = VisualTreeHelper.GetChild(DataGridFriends, 0) as Decorator;
                if (border != null)
                {
                    var scroll = border.Child as ScrollViewer;
                    if (scroll.ScrollableHeight == e.VerticalOffset)
                    {
                      
                            this.Cursor = Cursors.Wait;

                        currentOffset+=10;
                        GetFriendsToList(10, currentOffset);

                        
                            this.Cursor = Cursors.Arrow;
                            System.Threading.Thread.Sleep(5);
                        }
                    }
                }
            }

    
        private void DataGridFriends_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            
        }

        private void DataGridFriends_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var tmp = DataGridFriends.SelectedItem as FriendItem;
            this.ReturnValueID = tmp.id;
            System.Threading.Thread.Sleep(50);
            this.DialogResult = true;
            this.Close();
        }
        }
    }