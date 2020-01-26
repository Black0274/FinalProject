using System;
using System.Drawing;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Telegram.Bot;
using Telegram.Bot.Args;
using AIMLbot;
using Telegram.Bot.Types.Enums;

using System.ComponentModel;
using System.Data;
using System.Text;
using System.Threading.Tasks;

using CLIPSNET;


namespace FinalProject
{
    enum Net {
        Accord = 0,
        Dimas
    }

    public partial class Form1 : Form
    {

        private CLIPSNET.Environment clips = new CLIPSNET.Environment();

        private IVideoSource videoSource;
        private FilterInfoCollection videoDevicesList;

        private MagicEye processor = new MagicEye();

        string path_to_sampe_dir = "../../../images/";
        private Net net = Net.Accord;
        static int blockcount = 28;
        static int sensors_count = blockcount * blockcount;
        static int layer_count = sensors_count * 3;
        static int classes_count = 10;
        private double max_error = 0.2;
        private int epochs = 10;
        private Accord.Neuro.ActivationNetwork accord;
        private Network dimas;
        private Accord.Neuro.Learning.ParallelResilientBackpropagationLearning backprog;
        Accord.Neuro.NguyenWidrow nguyen;
        Bitmap image = new Bitmap("../../../patterns/2.jpg");
        public string res_planet = "null"; // распознанная планета

        const string aimlPath = "..//..//dialog.aiml";
        const string token = "1024634275:AAGzK7fmIjb09emUmrYSBapPn73WQH_8U0g";
        static TelegramBotClient bot = new TelegramBotClient(token) { Timeout = TimeSpan.FromSeconds(10) };
        static Bot aiml = new Bot();
        static User user = new User("Dmitry", new Bot());

        public Form1()
        {
            InitializeComponent();

            videoDevicesList = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo videoDevice in videoDevicesList)
            {
                cmbVideoSource.Items.Add(videoDevice.Name);
            }
            if (cmbVideoSource.Items.Count > 0)
            {
                cmbVideoSource.SelectedIndex = 0;
            }

            accord = new Accord.Neuro.ActivationNetwork(new Accord.Neuro.BipolarSigmoidFunction(),
                sensors_count, sensors_count * 3, sensors_count * 2, sensors_count, 100, classes_count);
            backprog = new Accord.Neuro.Learning.ParallelResilientBackpropagationLearning(accord);
            nguyen = new Accord.Neuro.NguyenWidrow(accord);
            nguyen.Randomize();
            int[] arr = { sensors_count * 3, sensors_count * 2, sensors_count, 100, classes_count };
            dimas = new Network(sensors_count, arr);
            comboBox1.SelectedIndex = 0;
            bot_init();

            clips_init();
        }

        //CLIPS
        //------------------------------------------------------------------------

        private void clips_init()
        {
            // читаем файл в строку
            string fileText = System.IO.File.ReadAllText("final.clp");

            clips.Clear();
            clips.LoadFromString(fileText);
            clips.Reset();
        }

        private string HandleResponse()
        {
            string result = "";
            //  Вытаскиаваем факт из ЭС
            String evalStr = "(find-fact ((?f ioproxy)) TRUE)";
            FactAddressValue fv = (FactAddressValue)((MultifieldValue)clips.Eval(evalStr))[0];

            MultifieldValue damf = (MultifieldValue)fv["messages"];
            MultifieldValue vamf = (MultifieldValue)fv["answers"];

            for (int i = 0; i < damf.Count; i++)
            {
                LexemeValue da = (LexemeValue)damf[i];
                byte[] bytes = Encoding.Default.GetBytes(da.Value);
                result += Encoding.UTF8.GetString(bytes) + System.Environment.NewLine;
            }

            clips.Eval("(assert (clearmessage))");

            return result;
        }

        private string clips_get_ans(string planet)
        {
            string result = "";

            string mess = "(assert (planet (plan " + planet + ")))";

            clips.Eval(mess);

            clips.Run();
            result = HandleResponse();

            clips.Reset();

            return result;
        }

        //------------------------------------------------------------------------

        private void bot_init()
        {
            aiml.loadSettings();
            aiml.isAcceptingUserInput = false;
            XmlDocument source = new XmlDocument();
            source.Load(aimlPath);
            aiml.loadAIMLFromXML(source, aimlPath);
            aiml.isAcceptingUserInput = true;
            bot.OnMessage += Bot_OnMessage;
            bot.StartReceiving();
        }

        private async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            string received = e.Message.Text;
            string response = "empty";
            
            if (received == null)   // Обработка изображения (текста нет)
            {
                var test = await bot.GetFileAsync(e.Message.Photo[e.Message.Photo.Count() - 1].FileId);
                Stream stream = await bot.DownloadFileAsync(test.FilePath);
                image = new Bitmap(Bitmap.FromStream(stream));
                refresh();
                button3_Click(null, null);  // Распознавание изображения
                response = "Распознано как: " + res_planet + ".\n" + clips_get_ans(res_planet);
            }
            else {  // Обработка сообщения
                received = received.ToLower();

                if (received == "accord")
                {
                    net = Net.Accord;
                    return;
                }
                if (received == "dimas") { 
                    net = Net.Dimas;
                    return;
                }
                if (received == "пересоздать") { 
                    button1_Click(null, null);
                    return;
                }
                if (received == "обучить")
                {
                    button4_Click(null, null);
                    response = "Сеть обучена";
                }
                else
                {
                    Console.WriteLine(received);
                    response = getOutput(received, aiml);
                }
            }

            // Отправка сообщения
            await bot.SendTextMessageAsync(
                chatId: e.Message.Chat,
                text: response
                ).ConfigureAwait(false);
        }

        private string getOutput(string input, Bot aiml)
        {
            Request r = new Request(input, user, aiml);
            Result res = aiml.Chat(r);
            return (res.Output);
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            bot_init();
        }

        private void refresh()
        {
            processor.ProcessImage(image);
            processor.GetPicture(out Bitmap or, out Bitmap num);
            pictureBox1.Image = image;
            pictureBox2.Image = num;
        }

        void CloseOpenVideoSource()
        {
            if (videoSource == null)
            {
                refresh();
                //videoSource = new VideoCaptureDevice(videoDevicesList[cmbVideoSource.SelectedIndex].MonikerString);
                //videoSource.NewFrame += new NewFrameEventHandler(video_NewFrame);
                //videoSource.Start();
                //btnStart.Text = "Стоп";
            }
            else
            {
                videoSource.SignalToStop();
                videoSource = null;
                btnStart.Text = "Старт";

            }

        }

        // Чтобы вебка не падала в обморок при неожиданном закрытии окна
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (btnStart.Text == "Стоп")
                videoSource.SignalToStop();
            if (videoSource != null && videoSource.IsRunning && pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
            }
            videoSource = null;
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CloseOpenVideoSource();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {           
        }

        // Распознавание образа
        private void button3_Click(object sender, EventArgs e)
        {
            if (net == Net.Accord)
                accord.Compute(imgToData(processor.GetImage()));
            else if (net == Net.Dimas)
                dimas.Compute(imgToData(processor.GetImage()));
            ShowResult();
        }
     
        private void TrainOnOurData()
        {
            List<double[]> input = new List<double[]>();
            List <double[]> output = new List<double[]>();

            /*foreach (var d in Directory.GetDirectories(path_to_sampe_dir))
            {
                int clas = 0;
                string planet = d.Split('/').Last();
                switch (planet)
                {
                    case "Меркурий": clas = 1; break;
                    case "Венера": clas = 2; break;
                    case "Земля": clas = 3; break;
                    case "Марс": clas = 4; break;
                    case "Юпитер": clas = 5; break;
                    case "Сатурн": clas = 6; break;
                    case "Уран": clas = 7; break;
                    case "Нептун": clas = 8; break;
                    case "Плутон": clas = 9; break;
                }*/
                foreach (var file in Directory.GetFiles("../../../heap/"))
                {
                    string path = file.ToString();
                    int clas = path[path.Length - 5] - '0';
                    //System.Diagnostics.Debug.WriteLine(path[path.Length - 5]);
                    //System.IO.File.Move(path, path.Insert(path.Length - 4, clas.ToString()));
                    var img = AForge.Imaging.UnmanagedImage.FromManagedImage(new Bitmap(file));
                    input.Add(imgToData(img));
                    output.Add(new double[classes_count].Select((p, ind) => ind == clas ? 1.0 : 0.0).ToArray());
                }
                
         //   }
           // return;
            if (net == Net.Accord)
                accord.Randomize();
            double error = double.PositiveInfinity;
            var inputArr = input.ToArray();
            var outputArr = output.ToArray();
            int iterations = 0;

            if (net == Net.Accord)
                while (error > max_error && iterations < epochs)
                {
                    error = backprog.RunEpoch(inputArr, outputArr) / input.Count;
                    iterations++;

                }
            else if (net == Net.Dimas)
                while (error > max_error && iterations < epochs)
                {
                    error = dimas.RunEpoch(inputArr, outputArr) / input.Count;
                    iterations++;
                }
        }

        private double[] imgToData(AForge.Imaging.UnmanagedImage img)
        {
            double[] res = new double[img.Width * img.Height];
            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    res[i * img.Width + j] = img.GetPixel(i, j).GetBrightness(); // maybe threshold
                }
            }
            return res;
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox2.SelectedIndex == 0)
                net = Net.Accord;
            else
                net = Net.Dimas;
        }

        private void ShowResult()
        {
            double[] res = { };
            if (net == Net.Accord)
                res = accord.Output;
            else if (net == Net.Dimas)
                res = dimas.getOutput().ToArray();
            int ind = 0;
            double mx = res[0];
            for (int i = 1; i < res.Length; i++)
            {
                if (res[i] > mx)
                {
                    ind = i;
                    mx = res[i];
                }
            }
            string spaces = "          ";
            string message = "Распознано как: " + ((Planet)ind).ToString() + "\n";
            //ResLabel.Text = "Распознано как: " + ((Planet)ind).ToString() + "\n";
            res_planet = ((Planet)ind).ToString();
            for (int i = 0; i < res.Length; i++)
            {
                string planet = ((Planet)i).ToString();
                //ResLabel.Text += planet + ": " + spaces.Substring(planet.Length) + res[i].ToString("F4") + "\n";
                message += planet + ": " + spaces.Substring(planet.Length) + res[i].ToString("F4") + "\n";
            }
            Console.WriteLine(message);
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            epochs = (int)numericUpDown2.Value;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (net == Net.Accord)
                nguyen.Randomize();
            else if (net == Net.Dimas)
            {
                int[] arr = { sensors_count * 3, sensors_count * 2, sensors_count, 100, classes_count };
                dimas = new Network(sensors_count, arr);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //ResLabel.Text = "Выполняется обучение сети,\nподождите...";
            TrainOnOurData();
            return;
           
        }

        private void button5_Click(object sender, EventArgs e)
        {
            DialogResult res =  saveFileDialog1.ShowDialog();
            if(res == DialogResult.OK)
            {
                accord.Save(saveFileDialog1.FileName);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            DialogResult res = openFileDialog1.ShowDialog();
            if(res == DialogResult.OK)
            {
                accord = Accord.Neuro.Network.Load(openFileDialog1.FileName) as Accord.Neuro.ActivationNetwork;

            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            var r = new Random();
            processor.presave();
            processor.Save(path_to_sampe_dir + comboBox1.SelectedItem.ToString()+"\\" + r.Next().ToString() + r.Next().ToString() + ".png");
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if(double.TryParse((sender as TextBox).Text, out double r)){
                max_error = r;
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            learnFromScreen();
            ShowResult();
        }

        private void learnFromScreen()
        {
            double[] inp = imgToData(processor.GetImage());
            int type = comboBox1.SelectedIndex;
            if (net == Net.Accord)
                backprog.Run(inp, new int[classes_count].Select((d, i) => i == type ? 1.0 : 0.0).ToArray());
            else if (net == Net.Dimas)
                dimas.Run(inp, new int[classes_count].Select((d, i) => i == type ? 1.0 : 0.0).ToArray());
        }

        private void Thershold_Click(object sender, EventArgs e)
        {

        }

        private void NetSettingBox_Enter(object sender, EventArgs e)
        {

        }

    }
}
