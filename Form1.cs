using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MongoLogParse
{
    public partial class Form1 : Form
    {
        int timeOuts = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private List<LogEntry> LogEntries = new List<LogEntry>();
        private List<LogEntry> TrashEntries = new List<LogEntry>();

        private void button1_Click(object sender, EventArgs e)
        {
            
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                this.Text = this.Text + ", " + openFileDialog1.FileName;

                using (var inStream = openFileDialog1.OpenFile())
                {
                    if(v2Radio.Checked)
                    {
                        V2Parse(inStream);
                    }
                    if (v3Radio.Checked)
                    {
                        V3Parse(inStream);
                    }
                }
            }

            tbLines.Text = LogEntries.Count.ToString();
            tbTimeOuts.Text = timeOuts.ToString();

            dataGridView1.DataSource = LogEntries;

            var poorPerformers = LogEntries.GroupBy(le => le.Collection).Select(group => new {
                DataBase = group.Select(le => le.Database).Distinct().Count() == 1 ? group.Select(le => le.Database).First() : "",
                Collection = group.Key,
                Count = group.Count(),
                Min = group.Min(le => le.Time),
                Max = group.Max(le => le.Time),
                Avg = group.Average(le => le.Time),
                ColScans = group.Count(le => le.CollScan)
            });

            dataGridView2.DataSource = poorPerformers.OrderByDescending(pp => pp.Avg).OrderByDescending(pp => pp.ColScans).ToList();
        }

        private void V2Parse(Stream inStream)
        {
            TextReader reader = new StreamReader(inStream);
            string line = reader.ReadLine();
            while (line != null)
            {
                LogEntry logEntry = new LogEntry();
                logEntry.Line = line;

                if (line.Contains("CTRL_SHUTDOWN_EVENT"))
                {
                    //might as well just bail
                    MessageBox.Show("Warning, this file was not entirly parsed.");
                    break;
                }
                if (line.Length > 28)
                {
                    int dtEnd = line.IndexOf(' ');
                    string dt = line.Substring(0, dtEnd);
                    if (dt.Length == 28)
                    {
                        logEntry.DateTime = DateTime.Parse(dt);
                        int conEnd = line.IndexOf(' ', dtEnd + 1);
                        string con = line.Substring(dtEnd + 1, conEnd - dtEnd - 1);
                        logEntry.Connection = con;
                        int commandEnd = line.IndexOf(' ', conEnd + 1);
                        if (commandEnd == -1)
                        {
                            TrashEntries.Add(logEntry);
                            line = reader.ReadLine();
                            continue;
                        }
                        string command = line.Substring(conEnd + 1, commandEnd - conEnd - 1);
                        logEntry.Command = command;

                        if (command == "query" || command == "command" || command == "insert")
                        {
                            int dbEnd = line.IndexOf('.', commandEnd + 1);
                            string db = line.Substring(commandEnd + 1, dbEnd - commandEnd - 1);
                            logEntry.Database = db;
                            int collectionEnd = line.IndexOf(' ', dbEnd + 1);
                            string collection = line.Substring(dbEnd + 1, collectionEnd - dbEnd - 1);

                            if (collection == "$cmd")
                            {
                                int cmdTypeEnd = line.IndexOf('{', collectionEnd + 10) - 2;
                                string cmdType = line.Substring(collectionEnd + 10, cmdTypeEnd - (collectionEnd + 10) + 1);
                                collection = collection + ": " + cmdType;
                            }
                            logEntry.Collection = collection;

                            //it gets scary from here to end crazy times
                            int timeInt = 0;
                            ReTryIt:
                            try
                            {
                                int timeStart = line.LastIndexOf(' ');
                                string time = line.Substring(timeStart + 1, line.Length - timeStart - 3);
                                timeInt=int.Parse(time);
                            }
                            catch
                            {
                                line = line + reader.ReadLine();
                                while (line.Substring(line.Length - 2, 2) != "ms")
                                {
                                    line = line + reader.ReadLine();
                                }
                                logEntry.Line = line;
                                goto ReTryIt;
                            }
                            logEntry.Time = timeInt;
                            //end crazy times

                            logEntry.CollScan = line.Contains("COLLSCAN");                       
                            
                            if (logEntry.Time < 1271310000)
                            {
                                LogEntries.Add(logEntry);
                            }
                            else
                            {
                                timeOuts++;
                                if (!cbDropTimeOuts.Checked)
                                {
                                    LogEntries.Add(logEntry);
                                }
                            }
                        }
                        else
                            TrashEntries.Add(logEntry);
                    }
                    else
                        TrashEntries.Add(logEntry);
                }
                else
                    TrashEntries.Add(logEntry);
                line = reader.ReadLine();
            }
        }

        private void V3Parse(Stream inStream)
        {
            TextReader reader = new StreamReader(inStream);
            string line = reader.ReadLine();
            while (line != null)
            {
                if (line.Contains("CTRL_SHUTDOWN_EVENT"))
                {
                    //might as well just bail
                    MessageBox.Show("Warning, this file was not entirly parsed.");
                    break;
                }
                if (line.Length > 28)
                {
                    int dtEnd = line.IndexOf(' ');
                    string dt = line.Substring(0, dtEnd);
                    if (dt.Length == 28)
                    {
                        int conEnd = line.IndexOf(' ', dtEnd + 12);
                        string con = line.Substring(dtEnd + 12, conEnd - (dtEnd + 11));
                        int commandEnd = line.IndexOf(' ', conEnd + 1);
                        if (commandEnd == -1)
                        {
                            line = reader.ReadLine();
                            continue;
                        }
                        string command = line.Substring(conEnd + 1, commandEnd - conEnd - 1);
                        if (command == "query" || command == "command")
                        {
                            bool isQuery = command == "query";

                            int dbEnd = line.IndexOf('.', commandEnd + 1);
                            if (dbEnd == -1)
                            {
                                line = reader.ReadLine();
                                continue;
                            }
                            string db = line.Substring(commandEnd + 1, dbEnd - commandEnd - 1);
                            int collectionEnd = line.IndexOf(' ', dbEnd + 1);
                            string collection = line.Substring(dbEnd + 1, collectionEnd - dbEnd - 1);

                            if (!isQuery)
                            {
                                int commandStart = line.IndexOf("command:", collectionEnd);
                                commandEnd = line.IndexOf(" ", commandStart + 9);
                                command = line.Substring(commandStart + 9, commandEnd - (commandStart + 9));
                                collection = collection + ": " + command;
                            }

                            //if (collection == "$cmd")
                            //{
                            //    int cmdTypeEnd = line.IndexOf('{', collectionEnd + 10) - 2;
                            //    string cmdType = line.Substring(collectionEnd + 10, cmdTypeEnd - (collectionEnd + 10) + 1);
                            //    collection = collection + ": " + cmdType;
                            //}

                            int timeStart = line.LastIndexOf(' ');
                            string time = line.Substring(timeStart + 1, line.Length - timeStart - 3);

                            LogEntry logEntry = new LogEntry()
                            {
                                DateTime = DateTime.Parse(dt),
                                Connection = con,
                                Command = command,
                                Database = db,
                                Collection = collection,
                                Time = int.Parse(time),
                                CollScan = line.Contains("COLLSCAN"),
                                Line = line
                            };

                            if (logEntry.Time < 1271310000)
                            {
                                LogEntries.Add(logEntry);
                            }
                            else
                            {
                                timeOuts++;
                                if (!cbDropTimeOuts.Checked)
                                {
                                    LogEntries.Add(logEntry);
                                }
                            }
                        }
                    }
                }
                line = reader.ReadLine();
            }
        }


        private void dataGridView2_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView2.SelectedRows.Count == 1)
            { 
                var key = dataGridView2.SelectedRows[0].Cells[1].Value.ToString();
                List<LogEntry> entrysForCollection = LogEntries.Where(le => le.Collection == key).
                    OrderBy(le => le.DateTime).ToList();

                DateTime minDT = entrysForCollection.Min(le => le.DateTime);
                DateTime maxDT = entrysForCollection.Max(le => le.DateTime);
                if (maxDT.Subtract(minDT).TotalHours < 24)
                    chart1.Series[0].XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.Time;
                else
                    chart1.Series[0].XValueType = System.Windows.Forms.DataVisualization.Charting.ChartValueType.DateTime;

                chart1.Series[0].Points.Clear();
                entrysForCollection.ForEach(le =>
                {
                    chart1.Series[0].Points.AddXY(le.DateTime, le.Time);
                });

                if (entrysForCollection.Max(le => le.Time) > 90000)
                {
                    chart1.ChartAreas[0].AxisY.Maximum = 90000;
                }
                else
                {
                    chart1.ChartAreas[0].RecalculateAxesScale();
                    chart1.ChartAreas[0].AxisY.Maximum = double.NaN;
                }

                if (checkBox1.Checked)
                {
                    dataGridView1.DataSource = entrysForCollection;
                }
            }
        }

        private void dataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            List<LogEntry> data = (List<LogEntry>) dataGridView1.DataSource;
            switch(e.ColumnIndex)
            {
                case 0:
                    dataGridView1.DataSource = data.OrderBy(le => le.DateTime).ToList();
                    break;
                case 6:
                    dataGridView1.DataSource = data.OrderBy(le => le.Time).ToList();
                    break;
                case 4:
                    dataGridView1.DataSource = data.OrderBy(le => le.Connection).ToList();
                    break;
            }
        }

        private void dataGridView1_ColumnHeaderMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            List<LogEntry> data = (List<LogEntry>)dataGridView1.DataSource;
            switch (e.ColumnIndex)
            {
                case 0:
                    dataGridView1.DataSource = data.OrderByDescending(le => le.DateTime).ToList();
                    break;
                case 6:
                    dataGridView1.DataSource = data.OrderByDescending(le => le.Time).ToList();
                    break;
                case 4:
                    dataGridView1.DataSource = data.OrderByDescending(le => le.Connection).ToList();
                    break;
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                if (dataGridView2.SelectedRows.Count == 1)
                {
                    var key = dataGridView2.SelectedRows[0].Cells[1].Value.ToString();
                    List<LogEntry> entrysForCollection = LogEntries.Where(le => le.Collection == key).
                        OrderBy(le => le.DateTime).ToList();
                    dataGridView1.DataSource = entrysForCollection;
                }
            }
            else
            {
                dataGridView1.DataSource = LogEntries;
            }
        }

        private void btFilter_Click(object sender, EventArgs e)
        {
            List<LogEntry> inGrid = (List<LogEntry>)dataGridView1.DataSource;
            List<LogEntry> filteredEntries = inGrid.Where(le => le.Line.Contains(tbFilter.Text)).
                        OrderBy(le => le.DateTime).ToList();
            dataGridView1.DataSource = filteredEntries;
        }

        private void btClearFilter_Click(object sender, EventArgs e)
        {
            dataGridView1.DataSource = LogEntries;
        }

        private void checkBox2_Click(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                dataGridView1.DataSource = TrashEntries;
            }
            else
            {
                dataGridView1.DataSource = LogEntries;

            }
        }
    }
}
