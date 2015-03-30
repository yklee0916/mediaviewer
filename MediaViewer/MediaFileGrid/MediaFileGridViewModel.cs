﻿using MediaViewer.MediaFileBrowser;
using MediaViewer.Model.Global.Events;
using MediaViewer.Model.Media.Base;
using MediaViewer.Model.Media.File;
using MediaViewer.Model.Media.State;
using MediaViewer.Model.Media.State.CollectionView;
using MediaViewer.Model.Mvvm;
using MediaViewer.Model.Utils;
using Microsoft.Practices.Prism.Mvvm;
using Microsoft.Practices.Prism.PubSubEvents;
using Microsoft.Practices.Prism.Regions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaViewer.MediaFileGrid
{
    public class MediaFileGridViewModel : BindableBase, IMediaFileBrowserContentViewModel
    {
        IEventAggregator EventAggregator { get; set; }

        public MediaFileGridViewModel(MediaFileState mediaState, IEventAggregator eventAggregator)             
        {
            EventAggregator = eventAggregator;
            NrColumns = 4;

            MediaStateCollectionView = new MediaFileStateCollectionView(mediaState);
            MediaStateCollectionView.SelectionChanged += MediaStateCollectionView_SelectionChanged;
            

            ViewCommand = new Command<SelectableMediaItem>((selectableItem) =>
            {
                MediaItem item = selectableItem.Item;

                if (MediaFormatConvert.isImageFile(item.Location))
                {
                    Shell.ShellViewModel.navigateToImageView(item.Location);
                }
                else if (MediaFormatConvert.isVideoFile(item.Location))
                {
                    Shell.ShellViewModel.navigateToVideoView(item.Location);
                }
            });

            SelectAllCommand = new Command(() =>
            {
                MediaStateCollectionView.selectAll();
            });

            DeselectAllCommand = new Command(() =>
            {
                MediaStateCollectionView.deselectAll();
            });

            BrowseLocationCommand = new Command<SelectableMediaItem>((selectableItem) =>
                {
                    MediaItem item = selectableItem.Item;

                    String location = FileUtils.getPathWithoutFileName(item.Location);

                    EventAggregator.GetEvent<MediaBrowserPathChangedEvent>().Publish(location);
                });

            OpenLocationCommand = new Command<SelectableMediaItem>((selectableItem) =>
                {
                    MediaItem item = selectableItem.Item;

                    String location = FileUtils.getPathWithoutFileName(item.Location);

                    Process.Start(location);
                });

            
        }

        protected void MediaStateCollectionView_SelectionChanged(object sender, EventArgs e)
        {
            List<MediaItem> selectedItems = MediaStateCollectionView.getSelectedItems();

            EventAggregator.GetEvent<MediaViewer.Model.Global.Events.MediaBatchSelectionEvent>().Publish(selectedItems);
        }

        MediaFileStateCollectionView mediaStateCollectionView;

        public MediaFileStateCollectionView MediaStateCollectionView
        {
            get { return mediaStateCollectionView; }
            protected set
            {
                SetProperty(ref mediaStateCollectionView, value);
            }
        }
        
        int nrColumns;

        public int NrColumns
        {
            get { return nrColumns; }
            set
            {               
                SetProperty(ref nrColumns, value);
            }
        }

        public Command<SelectableMediaItem> ViewCommand { get; set; }
        public Command SelectAllCommand { get; set; }
        public Command DeselectAllCommand { get; set; }
        public Command<SelectableMediaItem> BrowseLocationCommand { get; set; }
        public Command<SelectableMediaItem> OpenLocationCommand { get; set; }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            EventAggregator.GetEvent<MediaBatchSelectionEvent>().Publish(MediaStateCollectionView.getSelectedItems());
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
           
        }
    }                                     
}