﻿using MediaViewer.Model.Utils;
// Note that a custom SSDL to DDL script is used to generate timestamp columns for concurrency checks, see:
// http://msdn.microsoft.com/en-us/library/vstudio/dd560887%28v=vs.100%29.aspx
// http://www.undisciplinedbytes.com/2012/03/creating-a-timestamp-column-with-entity-framework/
// the script location is: 
// C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\Extensions\Microsoft\Entity Framework Tools\DBGen\SSDLToSQL10_CustomTimestamp.tt
using Microsoft.Practices.Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaViewer.MediaDatabase
{
    [Serializable]
    partial class BaseMetadata : BindableBase
    {
        
        protected BaseMetadata(String location, Stream data)
        {           
            Location = location;
            Data = data;
            MimeType = MediaFormatConvert.fileNameToMimeType(location);            
            Tags = new HashSet<Tag>();
            Thumbnails = new HashSet<Thumbnail>();
           
            isImported = false;
            isModified = false;
            metadataReadError = null;            
        }

        Stream data;

        public Stream Data
        {
            set { data = value; }
            get { return data; }           
        }

        Exception metadataReadError;

        public Exception MetadataReadError
        {
            get { return metadataReadError; }
            set { metadataReadError = value; }
        }

        bool isImported;

        [NotMapped]
        public bool IsImported
        {
            get { return isImported; }
            set {

                SetProperty(ref isImported, value);              
            }
        }

        bool isModified;

        [NotMapped]
        public bool IsModified
        {
            get { return isModified; }
            set
            {
                SetProperty(ref isModified, value);
            }
        }

        public abstract String DefaultFormatCaption
        {
            get;            
        }

        public virtual void clear()
        {

            Author = null;
            Copyright = null;
            CreationDate = null;
            Description = null;

            Latitude = null;
            Longitude = null;

            MetadataDate = null;
            MetadataModifiedDate = null;

            Rating = null;

            Software = null;
            Tags = new HashSet<Tag>();
            Thumbnails = new HashSet<Thumbnail>();           
            Title = null;
   
        }

        public virtual void close()
        {
            if (data != null)
            {
                data.Close();
                data = null;
            }
        }
      
    }
}
