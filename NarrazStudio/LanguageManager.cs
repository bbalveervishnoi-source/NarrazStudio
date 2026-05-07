using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Windows.Storage;

namespace NarrazStudio
{
    public class LanguageManager : INotifyPropertyChanged
    {
        private static LanguageManager? _instance;
        public static LanguageManager Instance => _instance ??= new LanguageManager();

        public event PropertyChangedEventHandler? PropertyChanged;

        private string _currentLanguage;

        public LanguageManager()
        {
            var settings = ApplicationData.Current.LocalSettings.Values;
            _currentLanguage = (settings["AppLanguage"] as string) ?? "System";
        }

        public void SetLanguage(string lang)
        {
            _currentLanguage = lang;
            var settings = ApplicationData.Current.LocalSettings.Values;
            settings["AppLanguage"] = lang;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }

        private string GetActiveLangCode()
        {
            if (_currentLanguage == "System")
            {
                return "en";
            }
            return _currentLanguage;
        }

        public string this[string key]
        {
            get
            {
                string lang = GetActiveLangCode();
                if (lang == "hi" && _hindi.ContainsKey(key))
                    return _hindi[key];
                if (lang == "zh" && _chinese.ContainsKey(key))
                    return _chinese[key];
                if (lang == "ur" && _urdu.ContainsKey(key))
                    return _urdu[key];
                
                if (_english.ContainsKey(key))
                    return _english[key];
                
                return key;
            }
        }

        public string AppTitle => this["AppTitle"];
        public string AppSubtitle => this["AppSubtitle"];
        public string ToolTipSaved => this["ToolTipSaved"];
        public string ToolTipSettings => this["ToolTipSettings"];
        public string NarratorSettings => this["NarratorSettings"];
        public string VoiceRefresh => this["VoiceRefresh"];
        public string VoiceSearch => this["VoiceSearch"];
        public string VoiceConnecting => this["VoiceConnecting"];
        public string AudioAdjustments => this["AudioAdjustments"];
        public string Rate => this["Rate"];
        public string Pitch => this["Pitch"];
        public string ResetRate => this["ResetRate"];
        public string ResetPitch => this["ResetPitch"];
        public string PlayAudio => this["PlayAudio"];
        public string ExportMP3 => this["ExportMP3"];
        public string SystemReady => this["SystemReady"];
        public string WordCharCount => this["WordCharCount"];
        public string ToolTipClear => this["ToolTipClear"];
        public string SavedNarrations => this["SavedNarrations"];
        public string SearchByFilename => this["SearchByFilename"];
        public string Period => this["Period"];
        public string SortBy => this["SortBy"];
        public string AllRecords => this["AllRecords"];
        public string Today => this["Today"];
        public string Yesterday => this["Yesterday"];
        public string LastWeek => this["LastWeek"];
        public string SortNewest => this["SortNewest"];
        public string SortOldest => this["SortOldest"];
        public string SortAZ => this["SortAZ"];
        public string SortChar => this["SortChar"];
        public string HeaderSrNo => this["HeaderSrNo"];
        public string HeaderTitle => this["HeaderTitle"];
        public string HeaderNarrator => this["HeaderNarrator"];
        public string HeaderInfo => this["HeaderInfo"];
        public string HeaderActions => this["HeaderActions"];
        public string Chars => this["Chars"];
        public string Words => this["Words"];
        public string ToolTipOpen => this["ToolTipOpen"];
        public string ToolTipFolder => this["ToolTipFolder"];
        public string ToolTipDelete => this["ToolTipDelete"];
        public string SettingsTitle => this["SettingsTitle"];
        public string Appearance => this["Appearance"];
        public string ThemeEngine => this["ThemeEngine"];
        public string ThemeSelectorTooltip => this["ThemeSelectorTooltip"];
        public string SystemDefault => this["SystemDefault"];
        public string Light => this["Light"];
        public string Dark => this["Dark"];
        public string LanguageLabel => this["LanguageLabel"];
        public string English => this["English"];
        public string Hindi => this["Hindi"];
        public string Chinese => this["Chinese"];
        public string Urdu => this["Urdu"];
        public string MicaToggle => this["MicaToggle"];
        public string MicaTooltip => this["MicaTooltip"];
        public string SystemDiagnostics => this["SystemDiagnostics"];
        public string FFmpegLinked => this["FFmpegLinked"];
        public string FFmpegMissing => this["FFmpegMissing"];
        public string AboutDev => this["AboutDev"];
        public string Version => this["Version"];
        public string BackToHome => this["BackToHome"];
        public string ExportingAudio => this["ExportingAudio"];
        public string ProcessingChunks => this["ProcessingChunks"];
        public string CancelExport => this["CancelExport"];
        public string ExportSuccessful => this["ExportSuccessful"];
        public string OpenFolder => this["OpenFolder"];
        public string Cancel => this["Cancel"];
        public string Done => this["Done"];
        public string ExportFailed => this["ExportFailed"];
        public string SomethingWentWrong => this["SomethingWentWrong"];
        public string OK => this["OK"];
        public string TryAgain => this["TryAgain"];
        public string ToolTipBatch => this["ToolTipBatch"];
        public string ParallelChunkLimit => this["ParallelChunkLimit"];
        public string ParallelChunkDesc => this["ParallelChunkDesc"];
        public string ResetParallelLimit => this["ResetParallelLimit"];
        public string BatchExportTitle => this["BatchExportTitle"];
        public string SelectTextFiles => this["SelectTextFiles"];
        public string OpenFiles => this["OpenFiles"];
        public string Convert => this["Convert"];
        public string SelectSavePath => this["SelectSavePath"];
        public string ExportAll => this["ExportAll"];
        public string CancelAll => this["CancelAll"];
        public string ClearAll => this["ClearAll"];
        public string LocateAll => this["LocateAll"];
        public string Locate => this["Locate"];
        public string LegalTitle => this["LegalTitle"];
        public string LegalFFmpeg => this["LegalFFmpeg"];
        public string LegalApp => this["LegalApp"];
        public string LegalEdgeTts => this["LegalEdgeTts"];
        public string LegalWebView => this["LegalWebView"];
        public string LegalDisclaimer => this["LegalDisclaimer"];
        public string LegalPrivacy => this["LegalPrivacy"];
        public string DevName => this["DevName"];
        public string DevVersion => this["DevVersion"];
        public string SaveAndExit => this["SaveAndExit"];

        private Dictionary<string, string> _english = new()
        {
            {"AppTitle", "Narraz"},
            {"AppSubtitle", "Simple TTS Engine"},
            {"ToolTipSaved", "Saved Narrations"},
            {"ToolTipSettings", "App Settings"},
            {"NarratorSettings", "Narrator settings"},
            {"VoiceRefresh", "Refresh Voices List"},
            {"VoiceSearch", "Search voices..."},
            {"VoiceConnecting", "Connecting..."},
            {"AudioAdjustments", "Audio adjustments"},
            {"Rate", "Rate"},
            {"Pitch", "Pitch"},
            {"ResetRate", "Reset Rate"},
            {"ResetPitch", "Reset Pitch"},
            {"PlayAudio", "Play Audio"},
            {"ExportMP3", "Export MP3"},
            {"SystemReady", "System Ready"},
            {"WordCharCount", "{0} Words • {1} Characters"},
            {"ToolTipClear", "Clear All Text"},
            {"SavedNarrations", "Saved Narrations"},
            {"SearchByFilename", "Search by filename..."},
            {"Period", "Period:"},
            {"SortBy", "Sort by:"},
            {"AllRecords", "All Records"},
            {"Today", "Today"},
            {"Yesterday", "Yesterday"},
            {"LastWeek", "Last Week"},
            {"SortNewest", "Date (Newest)"},
            {"SortOldest", "Date (Oldest)"},
            {"SortAZ", "Name (A-Z)"},
            {"SortChar", "Char Count"},
            {"HeaderSrNo", "Sr. No"},
            {"HeaderTitle", "Title & Dates"},
            {"HeaderNarrator", "Narrator"},
            {"HeaderInfo", "General Info"},
            {"HeaderActions", "Quick Actions"},
            {"Chars", "Chars"},
            {"Words", "Words"},
            {"ToolTipOpen", "Open File"},
            {"ToolTipFolder", "Open Folder"},
            {"ToolTipDelete", "Delete Record"},
            {"SettingsTitle", "App Settings & Info"},
            {"Appearance", "Appearance"},
            {"ThemeEngine", "Theme Engine"},
            {"ThemeSelectorTooltip", "Select Application Theme"},
            {"SystemDefault", "System Default"},
            {"Light", "Light"},
            {"Dark", "Dark"},
            {"LanguageLabel", "Language"},
            {"English", "English"},
            {"Hindi", "Hindi (हिंदी)"},
            {"Chinese", "Chinese (中文)"},
            {"Urdu", "Urdu (اردو)"},
            {"MicaToggle", "Enable Mica Backdrop"},
            {"MicaTooltip", "Applies a blurred desktop background effect..."},
            {"SystemDiagnostics", "System Diagnostics"},
            {"FFmpegLinked", "FFmpeg Linked: System is ready for merging."},
            {"FFmpegMissing", "FFmpeg Missing: Please check the External folder."},
            {"AboutDev", "About the Developer"},
            {"Version", "Version:"},
            {"BackToHome", "Back to Home"},
            {"ExportingAudio", "Exporting Audio..."},
            {"ProcessingChunks", "Processing chunks..."},
            {"CancelExport", "Cancel Export"},
            { "ExportSuccessful", "Export Successful!" },
            { "OpenFolder", "Open Folder" },
            { "Done", "Done" },
            { "ExportFailed", "Export Failed" },
            { "SomethingWentWrong", "Something went wrong." },
            { "OK", "OK" },
            {"TryAgain", "Try Again (Retry)"},
            {"Cancel", "Cancel"},
            {"ToolTipBatch", "Batch Text Export"},
            {"ParallelChunkLimit", "Parallel Chunk Limit"},
            {"ParallelChunkDesc", "Max concurrent generation tasks"},
            {"ResetParallelLimit", "Reset Limit"},
            {"BatchExportTitle", "Batch Text File Export"},
            {"SelectTextFiles", "Select .txt files to export"},
            {"OpenFiles", "Open Files"},
            {"Convert", "Convert"},
            {"SelectSavePath", "Select Save Path"},
            {"ExportAll", "Export All"},
            {"CancelAll", "Cancel All"},
            {"ClearAll", "Clear All"},
            {"LocateAll", "Locate All"},
            {"Locate", "Locate"},
            {"LegalTitle", "Legal & Licenses"},
            {"LegalFFmpeg", "FFmpeg is used under the GNU LGPL v2.1+ license for audio chunk merging and post-processing. FFmpeg binaries are bundled in the External folder and are not modified."},
            {"LegalApp", "Narraz Studio is an open-source project released under the MIT License. You are free to use, modify, and distribute it."},
            {"LegalEdgeTts", "Audio synthesis is powered by edge-tts, an open-source Python library (MIT License) that uses Microsoft Edge's online TTS service."},
            {"LegalWebView", "The text editor is rendered using Microsoft WebView2 (Edge Chromium), redistributed under Microsoft's WebView2 license terms."},
            {"LegalDisclaimer", "Disclaimer: This software is provided as-is, without any warranty. The developer is not responsible for any data loss or misuse."},
            {"LegalPrivacy", "Privacy: Narraz Studio does not collect, store, or transmit any personal data. All processing happens locally on your device. Text is sent to Microsoft's TTS service for audio synthesis only."},
            {"DevName", "Balveer Vishnoi"},
            {"DevVersion", "Narraz Studio Beta"},
            {"SaveAndExit", "Save & Exit Settings"}
        };

        private Dictionary<string, string> _hindi = new()
        {
            {"AppTitle", "Narraz (नराज़)"},
            {"AppSubtitle", "साधारण TTS इंजन"},
            {"ToolTipSaved", "सेव की गई आवाज़ें"},
            {"ToolTipSettings", "ऐप सेटिंग्स"},
            {"NarratorSettings", "नैरेटर सेटिंग्स"},
            {"VoiceRefresh", "वॉइस लिस्ट रिफ्रेश करें"},
            {"VoiceSearch", "आवाज़ें खोंजें..."},
            {"VoiceConnecting", "कनेक्ट हो रहा है..."},
            {"AudioAdjustments", "आवाज़ सेटिंग्स"},
            {"Rate", "गति (Rate)"},
            {"Pitch", "पिच (Pitch)"},
            {"ResetRate", "गति रीसेट करें"},
            {"ResetPitch", "पिच रीसेट करें"},
            {"PlayAudio", "ऑडियो चलाएं"},
            {"ExportMP3", "MP3 में सेव करें"},
            {"SystemReady", "सिस्टम तैयार है"},
            {"WordCharCount", "{0} शब्द • {1} अक्षर"},
            {"ToolTipClear", "सभी टेक्स्ट हटाएं"},
            {"SavedNarrations", "सेव की गई आवाज़ें"},
            {"SearchByFilename", "फ़ाइल के नाम से खोजें..."},
            {"Period", "समयावधि:"},
            {"SortBy", "क्रमबद्ध:"},
            {"AllRecords", "सभी रिकॉर्ड"},
            {"Today", "आज"},
            {"Yesterday", "बीता हुआ कल"},
            {"LastWeek", "पिछले सप्ताह"},
            {"SortNewest", "तारीख (नई पहले)"},
            {"SortOldest", "तारीख (पुरानी पहले)"},
            {"SortAZ", "नाम (A-Z)"},
            {"SortChar", "अक्षर संख्या (Char Count)"},
            {"HeaderSrNo", "क्रमांक"},
            {"HeaderTitle", "शीर्षक और तारीख"},
            {"HeaderNarrator", "नैरेटर"},
            {"HeaderInfo", "सामान्य जानकारी"},
            {"HeaderActions", "त्वरित विकल्प"},
            {"Chars", "अक्षर (Chars)"},
            {"Words", "शब्द (Words)"},
            {"ToolTipOpen", "फाइल खोलें"},
            {"ToolTipFolder", "फोल्डर खोलें"},
            {"ToolTipDelete", "रिकॉर्ड डिलीट करें"},
            {"SettingsTitle", "ऐप सेटिंग्स और जानकारी"},
            {"Appearance", "दिखावट (Appearance)"},
            {"ThemeEngine", "थीम इंजन (Theme)"},
            {"ThemeSelectorTooltip", "एप्लिकेशन थीम चुनें"},
            {"SystemDefault", "सिस्टम डिफ़ॉल्ट"},
            {"Light", "लाइट (Light)"},
            {"Dark", "डार्क (Dark)"},
            {"LanguageLabel", "भाषा (Language)"},
            {"English", "English"},
            {"Hindi", "Hindi (हिंदी)"},
            {"Chinese", "Chinese (中文)"},
            {"Urdu", "Urdu (اردو)"},
            {"MicaToggle", "माइका (Mica) बैकड्रॉप सक्षम करें"},
            {"MicaTooltip", "यह बैकग्राउंड पर एक शानदार ब्लर इफ़ेक्ट लगाता है।"},
            {"SystemDiagnostics", "सिस्टम डायग्नोस्टिक्स"},
            {"FFmpegLinked", "FFmpeg लिंक्ड है: फाइल सेव करने के लिए तैयार है।"},
            {"FFmpegMissing", "FFmpeg नहीं मिला: कृपया External फोल्डर चेक करें।"},
            {"AboutDev", "डेवलपर के बारे में"},
            {"Version", "संस्करण:"},
            {"BackToHome", "वापस होम पेज पर"},
            {"ExportingAudio", "ऑडियो बन रहा है..."},
            {"ProcessingChunks", "चंक (chunks) प्रोसेस हो रहे हैं..."},
            {"CancelExport", "एक्सपोर्ट कैंसल करें"},
            {"ExportSuccessful", "फाइल पूरी बन गई!"},
            {"OpenFolder", "फोल्डर खोलें"},
            {"Done", "ओके करें (Done)"},
            {"ExportFailed", "फाइल नहीं बन सकी"},
            {"SomethingWentWrong", "कुछ गड़बड़ हो गई, दोबारा कोशिश करें।"},
            {"OK", "ठीक (OK)"},
            {"TryAgain", "दोबारा कोशिश करें (Retry)"},
            {"Cancel", "कैंसल करें"},
            {"ToolTipBatch", "बैच टेक्स्ट एक्सपोर्ट"},
            {"ParallelChunkLimit", "समानांतर चंक सीमा (Parallel Chunk Limit)"},
            {"ParallelChunkDesc", "एक साथ जनरेट होने वाले टास्क की अधिकतम संख्या"},
            {"ResetParallelLimit", "लिमिट रीसेट करें"},
            {"BatchExportTitle", "बैच टेक्स्ट फ़ाइल एक्सपोर्ट"},
            {"SelectTextFiles", "एक्सपोर्ट करने के लिए .txt फ़ाइलें चुनें"},
            {"OpenFiles", "फ़ाइलें खोलें"},
            {"Convert", "बदलें (Convert)"},
            {"SelectSavePath", "सेव करने की जगह चुनें"},
            {"ExportAll", "सभी को एक्सपोर्ट करें"},
            {"CancelAll", "सभी कैंसल करें"},
            {"ClearAll", "सब हटाएं"},
            {"LocateAll", "सभी खोजें"},
            {"Locate", "खोजें"},
            {"LegalTitle", "कानूनी जानकारी और लाइसेंस"},
            {"LegalFFmpeg", "FFmpeg का उपयोग GNU LGPL v2.1+ लाइसेंस के अंतर्गत ऑडियो चंक मर्जिंग और पोस्ट-प्रोसेसिंग के लिए किया जाता है। FFmpeg बाइनरी External फ़ोल्डर में शामिल हैं और इनमें कोई बदलाव नहीं किया गया है।"},
            {"LegalApp", "Narraz Studio एक ओपन-सोर्स प्रोजेक्ट है जो MIT लाइसेंस के तहत जारी किया गया है। आप इसे स्वतंत्र रूप से उपयोग, संशोधित और वितरित कर सकते हैं।"},
            {"LegalEdgeTts", "ऑडियो सिंथेसिस edge-tts द्वारा संचालित है, जो एक ओपन-सोर्स Python लाइब्रेरी (MIT लाइसेंस) है जो Microsoft Edge की ऑनलाइन TTS सेवा का उपयोग करती है।"},
            {"LegalWebView", "टेक्स्ट एडिटर Microsoft WebView2 (Edge Chromium) का उपयोग करके रेंडर किया जाता है, जो Microsoft की WebView2 लाइसेंस शर्तों के तहत पुनर्वितरित किया गया है।"},
            {"LegalDisclaimer", "अस्वीकरण: यह सॉफ़्टवेयर जैसा है वैसा प्रदान किया गया है, बिना किसी वारंटी के। डेवलपर किसी भी डेटा हानि या दुरुपयोग के लिए ज़िम्मेदार नहीं है।"},
            {"LegalPrivacy", "गोपनीयता: Narraz Studio कोई भी व्यक्तिगत डेटा एकत्र, संग्रहीत या प्रसारित नहीं करता है। सारी प्रोसेसिंग आपके डिवाइस पर स्थानीय रूप से होती है। टेक्स्ट केवल ऑडियो सिंथेसिस के लिए Microsoft की TTS सेवा को भेजा जाता है।"},
            {"DevName", "बलवीर विष्णोई"},
            {"DevVersion", "Narraz Studio Beta"},
            {"SaveAndExit", "सेटिंग्स सेव करें और बाहर जाएं"}
        };

        private Dictionary<string, string> _chinese = new()
        {
            {"AppTitle", "Narraz"},
            {"AppSubtitle", "简单的 TTS 引擎"},
            {"ToolTipSaved", "已保存的解说"},
            {"ToolTipSettings", "应用设置"},
            {"NarratorSettings", "讲述人设置"},
            {"VoiceRefresh", "刷新语音列表"},
            {"VoiceSearch", "搜索语音..."},
            {"VoiceConnecting", "连接中..."},
            {"AudioAdjustments", "音频调整"},
            {"Rate", "语速"},
            {"Pitch", "音调"},
            {"ResetRate", "重置语速"},
            {"ResetPitch", "重置音调"},
            {"PlayAudio", "播放音频"},
            {"ExportMP3", "导出 MP3"},
            {"SystemReady", "系统就绪"},
            {"WordCharCount", "{0} 个字 • {1} 个字符"},
            {"ToolTipClear", "清除所有文本"},
            {"SavedNarrations", "已保存的解说"},
            {"SearchByFilename", "按文件名搜索..."},
            {"Period", "时段:"},
            {"SortBy", "排序方式:"},
            {"AllRecords", "所有记录"},
            {"Today", "今天"},
            {"Yesterday", "昨天"},
            {"LastWeek", "上周"},
            {"SortNewest", "日期 (最新)"},
            {"SortOldest", "日期 (最旧)"},
            {"SortAZ", "名称 (A-Z)"},
            {"SortChar", "字符数"},
            {"HeaderSrNo", "序号"},
            {"HeaderTitle", "标题 & 日期"},
            {"HeaderNarrator", "讲述人"},
            {"HeaderInfo", "常规信息"},
            {"HeaderActions", "快捷操作"},
            {"Chars", "字符"},
            {"Words", "字数"},
            {"ToolTipOpen", "打开文件"},
            {"ToolTipFolder", "打开文件夹"},
            {"ToolTipDelete", "删除记录"},
            {"SettingsTitle", "应用设置和信息"},
            {"Appearance", "外观"},
            {"ThemeEngine", "主题引擎"},
            {"ThemeSelectorTooltip", "选择应用主题"},
            {"SystemDefault", "系统默认"},
            {"Light", "浅色"},
            {"Dark", "深色"},
            {"LanguageLabel", "语言"},
            {"English", "English"},
            {"Hindi", "Hindi (हिंदी)"},
            {"Chinese", "Chinese (中文)"},
            {"Urdu", "Urdu (اردو)"},
            {"MicaToggle", "启用云母 (Mica) 背景"},
            {"MicaTooltip", "应用模糊的桌面背景效果..."},
            {"SystemDiagnostics", "系统诊断"},
            {"FFmpegLinked", "已链接 FFmpeg：系统已准备好进行合并。"},
            {"FFmpegMissing", "缺失 FFmpeg：请检查 External 文件夹。"},
            {"AboutDev", "关于开发者"},
            {"Version", "版本:"},
            {"BackToHome", "返回主页"},
            {"ExportingAudio", "正在导出音频..."},
            {"ProcessingChunks", "正在处理块..."},
            {"CancelExport", "取消导出"},
            { "ExportSuccessful", "导出成功！" },
            { "OpenFolder", "打开文件夹" },
            { "Done", "完成" },
            { "ExportFailed", "导出失败" },
            { "SomethingWentWrong", "出错了。" },
            { "OK", "确定" },
            {"TryAgain", "重试"},
            {"Cancel", "取消"},
            {"ToolTipBatch", "批量文本导出"},
            {"ParallelChunkLimit", "并行块限制"},
            {"ParallelChunkDesc", "最大并发生成任务数"},
            {"ResetParallelLimit", "重置限制"},
            {"BatchExportTitle", "批量文本文件导出"},
            {"SelectTextFiles", "选择要导出的 .txt 文件"},
            {"OpenFiles", "打开文件"},
            {"Convert", "转换"},
            {"SelectSavePath", "选择保存路径"},
            {"ExportAll", "全部导出"},
            {"CancelAll", "全部取消"},
            {"ClearAll", "全部清除"},
            {"LocateAll", "全部定位"},
            {"Locate", "定位"},
            {"LegalTitle", "法律和许可证"},
            {"LegalFFmpeg", "FFmpeg 在 GNU LGPL v2.1+ 许可证下用于音频块合并和后处理。FFmpeg 二进制文件捆绑在 External 文件夹中，未被修改。"},
            {"LegalApp", "Narraz Studio 是根据 MIT 许可证发布的开源项目。您可以自由使用、修改和分发。"},
            {"LegalEdgeTts", "音频合成由 edge-tts 提供支持，这是一个使用 Microsoft Edge 在线 TTS 服务的开源 Python 库（MIT 许可证）。"},
            {"LegalWebView", "文本编辑器使用 Microsoft WebView2（Edge Chromium）呈现，并根据 Microsoft 的 WebView2 许可条款重新分发。"},
            {"LegalDisclaimer", "免责声明：本软件按“原样”提供，没有任何保证。开发者对任何数据丢失或滥用不承担任何责任。"},
            {"LegalPrivacy", "隐私：Narraz Studio 不会收集、存储或传输任何个人数据。所有处理均在您的设备上本地进行。文本仅发送到 Microsoft 的 TTS 服务以进行音频合成。"},
            {"DevName", "Balveer Vishnoi"},
            {"DevVersion", "Narraz Studio Beta"},
            {"SaveAndExit", "保存并退出设置"}
        };

        private Dictionary<string, string> _urdu = new()
        {
            {"AppTitle", "Narraz"},
            {"AppSubtitle", "سادہ ٹی ٹی ایس انجن"},
            {"ToolTipSaved", "محفوظ کردہ بیانات"},
            {"ToolTipSettings", "ایپ کی ترتیبات"},
            {"NarratorSettings", "راوی کی ترتیبات"},
            {"VoiceRefresh", "آوازوں کی فہرست کو ریفریش کریں"},
            {"VoiceSearch", "آوازیں تلاش کریں..."},
            {"VoiceConnecting", "منسلک ہو رہا ہے..."},
            {"AudioAdjustments", "آڈیو ترتیبات"},
            {"Rate", "رفتار"},
            {"Pitch", "پچ"},
            {"ResetRate", "رفتار ری سیٹ کریں"},
            {"ResetPitch", "پچ ری سیٹ کریں"},
            {"PlayAudio", "آڈیو چلائیں"},
            {"ExportMP3", "ایم پی تھری (MP3) ایکسپورٹ کریں"},
            {"SystemReady", "سسٹم تیار ہے"},
            {"WordCharCount", "{0} الفاظ • {1} حروف"},
            {"ToolTipClear", "تمام متن صاف کریں"},
            {"SavedNarrations", "محفوظ کردہ بیانات"},
            {"SearchByFilename", "فائل کے نام سے تلاش کریں..."},
            {"Period", "مدت:"},
            {"SortBy", "ترتیب دیں بذریعہ:"},
            {"AllRecords", "تمام ریکارڈز"},
            {"Today", "آج"},
            {"Yesterday", "کل"},
            {"LastWeek", "پچھلا ہفتہ"},
            {"SortNewest", "تاریخ (جدید ترین)"},
            {"SortOldest", "تاریخ (قدیم ترین)"},
            {"SortAZ", "نام (A-Z)"},
            {"SortChar", "حروف کی تعداد"},
            {"HeaderSrNo", "سیریل نمبر"},
            {"HeaderTitle", "عنوان اور تاریخیں"},
            {"HeaderNarrator", "راوی"},
            {"HeaderInfo", "عمومی معلومات"},
            {"HeaderActions", "فوری اقدامات"},
            {"Chars", "حروف"},
            {"Words", "الفاظ"},
            {"ToolTipOpen", "فائل کھولیں"},
            {"ToolTipFolder", "فولڈر کھولیں"},
            {"ToolTipDelete", "ریکارڈ حذف کریں"},
            {"SettingsTitle", "ایپ کی ترتیبات اور معلومات"},
            {"Appearance", "ظاہری شکل"},
            {"ThemeEngine", "تھیم انجن"},
            {"ThemeSelectorTooltip", "ایپلی کیشن تھیم منتخب کریں"},
            {"SystemDefault", "سسٹم ڈیفالٹ"},
            {"Light", "روشن (Light)"},
            {"Dark", "تاریک (Dark)"},
            {"LanguageLabel", "زبان"},
            {"English", "English"},
            {"Hindi", "Hindi (हिंदी)"},
            {"Chinese", "Chinese (中文)"},
            {"Urdu", "Urdu (اردو)"},
            {"MicaToggle", "میکا (Mica) پس منظر کو فعال کریں"},
            {"MicaTooltip", "ڈیسک ٹاپ کے دھندلے پس منظر کا اثر لاگو کرتا ہے..."},
            {"SystemDiagnostics", "سسٹم تشخیص"},
            {"FFmpegLinked", "FFmpeg منسلک: سسٹم انضمام کے لیے تیار ہے۔"},
            {"FFmpegMissing", "FFmpeg غائب: براہ کرم External فولڈر چیک کریں۔"},
            {"AboutDev", "ڈویلپر کے بارے میں"},
            {"Version", "ورژن:"},
            {"BackToHome", "ہوم پر واپس جائیں"},
            {"ExportingAudio", "آڈیو ایکسپورٹ ہو رہی ہے..."},
            {"ProcessingChunks", "حصوں پر کارروائی ہو رہی ہے..."},
            {"CancelExport", "ایکسپورٹ منسوخ کریں"},
            { "ExportSuccessful", "ایکسپورٹ کامیاب!" },
            { "OpenFolder", "فولڈر کھولیں" },
            { "Done", "ہو گیا" },
            { "ExportFailed", "ایکسپورٹ ناکام" },
            { "SomethingWentWrong", "کچھ غلط ہو گیا۔" },
            { "OK", "ٹھیک ہے" },
            {"TryAgain", "دوبارہ کوشش کریں (Retry)"},
            {"Cancel", "منسوخ کریں"},
            {"ToolTipBatch", "بیچ ٹیکسٹ ایکسپورٹ"},
            {"ParallelChunkLimit", "متوازی چنک کی حد"},
            {"ParallelChunkDesc", "زیادہ سے زیادہ متوازی تخلیق کے کام"},
            {"ResetParallelLimit", "حد ری سیٹ کریں"},
            {"BatchExportTitle", "بیچ ٹیکسٹ فائل ایکسپورٹ"},
            {"SelectTextFiles", "ایکسپورٹ کرنے کے لیے .txt فائلیں منتخب کریں"},
            {"OpenFiles", "فائلیں کھولیں"},
            {"Convert", "تبدیل کریں (Convert)"},
            {"SelectSavePath", "محفوظ کرنے کا راستہ منتخب کریں"},
            {"ExportAll", "سب ایکسپورٹ کریں"},
            {"CancelAll", "سب منسوخ کریں"},
            {"ClearAll", "سب صاف کریں"},
            {"LocateAll", "سب تلاش کریں"},
            {"Locate", "تلاش کریں"},
            {"LegalTitle", "قانونی معلومات اور لائسنس"},
            {"LegalFFmpeg", "FFmpeg آڈیو حصوں کو ملانے اور پوسٹ پروسیسنگ کے لیے GNU LGPL v2.1+ لائسنس کے تحت استعمال ہوتا ہے۔ FFmpeg بائنریز External فولڈر میں شامل ہیں اور ان میں کوئی ترمیم نہیں کی گئی ہے۔"},
            {"LegalApp", "Narraz Studio ایک اوپن سورس پروجیکٹ ہے جو MIT لائسنس کے تحت جاری کیا گیا ہے۔ آپ اسے آزادانہ طور پر استعمال، ترمیم، اور تقسیم کر سکتے ہیں۔"},
            {"LegalEdgeTts", "آڈیو کی ترکیب edge-tts کے ذریعے چلائی جاتی ہے، جو ایک اوپن سورس پائتھون لائبریری (MIT لائسنس) ہے اور مائیکروسافٹ ایج کی آن لائن TTS سروس استعمال کرتی ہے۔"},
            {"LegalWebView", "متن کے ایڈیٹر کو مائیکروسافٹ WebView2 (Edge Chromium) کے ذریعے رینڈر کیا گیا ہے، جسے مائیکروسافٹ کے WebView2 لائسنس کی شرائط کے تحت دوبارہ تقسیم کیا گیا ہے۔"},
            {"LegalDisclaimer", "ڈس کلیمر: یہ سافٹ ویئر جیسا ہے کی بنیاد پر فراہم کیا گیا ہے، بغیر کسی وارنٹی کے۔ ڈویلپر کسی بھی ڈیٹا کے نقصان یا غلط استعمال کا ذمہ دار نہیں ہے۔"},
            {"LegalPrivacy", "پرائیویسی: Narraz Studio کوئی بھی ذاتی ڈیٹا اکٹھا، محفوظ، یا منتقل نہیں کرتا ہے۔ تمام پروسیسنگ مقامی طور پر آپ کے آلے پر ہوتی ہے۔ متن صرف آڈیو ترکیب کے لیے مائیکروسافٹ کی TTS سروس کو بھیجا جاتا ہے۔"},
            {"DevName", "بلویر وشنوئی"},
            {"DevVersion", "Narraz Studio Beta"},
            {"SaveAndExit", "محفوظ کریں اور ترتیبات سے باہر نکلیں"}
        };
    }
}

