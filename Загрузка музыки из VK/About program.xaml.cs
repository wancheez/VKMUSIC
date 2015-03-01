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
using System.Diagnostics;

namespace Загрузка_музыки_из_VK
{
    /// <summary>
    /// Логика взаимодействия для About_program.xaml
    /// </summary>
    public partial class About_program : Window
    {
        public About_program()
        {
            InitializeComponent();
        }

        private void Hyperlink_VkSite_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://vk.com/wancheez");
        }
       
    }
}
