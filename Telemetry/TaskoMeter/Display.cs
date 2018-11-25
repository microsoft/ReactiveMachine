// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace ReactiveMachine.Tools.Taskometer
{
    internal class Display
    {
        // back pointers
        internal Stats owner;
        internal TaskGroup meter;

        internal Display refdisplay;

        internal class Box
        {
            public long start;
            public long end;
            public int tid;
            public long vpos;
            public Brush fillbrush;
            public Pen borderpen;
            public Box(long s, long e, int t, Brush fb, Pen bp) { start = s; end = e; tid = t; fillbrush = fb; borderpen = bp;  }
        }

      
        // calculated fields
        internal List<Box> allboxes = new List<Box>();
        internal List<Box> visibleboxes = new List<Box>();
        internal int divisions = 1;
        internal double calculatedduration = 0.0;

        // displayed columns
        internal string task = "";
        internal string number = "";
        internal string avg = "";
        internal string sum = "";
        internal string start = "";
        internal string end = "";
        internal string speedup = "";

        internal object[] Format()
        {
            return new object[] { task, number, avg, sum, start, end, this };
        }
        public override string ToString()
        {
            return ""; // we are using this with text box grid view cell, where no text should be displ.
        }

        public Display(Stats owner, TaskGroup meter)
        {
            this.owner = owner;
            this.meter = meter;
            task = meter.Name;

        }

        public void ClearData()
        {
            meter.Intervals.Clear();
        }

        public static Pen[] Pens = new Pen[] 
        {
            new Pen(Color.Black),
            new Pen(Color.Red),
            new Pen(Color.Green),
            new Pen(Color.Blue),
            new Pen(Color.Brown),
            new Pen(Color.Purple),
            new Pen(Color.DarkGray),
            new Pen(Color.Orange),
        };

        public void RefreshData()
        {
            allboxes.Clear();
            Dictionary<int, Brush> brushes = new Dictionary<int, Brush>();
            foreach (TaskGroup.Interval iv in meter.Intervals)
            {
                Brush fillbrush;
                if (!brushes.TryGetValue(iv.reqno, out fillbrush))
                {
                    if (iv.reqno == 0)
                        fillbrush = Brushes.White;
                    else
                        fillbrush = new SolidBrush(Color.FromKnownColor((KnownColor)  ((int) (Math.Abs(iv.reqno % 139) + 28))));
                    brushes[iv.reqno] = fillbrush;
                }
                var borderpen = Pens[iv.filecount % 8];
                allboxes.Add(new Box(iv.Start-TaskGroup.offset, iv.End - TaskGroup.offset, iv.Tid, fillbrush, borderpen));
            }
            Recalculate();
        }


        private string FormatTime(double val)
        {
            return val.ToString();
        }

        public void Recalculate()
        {
            visibleboxes.Clear();
            List<long> filledto = new List<long>();


            SortedList<int, int> tidmap = new SortedList<int, int>();

            foreach (Box box in allboxes)
                if (box.end > owner.leftticks && box.start < owner.leftticks + owner.widthticks)
                {
                    visibleboxes.Add(box);
                    int vpos;
                    if (!tidmap.TryGetValue(box.tid, out vpos))
                    {
                        vpos = tidmap.Count;
                        tidmap.Add(box.tid, vpos);
                    }
                    box.vpos = vpos;
                }

            divisions = tidmap.Count;

            number = visibleboxes.Count.ToString();
            calculatedduration = 0.0;
            avg = "";
            sum = "";
            start = "";
            end = "";

            if (visibleboxes.Count > 0)
            {
                if (visibleboxes.Count == 1)
                {
                    start = FormatTime(owner.scale * visibleboxes[0].start);
                    end = FormatTime(owner.scale * visibleboxes[0].end);
                    calculatedduration = owner.scale * (visibleboxes[0].end - visibleboxes[0].start);
                    avg = FormatTime(calculatedduration);
                }
                else
                {
                    long ssum = 0;
                    foreach (Box iv in visibleboxes)
                        ssum += iv.end - iv.start;
                    calculatedduration = (owner.scale * ssum) / visibleboxes.Count;
                    sum = FormatTime(ssum * owner.scale);
                    avg = FormatTime(calculatedduration);
                }
            }
        }

        public void DrawAllBoxes(Stats.DataGridViewCustomCell cell)
        {
            foreach (Box iv in visibleboxes)
            {
                double top = iv.vpos * (1.0 / divisions);
                double bottom = (iv.vpos + 1) * (1.0 / divisions);
                cell.DrawBox(iv.start, iv.end, top, bottom, iv.borderpen, iv.fillbrush);
            }
        }

    }
}
