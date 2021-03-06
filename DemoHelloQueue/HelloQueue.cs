﻿using System;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Configuration;
using System.IO;
using DemoHelloQueue.Properties;
using System.Data;

namespace DemoHelloQueue
{
    public partial class HelloQueue : Form
    {
        private Thread TwitchIRCThread;
        public bool useUserList = false;
        public bool formIsClosing = false;
        public bool enableDing = false;
        TwitchIRC _helloQueueConn;
        List<string> userList;
        public List<string> quoteList;
        public List<string> modList; 
        public static Dictionary<string, string> commandDict;
        public static dbDataSet swabbotDB = new dbDataSet();
        public static dbDataSetTableAdapters.changeBot_commandTableAdapter commandsTA =
            new dbDataSetTableAdapters.changeBot_commandTableAdapter();
        public static dbDataSetTableAdapters.changeBot_viewerTableAdapter viewersTA =
            new dbDataSetTableAdapters.changeBot_viewerTableAdapter();
        public static dbDataSetTableAdapters.changeBot_quoteTableAdapter quotesTA =
            new dbDataSetTableAdapters.changeBot_quoteTableAdapter();
        public delegate void dgAddToList(object sender, IRCEventArgs e);

        public HelloQueue()
        {
            InitializeComponent();
            userList = new List<string>();
            getCommandDict();
            getQuoteList();
        }

        private void getQuoteList()
        {
            quoteList = new List<string>();
            string qteFilePath = Application.StartupPath + "\\quoteList.txt";
            if (File.Exists(qteFilePath))
            {
                StreamReader qteFile = new StreamReader(qteFilePath);

                string line;

                while ((line = qteFile.ReadLine()) != null)
                    quoteList.Add(line);

                qteFile.Close();
            }
        }

        private void getCommandDict()
        {
            commandDict = new Dictionary<string, string>();
            commandsTA.Fill(swabbotDB.changeBot_command);
            DataTable commandTable = new DataTable();
            commandTable = commandsTA.GetData();
            foreach (DataRow row in commandTable.Rows)
            {
                commandDict.Add(row[0].ToString(), row[1].ToString());
            }
            /*
            commandDict = new Dictionary<string, string>();
            //For each command found in command file, add it to dictionary.
            string cmdFilePath = Application.StartupPath + "\\cmdList.txt";
            if (File.Exists(cmdFilePath))
            {
                StreamReader cmdFile = new StreamReader(cmdFilePath);
                string line;
                while((line = cmdFile.ReadLine()) != null)
                {
                    string command = line.Substring(0, line.IndexOf(':')).Trim();
                    string result = line.Substring(line.IndexOf(':') + 1).Trim();
                    commandDict.Add(command, result);
                }
                cmdFile.Close();
            }
            */
        }

        private void sendToQueue(object sender, IRCEventArgs e)
        {
            if (richTextBox1.InvokeRequired)
            {
                this.BeginInvoke(new dgAddToList(sendToQueue), new object[] { sender, e });
            }
            else {

                if (!useUserList && String.IsNullOrEmpty(filterTxtBox.Text))
                {
                    richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Bold);
                    richTextBox1.AppendText(e.Username + ": ");
                    richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Regular);
                    richTextBox1.AppendText(e.Message + "\r\n");
                    if(!richTextBox1.ContainsFocus)
                        richTextBox1.ScrollToCaret();
                    if (enableDing)
                        System.Media.SystemSounds.Asterisk.Play();
                }
                else if (useUserList && String.IsNullOrEmpty(filterTxtBox.Text) && !userList.Contains(e.Username))
                {
                    userList.Add(e.Username);
                    richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Bold);
                    richTextBox1.AppendText(e.Username + ": ");
                    richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Regular);
                    richTextBox1.AppendText(e.Message + "\r\n");
                    if (enableDing)
                        System.Media.SystemSounds.Asterisk.Play();
                }
                else if (!useUserList && !String.IsNullOrEmpty(filterTxtBox.Text) && (e.Message.Contains(filterTxtBox.Text) || e.Username.Contains(filterTxtBox.Text)))
                {
                    richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Bold);
                    richTextBox1.AppendText(e.Username + ": ");
                    richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Regular);
                    richTextBox1.AppendText(e.Message + "\r\n");
                    if (enableDing)
                        System.Media.SystemSounds.Asterisk.Play();
                }
                else if (useUserList && !String.IsNullOrEmpty(filterTxtBox.Text) && !userList.Contains(e.Username) && (e.Message.Contains(filterTxtBox.Text) || e.Username.Contains(filterTxtBox.Text)))
                {
                    userList.Add(e.Username);
                    richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Bold);
                    richTextBox1.AppendText(e.Username + ": ");
                    richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Regular);
                    richTextBox1.AppendText(e.Message + "\r\n");
                    if (enableDing)
                        System.Media.SystemSounds.Asterisk.Play();
                }

            }
        }

        private void ConnectTwitchMenuItem_Click(object sender, EventArgs e)
        {
            if (TwitchIRCThread == null || !TwitchIRCThread.IsAlive)
            {
                modList = new List<string>();
                _helloQueueConn = new TwitchIRC(Globals.IrcServer, Globals.IrcPort, Globals.IrcUser, Globals.IrcChan, Globals.IrcPass);
                _helloQueueConn.OnGotMessage += new TwitchIRC.RecievedMessage(sendToQueue);
                _helloQueueConn.ConnectionLost += new TwitchIRC.lostConnection(lostConnection);
                _helloQueueConn.GotConnection += new TwitchIRC.gotConnection(gotConnect);
                TwitchIRCThread = new Thread(() => _helloQueueConn.Connect());
                TwitchIRCThread.Start();
            }

        }

        private void gotConnect(object sender, EventArgs e)
        {
            this.BeginInvoke(new MethodInvoker(delegate()
            {
                ConnectTwitchMenuItem.Text = "Connected!";
            }
            ));
        }

        private void lostConnection(object sender, EventArgs e)
        {
            this.BeginInvoke(new MethodInvoker(delegate()
            {
                if (!formIsClosing)
                {
                    MessageBox.Show("Could not connect/lost connection to IRC Server!");
                    ConnectTwitchMenuItem.Text = "Connect to Twitch";
                }
            }
            ));
        }

        private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            UpdateDB();
            commandsTA.Dispose();
            quotesTA.Dispose();
            viewersTA.Dispose();
            swabbotDB.Dispose();
            /*
            string cmdFilePath = Application.StartupPath + "\\cmdList.txt";
            TextWriter txtWriter = new StreamWriter(cmdFilePath);

            foreach (KeyValuePair<string, string> pair in commandDict)
            {
                //For each command found, save it to a file.
                txtWriter.WriteLine(pair.Key + ": " + pair.Value);
            }

            txtWriter.Close();

            formIsClosing = true;
            if (_helloQueueConn != null)
            {
                _helloQueueConn.shouldRun = false;
                _helloQueueConn.IrcReader.Close();
            }
            if (TwitchIRCThread != null && TwitchIRCThread.IsAlive)
            {
                _helloQueueConn.RequestStop();
                TwitchIRCThread.Join();
            } */
        }

        private void configureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Configuration configWindow = new Configuration();
            configWindow.Show();
        }

        private void HelloQueue_Load(object sender, EventArgs e)
        {
            Globals.IrcServer = Properties.Settings.Default.IrcServer;
            Globals.IrcUser = Properties.Settings.Default.IrcUser;
            Globals.IrcChan = Properties.Settings.Default.IrcChannel;
            Globals.IrcPass = Properties.Settings.Default.IrcPass;
            Globals.IrcPort = Properties.Settings.Default.IrcPort;
        }

        /*
         * This commented code is required for making a custom word wrap type entry in listbox1
        private void listBox1_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            e.ItemHeight = (int)e.Graphics.MeasureString(listBox1.Items[e.Index].ToString(), listBox1.Font, listBox1.Width).Height;
        }

        private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index != -1)
            {
                e.DrawBackground();
                e.DrawFocusRectangle();
                e.Graphics.DrawString(listBox1.Items[e.Index].ToString(), e.Font, new SolidBrush(e.ForeColor), e.Bounds);
            }
        }
        */

        private void button1_Click(object sender, EventArgs e)
        {
            //textBox1.Clear();
            richTextBox1.Clear();
            richTextBox1.Update();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About aboutForm = new About();
            aboutForm.Show();
        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            userList.Clear();
        }

        private void noDupUsersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (noDupUsersToolStripMenuItem.Checked)
                useUserList = true;
            else
                useUserList = false;
        }

        private void dingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dingToolStripMenuItem.Checked)
                enableDing = true;
            else
                enableDing = false;
        }

        private void richTextBox1_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.LinkText);
        }

        private void richTextBox2_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (richTextBox2.Text.Contains("\n"))
                    richTextBox2.Text.Replace("\n", "");
                _helloQueueConn.SendMessage(richTextBox2.Text.Trim());
                richTextBox2.Clear();
            }
        }

        private void commandsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Commands cmd = new Commands();
            cmd.Show();
        }

        private void editLoyaltySettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private static void ProcessDialogResult(DialogResult response)
        {
            switch (response)
            {
                case DialogResult.Yes:
                    swabbotDB.Merge(tempCommandsDataTable, true, MissingSchemaAction.Ignore);
                    UpdateDB();
                    break;

                case DialogResult.No:
                    swabbotDB.Merge(tempCommandsDataTable);
                    MessageBox.Show("Update cancelled");
                    break;
            }
        }

        public static void UpdateDB()
        {
            dbDataSet.changeBot_commandDataTable deletedCommands = (dbDataSet.changeBot_commandDataTable)swabbotDB.changeBot_command.GetChanges(DataRowState.Deleted);
            dbDataSet.changeBot_commandDataTable addedCommands = (dbDataSet.changeBot_commandDataTable)swabbotDB.changeBot_command.GetChanges(DataRowState.Added);
            dbDataSet.changeBot_commandDataTable modifiedCommands = (dbDataSet.changeBot_commandDataTable)swabbotDB.changeBot_command.GetChanges(DataRowState.Modified);

            try
            {
                if (deletedCommands != null)
                    commandsTA.Update(deletedCommands);

                if (addedCommands != null)
                    commandsTA.Update(addedCommands);

                if (modifiedCommands != null)
                    commandsTA.Update(modifiedCommands);

                swabbotDB.AcceptChanges();
            }
            catch (DBConcurrencyException dbcx)
            {
                //swabbotDB.changeBot_command.Rows.Remove(dbcx.Row);
                commandsTA.Insert(dbcx.Row["trigger"].ToString(), dbcx.Row["result"].ToString());
                swabbotDB.AcceptChanges();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Update process error: " + ex.Message);
            }

        }

        private static string CreateDBConcurrentMessage(dbDataSet.changeBot_commandRow cr)
        {
            return
                "Database: " + GetRowData(GetCurrentRowInDB(cr), DataRowVersion.Default) + "\n" +
                "Original: " + GetRowData(cr, DataRowVersion.Original) + "\n" +
                "Proposed: " + GetRowData(cr, DataRowVersion.Current) + "\n" +
                "Do you still want to update the database with the proposed value?";
        }

        private static dbDataSet.changeBot_commandDataTable tempCommandsDataTable = new dbDataSet.changeBot_commandDataTable();

        private static dbDataSet.changeBot_commandRow GetCurrentRowInDB(dbDataSet.changeBot_commandRow RowWithError)
        {
            commandsTA.Fill(tempCommandsDataTable);

            dbDataSet.changeBot_commandRow currentRowInDb =
                tempCommandsDataTable.FindBytrigger(RowWithError.trigger);

            return currentRowInDb;
        }

        private static string GetRowData(dbDataSet.changeBot_commandRow comRow, DataRowVersion RowVersion)
        {
            string rowData = "";

            for (int i = 0; i < comRow.ItemArray.Length; i++)
            {
                rowData = rowData + comRow[i, RowVersion].ToString() + " ";
            }
            return rowData;
        }
    }

}
