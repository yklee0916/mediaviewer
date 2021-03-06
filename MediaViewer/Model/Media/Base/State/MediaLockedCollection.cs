﻿using MediaViewer.Model.Collections;
using MediaViewer.Model.Media.Base.Item;
using MediaViewer.Model.Media.File;
using Microsoft.Practices.Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaViewer.Model.Media.Base.State
{

    /// <summary>
    /// Concurrent Collection that supports multiple readers and a single writer
    /// Also makes sure the items in the collection are unique
    /// </summary>
    /// <typeparam name="MediaFileItem"></typeparam>
    public class MediaLockedCollection : LockedObservableCollection<MediaItem>
    {
        public event EventHandler IsLoadingChanged;

        MediaItemMetadataLoader itemLoader;
                              
        public MediaLockedCollection(bool autoLoadItems = false)
        {                             
            if (autoLoadItems == true)
            {
                itemLoader = new MediaItemMetadataLoader();
                

                itemLoader.ItemFinishedLoading += itemLoader_ItemFinishedLoading;
            }

            this.autoLoadItems = autoLoadItems;
            IsLoading = false;
            
        }

        void itemLoader_ItemFinishedLoading(object sender, EventArgs e)
        {
            rwLock.EnterWriteLock();
            try
            {
                // check to make sure the loaded item is actually in the current state
                if(Contains(sender as MediaItem)) {
                   
                    NrLoadedItems++;
                    
                    if (NrLoadedItems == Count)
                    {
                        IsLoading = false;
                    }
                    else
                    {
                        IsLoading = true;
                    }
                }                             
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
           
        }

        bool isLoading;

        public bool IsLoading
        {
            get { return isLoading; }
            set {
                if (isLoading == value) return;

                isLoading = value;
                OnPropertyChanged(new PropertyChangedEventArgs("IsLoading"));

                if (IsLoadingChanged != null)
                {
                    IsLoadingChanged(this, EventArgs.Empty);
                }
            }
        }

        int nrLoadedItems;

        public int NrLoadedItems
        {
            get { return nrLoadedItems; }
            protected set {
                
                if (nrLoadedItems == value) return;

                nrLoadedItems = value;                
                OnPropertyChanged(new PropertyChangedEventArgs("NrLoadedItems"));           
            }
        }
        
        bool autoLoadItems;

        public bool AutoLoadItems
        {
            get { return autoLoadItems; }           
        }
          

        /// <summary>
        /// Remove all elements from the collection
        /// </summary>
        override protected void ClearItems()
        {        
            base.ClearItems();

            if (AutoLoadItems)
            {
                NrLoadedItems = 0;
                itemLoader.clear();
            }                
        }


        public bool RenameRange(IEnumerable<MediaItem> oldItems, IEnumerable<String> newLocations)
        {                      
            bool success = true;

            int nrOldItems = oldItems.Count();
            int nrNewItems = newLocations.Count();

            Debug.Assert(nrOldItems == nrNewItems);
              
            for (int i = 0; i < nrOldItems; i++)
            {                   
                MediaItem oldItem = this.FirstOrDefault((elem) => {
                    return(oldItems.ElementAt(i).Location.Equals(elem.Location));
                });

                if (oldItem == null)
                {
                    success = false;
                    continue;
                }
                oldItem.EnterWriteLock();
                try
                {
                    oldItem.Location = newLocations.ElementAt(i);
                }
                finally
                {
                    oldItem.ExitWriteLock();
                }
            }
            
            return (success);
           
        }

        public void Reload(MediaItem item)
        {
            if (item.ItemState != MediaItemState.LOADED)
            {
                // cannot reload item that hasn't been loaded
                return;
            }

            item.EnterWriteLock();                                         
            item.ItemState = MediaItemState.RELOAD;           
            item.ExitWriteLock();
                        
            NrLoadedItems--;            
            itemLoader.add(item);
        }
        

        override protected void afterItemAdded(MediaItem item)
        {
            base.afterItemAdded(item);

            if (AutoLoadItems)
            {
                if (item.ItemState == MediaItemState.LOADED)
                {
                    NrLoadedItems++;
                }
                else
                {
                    itemLoader.add(item);
                }
            }
        }

        override protected void beforeItemRemoved(MediaItem item)
        {
            base.beforeItemRemoved(item);

            if (AutoLoadItems)
            {
                if (item.ItemState != MediaItemState.LOADING)
                {
                    NrLoadedItems--;
                }
                itemLoader.remove(item);
            }
        }
                               
    }
}

