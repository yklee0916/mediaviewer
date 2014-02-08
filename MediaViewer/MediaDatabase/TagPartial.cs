﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaViewer.MediaDatabase
{
    partial class Tag : IComparable<Tag>
    {

        public override string ToString()
        {
            return Name;
        }

     
        public int CompareTo(Tag other)
        {
            if (other == null)
            {
                throw new ArgumentException();
            }

            return (other.Name.CompareTo(Name));
        }
    }
}