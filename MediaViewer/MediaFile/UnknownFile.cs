﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaViewer.MetaData;

namespace MediaViewer.MediaFile
{
    class UnknownFile : MediaFileBase
    {

        public UnknownFile(String location, Stream data)
            : base(location, null, data, MetaDataMode.AUTO)
        {

        }

        public override List<MetaDataThumb> generateThumbnails()
        {

            return (new List<MetaDataThumb>());
        }

        public MediaType MediaFormat
        {
            override get
            {

                return (MediaType.UNKNOWN);
            }
        }

    }
}
