﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FF7OptimalHP.Objects
{
    public class Path
    {
        public LinkedList<Node> PathList;

        public double Resets;

        public ushort Chances;

        public Path()
        {
            PathList = new LinkedList<Node>();
        }
    }
}
