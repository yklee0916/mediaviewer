﻿using MediaViewer.Model.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaViewer.MediaDatabase
{
    [Serializable]
    partial class ImageMetadata
    {
        public ImageMetadata()
        {

        }

        public ImageMetadata(String location, Stream data) : base(location, data)  
        {
            
        }

        public override string DefaultFormatCaption
        {
            get
            {
                if (MetadataReadError != null)
                {
                    return MetadataReadError.Message;
                }

                StringBuilder sb = new StringBuilder();

                sb.AppendLine(Path.GetFileName(Location));
                sb.AppendLine();

                sb.AppendLine("Mime type:");
                sb.Append(MimeType);
                sb.AppendLine();
                sb.AppendLine();

                sb.AppendLine("Resolution:");
                sb.Append(Width);
                sb.Append("x");
                sb.Append(Height);
                sb.AppendLine();
                sb.AppendLine();

                sb.AppendLine("Size:");
                sb.Append(MiscUtils.formatSizeBytes(SizeBytes));
               
                return (sb.ToString());
            }
        }
    }
}
