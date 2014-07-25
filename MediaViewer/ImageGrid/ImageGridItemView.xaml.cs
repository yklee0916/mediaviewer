﻿using MediaViewer.MediaFileModel.Watcher;
using MediaViewer.Torrent;
using MediaViewer.VideoPreviewImage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace MediaViewer.ImageGrid
{
    /// <summary>
    /// Interaction logic for ImageGridItemView.xaml
    /// </summary>
    public partial class ImageGridItemView : UserControl
    {
    

        public ImageGridItemView()
        {
            InitializeComponent();
           
        }

        public MediaFileItem MediaFileItem
        {
            get { return (MediaFileItem)GetValue(MediaFileItemProperty); }
            set { SetValue(MediaFileItemProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MediaFileItem.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MediaFileItemProperty =
            DependencyProperty.Register("MediaFileItem", typeof(MediaFileItem), typeof(ImageGridItemView), new PropertyMetadata(null));

        private void viewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MediaFileItem item = (MediaFileItem)DataContext;

            GlobalMessenger.Instance.NotifyColleagues("MainWindowViewModel.ViewMediaCommand", item.Location);

        }



        public SortMode ExtraInfoType
        {
            get { return (SortMode)GetValue(ExtraInfoTypeProperty); }
            set { SetValue(ExtraInfoTypeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ExtraInfoType.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ExtraInfoTypeProperty =
            DependencyProperty.Register("ExtraInfoType", typeof(SortMode), typeof(ImageGridItemView), new PropertyMetadata(SortMode.Name, imageGridItemView_ExtraInfoTypeChangedCallback));

        private static void imageGridItemView_ExtraInfoTypeChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ImageGridItemView view = d as ImageGridItemView;
            view.extraInfo.InfoType = (SortMode)e.NewValue;
        }
        
        private void selectAllMenuItem_Click(object sender, RoutedEventArgs e)
        {

            ImageGridViewModel vm = (ImageGridViewModel)(this.Tag as ItemsControl).DataContext;
            vm.selectAll();
        }

        private void deselectAllMenuItem_Click(object sender, RoutedEventArgs e)
        {

            ImageGridViewModel vm = (ImageGridViewModel)(this.Tag as ItemsControl).DataContext;
            vm.deselectAll();

        }

        private void browseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MediaFileItem item = (MediaFileItem)DataContext;

            String location = Utils.FileUtils.getPathWithoutFileName(item.Location);

            GlobalMessenger.Instance.NotifyColleagues("MediaFileBrowser_SetPath", location);
        }

        private void openInExplorerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MediaFileItem item = (MediaFileItem)DataContext;

            String location = Utils.FileUtils.getPathWithoutFileName(item.Location);

            Process.Start(location);
        }

     

        private void imageGridItem_Checked(object sender, RoutedEventArgs e)
        {
            MediaFileItem item = (MediaFileItem)DataContext;

            if (item.IsSelected == true) return;

            if (Keyboard.Modifiers != ModifierKeys.Control)
            {                     
                ImageGridViewModel vm = (ImageGridViewModel)(this.Tag as ItemsControl).DataContext;
                vm.MediaState.deselectAllUIState();                
            }

            item.IsSelected = true;
         
        }

        private void imageGridItem_Unchecked(object sender, RoutedEventArgs e)
        {
            MediaFileItem item = (MediaFileItem)DataContext;        

            item.IsSelected = false;
        }
    }
}
