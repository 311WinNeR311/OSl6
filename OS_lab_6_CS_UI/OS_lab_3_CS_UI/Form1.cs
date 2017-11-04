using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace OS_lab_3_CS_UI
{
    public partial class Form1 : Form
    {
        private List<string> files = Directory.GetFiles(@"C:\").ToList<string>();
        private List<string> folders = Directory.GetDirectories(@"C:\").ToList<string>();
        public Form1()
        {
            InitializeComponent();
        }


        // critical section variables
        private Object fileCS = new Object();
        private Mutex folderCS = new Mutex();

        delegate void SetTextCallback(string text);
        delegate void SetUpdateProcess(int value);
        delegate void SetValueProcess(int min, int max, int val);
        delegate void SetButton1EnabledDelegate(bool isEnabled);
        delegate void SetButton3EnabledDelegate(bool isEnabled);

        List<Thread> T = new List<Thread>();

        /*
         Functions
        */

        private void SetText(string text)
        {
            if (this.richTextBox1.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.richTextBox1.Text += text;
            }
        }

        private void IncrementProgressBar(int value)
        {
            if (this.progressBar1.InvokeRequired)
            {
                SetUpdateProcess d = new SetUpdateProcess(IncrementProgressBar);
                this.Invoke(d, new object[] { value });
            }
            else
            {
                this.progressBar1.Increment(value);
                this.progressBar1.Update();
            }
        }

        private void UpdateProgressBar(int min, int max, int val)
        {
            if (this.progressBar1.InvokeRequired)
            {
                SetValueProcess d = new SetValueProcess(UpdateProgressBar);
                this.Invoke(d, new object[] { min, max, val });
            }
            else
            {
                this.progressBar1.Minimum = min;
                this.progressBar1.Maximum = max;
                this.progressBar1.Value = val;
                this.progressBar1.Update();
            }
        }

        private void SetButton1Enabled(bool isEnabled)
        {
            if (this.button1.InvokeRequired)
            {
                SetButton1EnabledDelegate d = new SetButton1EnabledDelegate(SetButton1Enabled);
                this.Invoke(d, new object[] { isEnabled });
            }
            else
            {
                this.button1.Enabled = isEnabled;
            }
        }

        private void SetButton3Enabled(bool isEnabled)
        {
            if (this.button3.InvokeRequired)
            {
                SetButton3EnabledDelegate d = new SetButton3EnabledDelegate(SetButton3Enabled);
                this.Invoke(d, new object[] { isEnabled });
            }
            else
            {
                this.button3.Enabled = isEnabled;
            }
        }

        private void PrintResults()
        {
            SetButton1Enabled(false);
            SetButton3Enabled(false);
            var folderslineCount = File.ReadLines(@"folders.txt").Count();
            var fileslineCount = File.ReadLines(@"files.txt").Count();
            SetText("Folders:\n");
            string sFilesName = "folders.txt";
            UpdateProgressBar(0, folderslineCount + fileslineCount, 0);
            lock (fileCS)
            {
                folderCS.WaitOne();
                if (File.Exists(sFilesName))
                {
                    StreamReader sr = new StreamReader(sFilesName);
                    string line = sr.ReadLine();

                    //Continue to read until you reach end of file
                    while (line != null)
                    {
                        SetText(line + '\n');
                        IncrementProgressBar(1);
                        line = sr.ReadLine();
                    }
                    sr.Close();
                }

                SetText("\nFiles:\n");
                sFilesName = "files.txt";
                if (File.Exists(sFilesName))
                {
                    StreamReader sr = new StreamReader(sFilesName);
                    string line = sr.ReadLine();

                    //Continue to read until you reach end of file
                    while (line != null)
                    {
                        SetText(line + '\n');
                        IncrementProgressBar(1);
                        line = sr.ReadLine();
                    }
                    sr.Close();
                }
                folderCS.ReleaseMutex();
            }
            SetButton1Enabled(true);
            SetButton3Enabled(true);
        }

        // Function that searching files 
        private List<string> SearchFiles(string[] files, string sFileName)
        {
            List<string> SearchedFiles = new List<string>();

            string[] tempfiles = new string[files.Length];

            //Clearing filepath
            for (int i = 0; i < files.Length; i++)
            {
                int iLastPosOfBackSlash = 0;

                for (int j = 0; j < files[i].Length; ++j)
                {
                    if (files[i][j] == '\\')
                    {
                        iLastPosOfBackSlash = j;
                    }
                }

                tempfiles[i] = files[i].Remove(0, iLastPosOfBackSlash + 1);
            }

            bool isAnyFilesEquals = false;

            if (sFileName[sFileName.Length - 1] == '*')
            {
                for (int i = 0; i < tempfiles.Length; ++i)
                {
                    bool isIdentity = true;
                    for (int j = 0; j < sFileName.Length; ++j)
                    {
                        if (sFileName[j] == '*')
                            break;
                        if (sFileName[j] != tempfiles[i][j])
                        {
                            isIdentity = false;
                            break;
                        }
                    }
                    if (isIdentity)
                    {
                        SearchedFiles.Add(files[i]);
                        isAnyFilesEquals = true;
                    }
                }
            }

            else
            {
                for (int i = 0; i < tempfiles.Length; ++i)
                {
                    if (tempfiles[i].Length == sFileName.Length)
                    {
                        bool isIdentity = true;
                        for (int j = 0; j < sFileName.Length; ++j)
                        {
                            if (sFileName[j] != tempfiles[i][j])
                            {
                                isIdentity = false;
                                break;
                            }
                        }
                        if (isIdentity)
                        {
                            SearchedFiles.Add(files[i]);
                            isAnyFilesEquals = true;
                        }
                    }
                }
            }
            lock (fileCS)
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter("files.txt", true))
                {
                    for (int i = 0; i < SearchedFiles.Count; ++i)
                    {
                        file.WriteLine(SearchedFiles[i].ToString());
                    }
                    if (!isAnyFilesEquals)
                    {
                        file.WriteLine("There are no founded files in choosed directory by your template");
                    }
                }
            }
            
            return SearchedFiles;
        }

        // Function that searching folders 
        private List<string> SearchFolders(string[] folders, string sFileName)
        {
            List<string> SearchedFolders = new List<string>();
            string[] tempfolders = new string[folders.Length];

            //Clearing filepath
            for (int i = 0; i < folders.Length; i++)
            {
                int iLastPosOfBackSlash = 0;

                for (int j = 0; j < folders[i].Length; ++j)
                {
                    if (folders[i][j] == '\\')
                    {
                        iLastPosOfBackSlash = j;
                    }
                }

                tempfolders[i] = folders[i].Remove(0, iLastPosOfBackSlash + 1);
            }
            bool isAnyFoldersEquals = false;

            if (sFileName[sFileName.Length - 1] == '*')
            {
                for (int i = 0; i < tempfolders.Length; ++i)
                {
                    bool isIdentity = true;
                    for (int j = 0; j < sFileName.Length; ++j)
                    {
                        if (sFileName[j] == '*')
                            break;
                        if (sFileName[j] != tempfolders[i][j])
                        {
                            isIdentity = false;
                            break;
                        }
                    }
                    if (isIdentity)
                    {
                        SearchedFolders.Add(folders[i]);
                        isAnyFoldersEquals = true;
                    }
                }
            }
            else
            {
                for (int i = 0; i < tempfolders.Length; ++i)
                {
                    if (tempfolders[i].Length == sFileName.Length)
                    {
                        bool isIdentity = true;
                        for (int j = 0; j < sFileName.Length; ++j)
                        {
                            if (sFileName[j] != tempfolders[i][j])
                            {
                                isIdentity = false;
                                break;
                            }
                        }
                        if (isIdentity)
                        {
                            SearchedFolders.Add(folders[i]);
                            isAnyFoldersEquals = true;
                        }
                    }
                }
            }
            folderCS.WaitOne();
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter("folders.txt", true))
                {
                    for (int i = 0; i < SearchedFolders.Count; ++i)
                    {
                        file.WriteLine(SearchedFolders[i].ToString());
                    }
                    if (!isAnyFoldersEquals)
                    {
                        file.WriteLine("There are no founded folders in choosed directory by your template");
                    }
                }
            }
            folderCS.ReleaseMutex();
            return SearchedFolders;
        }

        public void RecursivelySearch(string path, ref List<string> folders, ref List<string> files)
        {
            List<string> TempFiles = Directory.GetFiles(path).ToList<string>();
            List<string> TempFolders = Directory.GetDirectories(path).ToList<string>();

            foreach (var TempFolder in TempFolders)
            {
                RecursivelySearch(TempFolder, ref folders, ref files);
                folders.Add(TempFolder);
            }
            foreach (var TempFile in TempFiles)
            {
                files.Add(TempFile);
            }
        }
        
        private void Button1_Click(object sender, System.EventArgs e)
        {
            SetButton1Enabled(false);
            progressBar1.Value = 0;
            progressBar1.Update();
            files = new List<string>();
            folders = new List<string>();

            RecursivelySearch(textBox2.Text, ref folders, ref files);

            lock (fileCS)
            {
                folderCS.WaitOne();
                System.IO.StreamWriter fileT = new System.IO.StreamWriter("files.txt", false);
                fileT.Close();
                System.IO.StreamWriter fileF = new System.IO.StreamWriter("folders.txt", false);
                fileF.Close();
                folderCS.ReleaseMutex();
            }
            

            // Getting template file name
            string sFileName = textBox1.Text;
            if (sFileName == "")
                sFileName = "*";

            // Clearing filepath from files strings
            for (int i = 0; i < files.Count; ++i)
            {
                files[i] = files[i].Remove(0, textBox2.Text.Length);
                for (int j = 0; files[i][j] == '\\'; ++j)
                    files[i] = files[i].Remove(0, 1);
            }
            // Clearing filepath from folders strings
            for (int i = 0; i < folders.Count; ++i)
            {
                folders[i] = folders[i].Remove(0, textBox2.Text.Length);
                for (int j = 0; folders[i][j] == '\\'; ++j)
                    folders[i] = folders[i].Remove(0, 1);
            }

            // Threads count
            int iCountOfFilesThreads = int.Parse(comboBox1.SelectedItem.ToString()) / 2;
            int iCountOfFoldersThreads = int.Parse(comboBox1.SelectedItem.ToString()) / 2;
            while (folders.Count < iCountOfFoldersThreads)
                --iCountOfFoldersThreads;
            while (files.Count < iCountOfFilesThreads)
                --iCountOfFilesThreads;

            // progressBar Max Value

            progressBar1.Maximum = files.Count + folders.Count;
            progressBar1.Update();

            T.Clear();
            // Creating files and threads for folders
            for (int i = 0; i < iCountOfFoldersThreads; ++i)
            {
                // Creating files
                string sFilesWrite = "Folders";
                sFilesWrite += comboBox1.SelectedItem.ToString() + "-";
                sFilesWrite += i.ToString() + ".txt";
                string[] folderstemp;
                int iDec = folders.Count / iCountOfFoldersThreads;
                if (folders.Count % iCountOfFoldersThreads != 0 && i == iCountOfFoldersThreads - 1)
                {
                    folderstemp = new string[iDec + folders.Count % iCountOfFoldersThreads];
                    folders.CopyTo(folderstemp);
                    //Array.Copy(folders, i * iDec, folderstemp, 0, iDec + folders.Count % iCountOfFoldersThreads);
                }
                else
                {
                    folderstemp = new string[iDec];
                    folders.CopyTo(folderstemp);
                    //Array.Copy(folders, i * iDec, folderstemp, 0, iDec);
                }

                // Starting threads
                T.Add(new Thread(() => SearchFolders(folderstemp, sFileName)));
                T[T.Count - 1].Start();
            }

            // Creating files and threads for files
            for (int i = 0; i < iCountOfFilesThreads; ++i)
            {
                // Creating files
                string sFilesWrite = "Files";
                sFilesWrite += comboBox1.SelectedItem.ToString() + "-";
                sFilesWrite += i.ToString() + ".txt";
                string[] filestemp;
                int iDec = files.Count / iCountOfFilesThreads;
                if (files.Count % iCountOfFilesThreads != 0 && i == iCountOfFilesThreads - 1)
                {
                    filestemp = new string[iDec + files.Count % iCountOfFilesThreads];
                    files.CopyTo(filestemp);
                    //Array.Copy(files, i * iDec, filestemp, 0, iDec + files.Count % iCountOfFilesThreads);
                }
                else
                {
                    filestemp = new string[iDec];
                    files.CopyTo(filestemp);
                    //Array.Copy(files, i * iDec, filestemp, 0, iDec);
                }

                // Starting threads
                T.Add(new Thread(() => SearchFiles(filestemp, sFileName)));
                T[T.Count - 1].Start();
            }

            foreach (var thread in T)
            {
                thread.Join();
            }

            richTextBox1.Clear();
            Thread PRT = new Thread(PrintResults);
            PRT.Start();
        }


        // Choosing folder path; get files and directories from choosed path
        private void Button2_Click(object sender, System.EventArgs e)
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();

            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog1.SelectedPath))
                textBox2.Text = folderBrowserDialog1.SelectedPath;
        }




        // Protection from incorrect template
        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Text.Length > 0)
            {
                if (textBox1.Text[0] == '.')
                {
                    textBox1.Text = textBox1.Text.Remove(0, 1);
                }
                int iStarPos = 0;
                bool isStar = false;
                for (int i = 0; i < textBox1.Text.Length; ++i)
                {
                    if (textBox1.Text[i] == '*')
                    {
                        iStarPos = i;
                        isStar = true;
                        break;
                    }
                }
                if (isStar && textBox1.Text.Length - 1 > iStarPos)
                {
                    textBox1.Text = textBox1.Text.Remove(iStarPos + 1);
                }
            }
        }
        private void TextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != '/' &&
                e.KeyChar != '\\' &&
                e.KeyChar != ':' &&
                e.KeyChar != '?' &&
                e.KeyChar != '"' &&
                e.KeyChar != '<' &&
                e.KeyChar != '>' &&
                e.KeyChar != '|')
                return;
            else
                e.Handled = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 100;
            progressBar1.Value = 0;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            System.IO.StreamWriter fileT = new System.IO.StreamWriter(@"C:\Users\saveu\Documents\Visual Studio 2017\Projects\OS_lab_6_CS_UI\OS_lab_3_CS_UI\bin\Debug\result.txt", false);
            fileT.Close();

            var file = MemoryMappedFile.CreateFromFile(@"C:\Users\saveu\Documents\Visual Studio 2017\Projects\OS_lab_6_CS_UI\OS_lab_3_CS_UI\bin\Debug\result.txt", FileMode.Open, Name, richTextBox1.Text.Length + richTextBox1.Lines.Length);

            using (var streamwriter = new StreamWriter(file.CreateViewStream()))
            {
                for (int i = 0; i < richTextBox1.Lines.Length; ++i)
                {
                    streamwriter.WriteLine(richTextBox1.Lines[i]);
                }
                streamwriter.Close();
            }
            file.Dispose();
        }
    }
}