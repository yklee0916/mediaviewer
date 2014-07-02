﻿using MediaViewer.MediaDatabase.DataTransferObjects;
using MvvmFoundation.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MediaViewer
{
    public partial class App : Application
    {

        public static string[] Args;
        public static SplashScreen SplashScreen;

        public App()
        {
            Startup += Application_Startup;
            Exit += Application_Exit;        

            InitializeComponent();

            
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Args = e.Args;
            MediaViewer.Settings.AppSettings.load();
            SplashScreen = new SplashScreen("Resources/Images/splash.png");

            #if !DEBUG
              SplashScreen.Show(false, true);
            #endif
       
            AutoMapperSetup.Run();
        }

        private void Application_Exit(object sender, EventArgs e)
        {

          
        }

        public static String getAppInfoString()
        {
            return ("MediaViewer v" + Assembly.GetEntryAssembly().GetName().Version.ToString());
        }

    }
}
