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

namespace APKdevastate
{
    public partial class MainForm : Form
    {

        SoundPlayer player = new SoundPlayer();
        bool isPlaying = false;

        private Timer countdownTimer;
        private Stopwatch analysisStopwatch;
        private int elapsedSeconds = 0;

        private string selectedApkPath;

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
            selectedApkPath = apkFilePath;

            string mahniYolu = Path.Combine(Application.StartupPath, "resources", "music", "aphextwin.wav");
            if (File.Exists(mahniYolu))
            {
                player = new SoundPlayer(mahniYolu);
            }
            else
            {
                
            }
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
        private string RunProcess(string exePath, string arguments)
        {

            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = exePath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }

        private string GetMatch(string input, string pattern)
        {
            var match = Regex.Match(input, pattern);
            return match.Success ? match.Groups[1].Value : "null";
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
        private string GetCertificateInfo(string apkPath)
        {
            richTextBoxlog.Clear();
            Invoke((MethodInvoker)(() => richTextBoxlog.AppendText("Apksigner working...")));
            string resourcesPath = Path.Combine(Application.StartupPath, "resources");
            string apksignerPath = Path.Combine(resourcesPath, "apksigner.jar");

            string javaPath = "java";
            string arguments = $"-jar \"{apksignerPath}\" verify --print-certs \"{apkPath}\"";

            string output = RunProcess(javaPath, arguments);

            var match = Regex.Match(output, @"Signer #1 certificate DN:\s*(.+)");
            return match.Success ? match.Groups[1].Value : "Certificate info not found.";
        }
        private async void analizbutton_Click(object sender, EventArgs e)
        {
            analizbutton.Visible = false;
            analizinaltindakibutton.Visible = true;

            if (string.IsNullOrEmpty(selectedApkPath) || !File.Exists(selectedApkPath))
            {
                MessageBox.Show("Error");
                return;
            }
            
            elapsedSeconds = 0;
            richTextBoxanaliz.Text = "null:null";
            countdownTimer.Start();
            analysisStopwatch.Start();

            textboxalert.Text = "Waiting...";
            allprosessbar.Value = 0;
            allprosessbar.Visible = true;

            string resourcesPath = Path.Combine(Application.StartupPath, "resources");
            string aaptPath = Path.Combine(resourcesPath, "aapt.exe");
            string apktoolPath = Path.Combine(resourcesPath, "apktool.jar");
            string tempPath = Path.Combine(Application.StartupPath, "temp");

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

                richTextBoxlog.Clear();
                Invoke((MethodInvoker)(() => richTextBoxlog.AppendText("Extracting .xml.")));
                Invoke((MethodInvoker)(() => allprosessbar.Value = 15));

                string javaPath = "java";
                string apktoolArgs = $"-jar \"{apktoolPath}\" d \"{selectedApkPath}\" -o \"{tempPath}\" -f";
                RunProcess(javaPath, apktoolArgs);

                string manifestPath = Path.Combine(tempPath, "AndroidManifest.xml");
                if (File.Exists(manifestPath))
                {
                    string manifestContent = File.ReadAllText(manifestPath);
                    Invoke((MethodInvoker)(() =>
                    {
                        richtextboxapktoolyml.Clear();
                        richtextboxapktoolyml.Text = manifestContent;
                    }));
                }
                                                               
                string[] ratadlari = new string[] { 
                    "spynote",                  
                    "spymax",
                    "craxsrat",
                    "cellikrat",
                    "insomniaspy",
                    "cypherrat",
                    "eaglespy",
                    "g-700rat",
                    "enccn",
                    "bratarat",
                    "everspy",
                    "blackspy",
                    "bigsharkrat",
                };

                richTextBoxlog.Clear();
                Invoke((MethodInvoker)(() => richTextBoxlog.AppendText("Looking for RAT.")));
                textboxalert.Clear();
                textboxalert.Text = "scanning for RAT. may take a long time please wait";
                
                
                bool ratFound = false;
                string foundRatName = "";


                var allFiles = Directory.GetFiles(tempPath, "*.*", SearchOption.AllDirectories);
                foreach (var file in allFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    if (fileName.Length > 25)
                        continue;

                    try
                    {
                        string content = File.ReadAllText(file).ToLower();

                        //CraxsRati dedect etmek ucun
                        if (fileName.Equals("accessdiecrip", StringComparison.OrdinalIgnoreCase) && content.Contains("spymax"))
                        {
                            ratFound = true;
                            foundRatName = "craxsrat";
                            break;
                        }

                        //Spynote version 5i dedect etmek ucun
                        if (content.Contains("camera_managerfxf0x4x4x0fxf"))
                        {
                            ratFound = true;
                            foundRatName = "spynote";
                            break;
                        }

                        //Spynote version 6.4u dedect etmek ucun
                        if (content.Contains("spy_note"))
                        {
                            ratFound = true;
                            foundRatName = "spynote";
                            break;
                        }

                        //G-700 dedect etmek ucun
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
                            if (content.Contains(keyword))
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

                Invoke((MethodInvoker)(() => allprosessbar.Value = 50));

                bool isProtected = false;
                var smaliFiles = Directory.GetFiles(tempPath, "*.smali", SearchOption.AllDirectories);
                foreach (var file in smaliFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    if (fileName.Length > 40)
                    {
                        isProtected = true;
                        break;
                    }
                }

                richTextBoxlog.Clear();
                Invoke((MethodInvoker)(() => richTextBoxlog.AppendText("Dumping permissions")));
                Invoke((MethodInvoker)(() => allprosessbar.Value = 50));

                string aaptOutput = RunProcess(aaptPath, $"dump badging \"{selectedApkPath}\"");
                string packageName = GetMatch(aaptOutput, @"package: name='(.*?)'");
                string sdkVersion = GetMatch(aaptOutput, @"sdkVersion:'(.*?)'");
                var permissionMatches = Regex.Matches(aaptOutput, @"uses-permission: name='(.*?)'");
                int permissionCount = permissionMatches.Count;

                Invoke((MethodInvoker)(() => allprosessbar.Value = 70));
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

                        richTextBoxlog.Clear();
                        Invoke((MethodInvoker)(() => richTextBoxlog.AppendText("Extracting hash...")));
                        md5Text = "MD5: " + BitConverter.ToString(md5Bytes).Replace("-", "").ToLowerInvariant();
                        sha1Text = "SHA1: " + BitConverter.ToString(sha1Bytes).Replace("-", "").ToLowerInvariant();
                        sha256Text = "SHA256: " + BitConverter.ToString(sha256Bytes).Replace("-", "").ToLowerInvariant();

                    }

                    textBoxmd5.Text = md5Text;
                    textBoxsha1.Text = sha1Text;
                    textBoxsha256.Text = sha256Text;

                    ///CODED BY RAFIG ZARBALIYEV\\\
                    ///   INSTAGRAM @rafok2v9c    \\\
                }


                Invoke((MethodInvoker)(() =>
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
                }));

                string[] permissions = permissionMatches.Cast<Match>().Select(m => m.Groups[1].Value).ToArray();

                string analysisResult = AnalyzeApk(permissions, certInfo, isProtected, ratFound, permissionCount);

                Invoke((MethodInvoker)(() =>
                {
                    richtextboxapksays.Clear();
                    richtextboxapksays.Text = analysisResult;

                }));

                ClearTempFolder(tempPath);

                Invoke((MethodInvoker)(() =>
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
                    richtextboxprotectet.Text = isProtected
                        ? "The content of this apk is too long this apk maybe encrypted!"
                        : "This apk is not encrypted!";

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

                    richTextBoxlog.Clear();
                    Invoke((MethodInvoker)(() => richTextBoxlog.AppendText("Done")));
                    allprosessbar.Value = 100;
                }));

                allprosessbar.Value = 0;
            });
            countdownTimer.Stop();
            analysisStopwatch.Stop();

            double totalSeconds = analysisStopwatch.Elapsed.TotalSeconds;
            int minutes = (int)totalSeconds / 60;
            int seconds = (int)totalSeconds % 60;
            richTextBoxanaliz.Text = $"Analysis completed in: {minutes:D2}:{seconds:D2} | 9 main operations were completed.";

            analysisStopwatch.Reset();
        }

        private string AnalyzeApk(string[] permissions, string certInfo, bool isProtected, bool ratFound, int permissionCount)
        {

            Dictionary<string, string[]> trustedOrgsDict = new Dictionary<string, string[]>
    {
{"apple", new string[] {"apple inc", "apple"}},
{"amazon", new string[] {"amazon inc", "amazon.com", "amazon"}},
{"netflix", new string[] {"netflix inc", "netflix"}},
{"discord", new string[] {"discord inc", "discord"}},
{"activision", new string[] {"activision blizzard", "activision"}},
{"blizzard", new string[] {"blizzard entertainment", "blizzard"}},
{"zoom", new string[] {"zoom video communications", "zoom"}},
{"adobe", new string[] {"adobe inc", "adobe"}},
{"oracle", new string[] {"oracle corporation", "oracle"}},
{"samsung", new string[] {"samsung electronics", "samsung"}},
{"nintendo", new string[] {"nintendo co ltd", "nintendo"}},
{"sony", new string[] {"sony group corporation", "sony"}},
{"uber", new string[] {"uber technologies inc", "uber"}},
{"airbnb", new string[] {"airbnb inc", "airbnb"}},
{"paypal", new string[] {"paypal holdings inc", "paypal"}},
{"dropbox", new string[] {"dropbox inc", "dropbox"}},
{"pinterest", new string[] {"pinterest inc", "pinterest"}},
{"reddit", new string[] {"reddit inc", "reddit"}},
{"twitch", new string[] {"twitch interactive inc", "twitch"}},
{"baidu", new string[] {"baidu inc", "baidu"}},
{"alibaba", new string[] {"alibaba group", "alibaba"}},
{"huawei", new string[] {"huawei technologies", "huawei"}},
{"nvidia", new string[] {"nvidia corporation", "nvidia"}},
{"intel", new string[] {"intel corporation", "intel"}},
{"temu", new string[] {"pdd holdings inc.", "temu"}},
{"capcut", new string[] {"bytedance ltd", "capcut"}},
{"threads", new string[] {"meta platforms, inc.", "threads"}},
{"spotify", new string[] {"spotify ab", "spotify"}},
{"shein", new string[] {"shein group ltd", "shein"}},
{"messenger", new string[] {"meta platforms, inc.", "messenger"}},
{"whatsapp business", new string[] {"meta platforms, inc.", "whatsapp business"}},
{"roblox", new string[] {"roblox corporation", "roblox"}},
{"google", new string[] {"google inc", "google llc", "google"}},
{"meta", new string[] {"meta platforms", "facebook", "instagram", "meta"}},
{"mojang", new string[] {"mojang ab", "mojang"}},
{"microsoft", new string[] {"microsoft corporation", "microsoft"}},
{"twitter", new string[] {"twitter inc", "twitter", "x corp"}},
{"x", new string[] {"x corp", "twitter"}},
{"linkedin", new string[] {"linkedin corporation", "linkedin"}},
{"youtube", new string[] {"youtube llc", "youtube"}},
{"duolingo", new string[] {"duolingo inc", "duolingo"}},
{"shazam", new string[] {"shazam entertainment ltd", "shazam"}},
{"viber", new string[] {"rakuten viber", "viber"}},
{"line", new string[] {"line corporation", "line"}},
{"bigo live", new string[] {"bigo technology", "bigo live"}},
{"imo", new string[] {"imo.im", "imo"}},
{"zalo", new string[] {"vng corporation", "zalo"}},
{"truecaller", new string[] {"true software scandinavia ab", "truecaller"}},
{"clubhouse", new string[] {"alpha exploration co", "clubhouse"}},
{"yandex", new string[] {"yandex nv", "yandex"}},
{"booking", new string[] {"booking holdings", "booking.com", "booking"}},
{"yubo", new string[] {"twelve app", "yubo"}},
{"candy crush", new string[] {"king", "candy crush"}},
{"hbo max", new string[] {"warner bros. discovery", "hbo max"}},
{"veepee", new string[] {"veepee", "vente-privee"}},
{"glovo", new string[] {"glovoapp23", "glovo"}},
{"bolt", new string[] {"bolt technology ou", "bolt"}},
{"tinder", new string[] {"match group", "tinder"}},
{"bumble", new string[] {"bumble inc", "bumble"}},
{"okcupid", new string[] {"match group", "okcupid"}},
{"zoosk", new string[] {"spark networks", "zoosk"}},
{"hily", new string[] {"hily corp", "hily"}},
{"duo mobile", new string[] {"duo security", "duo mobile"}},
{"notion", new string[] {"notion labs inc", "notion"}},
{"medium", new string[] {"medium corporation", "medium"}},
{"be real", new string[] {"be real", "bereal"}},
{"photomath", new string[] {"photomath inc", "photomath"}},
{"musixmatch", new string[] {"musixmatch spa", "musixmatch"}},
{"flo", new string[] {"flo health inc", "flo"}},
{"fitbit", new string[] {"fitbit inc", "fitbit"}},
{"calm", new string[] {"calm.com inc", "calm"}},

    };

            List<string> trustedOrgs = new List<string>();
            foreach (var entry in trustedOrgsDict)
            {
                trustedOrgs.AddRange(entry.Value);
            }

            string[] unwantedCertKeywords = new string[] {
        "debug",
        "android",
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
    };

            if (ratFound)
            {
                richtextboxapksays.ForeColor = Color.Red;
                return "APKdevastate says: MALICIOUS (This apk is a payload created by a RAT)";
            }

            bool isTrustedCert = false;

            string certInfoLower = certInfo.ToLower();

            foreach (var org in trustedOrgs)
            {
                if (Regex.IsMatch(certInfoLower, $@"\bo\s*=\s*[^,]*{Regex.Escape(org)}[^,]*", RegexOptions.IgnoreCase) ||    
                    Regex.IsMatch(certInfoLower, $@"\bou\s*=\s*[^,]*{Regex.Escape(org)}[^,]*", RegexOptions.IgnoreCase) ||   
                    Regex.IsMatch(certInfoLower, $@"\bcn\s*=\s*[^,]*{Regex.Escape(org)}[^,]*", RegexOptions.IgnoreCase) ||  
                    Regex.IsMatch(certInfoLower, $@"\bl\s*=\s*[^,]*{Regex.Escape(org)}[^,]*", RegexOptions.IgnoreCase))
                {
                    isTrustedCert = true;
                    break;
                }

                string pattern = $@"\b{Regex.Escape(org)}\b";
                if (Regex.IsMatch(certInfoLower, pattern))
                {
                    isTrustedCert = true;
                    break;
                }
            }

            if (isTrustedCert)
            {
                richtextboxapksays.ForeColor = Color.Green;
                return "APKdevastate says: CLEAN (Trusted company certificate detected)";
            }

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
        "android.permission.CALL_PHONE"
            };

            int dangerousPermissionCount = permissions.Count(p => dangerousPermissions.Contains(p));
    
            if (permissionCount > 15 && !isTrustedCert)
            {
                richtextboxapksays.ForeColor = Color.Red;
                return "APKdevastate says: MALICIOUS (This apk file asks for too many unnecessary permissions and the valid certificate could not be found. this could be a dangerous apk)";
            }

            bool isUnwantedCert = unwantedCertKeywords.Any(keyword => certInfo.ToLower().Contains(keyword));

            if (dangerousPermissionCount > 3 && isUnwantedCert)
            {
           
                string detectedKeywords = string.Join(", ", unwantedCertKeywords.Where(k => certInfo.ToLower().Contains(k)));
                return $"APKdevastate says: UNWANTED (Suspicious certification: {detectedKeywords} + dangerous permissions)";
            }

            if (dangerousPermissionCount > 3 && !isTrustedCert)
            {
                return "APKdevastate says: MALICIOUS (No certification found and dangerous permissions. this could be a dangerous apk)";
            }

            if (isProtected && permissionCount > 10)
            {
                return "APKdevastate says: SUSPICIOUS (This apk's content is very complicated and it is detected as encrypted and it has multiple permissions, it may be a suspicious apk.)";
            }

                return "APKdevastate says: CLEAN (No malicious intent was matched with the algorithm)";
        }

        private void newapkButton_Click(object sender, EventArgs e)
        {
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

                    analizbutton.Visible = true;
                    analizinaltindakibutton.Visible = false;
                    pictureBoxredandro.Visible = false;
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
            guideform guideform = new guideform();

            guideform.Show();
        }

        private void guna2Buttonabout_Click(object sender, EventArgs e)
        {
            aboutform aboutform = new aboutform();

            aboutform.Show();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
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

            base.OnFormClosed(e);
        }

    }
}

