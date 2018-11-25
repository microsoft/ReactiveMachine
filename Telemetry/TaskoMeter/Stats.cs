using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace ReactiveMachine.Tools.Taskometer
{
    internal partial class Stats : Form, System.Collections.IComparer
    {
        private readonly string connectionString;
        
        public Stats(string connectionString, string containerName, string folderName)
        {
            this.connectionString = connectionString;

            InitializeComponent();

            DoubleBuffered = true;

            Text = "TaskoMeter";
            
            lock (TaskGroup.TaskGroups)
            {
                foreach (TaskGroup m in TaskGroup.TaskGroups.Values)
                    if (m.Intervals != null)
                    {
                       dataGridView1.Rows.Add((new Display(this, m)).Format());
                    }
            }

            //foreach (var v in Enum.GetNames(typeof (StorageAccounts.Account)))
            comboBoxAccount.Items.Add(containerName);
            comboBoxAccount.SelectedIndex = 0;

            comboBoxFolder.Items.Add(folderName);
            comboBoxFolder.SelectedIndex = 0;

            RefreshData();
        }

        public void RefreshData()
        {
            // set defaults
            this.maxticks = 1 + (long)(TaskGroup.end * 1.2); // leave some extra room on the right
            this.scale = ((double)1) / TimeSpan.TicksPerMillisecond;
            leftticks = 0;
            widthticks = maxticks;

            // add missing rows / delete superfluous rows
            {
                Dictionary<TaskGroup, List<Display>> displaymap = new Dictionary<TaskGroup, List<ReactiveMachine.Tools.Taskometer.Display>>();
                Dictionary<DataGridViewRow, bool> toberemoved = new Dictionary<DataGridViewRow, bool>();

                foreach (DataGridViewRow r in dataGridView1.Rows)
                {
                    Display display = r.Cells[display_col].Value as Display;
                    TaskGroup meter = display.meter;
                    List<ReactiveMachine.Tools.Taskometer.Display> dlist;
                    if (meter.Intervals == null)
                    {
                        toberemoved.Add(r, true);
                    }
                    else
                    {
                        if (!displaymap.TryGetValue(meter, out dlist))
                            displaymap.Add(meter, dlist = new List<ReactiveMachine.Tools.Taskometer.Display>());
                        while (dlist.Count <= 0)
                            dlist.Add(null);
                        dlist[0] = display;
                    }
                }
                foreach (DataGridViewRow r in toberemoved.Keys)
                    dataGridView1.Rows.Remove(r);

                lock (TaskGroup.TaskGroups)
                {
                    foreach (TaskGroup m in TaskGroup.TaskGroups.Values)
                    {
                        if (m.Intervals != null)
                        {

                            List<ReactiveMachine.Tools.Taskometer.Display> dlist;
                            if (!displaymap.TryGetValue(m, out dlist))
                                displaymap.Add(m, dlist = new List<ReactiveMachine.Tools.Taskometer.Display>());

                            while (dlist.Count <= 0)
                                dlist.Add(null);
                            if (dlist[0] == null)
                            {
                                Display d = new Display(this, m);
                                dataGridView1.Rows.Add(d.Format());
                                dlist[0] = d;
                            }
                        }
                    }

                    dataGridView1.Sort(this);

                }

                foreach (DataGridViewRow r in dataGridView1.Rows)
                    (r.Cells[display_col].Value as Display).RefreshData();

                UpdateScrollBar();
                UpdateTextBox();
                UpdateZoom();

                UpdateView();
            }
        }

        // display TOTAL row on top
        public int Compare(Object a, Object b)
        {
            Display aa = (a as DataGridViewRow).Cells[display_col].Value as Display;
            Display bb = (b as DataGridViewRow).Cells[display_col].Value as Display;

            // total line on top
            if (aa.task == "TOTAL")
                return -1;
            else if (bb.task == "TOTAL")
                return 1;

            // different meters by creation time
            return (aa.meter.seqnum - bb.meter.seqnum);
        }

        internal double scale;
       
        // current view
        internal long maxticks = 1;
        internal long leftticks;
        internal long widthticks;

        internal double tickmarks_msec;

        private const int task_col = 0;
        private const int display_col = 6;

        private void UpdateScrollBar()
        {
            hScrollBar1.Value = (int) (100000 * leftticks / maxticks);
            hScrollBar1.LargeChange = (int) (100000 * widthticks / maxticks);
            hScrollBar1.SmallChange = hScrollBar1.LargeChange / 30;
            hScrollBar1.Invalidate();
        }
        private void UpdateTextBox()
        {
            textBox2.Text = (widthticks * scale).ToString();
            textBox2.Invalidate();
        }
        private void UpdateZoom()
        {
            int val = 100000 - (int) (100000 * widthticks / (maxticks));
            if (val < 0)
                trackBar1.Value = 0;
            else if (val > 100000)
                trackBar1.Value = 100000;
            else
                trackBar1.Value = val;
        }
        private void UpdateView()
        {
            foreach (DataGridViewRow r in dataGridView1.Rows)
            {
                Display d = r.Cells[display_col].Value as Display;
                d.Recalculate();
                r.SetValues(d.Format());
                double height = 25;
                double inc = height;
                for (int i = 1; i < d.divisions; i++)
                {
                    height += inc;
                    inc *= 0.85;
                }
                if (height < d.divisions * 2 + 4)
                    height = d.divisions * 2 + 4; 
                r.Height = (int)height;
            }
            double pixel_per_msec =  (double) (dataGridView1.Columns[display_col].Width / (scale * widthticks));
            tickmarks_msec = 0.0001;
            while (tickmarks_msec * pixel_per_msec < 30)
                tickmarks_msec *= 10;
            dataGridView1.Invalidate();
        }

        public class DataGridViewCustomCell : DataGridViewTextBoxCell
        {

            protected override void Paint(
                       Graphics graphics,
                       Rectangle clipBounds,
                       Rectangle cellBounds,
                       int rowIndex,
                       DataGridViewElementStates cellState,
                       object value,
                       object formattedValue,
                       string errorText,
                       DataGridViewCellStyle cellStyle,
                       DataGridViewAdvancedBorderStyle advancedBorderStyle,
                       DataGridViewPaintParts paintParts)
            {
 
                // retrieve stats
                Display d = (Display)this.Value;

                // create a graphics buffer for better performance
                BufferedGraphicsContext currentContext = BufferedGraphicsManager.Current;
                BufferedGraphics myBuffer = currentContext.Allocate(d.owner.CreateGraphics(), cellBounds);
                this.graphics = myBuffer.Graphics;

                // Call the base class method to paint the default cell appearance.
                base.Paint(this.graphics, clipBounds, cellBounds, rowIndex, cellState,
                    value, formattedValue, errorText, cellStyle,
                    advancedBorderStyle, paintParts);
                
                this.cellBounds = cellBounds;
                this.leftticks = d.owner.leftticks;
                this.hscale = (1.0f * cellBounds.Width) / d.owner.widthticks;
                this.vscale = (1.0f * cellBounds.Height);
                this.scale = d.owner.scale;

                // draw boxes
                d.DrawAllBoxes(this);

                // draw ticks
                DrawAllTicks(d, d.owner.tickmarks_msec, false);

                // draw speedup
                if (d.calculatedduration != 0 && d.refdisplay != null && d.refdisplay.calculatedduration != 0)
                {
                    DrawSpeedup(d, cellBounds, d.refdisplay.calculatedduration / d.calculatedduration );
                }
                
                // render & dispose buffer
                myBuffer.Render(graphics);
                myBuffer.Dispose();

                // draw column header
                if (rowIndex == 0)
                {
                    this.graphics = graphics;
                    DrawAllTicks(d, d.owner.tickmarks_msec, true);
                }
            }

            private Graphics graphics;
            private Rectangle cellBounds;
            private double scale;
            private long leftticks;
            private double hscale;
            private double vscale;

            private void DrawAllTicks(Display d, double msdistance, bool label)
            {
                if (!label)
                {
                    DrawTicks(d, msdistance / 10, cellBounds.Bottom - 3, cellBounds.Bottom - 2);
                    DrawTicks(d, msdistance / 2, cellBounds.Bottom - 5, cellBounds.Bottom - 2);
                    DrawTicks(d, msdistance, cellBounds.Bottom - 8, cellBounds.Bottom - 2);
                }
                else
                {
                    DrawTicks(d, msdistance / 10, cellBounds.Top - 3, cellBounds.Top - 2);
                    DrawTicks(d, msdistance / 2, cellBounds.Top - 5, cellBounds.Top - 2);
                    DrawTicks(d, msdistance, cellBounds.Top - 8, cellBounds.Top - 2);
                    LabelTicks(d, msdistance, cellBounds.Top - 21, cellBounds.Left);
                }
            }

            private Font textfont = new Font("Microsoft Sans Serif", 8.25f);
            private Brush textbrush = Brushes.Black;
 
            private void LabelTicks(Display d, double msdistance, int top, int left)
            {
                // Construct 2 new StringFormat objects
                StringFormat format1 = new StringFormat(StringFormatFlags.NoClip);

                // Set the LineAlignment and Alignment properties for
                // both StringFormat objects to different values.
                format1.LineAlignment = StringAlignment.Near;
                format1.Alignment = StringAlignment.Center;
                
                double tickdistance = msdistance / scale;
                double mspos = ((long)(leftticks / tickdistance)) * msdistance;

                while (mspos < (scale * (leftticks + d.owner.widthticks)))
                {
                    if (mspos > scale*leftticks)
                    {
                        int hpos = cellBounds.Left + (int)(((mspos/scale) - leftticks) * hscale);
                        double roundedmspos = msdistance * (long) (mspos/msdistance);  // to reduce rounding errors
                        graphics.DrawString(roundedmspos.ToString(), textfont, textbrush, hpos, top, format1);
                    }
                    mspos += msdistance;
                }

            }

            private void DrawTicks(Display d, double msdistance, int top, int bottom)
            {
                double tickdistance = msdistance / scale;
                double pos = ((long)(leftticks / tickdistance)) * tickdistance;
                while (pos < leftticks + d.owner.widthticks)
                {
                    if (pos > leftticks)
                    {
                        int hpos = cellBounds.Left + (int)((pos - leftticks) * hscale);
                        graphics.DrawLine(Pens.Black, new Point(hpos, top),
                        new Point(hpos, bottom));
                    }
                    pos += tickdistance;
                }
            }

           

            private Font speedupfont = new Font("Microsoft Sans Serif", 15.0f, FontStyle.Bold);
            private Brush speedupbrush = Brushes.Red;
            
            private void DrawSpeedup(Display d, Rectangle cellBounds, double factor)
            {
                // Construct 2 new StringFormat objects
                StringFormat format1 = new StringFormat(StringFormatFlags.NoClip);

                // Set the LineAlignment and Alignment properties for
                // both StringFormat objects to different values.
                format1.LineAlignment = StringAlignment.Near;
                format1.Alignment = StringAlignment.Far;

                RectangleF layoutrectangle =  new RectangleF(cellBounds.X, cellBounds.Y, cellBounds.Width, cellBounds.Height);

                graphics.DrawString(factor.ToString("F2") + "X", speedupfont, speedupbrush, layoutrectangle, format1);
                   
            }

            internal void DrawBox(double start, double end, double top, double bottom, Pen pen, Brush brush)
            {
                int width = (int)((end - start) * hscale) - 1;
                int height = (int)((bottom - top) * vscale) - 1;
                int offset_h = (int)((start - leftticks) * hscale);
                if (offset_h <= cellBounds.Width)
                {
                    if (width + offset_h > cellBounds.Width)
                        width = cellBounds.Width - offset_h;
                    else if (width < 1)
                        width = 1;
                    if (offset_h < 0)
                    {
                        width = width + offset_h;
                        offset_h = 0;
                    }
                    if (width > 0)
                    {
                        Rectangle newRect = new Rectangle(
                                cellBounds.X + offset_h,
                                cellBounds.Y + (int)(top * vscale),
                                width,
                                height);
                        if (brush != null)
                            graphics.FillRectangle(brush, newRect);
                        if (pen != null)
                            graphics.DrawRectangle(pen, newRect);
                    }
                }

            }
        }

        public class DataGridViewCustomColumn : DataGridViewColumn
        {
            public DataGridViewCustomColumn()
            {
                this.CellTemplate = new DataGridViewCustomCell();
            }
        }


        private void textBox2_TextChanged_1(object sender, EventArgs e)
        {
            try
            {
                double width = double.Parse(textBox2.Text);
                if (width > 0)
                    widthticks = (long) (width / scale);

                UpdateZoom();
                UpdateScrollBar();
                UpdateView();
           }
            catch
            {
            }
        }

        private void hScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            leftticks = (maxticks - widthticks) * hScrollBar1.Value / (100000 - hScrollBar1.LargeChange);
            UpdateView();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            long center = leftticks + (widthticks / 2);

            widthticks = (100000 - trackBar1.Value) * (maxticks) / 100000;
            if (widthticks < 1)
                widthticks = 1;

            leftticks = center - (widthticks  / 2);
            if (leftticks < 0)
                leftticks = 0;
            else if (leftticks > (maxticks - widthticks))
                leftticks = (maxticks - widthticks);

            UpdateScrollBar();
            UpdateTextBox();
            UpdateView();
        }

        private void dataGridView1_SizeChanged(object sender, EventArgs e)
        {
            UpdateView();
        }

     
        private void dataGridView1_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            TaskGroup meter = (e.Row.Cells[display_col].Value as Display).meter;
            meter.Enabled = false;
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            string container = comboBoxAccount.Text;
            string folder = comboBoxFolder.Text;
            string pattern = txtDeployment.Text;
            bool success = TaskGroup.ReadFromFile(connectionString, container, folder, pattern);

            if (success)
            {
                int index = comboBoxFolder.FindString(folder);
                if (index == -1)
                    comboBoxFolder.Items.Add(folder);
            }

            RefreshData();

            foreach (TaskGroup m in TaskGroup.TaskGroups.Values)
                m.Intervals.Clear();
        }

      

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
           
        }

 
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void label1_Click(object sender, EventArgs e)
        {
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
             
        }

        private void dataGridView1_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            Console.WriteLine(string.Format("c={0} r={1} x={2} y={3} b={4}", e.ColumnIndex, e.RowIndex, e.X, e.Y, e.Button.ToString()));

            var ctrlclick = ((Control.ModifierKeys & Keys.Control) == Keys.Control);

            if (e.ColumnIndex == 6)
            {
                var cellwidth = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Size.Width;
                var pos = (long) (((double)e.X / (double)cellwidth) * this.widthticks);

                if (ctrlclick)
                {
                    //crop
                    if (e.Button == MouseButtons.Left)
                    {
                        this.leftticks += pos;
                        this.widthticks -= pos;
                    }
                    else
                    {
                        this.widthticks -= widthticks - pos;
                    }
                }
                else
                {
                    
                    if (e.Button == MouseButtons.Left)
                        this.leftticks += pos;
                    else
                        this.leftticks -= widthticks - pos;
                }

                UpdateZoom();
                UpdateTextBox();
                UpdateView();
            }
        
        }

    
       

    }
}
