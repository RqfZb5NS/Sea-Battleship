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

namespace Sea_Battleship
{
    /// <summary>
    /// Логика взаимодействия для MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
            WindowConfig.MainPage = this;
        }
        private void audioChanged(object sender, RoutedEventArgs e)//
        {
            WindowConfig.AudioChanged((Image)sender);
        }

        private void Loading_Click(object sender, RoutedEventArgs e)//
        {
            NavigationService.Navigate(new Uri("LoadingPage.xaml", UriKind.Relative));
        }

        private void NewGame_MouseLeftButtonDown(object sender, RoutedEventArgs e)//
        {
            WindowConfig.GameState = WindowConfig.State.Offline;
            ConfigOfflineWindow window = new ConfigOfflineWindow();
            if (window.ShowDialog() == true)
            {
                if (WindowConfig.game == null)
                {
                    PlacingPage placingPage = new PlacingPage(window.ArrangementClient, window.Game);
                    NavigationService.Navigate(placingPage, UriKind.Relative);
                }
                else
                {
                    PlayPage playPage = new PlayPage(WindowConfig.game);
                    NavigationService.Navigate(playPage, UriKind.Relative);
                }
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)//
        {
            try
            {
                System.Diagnostics.Process.Start("C:/Users/Пользователь/Desktop/Наиболее морской бой/Sea-Battleship/Sea Battleship/Resources/Spravka.html");
            }
            catch
            {
                MessageBox.Show("Справка отсутствует");
            }
        }

        private void CreateLobby_Click(object sender, RoutedEventArgs e)
        {
            WindowConfig.GameState = WindowConfig.State.Online;
            ConfigOnlineHostWindow window = new ConfigOnlineHostWindow();
            window.ShowDialog();
        }



        private void ConnetToLobbyItem_Click(object sender, RoutedEventArgs e)
        {
            WindowConfig.GameState = WindowConfig.State.Online;
            ConfigOnlineNotHostWindow window = new ConfigOnlineNotHostWindow();
            window.ShowDialog();
        }

        private void AboutItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Игру создали студенты группы 6403:\nКотов Алексей\nОнисич Степан\nШибаева Александра", "Об авторах", MessageBoxButton.OK);
        }

    }
}