﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.IO.Compression;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Net;
using System.Linq;
using System.Diagnostics;

namespace SPBU12._1MANAGER
{



    public partial class Form1 : Form
    {
        //Инициализация переменных
        private string rootLeft, rootRight;
        Dictionary<string, string> dirs;
        ListElements listLeft, listRight;
        FileSystemWatcher watcherLeft, watcherRight;
        bool isChanged1, isChanged2;

        ListView lwOnline;

        public static UserData data;
        private string login, password, rootUser;

        //Окно логина
        private void Initialization()
        {
            Start start = new Start();
            DialogResult r = start.ShowDialog();

            if (r == DialogResult.Yes)
            {
                if (start.Login != "" && start.Password != "")
                {
                    data = new UserData();
                    data.login = start.Login;
                    data.password = start.Password;
                    rootUser = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    rootUser += Path.DirectorySeparatorChar + data.login + ".dat";
                }
                else
                {
                    MessageBox.Show("Enter something");
                    Initialization();
                }
            }
            else if (r == DialogResult.OK)
            {
                login = start.Login;
                password = start.Password;
                rootUser = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                rootUser += Path.DirectorySeparatorChar + login + ".dat";

                if (!File.Exists(rootUser))
                {
                    MessageBox.Show("Incorrect login");
                    Initialization();
                }
                else
                {
                    BinaryFormatter binFormat = new BinaryFormatter();
                    Stream fStream = File.Open(rootUser, FileMode.Open);
                    data = (UserData)binFormat.Deserialize(fStream);
                    fStream.Close();

                    if (data.password != password)
                    {
                        MessageBox.Show("Incorrect password");
                        Initialization();
                    }
                }
            }
            else if (r == DialogResult.Cancel)
            {
                Environment.Exit(0);
            }

            UpdateForm();
        }

        //Передача кастомных данных в форму
        private void UpdateForm()
        {
            this.password = data.password;
            this.Font = data.mainFont;
            listView1.BackColor = data.color1;
            listView2.BackColor = data.color1;
            listView1.Font = data.fileFont;
            listView2.Font = data.fileFont;
        }

        //watcher - онлайн обновление директорий
        private void WatchersInitialize()
        {
            timer1.Interval = 10;
            timer1.Tick += timer1_Tick;
            timer1.Enabled = true;

            watcherLeft = new FileSystemWatcher();
            watcherRight = new FileSystemWatcher();

            watcherLeft.Changed += UpdateLeft;
            watcherLeft.Created += UpdateLeft;
            watcherLeft.Deleted += UpdateLeft;
            watcherLeft.Renamed += UpdateLeft;

            watcherRight.Changed += UpdateRight;
            watcherRight.Created += UpdateRight;
            watcherRight.Deleted += UpdateRight;
            watcherRight.Renamed += UpdateRight;

            isChanged1 = false;
            isChanged2 = false;
        }

        //Инициализация формы
        public Form1()
        {
            InitializeComponent();
            Initialization();

            WatchersInitialize();

            dirs = new Dictionary<string, string>();

            listLeft = new ListElements();
            listRight = new ListElements();

            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (DriveInfo info in drives)
            {
                try
                {
                    comboBox1.Items.Add(info.Name + "        " + info.VolumeLabel);
                    comboBox2.Items.Add(info.Name + "        " + info.VolumeLabel);
                }
                catch
                {
                }
            }

            comboBox1.Text = comboBox1.Items[0].ToString();
            comboBox2.Text = comboBox2.Items[0].ToString();

            lwOnline = listView1;
        }

        //Апдейт левой формы
        private void UpdateLeft(object sendler, FileSystemEventArgs e)
        {
            isChanged1 = true;
        }

        //Апдейт правой формы
        private void UpdateRight(object sendler, FileSystemEventArgs e)
        {
            isChanged2 = true;
        }

        //Директория формы
        public string Root(ListView lw)
        {
            if (lw == listView1)
                return rootLeft;
            return rootRight;
        }

        //Список элементов на форме
        private ListElements ListE(ListView lw)
        {
            if (lw == listView1)
                return listLeft;
            return listRight;
        }

        //Обновление формы
        private void UpdateListView(ListView lw)
        {
            if (lw.SelectedItems.Count > 0 && lw == WhichListView() && Path.GetFileName(lw.SelectedItems[0].Text) != lw.SelectedItems[0].Text)
            {
                if (lw == listView1)
                    rootLeft = lw.SelectedItems[0].Text;
                else
                    rootRight = lw.SelectedItems[0].Text;
            }

            ListE(lw).Update(Root(lw));
            lw.Items.Clear();

            foreach (ListViewItem item in ListE(lw).list)
            {
                lw.Items.Add(item);
                if ((item.Index % 2) == 0)
                    item.BackColor = data.color1;
                else
                    item.BackColor = data.color2;
                item.ForeColor = data.fontColor;
            }

            TB tb = new TB(() =>
            {
                if (lw == listView1)
                    return textBox4;
                return textBox5;
            });

            tb.Invoke().Text = Root(lw);

            if (lw == listView1)
            {
                watcherLeft.Path = rootLeft;
                watcherLeft.EnableRaisingEvents = true;
            }
            else
            {
                watcherRight.Path = rootRight;
                watcherRight.EnableRaisingEvents = true;
            }
        }

        delegate TextBox TB();

        delegate DriveInfo Disk();

        //Меняет имя пути в правом или левом окне
        private void PathName(string name, bool left)
        {
            name = name.Substring(0, name.IndexOf(" "));
            if (left)
                rootLeft = name;
            else
                rootRight = name;
        }

        //Смена диска (левое окно)
        private void comboBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            PathName(comboBox1.Text, true);
            UpdateListView(listView1);
        }
        //Смена диска (правое окно)
        private void comboBox2_SelectedValueChanged(object sender, EventArgs e)
        {
            PathName(comboBox2.Text, false);
            UpdateListView(listView2);
        }

        //Обработка двойного нажатия на элементы формы
        private void DoubleClickLV(ListView lw)
        {
            string path = ListE(lw).DoubleClick(lw, Root(lw));

            if (lw == listView1)
                rootLeft = path;
            else
                rootRight = path;

            UpdateListView(lw);
        }
        //Обработка двойного нажатия на элементы первой формы
        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            DoubleClickLV(listView1);
        }
        //Обработка двойного нажатия на элементы первой формы
        private void listView2_DoubleClick(object sender, EventArgs e)
        {
            DoubleClickLV(listView2);
        }

        //Кнопки возврата в левом окне
        private void button4_Click(object sender, EventArgs e)
        {
            if (Path.GetDirectoryName(rootLeft) != null)
                rootLeft = Path.GetDirectoryName(rootLeft);
            UpdateListView(listView1);
        }
        private void button3_Click(object sender, EventArgs e)
        {
            if (Path.GetDirectoryName(rootLeft) != null)
                rootLeft = Path.GetPathRoot(rootLeft);
            UpdateListView(listView1);
        }

        //Кнопки возврата в правом окне
        private void button5_Click(object sender, EventArgs e)
        {
            if (Path.GetDirectoryName(rootRight) != null)
                rootRight = Path.GetDirectoryName(rootRight);
            UpdateListView(listView2);
        }
        private void button6_Click(object sender, EventArgs e)
        {
            if (Path.GetDirectoryName(rootRight) != null)
                rootRight = Path.GetPathRoot(rootRight);
            UpdateListView(listView2);
        }


        //Вызов окна help
        private void Help()
        {
            HelpBox hb = new HelpBox();
            hb.Font = data.dialogFont;
            hb.ShowDialog();
        }

        //Возвращает директорию диска в текущем окне
        private string EndDir(string startDir)
        {
            if (startDir == rootLeft)
                return rootRight;
            return rootLeft;
        }

        //Метод копирования директории
        private void CopyDir(string nameDir, string endDir)
        {
            Directory.CreateDirectory(endDir + Path.DirectorySeparatorChar + Path.GetFileName(nameDir));

            DirectoryInfo di = new DirectoryInfo(nameDir);
            DirectoryInfo[] directories = di.GetDirectories();
            FileInfo[] files = di.GetFiles();

            foreach (DirectoryInfo info in directories)
            {
                CopyDir(nameDir + Path.DirectorySeparatorChar + info.Name, endDir);
            }

            foreach (FileInfo info in files)
            {
                CopyFile(info.Name, nameDir, endDir);
            }
        }

        //Метод копирования файла
        private void CopyFile(string nameFile, string startDir, string endDir)
        {
            using (FileStream SourceStream = File.Open(startDir + Path.DirectorySeparatorChar + nameFile, FileMode.Open))
            {
                using (FileStream DestinationStream = File.Create(endDir + Path.DirectorySeparatorChar + nameFile))
                {
                    SourceStream.CopyTo(DestinationStream);
                }
            }
        }

        //Если существует - возвращает false
        private bool IsDir(string path)
        {
            return File.Exists(path) ? false : true;
        }

        //Вывод окна копирования
        private bool ShowWind(string root, string label)
        {
            CopyBox cb = new CopyBox(root, label);
            cb.Font = data.dialogFont;
            if (cb.ShowDialog() == DialogResult.OK)
                return true;
            return false;
        }

        //Вывод окна загрузки
        private void ShowWindDownload()
        {
            DownloadBox cb = new DownloadBox();
            cb.Font = data.dialogFont;
            cb.Show();

        }

        //Вывод окна поиска файлов
        private void ShowWindSearch()
        {
            SearchBox cb = new SearchBox();
            cb.Font = data.dialogFont;
            cb.Show();

        }

        //Скопировать элемент
        private void CopyElement(ListView lw, string item)
        {
            string startDirectory = Root(lw);
            string endDirectory = EndDir(startDirectory);

            if (IsDir(startDirectory + Path.DirectorySeparatorChar + item))
                CopyDir(startDirectory + Path.DirectorySeparatorChar + item.Substring(1).Remove(item.Length - 2), endDirectory);
            else
                CopyFile(item, startDirectory, endDirectory);
        }

        //Скопировать файлы
        private void IsCopy()
        {
            var lw = WhichListView();
            if (ShowWind(EndDir(Root(lw)), "Copy " + lw.SelectedItems.Count + " file(s) to:"))
            {
                foreach (ListViewItem file in lw.SelectedItems)
                {
                    CopyElement(lw, file.Text + file.SubItems[1].Text);
                }
            }
        }







        //Переместить элемент
        private void MoveElement(ListView lw, string el)
        {
            CopyElement(lw, el);
            DeleteElement(Root(lw), el);
        }

        //Переместить файлы
        private void MoveF()
        {
            var lw = WhichListView();
            if (ShowWind(EndDir(Root(lw)), "Rename/move " + lw.SelectedItems.Count + " file(s) to:"))
            {
                foreach (ListViewItem item in lw.SelectedItems)
                {
                    MoveElement(lw, item.Text + item.SubItems[1].Text);
                }
            }
        }

        //Удалить элемент
        private void DeleteElement(string root, string name)
        {
            if (!IsDir(root + Path.DirectorySeparatorChar + name))
                File.Delete(root + Path.DirectorySeparatorChar + name);
            else
            {
                string pathDir = root + Path.DirectorySeparatorChar + name.Substring(1, name.Length - 2);
                Directory.Delete(pathDir, true);
            }
        }

        //Окно удаления
        private bool MBShowOK(int k)
        {
            if (MessageBox.Show("Do you really want to delete " + k + " file(s)?", "Custom file manager", MessageBoxButtons.OKCancel, MessageBoxIcon.Stop) == DialogResult.OK)
                return true;
            return false;
        }

        //Удалить несколько выбранных элементов
        private void Delete()
        {
            var lw = WhichListView();
            if (MBShowOK(lw.SelectedItems.Count))
            {
                foreach (ListViewItem item in lw.SelectedItems)
                {
                    string name = item.Text + item.SubItems[1].Text;
                    DeleteElement(Root(lw), name);
                }
            }
        }

        //Переименовать элемент
        private void RenameElement(string newPath, string path)
        {
            if (IsDir(path))
                Directory.Move(path, newPath);
            else
                File.Move(path, newPath);
        }



        //Закрыть программу
        private void Exit()
        {
            Application.Exit();
        }

        //Выбрано ли что-нибудь
        private bool IsSelected()
        {
            return (listView1.SelectedItems.Count > 0 || listView2.SelectedItems.Count > 0);
        }

        //Выбран ли один элемент
        private bool IsSelectedOne()
        {
            return (listView1.SelectedItems.Count == 1 || listView2.SelectedItems.Count == 1);
        }

        //Какой листвью выбран
        public ListView WhichListView()
        {
            if (listView2.Focused)
            {
                return listView2;
            }
            return listView1;
        }

        //Для watcher-a
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (isChanged1)
            {
                UpdateListView(listView1);
                isChanged1 = false;
            }
            if (isChanged2)
            {
                UpdateListView(listView2);
                isChanged2 = false;
            }

            if (IsSelected() && lwOnline != WhichListView())
            {
                lwOnline = WhichListView();
            }
        }

        //Сохранить настройки
        private void Save()
        {
            BinaryFormatter binFormat = new BinaryFormatter();
            Stream fStream = new FileStream(rootUser, FileMode.Create, FileAccess.Write, FileShare.None);
            binFormat.Serialize(fStream, data);
            fStream.Close();
        }

        //Закрытие формы
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Save();
        }



        //ОБРАБОТКА СОЧЕТАНИЙ КЛАВИШ И ОБЫЧНЫХ НАЖАТИЙ

        // Search patterns button
        private void findToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (IsSelected())
            {
                ListViewItem selectFile = WhichListView().SelectedItems[0];
                string fName = Root(WhichListView()) + Path.DirectorySeparatorChar + selectFile.Text;
                string dName = Root(WhichListView()) + Path.DirectorySeparatorChar + selectFile.Text.Substring(1).Remove(selectFile.Text.Length - 2);
                (new SearchPatternsParallels(new SearchHandler())).Search(fName, dName);
            }
        }

        //Сочетания
        private void Form1_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Enter)
                {
                    if (IsSelected())
                        DoubleClickLV(WhichListView());
                }
                else if (e.Alt && e.KeyCode == Keys.F1)
                {
                    comboBox1.Focus();
                    comboBox1.DroppedDown = true;
                }
                else if (e.Alt && e.KeyCode == Keys.F2)
                {
                    comboBox2.Focus();
                    comboBox2.DroppedDown = true;
                }
                //Pack button
                else if (e.Alt && e.KeyCode == Keys.F5)
                {
                    if (IsSelectedOne())
                    {
                        ArchiveFileSecond file = new ArchiveFileSecond();

                        // A path of file to pack
                        var lw = WhichListView();
                        ListViewItem fileChosen = lw.SelectedItems[0];
                        string path = Root(WhichListView()) + Path.DirectorySeparatorChar + fileChosen.Text;

                        file.Pack(path);
                    }
                }
                else if (e.Alt && e.KeyCode == Keys.F6)
                {
                    if (IsSelectedOne())
                        Unpack();
                }
                else if (e.KeyCode == Keys.F1)
                {
                    Help();
                }
                else if (e.KeyCode == Keys.F2)
                {
                    if (IsSelected())
                        UpdateListView(WhichListView());
                }
                else if (e.KeyCode == Keys.F5)
                {
                    if (IsSelected())
                        IsCopy();
                }
                else if (e.KeyCode == Keys.F6)
                {
                    if (IsSelected())
                        MoveF();
                }
                else if (e.KeyCode == Keys.Delete)
                {
                    if (IsSelected())
                        Delete();
                }
                else if (e.KeyCode == Keys.F7)
                {
                    if (IsSelectedOne())
                        StatisticsTXT();
                }
                else if (e.Alt && e.KeyCode == Keys.Left)
                {
                    if (WhichListView() == listView1)
                    {
                        if (Path.GetDirectoryName(rootLeft) != null)
                            rootLeft = Path.GetDirectoryName(rootLeft);
                        UpdateListView(listView1);
                    }
                    else if (WhichListView() == listView2)
                    {
                        if (Path.GetDirectoryName(rootRight) != null)
                            rootRight = Path.GetDirectoryName(rootRight);
                        UpdateListView(listView2);
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        //Загрузка
        private void downloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowWindDownload();
        }
        //Переместить
        private void move_Click(object sender, EventArgs e)
        {
            if (IsSelected())
                MoveF();
        }
        //Cкопировать
        private void copy_Click(object sender, EventArgs e)
        {
            if (IsSelected())
                IsCopy();
        }
        //Удалить
        private void delete_Click(object sender, EventArgs e)
        {
            if (IsSelected())
                Delete();
        }
        //Обработка кнопки help
        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Help();
        }

        //Обработка кнопки разархивации
        private void unpackToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (IsSelectedOne())
                Unpack();
        }
        //Обработка кнопки options
        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Configuration conf = new Configuration(data.fontColor, data.color1, data.color2, data.fileFont, data.mainFont, data.dialogFont);
            conf.Font = data.dialogFont;
            DialogResult res = conf.ShowDialog();

            if (res == DialogResult.OK)
            {
                data.fontColor = conf.FontColor();
                data.color1 = conf.Color1();
                data.color2 = conf.Color2();
                data.fileFont = conf.FileFont();
                data.mainFont = conf.MainFont();
                data.dialogFont = conf.DialogFont();

                this.Font = data.mainFont;
                listView1.BackColor = data.color1;
                listView2.BackColor = data.color1;
                listView1.Font = data.fileFont;
                listView2.Font = data.fileFont;
                UpdateListView(listView1);
                UpdateListView(listView2);
            }
        }
        //Обработка кнопки поиска файла
        private void searchFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowWindSearch();
        }
        //Обработка кнопки сохранения
        private void saveSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save();
        }
        //Обработка кнопки закрытия
        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Exit();
        }


        //Разархивация
        private void Unpack()
        {
            var lw = WhichListView();
            ListViewItem file = lw.SelectedItems[0];
            if (file.SubItems[1].Text != ".zip")
            {
                MessageBox.Show(
                       "Operation Cancelled: not *.zip file.",
                       "Unpack status",
                       MessageBoxButtons.OK,
                       MessageBoxIcon.Error
                                  );
                return;
            }
            string path = Root(lw) + Path.DirectorySeparatorChar + file.Text + file.SubItems[1].Text;
            ZipFile.ExtractToDirectory(path, path.Substring(0, path.Length - 4));
        }

        //---СТАТИСТИКА ПО ФАЙЛУ---

        private void StatisticsTXT()
        {
            var lw = WhichListView();
            ListViewItem file = lw.SelectedItems[0];

            try
            {
                //Length of top words
                var topWordsLength = 5;

                //Starting work message
                MessageBox.Show(
                       "Starting work...",
                       "File statistics",
                       MessageBoxButtons.OK,
                       MessageBoxIcon.Asterisk
                                       );

                string output = "";
                var lineCount = 0;
                var unicWordCount = 0;

                //Creating a file path
                string filePath = Root(WhichListView()) + Path.DirectorySeparatorChar + file.Text.Substring(0) + ".txt";

                //Initializing timer
                Stopwatch stopwatch = Stopwatch.StartNew();

                byte[] b = File.ReadAllBytes(filePath);


                //Counting the number of lines
                Task taskA = Task.Run(() =>
                {
                    using (var reader = File.OpenText(filePath))
                    {
                        while (reader.ReadLine() != null)
                            lineCount++;
                    }
                    output += "Number of lines: " + lineCount + "\n";
                });

                //Number of words, number of unic words, top 10 words
                Task taskB = Task.Run(() =>
                {
                    //Creating an array of words
                    string textToAnalyse = Encoding.Default.GetString(b).ToLower().Replace(",", "").Replace(".", "").Replace("(", "").Replace(")", "").Replace("-", "");
                    string[] arrayOfWords = textToAnalyse.Split();
                    //Counting the number of words
                    output += "Number of words: " + arrayOfWords.Length + "\n";

                    //Counting the number of unic words
                    unicWordCount = (from word in arrayOfWords.AsParallel() select word).Distinct().Count();
                    output += "Number of unic words: " + unicWordCount + "\n";

                    //Top 10 words
                    var presortedList = arrayOfWords.GroupBy(s => s).Where(g => g.Count() > 1).OrderByDescending(g => g.Count()).Select(g => g.Key).ToList();
                    presortedList.Remove("");
                    var sortedList = (from word in presortedList where word.Length > topWordsLength select word);

                    var topTenWords = sortedList.Take(10);

                    output += "Top ten words with length > " + topWordsLength + ":\n";
                    int i = 1;
                    foreach (var word in topTenWords)
                    {
                        output += i + ") " + word + "\n";
                        i++;
                    }
                });

                //MessageBox
                var finalTask = Task.Factory.ContinueWhenAll(new Task[] { taskA, taskB }, ant =>
                {
                    stopwatch.Stop();
                    output += "Time: " + stopwatch.Elapsed;
                    MessageBox.Show(
                        output,
                        "File statistics",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                                   );
                });

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
