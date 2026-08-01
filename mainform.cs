using System;
using System.IO;
using System.Linq;
using System.Media;
using System.Drawing;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

//The concept and idea for the APKdevastate application belong entirely to Rafig Zarbaliyev!
//If there is an error in the app or if you have any suggestions for additional features, please message me on Instagram(@rafok2v9c)
//Read this guide to learn how to use APKdevastate https://rafosw.github.io/posts/apkdevastaten/

namespace APKdevastate
{
    public partial class MainForm : Form
    {

        //SoundPlayer player = new SoundPlayer();
        //bool isPlaying = false;

        private Timer countdownTimer;
        private Stopwatch analysisStopwatch;
        private int elapsedSeconds = 0;

        private string selectedApkPath;
        private string nativeLibResult = "";
        private string dynamicLoadersResult = "";
        private string jadxAnalysisResult = "";
        private string protectionSummaryText = "";
        private string activeProtectionView = "summary";
        private string certificateDn = "";
        private bool signatureVerified = false;
        private bool encryptionEvidenceFound = false;
        private bool analysisRunning = false;
        private Color analysisResultColor = Color.Black;
        private guideform guideFormInstance = null;
        private aboutform aboutFormInstance = null;

        private const long maxScanFileSize = 64L * 1024 * 1024;
        private const int processTimeoutMs = 600000;
        private static readonly object trustedOrgsLock = new object();
        private static readonly Encoding scanEncoding = Encoding.GetEncoding(28591);

        private static Dictionary<string, string[]> trustedOrgsCache = null;

        public MainForm(string apkFilePath)
        {
            InitializeComponent();

            countdownTimer = new Timer();
            countdownTimer.Interval = 1000;
            countdownTimer.Tick += CountdownTimer_Tick;
            analysisStopwatch = new Stopwatch();

            //guna2ShadowPanel1.Visible = false;
            //textBoxmd5.Visible = false;
            //textBoxsha1.Visible = false;
            //textBoxsha256.Visible = false;
            //richtextboxprotectet.Visible = false;
            //mainRichTexbox.Visible = false;
            label5.Visible = false;
            richTextBoxanaliz.ReadOnly = true;
            analizinaltindakibutton.Visible = false;
            labelalertpayload.Visible = false;
            pictureBoxredandro.Visible = false;
            richtextboxapktoolyml.WordWrap = false;
            richTextBoxlog.ReadOnly = true;
            richtextboxapktoolyml.ReadOnly = true;
            richtextboxapksays.ReadOnly = true;
            textBoxmd5.ReadOnly = true;
            textBoxsha1.ReadOnly = true;
            textBoxsha256.ReadOnly = true;
            textboxalert.ReadOnly = true;
            richtextboxprotectet.ReadOnly = true;
            richtextboxcert.ReadOnly = true;
            mainRichTexbox.ReadOnly = true;
            button1.Visible = false;
            dynamicloaderbutton.Visible = false;
            selectedApkPath = apkFilePath;

            /*string mahniYolu = Path.Combine(Application.StartupPath, "resources", "music", "aphextwin.wav");
            if (File.Exists(mahniYolu))
            {
                player = new SoundPlayer(mahniYolu);
            }
            else
            {
                
            }
            */
            string fileName = Path.GetFileName(apkFilePath);

            apknamelabel.Text = fileName;
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            elapsedSeconds++;

            int minutes = elapsedSeconds / 60;
            int seconds = elapsedSeconds % 60;
            richTextBoxanaliz.Text = $"{minutes:D2}:{seconds:D2}";
        }


        /* private readonly string[] tehlukelipermissionlar = new string[]
{
    "android.permission.READ_SMS",
    "android.permission.SEND_SMS",
    "android.permission.RECEIVE_SMS",
    "android.permission.RECORD_AUDIO",
    "android.permission.CAMERA",
    "android.permission.READ_CONTACTS",
    "android.permission.WRITE_CONTACTS",
    "android.permission.ACCESS_FINE_LOCATION",
    "android.permission.ACCESS_COARSE_LOCATION",
    "android.permission.READ_PHONE_STATE",
    "android.permission.CALL_PHONE"
};
        */
        private sealed class ProcessResult
        {
            public string StandardOutput = "";
            public string StandardError = "";
            public int ExitCode = -1;
            public bool TimedOut = false;
            public bool Started = false;

            public string Combined
            {
                get { return StandardOutput + Environment.NewLine + StandardError; }
            }
        }

        private void SafeInvoke(Action action)
        {
            if (action == null)
                return;

            try
            {
                if (IsDisposed || Disposing || !IsHandleCreated)
                    return;

                if (InvokeRequired)
                    Invoke(action);
                else
                    action();
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }

        private void SetLog(string message)
        {
            SafeInvoke(() =>
            {
                richTextBoxlog.Clear();
                richTextBoxlog.AppendText(message);
            });
        }

        private void SetProgress(int value)
        {
            SafeInvoke(() =>
            {
                if (value < allprosessbar.Minimum) value = allprosessbar.Minimum;
                if (value > allprosessbar.Maximum) value = allprosessbar.Maximum;
                allprosessbar.Value = value;
            });
        }

        private static bool ContainsIgnoreCase(string source, string value)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value))
                return false;

            return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ContainsToken(string source, string token)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(token))
                return false;

            string pattern = Regex.Escape(token);

            if (IsTokenChar(token[0]))
                pattern = @"(?<![A-Za-z0-9_])" + pattern;

            if (IsTokenChar(token[token.Length - 1]))
                pattern = pattern + @"(?![A-Za-z0-9_])";

            try
            {
                return Regex.IsMatch(source, pattern);
            }
            catch (ArgumentException)
            {
                return source.IndexOf(token, StringComparison.Ordinal) >= 0;
            }
        }

        private static bool IsTokenChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        private static string ReadFileForScan(string path)
        {
            try
            {
                FileInfo info = new FileInfo(path);
                if (!info.Exists || info.Length == 0 || info.Length > maxScanFileSize)
                    return null;

                return scanEncoding.GetString(File.ReadAllBytes(path));
            }
            catch
            {
                return null;
            }
        }

        private static List<string> GetDnFieldValues(string dn, string[] fieldNames)
        {
            List<string> values = new List<string>();

            if (string.IsNullOrEmpty(dn) || fieldNames == null)
                return values;

            const string anyField = @"(?:cn|ou|o|l|st|c|dc|uid|street|serialnumber|emailaddress)";

            foreach (var field in fieldNames)
            {
                string pattern = $@"(?:^|,)\s*{Regex.Escape(field)}\s*=\s*(.*?)(?=(?<!\\),\s*{anyField}\s*=|$)";

                foreach (Match m in Regex.Matches(dn, pattern, RegexOptions.IgnoreCase))
                {
                    string value = m.Groups[1].Value.Replace("\\,", ",").Trim();
                    if (value.Length > 0)
                        values.Add(value);
                }
            }

            return values;
        }

        private static bool DnValuesContainToken(List<string> values, string token)
        {
            if (values == null || values.Count == 0 || string.IsNullOrEmpty(token))
                return false;

            string pattern = $@"(^|[^A-Za-z0-9]){Regex.Escape(token)}([^A-Za-z0-9]|$)";

            foreach (var value in values)
            {
                if (Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase))
                    return true;
            }

            return false;
        }

        private ProcessResult RunProcessDetailed(string exePath, string arguments, string workingDirectory)
        {
            var result = new ProcessResult();
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            try
            {
                using (var process = new System.Diagnostics.Process())
                using (var outputDone = new System.Threading.AutoResetEvent(false))
                using (var errorDone = new System.Threading.AutoResetEvent(false))
                {
                    process.StartInfo.FileName = exePath;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;

                    if (!string.IsNullOrEmpty(workingDirectory) && Directory.Exists(workingDirectory))
                        process.StartInfo.WorkingDirectory = workingDirectory;

                    process.OutputDataReceived += (s, e) =>
                    {
                        if (e.Data == null)
                        {
                            try { outputDone.Set(); } catch { }
                        }
                        else
                        {
                            lock (stdout) stdout.AppendLine(e.Data);
                        }
                    };

                    process.ErrorDataReceived += (s, e) =>
                    {
                        if (e.Data == null)
                        {
                            try { errorDone.Set(); } catch { }
                        }
                        else
                        {
                            lock (stderr) stderr.AppendLine(e.Data);
                        }
                    };

                    process.Start();
                    result.Started = true;

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    if (process.WaitForExit(processTimeoutMs) && outputDone.WaitOne(10000) && errorDone.WaitOne(10000))
                    {
                        process.WaitForExit();
                        result.ExitCode = process.ExitCode;
                    }
                    else
                    {
                        result.TimedOut = true;

                        try
                        {
                            if (!process.HasExited)
                                process.Kill();
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Started = false;
                lock (stderr) stderr.AppendLine(ex.Message);
            }

            lock (stdout) result.StandardOutput = stdout.ToString();
            lock (stderr) result.StandardError = stderr.ToString();

            return result;
        }

        private string RunProcess(string exePath, string arguments)
        {
            return RunProcessDetailed(exePath, arguments, null).StandardOutput;
        }

        private string GetMatch(string input, string pattern)
        {
            var match = Regex.Match(input, pattern);
            return match.Success ? match.Groups[1].Value : "null";
        }
  
        private string DecodeBase64(string base64String)
        {
            byte[] data = Convert.FromBase64String(base64String);
            return Encoding.UTF8.GetString(data);
        }

        private Dictionary<string, string[]> LoadTrustedOrganizations()
        {
            lock (trustedOrgsLock)
            {
                if (trustedOrgsCache != null)
                    return trustedOrgsCache;

                var loaded = new Dictionary<string, string[]>();

                try
                {
                    string jsonPath = Path.Combine(Application.StartupPath, "Resources", "certifications.json");

                    if (!File.Exists(jsonPath))
                        return loaded;

                    string jsonContent = File.ReadAllText(jsonPath);

                    var orgsMatch = Regex.Match(
                        jsonContent,
                        @"""trustedOrganizations""\s*:\s*\{((?:[^{}]|(?<open>\{)|(?<-open>\}))*(?(open)(?!)))\}",
                        RegexOptions.Singleline);

                    if (!orgsMatch.Success)
                        return loaded;

                    string orgsContent = orgsMatch.Groups[1].Value;

                    var orgMatches = Regex.Matches(orgsContent, @"""([^""]+)""\s*:\s*\[([^\]]*)\]", RegexOptions.Singleline);

                    foreach (Match match in orgMatches)
                    {
                        string key = match.Groups[1].Value;
                        string valuesStr = match.Groups[2].Value;

                        var valueMatches = Regex.Matches(valuesStr, @"""([^""]+)""");
                        var values = new List<string>(valueMatches.Count);

                        for (int i = 0; i < valueMatches.Count; i++)
                        {
                            string value = valueMatches[i].Groups[1].Value.Trim();
                            if (value.Length > 0)
                                values.Add(value);
                        }

                        if (values.Count > 0)
                            loaded[key] = values.ToArray();
                    }
                }
                catch
                {
                    return loaded;
                }

                if (loaded.Count > 0)
                    trustedOrgsCache = loaded;

                return loaded;
            }
        }

        private string AnalyzeDynamicLoaders(string tempPath)
        {
            StringBuilder result = new StringBuilder();
            
            try
            {
                bool isProtected = false;
                List<string> detectedLoaders = new List<string>();
                object lockObj = new object();
                
                string[] loaderNames = new string[]
                {
                    "DexClassLoader",
                    "InMemoryDexClassLoader",
                    "BaseDexClassLoader",
                    "SecureClassLoader",
                    "DelegateLastClassLoader",
                    "MultiDex",
                    "loadDex",
                    "defineClass",
                    "Ldalvik/system/DexClassLoader",
                    "Ldalvik/system/InMemoryDexClassLoader",
                    "Ldalvik/system/BaseDexClassLoader"
                };
                
                var smaliFiles = Directory.GetFiles(tempPath, "*.smali", SearchOption.AllDirectories);
                
                if (smaliFiles.Length == 0)
                {
                    result.AppendLine("Cannot detect dynamic loaders");
                    return result.ToString();
                }
                
                int checkedFiles = 0;

                var searchNames = loaderNames
                    .Select(n => n.StartsWith("Ldalvik/system/", StringComparison.Ordinal)
                        ? n.Substring("Ldalvik/system/".Length)
                        : n)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();

                Parallel.ForEach(smaliFiles, (file) =>
                {
                    System.Threading.Interlocked.Increment(ref checkedFiles);

                    string fileName = Path.GetFileNameWithoutExtension(file);
                    if (fileName.Length > 100)
                    {
                        lock (lockObj) isProtected = true;
                    }

                    string content = ReadFileForScan(file);
                    if (content == null)
                        return;

                    foreach (var loader in searchNames)
                    {
                        bool alreadyFound;
                        lock (lockObj) alreadyFound = detectedLoaders.Contains(loader);

                        if (!alreadyFound && ContainsToken(content, loader))
                        {
                            lock (lockObj)
                            {
                                if (!detectedLoaders.Contains(loader))
                                    detectedLoaders.Add(loader);
                            }
                        }
                    }
                });

                detectedLoaders.Sort(StringComparer.Ordinal);

                if (detectedLoaders.Count == 0)
                {
                    result.AppendLine("No dynamic loaders detected");
                    result.AppendLine("This APK uses standard loading methods");
                }
                else
                {
                    result.AppendLine($"{detectedLoaders.Count} dynamic loaders detected:");
                    result.AppendLine("");
                    
                    foreach (var loader in detectedLoaders)
                    {
                        result.AppendLine($"  • {loader}");
                    }
                    
                    result.AppendLine("");
                    
                    if (isProtected)
                    {
                        result.AppendLine("APK appears to be obfuscated or packed");
                        result.AppendLine("(Long smali file names detected)");
                    }
                }
                
                result.AppendLine($"Scanned {checkedFiles} smali files");
            }
            catch (Exception ex)
            {
                result.AppendLine("Error during loader detection");
                result.AppendLine($"Details: {ex.Message}");
            }
            
            return result.ToString();
        }

        private string AnalyzeNativeLibraries(string tempPath)
        {
            StringBuilder result = new StringBuilder();
            
            try
            {
                string libPath = Path.Combine(tempPath, "lib");
                
                if (!Directory.Exists(libPath))
                {
                    result.AppendLine("No native libraries found in this APK.");
                    return result.ToString();
                }

                var architectures = Directory.GetDirectories(libPath);
                List<string> allSoFiles = new List<string>();
                Dictionary<string, List<string>> archLibs = new Dictionary<string, List<string>>();
                List<string> suspiciousSoFiles = new List<string>();

                string[] suspiciousKeywords = { 
                    "encrypt",
                    "obfus",
                    "hook",
                    "inject",
                    "hide",
                    "root",
                    "xposed",
                    "frida",
                    "native",
                    "payload",
                    "shell",
                    "backdoor"
                };

                object lockObj = new object();

                var scanTargets = new List<string>(architectures);

                Parallel.ForEach(scanTargets, (archDir) =>
                {
                    string archName = Path.GetFileName(archDir);

                    string[] soFiles;
                    try
                    {
                        soFiles = Directory.GetFiles(archDir, "*.so", SearchOption.AllDirectories);
                    }
                    catch
                    {
                        return;
                    }

                    List<string> soPaths = new List<string>();
                    foreach (var soFile in soFiles)
                    {
                        string soName = Path.GetFileName(soFile);
                        soPaths.Add(soFile);

                        lock (lockObj)
                        {
                            allSoFiles.Add(soName);
                        }

                        string soNameLower = soName.ToLower();
                        foreach (var keyword in suspiciousKeywords)
                        {
                            if (soNameLower.Contains(keyword))
                            {
                                lock (lockObj)
                                {
                                    if (!suspiciousSoFiles.Contains(soName))
                                    {
                                        suspiciousSoFiles.Add(soName);
                                    }
                                }
                            }
                        }
                    }

                    lock (lockObj)
                    {
                        archLibs[archName] = soPaths;
                    }
                });

                var rootSoFiles = Directory.GetFiles(libPath, "*.so", SearchOption.TopDirectoryOnly);
                if (rootSoFiles.Length > 0)
                {
                    List<string> rootPaths = new List<string>();
                    foreach (var soFile in rootSoFiles)
                    {
                        string soName = Path.GetFileName(soFile);
                        rootPaths.Add(soFile);
                        allSoFiles.Add(soName);

                        string soNameLower = soName.ToLower();
                        foreach (var keyword in suspiciousKeywords)
                        {
                            if (soNameLower.Contains(keyword) && !suspiciousSoFiles.Contains(soName))
                            {
                                suspiciousSoFiles.Add(soName);
                            }
                        }
                    }

                    archLibs["lib"] = rootPaths;
                }

          
                //result.AppendLine("Native Library Analysis Results");
                //result.AppendLine("");
                result.AppendLine($"Native Libraries Found: {allSoFiles.Distinct().Count()}");
                result.AppendLine($"Architectures: {architectures.Length}\n");

                foreach (var arch in archLibs)
                {
                    result.AppendLine($"[{arch.Key}] - {arch.Value.Count} files");
                    foreach (var libFullPath in arch.Value)
                    {
                        string lib = Path.GetFileName(libFullPath);

                        try
                        {
                            FileInfo fileInfo = new FileInfo(libFullPath);
                            if (fileInfo.Exists)
                            {
                                double sizeKB = fileInfo.Length / 1024.0;
                                result.AppendLine($"   - {lib} ({sizeKB:F1} KB)");
                            }
                            else
                            {
                                result.AppendLine($"   - {lib}");
                            }
                        }
                        catch
                        {
                            result.AppendLine($"   - {lib}");
                        }
                    }
                    result.AppendLine();
                }

                if (suspiciousSoFiles.Count > 0)
                {
                    result.AppendLine($"WARNING: Suspicious Native Libraries Detected: {suspiciousSoFiles.Count}");
                    foreach (var suspiciousSo in suspiciousSoFiles)
                    {
                        result.AppendLine($"   - {suspiciousSo}");
                    }
                    result.AppendLine();
                }

               // result.AppendLine("Analysis completed succccessfully.");
            }
            catch (Exception ex)
            {
                result.AppendLine($"Error: {ex.Message}");
            }

            return result.ToString();
        }

        private void ClearTempFolder(string tempPath)
        {
            try
            {
                foreach (var file in Directory.GetFiles(tempPath))
                {
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                    }
                    catch { }
                }

                foreach (var dir in Directory.GetDirectories(tempPath))
                {
                    try
                    {
                        DirectoryInfo di = new DirectoryInfo(dir);
                        di.Attributes = FileAttributes.Normal;
                        di.Delete(true);
                    }
                    catch { }
                }
            }
            catch
            {

            }
        }

              //SIFRELI APK ANALIZI
        private string AnalyzeWithJadx(string apkPath, string tempPath)
        {
            StringBuilder result = new StringBuilder();
            
            try
            {
                SetLog("Jadx analyzing encrypted APK...");

                string resourcesPath = Path.Combine(Application.StartupPath, "resources");
                string jadxBatPath = Path.Combine(resourcesPath, "jadx.bat");
                string jadxExePath = Path.Combine(resourcesPath, "bin", "jadx.bat");
                
                string jadxPath = "";
                if (File.Exists(jadxBatPath))
                {
                    jadxPath = jadxBatPath;
                }
                else if (File.Exists(jadxExePath))
                {
                    jadxPath = jadxExePath;
                }
                else
                {
                    result.AppendLine("Jadx tool not found - cannot analyze encrypted APK");
                    return result.ToString();
                }

                string jadxOutputPath = Path.Combine(tempPath, "jadx_out");
                if (Directory.Exists(jadxOutputPath))
                {
                    Directory.Delete(jadxOutputPath, true);
                }

                string jadxArgs = $"-d \"{jadxOutputPath}\" \"{apkPath}\"";

                var run = RunProcessDetailed(jadxPath, jadxArgs, resourcesPath);

                if (!run.Started)
                {
                    result.AppendLine("Jadx could not be started - deep analysis unavailable");
                    return result.ToString();
                }

                if (run.TimedOut)
                {
                    result.AppendLine("Jadx timed out - deep analysis incomplete");
                    return result.ToString();
                }

                string sourcesPath = Path.Combine(jadxOutputPath, "sources");
                bool decompiled = Directory.Exists(sourcesPath) &&
                                  Directory.GetFiles(sourcesPath, "*.java", SearchOption.AllDirectories).Length > 0;

                if (!Directory.Exists(jadxOutputPath) || !decompiled)
                {
                    result.AppendLine("APK could not be decompiled - heavily encrypted");
                    encryptionEvidenceFound = true;
                    return result.ToString();
                }

                result.AppendLine("APK is encrypted or packed");
                encryptionEvidenceFound = true;

                if (Directory.Exists(sourcesPath))
                {
                    var javaFiles = Directory.GetFiles(sourcesPath, "*.java", SearchOption.AllDirectories);
                    result.AppendLine($"Decompiled Java files: {javaFiles.Length}");

                    List<string> loadedLibraries = new List<string>();
                    List<string> suspiciousPatterns = new List<string>();
                    List<string> assetOperations = new List<string>();
                    object lockObj = new object();

                    string[] searchPatterns = new string[]
                    {
                        "System.loadLibrary",
                        "DexClassLoader",
                        "PathClassLoader",
                        "InMemoryDexClassLoader",
                        "getAssets",
                        "AAssetManager",
                        "Cipher",
                        "SecretKeySpec",
                        "IvParameterSpec"
                    };

                    Parallel.ForEach(javaFiles, (javaFile) =>
                    {
                        try
                        {
                            string content = ReadFileForScan(javaFile);
                            if (content == null)
                                return;

                            foreach (var pattern in searchPatterns)
                            {
                                if (content.Contains(pattern))
                                {
                                    if (pattern == "System.loadLibrary")
                                    {
                                        var matches = Regex.Matches(content, @"System\.loadLibrary\([""']([^""']+)[""']\)");
                                        foreach (Match match in matches)
                                        {
                                            string libName = match.Groups[1].Value;
                                            lock (lockObj)
                                            {
                                                if (!loadedLibraries.Contains(libName))
                                                {
                                                    loadedLibraries.Add(libName);
                                                }
                                            }
                                        }
                                    }
                                    else if (pattern == "getAssets" || pattern == "AAssetManager")
                                    {
                                        string fileName = Path.GetFileName(javaFile);
                                        lock (lockObj)
                                        {
                                            if (!assetOperations.Contains(fileName))
                                            {
                                                assetOperations.Add(fileName);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        lock (lockObj)
                                        {
                                            if (!suspiciousPatterns.Contains(pattern))
                                            {
                                                suspiciousPatterns.Add(pattern);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch { /* paralel dongu */ }
                    });

                    if (loadedLibraries.Count > 0)
                    {
                        result.AppendLine("Native libraries loaded:");
                        foreach (var lib in loadedLibraries)
                        {
                            result.AppendLine($"  lib{lib}.so");
                        }
                    }

                    if (suspiciousPatterns.Count > 0)
                    {
                        result.AppendLine("Detected techniques:");
                        foreach (var pattern in suspiciousPatterns)
                        {
                            result.AppendLine($"  {pattern}");
                        }
                    }

                    if (assetOperations.Count > 0)
                    {
                        result.AppendLine("Asset operations detected in:");
                        foreach (var file in assetOperations.Take(5))
                        {
                            result.AppendLine($"  {file}");
                        }
                        if (assetOperations.Count > 5)
                        {
                            result.AppendLine($"  ... and {assetOperations.Count - 5} more files");
                        }
                    }
                }

                string libPath = Path.Combine(jadxOutputPath, "resources", "lib");
                if (!Directory.Exists(libPath))
                {
                    using (ZipArchive archive = ZipFile.OpenRead(apkPath))
                    {
                        var libEntries = archive.Entries.Where(e => e.FullName.StartsWith("lib/") && e.Name.EndsWith(".so"));
                        
                        if (libEntries.Any())
                        {
                            result.AppendLine("Native libraries in APK:");
                            Dictionary<string, List<string>> archLibs = new Dictionary<string, List<string>>();
                            
                            foreach (var entry in libEntries)
                            {
                                string[] parts = entry.FullName.Split('/');
                                if (parts.Length >= 3)
                                {
                                    string arch = parts[1];
                                    string libName = parts[2];
                                    
                                    if (!archLibs.ContainsKey(arch))
                                    {
                                        archLibs[arch] = new List<string>();
                                    }
                                    archLibs[arch].Add(libName);
                                }
                            }
                            
                            foreach (var arch in archLibs)
                            {
                                result.AppendLine($"  [{arch.Key}]");
                                foreach (var lib in arch.Value)
                                {
                                    result.AppendLine($"    {lib}");
                                }
                            }
                        }
                    }
                }

                result.AppendLine("Analysis completed");
            }
            catch (Exception ex)
            {
                result.AppendLine($"Jadx analysis error: {ex.Message}");
            }

            return result.ToString();
        }

        private string AnalyzeWithStrings(string apkPath, string tempPath)
        {
            StringBuilder result = new StringBuilder();
            
            try
            {
                result.AppendLine("Deep string analysis:");
                
                string resourcesPath = Path.Combine(Application.StartupPath, "resources");
                string stringsExePath = Path.Combine(resourcesPath, "strings.exe");
                
                if (!File.Exists(stringsExePath))
                {
                    result.AppendLine("String analysis tool not available");
                    return result.ToString();
                }

                Dictionary<string, string> packerSignatures = new Dictionary<string, string>
                {
                    {"libjiagu", "Qihoo 360 Jiagu Packer"},
                    {"ijiagu", "Qihoo 360 Jiagu"},
                    {"libDexHelper", "SecNeo Packer"},
                    {"alibaba", "Alibaba Packer"},
                    {"qihoo", "Qihoo 360"},
                    {"baidu", "Baidu Packer"},
                    {"tencent", "Tencent Legu"},
                    {"StubApplication", "Tencent Packer"},
                    {"bangcle", "Bangcle Packer"},
                    {"SecShell", "SecNeo/Bangcle Packer"},
                    {"libsecexe", "Bangcle"},
                    {"DexGuard", "DexGuard Obfuscator"},
                    {"ProGuard", "ProGuard Obfuscator"},
                    {"Allatori", "Allatori Obfuscator"},
                //    {"libexec.so", "Native Packer"},
                //    {"libexecmain.so", "Native Encryption"}
                };

                List<string> detectedPackers = new List<string>();
                List<string> antiAnalysis = new List<string>();

                string tempDexPath = Path.Combine(tempPath, "strings_analysis");

                if (Directory.Exists(tempDexPath))
                {
                    Directory.Delete(tempDexPath, true);
                }

                Directory.CreateDirectory(tempDexPath);

                try
                {
                using (ZipArchive archive = ZipFile.OpenRead(apkPath))
                {
                    var dexEntries = archive.Entries.Where(e => e.Name.EndsWith(".dex")).ToList();

                    if (dexEntries.Count == 0)
                    {
                        result.AppendLine("No DEX files found - APK is heavily protected");
                        encryptionEvidenceFound = true;
                        return result.ToString();
                    }

                    int extractedDexCount = 0;
                    foreach (var dexEntry in dexEntries)
                    {
                        extractedDexCount++;
                        string extractedDex = Path.Combine(tempDexPath, extractedDexCount + "_" + dexEntry.Name);

                        if (File.Exists(extractedDex))
                        {
                            File.Delete(extractedDex);
                        }

                        dexEntry.ExtractToFile(extractedDex, false);

                        var run = RunProcessDetailed(stringsExePath, $"-n 8 \"{extractedDex}\"", null);
                        string stringsOutput = run.StandardOutput;

                        foreach (var signature in packerSignatures)
                        {
                            if (ContainsToken(stringsOutput, signature.Key) && !detectedPackers.Contains(signature.Value))
                            {
                                detectedPackers.Add(signature.Value);
                            }
                        }

                        string[] antiAnalysisPatterns = {
                            "isDebuggerConnected", "Debug.isDebuggerConnected", 
                            "/proc/self/status", "TracerPid",
                            "android.os.Debug", "getRuntime().exec",
                            "su", "/system/xbin/su", "/system/bin/su",
                            "Xposed", "de.robv.android.xposed",
                            "frida", "fridaserver"
                        };
                        
                        foreach (var pattern in antiAnalysisPatterns)
                        {
                            if (ContainsToken(stringsOutput, pattern) && !antiAnalysis.Contains(pattern))
                            {
                                antiAnalysis.Add(pattern);
                            }
                        }
                    }

                    var soEntries = archive.Entries.Where(e => e.Name.EndsWith(".so"));
                    foreach (var soEntry in soEntries)
                    {
                        string uniqueSoName = soEntry.FullName.Replace("/", "_").Replace("\\", "_");
                        string extractedSo = Path.Combine(tempDexPath, uniqueSoName);

                        if (File.Exists(extractedSo))
                        {
                            continue;
                        }

                        soEntry.ExtractToFile(extractedSo, false);

                        var soRun = RunProcessDetailed(stringsExePath, $"-n 6 \"{extractedSo}\"", null);
                        string soStringsOutput = soRun.StandardOutput;

                        foreach (var signature in packerSignatures)
                        {
                            if (ContainsToken(soStringsOutput, signature.Key) && !detectedPackers.Contains(signature.Value))
                            {
                                detectedPackers.Add(signature.Value);
                            }
                        }
                    }
                }

                if (detectedPackers.Count > 0)
                {
                    result.AppendLine($"Packer/Protector: {string.Join(", ", detectedPackers)}");
                    encryptionEvidenceFound = true;
                }

                if (antiAnalysis.Count > 0)
                {
                    result.AppendLine($"Anti-analysis: {string.Join(", ", antiAnalysis.Take(3))}");
                    if (antiAnalysis.Count > 3)
                    {
                        result.AppendLine($"  ... and {antiAnalysis.Count - 3} more techniques");
                    }
                }
                }
                finally
                {
                    try
                    {
                        if (Directory.Exists(tempDexPath))
                            Directory.Delete(tempDexPath, true);
                    }
                    catch { }
                }
            }
            catch (Exception)
            {
                result.AppendLine("String analysis could not be completed");
            }

            return result.ToString();
        }

        private string AnalyzeWithDex2Jar(string apkPath, string tempPath)
        {
            StringBuilder result = new StringBuilder();
            
            try
            {
                string resourcesPath = Path.Combine(Application.StartupPath, "resources");
                string dex2jarPath = Path.Combine(resourcesPath, "d2j-dex2jar.bat");
                
                result.AppendLine("Dex2Jar analysis:");
                
                if (!File.Exists(dex2jarPath))
                {
                    result.AppendLine("Dex2Jar tool not available");
                    return result.ToString();
                }

                string dex2jarOutput = Path.Combine(tempPath, "dex2jar_out");
                
                if (Directory.Exists(dex2jarOutput))
                {
                    Directory.Delete(dex2jarOutput, true);
                }
                
                Directory.CreateDirectory(dex2jarOutput);

                var run = RunProcessDetailed(
                    dex2jarPath,
                    $"-f -o \"{Path.Combine(dex2jarOutput, "classes.jar")}\" \"{apkPath}\"",
                    resourcesPath);

                string dex2jarStderr = run.StandardError;

                if (run.TimedOut)
                {
                    result.AppendLine("Dex2Jar timed out");
                    return result.ToString();
                }

                if (!run.Started)
                {
                    result.AppendLine("Dex2Jar could not be started");
                    return result.ToString();
                }

                if (run.ExitCode == 0)
                {
                    string jarPath = Path.Combine(dex2jarOutput, "classes.jar");
                    if (File.Exists(jarPath))
                    {
                        FileInfo jarInfo = new FileInfo(jarPath);
                        result.AppendLine($"Successfully converted to JAR: {jarInfo.Length / 1024} KB");
                        
                        using (ZipArchive jarArchive = ZipFile.OpenRead(jarPath))
                        {
                            int classCount = jarArchive.Entries.Count(e => e.Name.EndsWith(".class"));
                            result.AppendLine($"Class files extracted: {classCount}");
                            
                            var suspiciousClasses = jarArchive.Entries
                                .Where(e => e.Name.EndsWith(".class"))
                                .Select(e => e.FullName)
                                .Where(n => n.Contains("$") || n.Length > 50)
                                .Take(5);
                            
                            if (suspiciousClasses.Any())
                            {
                                result.AppendLine("Obfuscated classes detected:");
                                foreach (var cls in suspiciousClasses)
                                {
                                    result.AppendLine($"  {cls}");
                                }
                            }
                        }
                    }
                }
                else
                {
                    result.AppendLine("Conversion failed - APK may be heavily protected");
                    if (!string.IsNullOrEmpty(dex2jarStderr))
                    {
                        if (ContainsIgnoreCase(dex2jarStderr, "decrypt") || ContainsIgnoreCase(dex2jarStderr, "encrypt"))
                        {
                            result.AppendLine("DEX encryption detected");
                            encryptionEvidenceFound = true;
                        }
                    }
                }
            }
            catch (Exception)
            {
                result.AppendLine("Dex2Jar analysis could not be completed");
            }

            return result.ToString();
        }

        private string GetCertificateInfo(string apkPath)
        {
            SetLog("Apksigner working...");

            signatureVerified = false;
            certificateDn = "";

            string resourcesPath = Path.Combine(Application.StartupPath, "resources");
            string apksignerPath = Path.Combine(resourcesPath, "apksigner.jar");

            if (!File.Exists(apksignerPath))
                return "Certificate could not be checked (apksigner.jar not found).";

            string javaPath = "java";
            string arguments = $"-jar \"{apksignerPath}\" verify --print-certs \"{apkPath}\"";

            var run = RunProcessDetailed(javaPath, arguments, resourcesPath);

            if (!run.Started)
                return "Certificate could not be checked (java could not be started).";

            if (run.TimedOut)
                return "Certificate could not be checked (apksigner timed out).";

            string output = run.Combined;

            var match = Regex.Match(output, @"Signer #1 certificate DN:\s*(.+)");
            if (!match.Success)
                return "Certificate info not found.";

            certificateDn = match.Groups[1].Value.Trim();
            signatureVerified = run.ExitCode == 0;

            if (!signatureVerified)
                return certificateDn + Environment.NewLine + "WARNING: APK signature did NOT verify.";

            return certificateDn;
        }

       // problemli ve ustunde istenilmesi lazim olan kod parcasi
       // private bool CheckPlayStoreAvailability(string packageName)
       // {
       //     try
       //     {
       //         string playStoreUrl = $"https://play.google.com/store/apps/details?id={packageName}";
       //         
       //         HttpWebRequest request = (HttpWebRequest)WebRequest.Create(playStoreUrl);
       //         request.Method = "HEAD";
       //          request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
       //        request.Timeout = 5000;
       //         request.AllowAutoRedirect = false;
       //         
       //         using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
       //         {
       //             return response.StatusCode == HttpStatusCode.OK;
       //         }
       //     }
       //     catch (WebException ex)
       //     {
       //         if (ex.Response is HttpWebResponse errorResponse)
       //         {
       //             return errorResponse.StatusCode == HttpStatusCode.OK;
       //         }
       //         return false;
       //     }
       //     catch
       //     {
       //         return false;
       //     }
       // }

        private async void analizbutton_Click(object sender, EventArgs e)
        {
            if (analysisRunning)
                return;

            analizbutton.Visible = false;
            analizinaltindakibutton.Visible = true;

            string jsonCheckPath = Path.Combine(Application.StartupPath, "Resources", "certifications.json");
            if (!File.Exists(jsonCheckPath))
            {
               MessageBox.Show("certifications.json not found in Resources folder! Analysis cannot start.", "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
               analizbutton.Visible = true;
               analizinaltindakibutton.Visible = false;
               return;
            }

            if (string.IsNullOrEmpty(selectedApkPath) || !File.Exists(selectedApkPath))
            {
                MessageBox.Show("The selected APK file could not be found.", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                analizbutton.Visible = true;
                analizinaltindakibutton.Visible = false;
                return;
            }

            string resourcesPath = Path.Combine(Application.StartupPath, "resources");
            string aaptPath = Path.Combine(resourcesPath, "aapt.exe");
            string apktoolPath = Path.Combine(resourcesPath, "apktool.jar");
            string tempPath = Path.Combine(Application.StartupPath, "temp");

            if (!File.Exists(aaptPath) || !File.Exists(apktoolPath))
            {
                MessageBox.Show("aapt.exe or apktool.jar not found in the resources folder! Analysis cannot start.", "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                analizbutton.Visible = true;
                analizinaltindakibutton.Visible = false;
                return;
            }

            analysisRunning = true;
            newapkButton.Enabled = false;

            elapsedSeconds = 0;
            richTextBoxanaliz.Text = "00:00";
            countdownTimer.Start();
            analysisStopwatch.Restart();

            textboxalert.Text = "Waiting...";
            allprosessbar.Value = 0;
            allprosessbar.Visible = true;

            jadxAnalysisResult = "";
            nativeLibResult = "";
            dynamicLoadersResult = "";
            protectionSummaryText = "";
            activeProtectionView = "summary";
            certificateDn = "";
            signatureVerified = false;
            encryptionEvidenceFound = false;
            analysisResultColor = Color.Black;

            string analysisError = null;

            try
            {
            await Task.Run(() =>
            {
                if (Directory.Exists(tempPath))
                {
                    ClearTempFolder(tempPath);
                }
                else
                {
                    Directory.CreateDirectory(tempPath);
                }

                SetLog("Extracting .xml.");
                SetProgress(15);

                string javaPath = "java";
                string apktoolArgs = $"-jar \"{apktoolPath}\" d \"{selectedApkPath}\" -o \"{tempPath}\" -f";
                var apktoolRun = RunProcessDetailed(javaPath, apktoolArgs, resourcesPath);

                if (!apktoolRun.Started)
                    throw new InvalidOperationException("Java could not be started. Please make sure Java is installed and available in PATH.");

                if (apktoolRun.TimedOut)
                    throw new TimeoutException("Apktool did not finish in time and was stopped.");

                string manifestPath = Path.Combine(tempPath, "AndroidManifest.xml");
                if (File.Exists(manifestPath))
                {
                    string manifestContent = File.ReadAllText(manifestPath);
                    SafeInvoke(() =>
                    {
                        richtextboxapktoolyml.Clear();
                        richtextboxapktoolyml.Text = manifestContent;
                    });
                }
                else
                {
                    SetLog("AndroidManifest.xml not found. Starting deep analysis...");
                    SetProgress(20);

                    jadxAnalysisResult = AnalyzeWithJadx(selectedApkPath, tempPath);

                    SetLog("Analyzing with strings.exe...");
                    SetProgress(30);

                    string stringAnalysis = AnalyzeWithStrings(selectedApkPath, tempPath);
                    if (!string.IsNullOrEmpty(stringAnalysis))
                    {
                        jadxAnalysisResult += "\n\n" + stringAnalysis;
                    }

                    SetLog("Converting with dex2jar...");
                    SetProgress(40);

                    string dex2jarAnalysis = AnalyzeWithDex2Jar(selectedApkPath, tempPath);
                    if (!string.IsNullOrEmpty(dex2jarAnalysis))
                    {
                        jadxAnalysisResult += "\n\n" + dex2jarAnalysis;
                    }

                    string manifestMessage = encryptionEvidenceFound
                        ? "AndroidManifest.xml could not be extracted - APK is encrypted or packed"
                        : "AndroidManifest.xml could not be extracted - apktool could not decode this APK";

                    SafeInvoke(() =>
                    {
                        richtextboxapktoolyml.Clear();
                        richtextboxapktoolyml.Text = manifestMessage;
                    });
                    SetProgress(45);
                }
                                                               
                // Base64 encoded RAT adlari - Windows Defender bypass ucun
                string[] ratadlariEncoded = new string[] { 
                    "c3B5bm90ZQ==",           // spynote
                    "c3B5bWF4",               // spymax
                    "Y3JheHNyYXQ=",           // craxsrat
                    "Y2VsbGlrcmF0",           // cellikrat
                    "Y3lwaGVycmF0",           // cypherrat
                    "ZWFnbGVzcHk=",           // eaglespy
                    "Zy03MDByYXQ=",           // g-700rat
                    "bWV0YXNwbG9pdA==",       // metasploit
                    "YnJhdGFyYXQ=",           // bratarat
                    "ZXZlcnNweQ==",           // everspy
                    "YmxhY2tzcHk=",           // blackspy
                    "Ymlnc2hhcmtyYXQ=",       // bigsharkrat
                    "ZHJvaWRqYWNr",           // droidjack
                    "YW5kcm9yYXQ=",           // androrat
                    "QWhNeXRo",               // ahmyth
                    "TllBTnhDQVQ=",           // massrat
                    "cWlSQVQ="                // qirat


                };
                
              
                string[] ratadlari = new string[ratadlariEncoded.Length];
                for (int i = 0; i < ratadlariEncoded.Length; i++)
                {
                    ratadlari[i] = DecodeBase64(ratadlariEncoded[i]);
                }

                SetLog("Looking for RAT.");
                SafeInvoke(() =>
                {
                    textboxalert.Clear();
                    textboxalert.Text = "scanning for RAT. may take a long time please wait";
                });


                bool ratFound = false;
                string foundRatName = "";

                //Bagimsiz calisan RAT adina gore dedect eden rule
                var allFiles = Directory.GetFiles(tempPath, "*.*", SearchOption.AllDirectories);



                //Metasploit/msfvenomu dedect etmek ucun
                string metasploitPackage = DecodeBase64("Y29tLm1ldGFzcGxvaXQuc3RhZ2U="); // com.metasploit.stage
                string metasploitName = DecodeBase64("bWV0YXNwbG9pdA=="); // metasploit
                
                foreach (var file in allFiles)
                {
                    try
                    {
                        string content = ReadFileForScan(file);
                        if (content == null)
                            continue;

                        if (ContainsIgnoreCase(content, metasploitPackage))
                        {
                            ratFound = true;
                            foundRatName = metasploitName;
                            break;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }

                //Metasploit/msfvenomu dedect etmek ucun 2x Payload smaliye esasen | islemir yeniden baxilmalidir
                //if (!ratFound)
                //{
                //    var smaliFilesForPayload = Directory.GetFiles(tempPath, "*.smali", SearchOption.AllDirectories);
                //    string payloadSmaliName = DecodeBase64("UGF5bG9hZC5zbWFsaQ=="); // Payload.smali
                //    
                //    foreach (var smaliFile in smaliFilesForPayload)
                //    {
                //        string smaliFileName = Path.GetFileName(smaliFile);
                //        if (smaliFileName.Equals(payloadSmaliName, StringComparison.OrdinalIgnoreCase))
                //        {
                //            ratFound = true;
                //            foundRatName = metasploitName;
                //            break;
                //        }
                //    }
                //}

           
                if (!ratFound)
                {
                foreach (var file in allFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);


                    try
                    {
                        string content = ReadFileForScan(file);
                        if (content == null)
                            continue;

                        //CraxsRati dedect etmek ucun
                        string craxsKeyword = DecodeBase64("c3B5bWF4"); // spymax
                        string craxsRatName = DecodeBase64("Y3JheHNyYXQ="); // craxsrat

                        if (fileName.Equals("accessdiecrip", StringComparison.OrdinalIgnoreCase) && ContainsIgnoreCase(content, craxsKeyword))
                        {
                            ratFound = true;
                            foundRatName = craxsRatName;
                            break;
                        }

                        //Spynote version 5i dedect etmek ucun

                        string spynoteV5Keyword = DecodeBase64("Y2FtZXJhX21hbmFnZXJmeGYweDR4NHgwZnhm"); // camera_managerfxf0x4x4x0fxf
                        string spynoteName = DecodeBase64("c3B5bm90ZQ=="); // spynote
                        
                        if (ContainsIgnoreCase(content, spynoteV5Keyword))
                        {
                            ratFound = true;
                            foundRatName = spynoteName;
                            break;
                        }

                        //Spynote version 6.4u dedect etmek ucun
                        string spynoteV6Keyword = DecodeBase64("c3B5X25vdGU="); // spy_note

                            if (ContainsIgnoreCase(content, spynoteV6Keyword))
                            {
                                ratFound = true;
                                foundRatName = spynoteName;
                                break;
                            }
                        
                        //Massrati dedect etmek ucun
                        string massratkeyword = DecodeBase64("TllBTnhDQVQ="); // NYANxCAT(massrat)
                        string massratname = DecodeBase64("TUFTU1JBVA==");

                            if (ContainsIgnoreCase(content, massratkeyword))
                            {
                                ratFound = true;
                                foundRatName = massratname;
                                break;
                            }
                       

                        //Qirati dedect etmek ucun
                            string qiratkeyword = DecodeBase64("Y29tLnFpcmF0LnN0dWI=");
                            string qiratname = DecodeBase64("cWlSQVQ=");

                            if (ContainsIgnoreCase(content, qiratkeyword)) {
                                ratFound = true;
                                foundRatName = qiratname;
                                break;
                            }

                            //Spynote dedect etmek ucun | yeni rule

                            string spynotenewrule = DecodeBase64("Y21mMC5jM2I1Ym05MHpxLnBhdGNo");

                            if (ContainsIgnoreCase(content, spynotenewrule))
                            {
                                ratFound = true;
                                foundRatName = spynoteName;
                                break;
                            }
                            







                            //Ahmyth dedect etmek ucun
                            string ahmythKeyword = DecodeBase64("YWhteXRoLm1pbmUua2luZy5haG15dGg="); // ahmyth.mine.king.ahmyth
                            string ahmythname = DecodeBase64("QWhteXRo");

                            if (ContainsIgnoreCase(content, ahmythKeyword))
                            {
                                ratFound = true;
                                foundRatName = ahmythname;
                                break;
                            }

                         
                        //Droidjack dedect etmek ucun
                        string drodjackKeyword = DecodeBase64("bmV0LmRyb2lkamFjaw=="); // net.doidjack
                        string droidjack = DecodeBase64("ZHJvaWRqYWNr"); // droidjack

                            if (ContainsIgnoreCase(content, drodjackKeyword))
                            {
                                ratFound = true;
                                foundRatName = droidjack;
                                break;
                            }

                         string androratKeyword = DecodeBase64("QW5kcm9yYXRBY3Rpdml0eQ=="); // AndroidActivity
                         string androrat = DecodeBase64("YW5kcm9yYXQ="); // androrat

                            if (fileName.Equals(androratKeyword, StringComparison.OrdinalIgnoreCase) && ContainsIgnoreCase(content, androratKeyword))
                              {
                                       ratFound = true;
                                       foundRatName = androrat;
                                       break;
                              }
                            







                            //G-700 dedect etmek ucun | spymax stubnan toqussma var islemir heleki buda 
                            //if (content.Contains("leader"))
                            //{
                            //    ratFound = true;
                            //    foundRatName = "G-700";
                            //    break;
                            //}

                            //Cellikrati dedect etmek ucun
                            //if (content.Contains("ClientHost"))
                            //{
                            //    ratFound = true;
                            //    foundRatName = "CellikRat";
                            //    break;
                            //}

                            foreach (var keyword in ratadlari)
                            {
                                if (ContainsIgnoreCase(content, keyword))
                                {
                                    ratFound = true;
                                    foundRatName = keyword;
                                    break;
                                }
                            }
                        if (ratFound)
                            break;
                    }
                    catch
                    {                       
                        continue;
                    }
                }
                }

                SetProgress(50);

                bool isProtected = false;
                var smaliFiles = Directory.GetFiles(tempPath, "*.smali", SearchOption.AllDirectories);
                foreach (var file in smaliFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    if (fileName.Length > 100)
                    {
                        isProtected = true;
                        break;
                    }
                }

                SetLog("Dumping permissions");
                SetProgress(60);

                var aaptRun = RunProcessDetailed(aaptPath, $"dump badging \"{selectedApkPath}\"", resourcesPath);
                string aaptOutput = aaptRun.StandardOutput;
                string packageName = GetMatch(aaptOutput, @"package: name='(.*?)'");
                string sdkVersion = GetMatch(aaptOutput, @"sdkVersion:'(.*?)'");
                var permissionMatches = Regex.Matches(aaptOutput, @"uses-permission: name='(.*?)'");
                int permissionCount = permissionMatches.Count;

                SetProgress(70);
                string certInfo = GetCertificateInfo(selectedApkPath);


                string md5Text = "", sha1Text = "", sha256Text = "";
                using (var stream = File.OpenRead(selectedApkPath))
                {
                    using (var md5 = MD5.Create())
                    using (var sha1 = SHA1.Create())
                    using (var sha256 = SHA256.Create())
                    {
                        byte[] md5Bytes = md5.ComputeHash(stream);
                        stream.Position = 0;
                        byte[] sha1Bytes = sha1.ComputeHash(stream);
                        stream.Position = 0;
                        byte[] sha256Bytes = sha256.ComputeHash(stream);

                        SetLog("Extracting hash...");
                        md5Text = "MD5: " + BitConverter.ToString(md5Bytes).Replace("-", "").ToLowerInvariant();
                        sha1Text = "SHA1: " + BitConverter.ToString(sha1Bytes).Replace("-", "").ToLowerInvariant();
                        sha256Text = "SHA256: " + BitConverter.ToString(sha256Bytes).Replace("-", "").ToLowerInvariant();

                    }

                    SafeInvoke(() =>
                    {
                        textBoxmd5.Text = md5Text;
                        textBoxsha1.Text = sha1Text;
                        textBoxsha256.Text = sha256Text;
                    });

                }


                SafeInvoke(() =>
                {
                    textboxalert.Clear();
                    if (ratFound)
                    {
                        textboxalert.Text = $"This APK contains a known RAT signature: {foundRatName.ToUpper()}";
                        pictureBoxredandro.Visible = true;
                        labelalertpayload.Visible = true;
                    }
                    else
                    {
                        textboxalert.Text = "No known RAT signatures found in APK.";

                    }
                });

                string[] permissions = permissionMatches.Cast<Match>().Select(m => m.Groups[1].Value).ToArray();

                string analysisResult = AnalyzeApk(permissions, certInfo, isProtected, ratFound, permissionCount, packageName);

                SafeInvoke(() =>
                {
                    richtextboxapksays.Clear();
                    richtextboxapksays.ForeColor = analysisResultColor;
                    richtextboxapksays.Text = analysisResult;

                });

                SetProgress(80);

                nativeLibResult = AnalyzeNativeLibraries(tempPath);
                dynamicLoadersResult = AnalyzeDynamicLoaders(tempPath);

                ClearTempFolder(tempPath);

                protectionSummaryText = !string.IsNullOrEmpty(jadxAnalysisResult)
                    ? jadxAnalysisResult
                    : (isProtected
                        ? "The content of this apk is too long this apk maybe encrypted!"
                        : "This apk is not encrypted!");

                SafeInvoke(() =>
                {
                    packagenamelabel.Text = packageName;
                    sdkverisonlabel.Text = sdkVersion;
                    permissionslabel.Text = $"{permissionCount}";
                    
                    guna2ShadowPanel1.Visible = true;
                    textBoxmd5.Visible = true;
                    textBoxsha1.Visible = true;
                    textBoxsha256.Visible = true;
                    richtextboxprotectet.Visible = true;
                    mainRichTexbox.Visible = true;
                    richtextboxcert.Visible = true;
                    richtextboxcert.Clear();
                    richtextboxcert.Text = certInfo;

                    richtextboxprotectet.Clear();
                    richtextboxprotectet.Text = protectionSummaryText;

                    button1.Visible = true;
                    dynamicloaderbutton.Visible = true;

                    mainRichTexbox.Clear();
                    if (permissionCount > 0)
                    {
                        foreach (Match match in permissionMatches)
                        {
                            string permission = match.Groups[1].Value;
                            mainRichTexbox.AppendText(permission + Environment.NewLine);
                        }
                    }
                    else
                    {
                        mainRichTexbox.Text = "No permissions found.";
                    }

                    mainRichTexbox.SelectionStart = 0;
                    mainRichTexbox.SelectionLength = 0;
                    mainRichTexbox.ScrollToCaret();

                    richTextBoxlog.Clear();
                    richTextBoxlog.AppendText("Done");
                    allprosessbar.Value = 100;
                });
            });
            }
            catch (Exception ex)
            {
                analysisError = ex.Message;
            }
            finally
            {
                countdownTimer.Stop();
                analysisStopwatch.Stop();
                analysisRunning = false;
                newapkButton.Enabled = true;
            }

            if (analysisError != null)
            {
                richTextBoxlog.Clear();
                richTextBoxlog.AppendText("Analysis failed");
                allprosessbar.Value = 0;
                richTextBoxanaliz.Text = "Analysis could not be completed.";

                analizbutton.Visible = true;
                analizinaltindakibutton.Visible = false;

                MessageBox.Show("Analysis could not be completed:" + Environment.NewLine + analysisError, "Analysis Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                analysisStopwatch.Reset();
                return;
            }

            double totalSeconds = analysisStopwatch.Elapsed.TotalSeconds;
            int minutes = (int)totalSeconds / 60;
            int seconds = (int)totalSeconds % 60;
            richTextBoxanaliz.Text = $"Analysis completed in: {minutes:D2}:{seconds:D2} | 15 main operations were completed.";

            analysisStopwatch.Reset();
        }

        private string AnalyzeApk(string[] permissions, string certInfo, bool isProtected, bool ratFound, int permissionCount, string packageName)
        {

            Dictionary<string, string[]> trustedOrgsDict = LoadTrustedOrganizations();

            List<string> trustedOrgs = new List<string>();
            foreach (var entry in trustedOrgsDict)
            {
                trustedOrgs.AddRange(entry.Value);
            }

            string[] unwantedCertKeywords = new string[] {
        "debug",
        "android",
        "hack",
        "android@android.com",
        "test",
        "sample",
        "unknown",
        "null",
        "dev",
        "release",
        "mycompany",
        "certificate",
        "developer",
        "com",
        "default",
        "issuer",
        "root",
        "admin",
        "my name",
        "benim ismim",
        "testkey",
        "company",
        "user",
        "owner",
        "test_cert",
        "testissuer",
        "androiddebugkey",
        "fake",
        "placeholder",
        "temp",
        "keystore",
        "nosign",
        "testsigning",
        "mydebugkey",
        "signingkey",
        "unsigned",
        "example",
        "staging",
        "nobody",
        "me",
        "cert",
        "na",
        "droidjack",
        "androrat"
    };

  
            if (ratFound && encryptionEvidenceFound)
            {
                analysisResultColor = Color.Red;
                return "APKdevastate says: MALICIOUS & ENCRYPTED (This apk is a payload created by a RAT and is also encrypted)";
            }

            if (ratFound)
            {
                analysisResultColor = Color.Red;
                return "APKdevastate says: MALICIOUS (This apk is a payload created by a RAT)";
            }

            bool certificateAvailable = signatureVerified && !string.IsNullOrEmpty(certificateDn);

            var organizationValues = GetDnFieldValues(certificateDn, new string[] { "o", "ou" });
            var allCertValues = GetDnFieldValues(certificateDn, new string[] { "cn", "o", "ou", "l", "st", "emailaddress" });

            bool isTrustedCert = false;

            //regular expressions ai onerisi
            if (certificateAvailable)
            {
                foreach (var org in trustedOrgs)
                {
                    if (DnValuesContainToken(organizationValues, org))
                    {
                        isTrustedCert = true;
                        break;
                    }
                }
            }

            List<string> detectedUnwantedKeywords = new List<string>();
            if (certificateAvailable)
            {
                foreach (var keyword in unwantedCertKeywords)
                {
                    if (DnValuesContainToken(allCertValues, keyword))
                        detectedUnwantedKeywords.Add(keyword);
                }
            }

            bool isUnwantedCert = !certificateAvailable || detectedUnwantedKeywords.Count > 0;

            
        //    bool isInPlayStore = false;
        //    if (!string.IsNullOrEmpty(packageName))
        //    {
        //        richTextBoxlog.Clear();
        //        Invoke((MethodInvoker)(() => richTextBoxlog.AppendText("Checking Play Protect (Play Store) availability...")));
        //        
        //        isInPlayStore = CheckPlayStoreAvailability(packageName);
        //        
        //        if (isInPlayStore)
        //        {
        //            richtextboxapksays.ForeColor = Color.Green;
        //            return "APKdevastate says: CLEAN (No malicious intent was matched with the algorithm)";
        //        }
        //    }

            string[] dangerousPermissions = new string[]
            {
        "android.permission.READ_SMS",
        "android.permission.SEND_SMS",
        "android.permission.RECEIVE_SMS",
        "android.permission.RECORD_AUDIO",
        "android.permission.CAMERA",
        "android.permission.READ_CONTACTS",
        "android.permission.WRITE_CONTACTS",
        "android.permission.ACCESS_FINE_LOCATION",
        "android.permission.ACCESS_COARSE_LOCATION",
        "android.permission.READ_PHONE_STATE",
        "android.permission.CALL_PHONE",
        "android.permission.SYSTEM_ALERT_WINDOW",
        //"android.permission.WRITE_SETTINGS",
        //"android.permission.READ_EXTERNAL_STORAGE",
        //"android.permission.WRITE_EXTERNAL_STORAGE",
            };

            int dangerousPermissionCount = permissions.Count(p => dangerousPermissions.Contains(p));

            if (dangerousPermissionCount > 4 && isUnwantedCert)
            {

                string detectedKeywords = detectedUnwantedKeywords.Count > 0
                    ? string.Join(", ", detectedUnwantedKeywords)
                    : "no verified certificate";
                analysisResultColor = Color.Red;
                return $"APKdevastate says: UNWANTED (Suspicious certification: {detectedKeywords} + dangerous permissions)";
            }

            if (dangerousPermissionCount > 3 && !isTrustedCert)


            {
                analysisResultColor = Color.Red;
                return "APKdevastate says: MALICIOUS (No certification found and dangerous permissions. this could be a dangerous apk)";
            }

            if (permissionCount > 15 && !isTrustedCert)
            {
                analysisResultColor = Color.Red;
                return "APKdevastate says: MALICIOUS (This apk file asks for too many unnecessary permissions and the valid certificate could not be found. this could be a dangerous apk)";
            }

            if (isProtected && permissionCount > 10 && !isTrustedCert)
            {
                analysisResultColor = Color.Red;
                return "APKdevastate says: SUSPICIOUS (This apk's content is very complicated and it is detected as encrypted and it has multiple permissions, it may be a suspicious apk.)";
            }

            if (encryptionEvidenceFound && !isTrustedCert)
            {
                analysisResultColor = Color.Red;
                return "APKdevastate says: ENCRYPTED or PACKED (This apk file is encrypted and packed, and is potentially an unwanted and malicious application)";
            }

            if (isTrustedCert)
            {
                analysisResultColor = Color.Green;
                return "APKdevastate says: CLEAN (Trusted company certificate detected)";
            }

            analysisResultColor = Color.Green;
            return "APKdevastate says: CLEAN (No malicious intent was matched with the algorithm)";
        }

        private void newapkButton_Click(object sender, EventArgs e)
        {
            if (analysisRunning)
                return;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "APK Files (*.apk)|*.apk";
                openFileDialog.Title = "Select a new APK file";


                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    selectedApkPath = openFileDialog.FileName;
                    string fileName = Path.GetFileName(selectedApkPath);
                    apknamelabel.Text = fileName;
                    labelalertpayload.Visible = false;

                    nativeLibResult = "";
                    dynamicLoadersResult = "";
                    jadxAnalysisResult = "";
                    protectionSummaryText = "";
                    activeProtectionView = "summary";
                    certificateDn = "";
                    signatureVerified = false;
                    encryptionEvidenceFound = false;
                    analysisResultColor = Color.Black;

                    analizbutton.Visible = true;
                    analizinaltindakibutton.Visible = false;
                    pictureBoxredandro.Visible = false;
                    dynamicloaderbutton.Visible = false;
                    button1.Visible = false;
                    dynamicloaderbutton.Visible = false;
                    richtextboxapksays.ForeColor = Color.Black;
                    packagenamelabel.Text = "";
                    sdkverisonlabel.Text = "";
                    permissionslabel.Text = "";
                    richTextBoxlog.Clear();
                    richtextboxapktoolyml.Clear();
                    richtextboxapksays.Clear();
                    textBoxmd5.Clear();
                    textBoxsha1.Clear();
                    textBoxsha256.Clear();
                    textboxalert.Clear();
                    richtextboxprotectet.Clear();
                    richtextboxcert.Clear();
                    mainRichTexbox.Clear();
                    richTextBoxanaliz.Clear();
                }
            }
        }
        /* burda qalsin helelik string musiqiyolu = Path.Combine(Application.StartupPath, "resources", "music", "ses.wav");
                 if (File.Exists(musiqiyolu))
                 {
                     SoundPlayer player = new SoundPlayer(musiqiyolu);
                     player.Play();
                 } */

        private void guna2Buttonguide_Click(object sender, EventArgs e)
        {
            if (guideFormInstance == null || guideFormInstance.IsDisposed)
            {
                guideFormInstance = new guideform();
            }

            guideFormInstance.Show();
            guideFormInstance.BringToFront();
        }

        private void guna2Buttonabout_Click(object sender, EventArgs e)
        {
            if (aboutFormInstance == null || aboutFormInstance.IsDisposed)
            {
                aboutFormInstance = new aboutform();
            }

            aboutFormInstance.Show();
            aboutFormInstance.BringToFront();
        }


        /*private void pictureBox1_Click(object sender, EventArgs e)
         {
             if (player == null) return;

             if (!isPlaying)
             {
                 player.PlayLooping();
                 isPlaying = true;
             }
             else
             {
                 player.Stop();
                 isPlaying = false;
             }
         }
        */
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (countdownTimer != null)
            {
                countdownTimer.Stop();
                countdownTimer.Dispose();
            }

            if (analysisStopwatch != null)
            {
                analysisStopwatch.Stop();
            }

            if (guideFormInstance != null && !guideFormInstance.IsDisposed)
            {
                guideFormInstance.Close();
                guideFormInstance.Dispose();
            }

            if (aboutFormInstance != null && !aboutFormInstance.IsDisposed)
            {
                aboutFormInstance.Close();
                aboutFormInstance.Dispose();
            }

            base.OnFormClosed(e);
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            richtextboxprotectet.Clear();

            if (activeProtectionView == "native")
            {
                activeProtectionView = "summary";
                richtextboxprotectet.Text = protectionSummaryText;
            }
            else
            {
                activeProtectionView = "native";
                richtextboxprotectet.Text = string.IsNullOrEmpty(nativeLibResult)
                    ? "No native library information available."
                    : nativeLibResult;
            }
        }

        private void dynamicloaderbutton_Click(object sender, EventArgs e)
        {
            richtextboxprotectet.Clear();

            if (activeProtectionView == "loaders")
            {
                activeProtectionView = "summary";
                richtextboxprotectet.Text = protectionSummaryText;
            }
            else
            {
                activeProtectionView = "loaders";
                richtextboxprotectet.Text = string.IsNullOrEmpty(dynamicLoadersResult)
                    ? "No dynamic loader information available."
                    : dynamicLoadersResult;
            }
        }
    }
}
