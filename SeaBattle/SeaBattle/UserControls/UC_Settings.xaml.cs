﻿using System.Windows;
using System.Windows.Controls;

namespace SeaBattle.UserControls
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class UC_Settings : UserControl
    {
        public UC_Settings()
        {
            InitializeComponent();
            if(Settings.Config.Width < 800) {
                WidthBox.Text = "800";
            }
            else {
                WidthBox.Text = Settings.Config.Width.ToString();
            }

            if(Settings.Config.Height < 450) {
                HeightBox.Text = "450";
            }
            else {
                HeightBox.Text = Settings.Config.Height.ToString();
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.Config = new Config();
            Settings.WriteInFile();
            Settings.Init();
            WidthBox.Text = Settings.Config.Width.ToString();
            HeightBox.Text = Settings.Config.Height.ToString();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Settings.Config.Width = int.Parse(WidthBox.Text);
                Settings.Config.Height = int.Parse(HeightBox.Text);
            }
            catch
            {
                MessageBox.Show("Incorrect window size!");
            }
            Settings.WriteInFile();
            Settings.Init();
            ChangeUserControler.CloseUserControler(this);

        }

        private void ExitButton_Click(object sender, RoutedEventArgs e) {
            ChangeUserControler.CloseUserControler(this);
        }
    }
}
