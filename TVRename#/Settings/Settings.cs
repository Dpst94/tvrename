// 
// Main website for TVRename is http://tvrename.com
// 
// Source code available at https://github.com/TV-Rename/tvrename
// 
// This code is released under GPLv3 https://github.com/TV-Rename/tvrename/blob/master/LICENSE.md
// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Alphaleonis.Win32.Filesystem;
using System.Text.RegularExpressions;
using System.Xml;
// ReSharper disable RedundantDefaultMemberInitializer

// Settings for TVRename.  All of this stuff is through Options->Preferences in the app.

namespace TVRename
{   
    public class TidySettings
    {
        public bool DeleteEmpty = false; // Delete empty folders after move
        public bool DeleteEmptyIsRecycle = true; // Recycle, rather than delete
        public bool EmptyIgnoreWords = false;
        public string EmptyIgnoreWordList = "sample";
        public bool EmptyIgnoreExtensions = false;
        public string EmptyIgnoreExtensionList = ".nzb;.nfo;.par2;.txt;.srt";
        public bool EmptyMaxSizeCheck = true;
        // ReSharper disable once InconsistentNaming
        public int EmptyMaxSizeMB = 100;

        public string[] EmptyIgnoreExtensionsArray => this.EmptyIgnoreExtensionList.Split(';');
        public string[] EmptyIgnoreWordsArray => this.EmptyIgnoreWordList.Split(';');
    }

    public class Replacement
    {
        // used for invalid (and general) character (and string) replacements in filenames

        public bool CaseInsensitive;
        public string That;
        public string This;

        public Replacement(string a, string b, bool insens)
        {
            if (b == null)
                b = "";
            this.This = a;
            this.That = b;
            this.CaseInsensitive = insens;
        }
    }

    public class FilenameProcessorRE
    {
        // A regular expression to find the season and episode number in a filename

        public bool Enabled;
        public string Notes;
        public string RE;
        public bool UseFullPath;

        public FilenameProcessorRE(bool enabled, string re, bool useFullPath, string notes)
        {
            this.Enabled = enabled;
            this.RE = re;
            this.UseFullPath = useFullPath;
            this.Notes = notes;
        }
    }

    [Serializable()]
    public class ShowStatusColoringTypeList : Dictionary<ShowStatusColoringType, System.Drawing.Color>
    {
        public ShowStatusColoringTypeList()
        {
        }
        protected ShowStatusColoringTypeList(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public bool IsShowStatusDefined(string showStatus)
        {
            foreach (KeyValuePair<ShowStatusColoringType, System.Drawing.Color> e in this)
            {
                if (!e.Key.IsMetaType && e.Key.IsShowLevel &&
                    e.Key.Status.Equals(showStatus, StringComparison.CurrentCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public System.Drawing.Color GetEntry(bool meta, bool showLevel, string status)
        {
            foreach (KeyValuePair<ShowStatusColoringType, System.Drawing.Color> e in this)
            {
                if (e.Key.IsMetaType == meta && e.Key.IsShowLevel == showLevel &&
                    e.Key.Status.Equals(status, StringComparison.CurrentCultureIgnoreCase))
                {
                    return e.Value;
                }
            }
            return System.Drawing.Color.Empty;
        }
    }

    public class ShowStatusColoringType
    {
        public ShowStatusColoringType(bool isMetaType, bool isShowLevel, string status)
        {
            this.IsMetaType = isMetaType;
            this.IsShowLevel = isShowLevel;
            this.Status = status;
        }

        public bool IsMetaType;
        public bool IsShowLevel;
        public string Status;

        public string Text
        {
            get
            {
                if (this.IsShowLevel && this.IsMetaType)
                {
                    return $"Show Seasons Status: {this.StatusTextForDisplay}";
                }
                if (!this.IsShowLevel && this.IsMetaType)
                {
                    return $"Season Status: {this.StatusTextForDisplay}";
                }
                if (this.IsShowLevel && !this.IsMetaType)
                {
                    return string.Format("Show Status: {0}", this.StatusTextForDisplay);
                }
                return "";
            }
        }

        private string StatusTextForDisplay
        {
            get
            {
                if (!this.IsMetaType)
                {
                    return this.Status;
                }
                if (this.IsShowLevel)
                {
                    ShowItem.ShowAirStatus status =
                        (ShowItem.ShowAirStatus) Enum.Parse(typeof (ShowItem.ShowAirStatus), this.Status);
                    switch (status)
                    {
                        case ShowItem.ShowAirStatus.Aired:
                            return "All aired";
                        case ShowItem.ShowAirStatus.NoEpisodesOrSeasons:
                            return "No Seasons or Episodes in Seasons";
                        case ShowItem.ShowAirStatus.NoneAired:
                            return "None aired";
                        case ShowItem.ShowAirStatus.PartiallyAired:
                            return "Partially aired";
                        default:
                            return this.Status;
                    }
                }
                else
                {
                    Season.SeasonStatus status =
                        (Season.SeasonStatus) Enum.Parse(typeof (Season.SeasonStatus), this.Status);
                    switch (status)
                    {
                        case Season.SeasonStatus.Aired:
                            return "All aired";
                        case Season.SeasonStatus.NoEpisodes:
                            return "No Episodes";
                        case Season.SeasonStatus.NoneAired:
                            return "None aired";
                        case Season.SeasonStatus.PartiallyAired:
                            return "Partially aired";
                        default:
                            return this.Status;
                    }
                }
            }
        }
    }

    public sealed class TVSettings
    {
        //We are using the singleton design pattern
        //http://msdn.microsoft.com/en-au/library/ff650316.aspx

        private static volatile TVSettings instance;
        private static readonly object syncRoot = new object();
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public static TVSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new TVSettings();
                    }
                }
                return instance;
            }
        }

        #region FolderJpgIsType enum

        public enum FolderJpgIsType
        {
            Banner,
            Poster,
            FanArt,
            SeasonPoster
        }
        #endregion

        #region WTWDoubleClickAction enum

        public enum WTWDoubleClickAction
        {
            Search,
            Scan
        }

        #endregion

        #region ScanType enum

        public enum ScanType
        {
            Full,
            Recent,
            Quick,
            SingleShow
        }

        #endregion

        public enum KODIType
        {
            Eden,
            Frodo,
            Both
        }

        public enum BetaMode
        {
            BetaToo,
            ProductionOnly
        }

        public enum KeepTogetherModes
        {
            All,
            AllBut,
            Just
        }

        public List<string> LibraryFolders;
        public List<string> IgnoreFolders;
        public List<string> DownloadFolders;
        public List<IgnoreItem> Ignore;
        public bool AutoSelectShowInMyShows = true;
        public bool AutoCreateFolders = false;
        public bool BGDownload = false;
        public bool CheckuTorrent = false;
        public bool EpTBNs = false;
        public bool EpJPGs = false;
        public bool SeriesJpg = false;
        public bool ShrinkLargeMede8erImages = false;
        public bool FanArtJpg = false;
        public bool Mede8erXML = false;
        public bool ExportFOXML = false;
        public string ExportFOXMLTo = "";
        public bool ExportMissingCSV = false;
        public string ExportMissingCSVTo = "";
        public bool ExportMissingXML = false;
        public string ExportMissingXMLTo = "";
        public bool ExportShowsTXT = false;
        public string ExportShowsTXTTo = "";
        public bool ExportShowsHTML = false;
        public string ExportShowsHTMLTo = "";
        public int ExportRSSMaxDays = 7;
        public int ExportRSSMaxShows = 10;
        public int ExportRSSDaysPast = 0;
        public bool ExportRenamingXML = false;
        public string ExportRenamingXMLTo = "";
        public bool ExportWTWRSS = false;
        public string ExportWTWRSSTo = "";
        public bool ExportWTWXML = false;
        public string ExportWTWXMLTo = "";
        public List<FilenameProcessorRE> FNPRegexs = DefaultFNPList();
        public bool FolderJpg = false;
        public FolderJpgIsType FolderJpgIs = FolderJpgIsType.Poster;
        public ScanType MonitoredFoldersScanType = ScanType.Full;
        public KODIType SelectedKODIType = KODIType.Both;
        public bool ForceLowercaseFilenames = false;
        public bool IgnoreSamples = true;
        public bool KeepTogether = true;
        public bool LeadingZeroOnSeason = false;
        public bool LeaveOriginals = false;
        public bool LookForDateInFilename = false;
        public bool MissingCheck = true;
        public bool CorrectFileDates = false;
        public bool NFOShows = false;
        public bool NFOEpisodes = false;
        public bool KODIImages = false;
        public bool pyTivoMeta = false;
        public bool pyTivoMetaSubFolder = false;
        public CustomName NamingStyle = new CustomName();
        public bool NotificationAreaIcon = false;
        public bool OfflineMode = false;

        public BetaMode mode = BetaMode.ProductionOnly;
        public float upgradeDirtyPercent = 20;
        public  KeepTogetherModes  keepTogetherMode = KeepTogetherModes.All;

        public bool BulkAddIgnoreRecycleBin = false;
        public bool BulkAddCompareNoVideoFolders = false;
        public string AutoAddMovieTerms = "dvdrip;camrip;screener;dvdscr;r5;bluray";
        public string AutoAddIgnoreSuffixes = "1080p;720p";

        public string[] AutoAddMovieTermsArray => this.AutoAddMovieTerms.Split(';');

        public string[] AutoAddIgnoreSuffixesArray => this.AutoAddIgnoreSuffixes.Split(';');

        public string[] keepTogetherExtensionsArray => this.keepTogetherExtensionsString.Split(';');
        public string keepTogetherExtensionsString = "";

        public string defaultSeasonWord = "Season";

        public string[] searchSeasonWordsArray => this.searchSeasonWordsString.Split(';');
        public string[] PreferredRSSSearchTerms() => this.preferredRSSSearchTermsString.Split(';');

        public string searchSeasonWordsString = "Season;Series;Saison;Temporada;Seizoen";
        public string preferredRSSSearchTermsString = "720p;1080p";

        internal bool IncludeBetaUpdates()
        {
            return (this.mode== BetaMode.BetaToo );
        }

        public string OtherExtensionsString = "";
        public ShowFilter Filter = new ShowFilter();

        public string[] OtherExtensionsArray => this.OtherExtensionsString.Split(';');

        public int ParallelDownloads = 4;
        public List<string> RSSURLs = DefaultRSSURLList();
        public bool RenameCheck = true;
        public bool PreventMove = false;
        public bool RenameTxtToSub = false;
        public List<Replacement> Replacements = DefaultListRE();
        public string ResumeDatPath = "";
        public int SampleFileMaxSizeMB = 50; // sample file must be smaller than this to be ignored
        public bool SearchLocally = true;
        public bool SearchRSS = false;
        public bool ShowEpisodePictures = true;
        public bool HideWtWSpoilers = false;
        public bool HideMyShowsSpoilers = false;
        public bool ShowInTaskbar = true;
        public bool AutoSearchForDownloadedFiles = false;
        public string SpecialsFolderName = "Specials";
        public int StartupTab = 0;
        public Searchers TheSearchers = new Searchers();

        public string[] VideoExtensionsArray => this.VideoExtensionsString.Split(';');
        public bool ForceBulkAddToUseSettingsOnly = false;
        public bool RetainLanguageSpecificSubtitles = true;
        public bool AutoMergeDownloadEpisodes = false;
        public bool AutoMergeLibraryEpisodes = false;
        public string VideoExtensionsString = "";
        public int WTWRecentDays = 7;
        public string uTorrentPath = "";
        public bool MonitorFolders = false;
        public bool RemoveDownloadDirectoriesFiles =false;
        public ShowStatusColoringTypeList ShowStatusColors = new ShowStatusColoringTypeList();
        public string SABHostPort = "";
        public string SABAPIKey = "";
        public bool CheckSABnzbd = false;
        public string PreferredLanguage = "en";
        public WTWDoubleClickAction WTWDoubleClick;

        public TidySettings Tidyup = new TidySettings();
        public bool runPeriodicCheck = false;
        public int periodCheckHours =1;
        public bool runStartupCheck = false;

        private TVSettings()
        {
            SetToDefaults();
        }

        public void load(XmlReader reader)
        {
            SetToDefaults();

            reader.Read();
            if (reader.Name != "Settings")
                return; // bail out

            reader.Read();
            while (!reader.EOF)
            {
                if ((reader.Name == "Settings") && !reader.IsStartElement())
                    break; // all done

                if (reader.Name == "Searcher")
                {
                    string srch = reader.ReadElementContentAsString(); // and match it based on name...
                    this.TheSearchers.CurrentSearch = srch;
                }
                else if (reader.Name == "TheSearchers")
                {
                    this.TheSearchers = new Searchers(reader.ReadSubtree());
                    reader.Read();
                }
                else if (reader.Name == "BGDownload")
                    this.BGDownload = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "OfflineMode")
                    this.OfflineMode = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "Replacements" && !reader.IsEmptyElement)
                {
                    this.Replacements.Clear();
                    reader.Read();
                    while (!reader.EOF)
                    {
                        if ((reader.Name == "Replacements") && (!reader.IsStartElement()))
                            break;
                        if (reader.Name == "Replace")
                        {
                            this.Replacements.Add(new Replacement(reader.GetAttribute("This"),
                                                                  reader.GetAttribute("That"),
                                                                  reader.GetAttribute("CaseInsensitive") == "Y"));
                            reader.Read();
                        }
                        else
                            reader.ReadOuterXml();
                    }
                    reader.Read();
                }
                else if (reader.Name == "ExportWTWRSS" && !reader.IsEmptyElement)
                    this.ExportWTWRSS = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "ExportWTWRSSTo")
                    this.ExportWTWRSSTo = reader.ReadElementContentAsString();
                else if (reader.Name == "ExportWTWXML")
                    this.ExportWTWXML = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "ExportWTWXMLTo")
                    this.ExportWTWXMLTo = reader.ReadElementContentAsString();
                else if (reader.Name == "WTWRecentDays")
                    this.WTWRecentDays = reader.ReadElementContentAsInt();
                else if (reader.Name == "StartupTab")
                {
                    int n = reader.ReadElementContentAsInt();
                    if (n == 6)
                        this.StartupTab = 2; // WTW is moved
                    else if ((n >= 1) && (n <= 3)) // any of the three scans
                        this.StartupTab = 1;
                    else
                        this.StartupTab = 0; // otherwise, My Shows
                }
                else if (reader.Name == "StartupTab2")
                    this.StartupTab = TabNumberFromName(reader.ReadElementContentAsString());
                else if (reader.Name == "DefaultNamingStyle") // old naming style
                    this.NamingStyle.StyleString = CustomName.OldNStyle(reader.ReadElementContentAsInt());
                else if (reader.Name == "NamingStyle")
                    this.NamingStyle.StyleString = reader.ReadElementContentAsString();
                else if (reader.Name == "NotificationAreaIcon")
                    this.NotificationAreaIcon = reader.ReadElementContentAsBoolean();
                else if ((reader.Name == "GoodExtensions") || (reader.Name == "VideoExtensions"))
                    this.VideoExtensionsString = reader.ReadElementContentAsString();
                else if (reader.Name == "OtherExtensions")
                    this.OtherExtensionsString = reader.ReadElementContentAsString();
                else if (reader.Name == "ExportRSSMaxDays")
                    this.ExportRSSMaxDays = reader.ReadElementContentAsInt();
                else if (reader.Name == "ExportRSSMaxShows")
                    this.ExportRSSMaxShows = reader.ReadElementContentAsInt();
                else if (reader.Name == "ExportRSSDaysPast")
                    this.ExportRSSDaysPast = reader.ReadElementContentAsInt();
                else if (reader.Name == "KeepTogether")
                    this.KeepTogether = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "LeadingZeroOnSeason")
                    this.LeadingZeroOnSeason = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "ShowInTaskbar")
                    this.ShowInTaskbar = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "RenameTxtToSub")
                    this.RenameTxtToSub = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "ShowEpisodePictures")
                    this.ShowEpisodePictures = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "HideWtWSpoilers")
                    this.HideWtWSpoilers = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "HideMyShowsSpoilers")
                    this.HideMyShowsSpoilers = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "AutoCreateFolders")
                    this.AutoCreateFolders = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "AutoSelectShowInMyShows")
                    this.AutoSelectShowInMyShows = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "SpecialsFolderName")
                    this.SpecialsFolderName = reader.ReadElementContentAsString();
                else if (reader.Name == "SABAPIKey")
                    this.SABAPIKey = reader.ReadElementContentAsString();
                else if (reader.Name == "CheckSABnzbd")
                    this.CheckSABnzbd = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "SABHostPort")
                    this.SABHostPort = reader.ReadElementContentAsString();
                else if (reader.Name == "PreferredLanguage")
                    this.PreferredLanguage = reader.ReadElementContentAsString();
                else if (reader.Name == "WTWDoubleClick")
                    this.WTWDoubleClick = (WTWDoubleClickAction)reader.ReadElementContentAsInt();
                else if (reader.Name == "ExportMissingXML")
                    this.ExportMissingXML = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "ExportMissingXMLTo")
                    this.ExportMissingXMLTo = reader.ReadElementContentAsString();
                else if (reader.Name == "ExportMissingCSV")
                    this.ExportMissingCSV = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "ExportMissingCSVTo")
                    this.ExportMissingCSVTo = reader.ReadElementContentAsString();
                else if (reader.Name == "ExportRenamingXML")
                    this.ExportRenamingXML = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "ExportRenamingXMLTo")
                    this.ExportRenamingXMLTo = reader.ReadElementContentAsString();
                else if (reader.Name == "ExportFOXML")
                    this.ExportFOXML = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "ExportFOXMLTo")
                    this.ExportFOXMLTo = reader.ReadElementContentAsString();
                else if (reader.Name == "ExportShowsTXT")
                    this.ExportShowsTXT = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "ExportShowsTXTTo")
                    this.ExportShowsTXTTo = reader.ReadElementContentAsString();
                else if (reader.Name == "ExportShowsHTML")
                    this.ExportShowsHTML = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "ExportShowsHTMLTo")
                    this.ExportShowsHTMLTo = reader.ReadElementContentAsString();
                else if (reader.Name == "ForceLowercaseFilenames")
                    this.ForceLowercaseFilenames = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "IgnoreSamples")
                    this.IgnoreSamples = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "SampleFileMaxSizeMB")
                    this.SampleFileMaxSizeMB = reader.ReadElementContentAsInt();
                else if (reader.Name == "ParallelDownloads")
                    this.ParallelDownloads = reader.ReadElementContentAsInt();
                else if (reader.Name == "uTorrentPath")
                    this.uTorrentPath = reader.ReadElementContentAsString();
                else if (reader.Name == "ResumeDatPath")
                    this.ResumeDatPath = reader.ReadElementContentAsString();
                else if (reader.Name == "SearchRSS")
                    this.SearchRSS = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "EpImgs")
                    this.EpTBNs = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "NFOs") //support legacy tag
                {
                    this.NFOShows = reader.ReadElementContentAsBoolean();
                    this.NFOEpisodes = this.NFOShows;
                }
                else if (reader.Name == "NFOShows")
                    this.NFOShows = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "NFOEpisodes")
                    this.NFOEpisodes = reader.ReadElementContentAsBoolean();
                else if ((reader.Name == "XBMCImages") || (reader.Name == "KODIImages")) //Backward Compatibilty
                    this.KODIImages = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "pyTivoMeta")
                    this.pyTivoMeta = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "pyTivoMetaSubFolder")
                    this.pyTivoMetaSubFolder = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "FolderJpg")
                    this.FolderJpg = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "FolderJpgIs")
                    this.FolderJpgIs = (FolderJpgIsType)reader.ReadElementContentAsInt();
                else if (reader.Name == "MonitoredFoldersScanType")
                    this.MonitoredFoldersScanType = (ScanType)reader.ReadElementContentAsInt();
                else if ((reader.Name == "SelectedXBMCType") || (reader.Name == "SelectedKODIType"))
                    this.SelectedKODIType = (KODIType)reader.ReadElementContentAsInt();
                else if (reader.Name == "RenameCheck")
                    this.RenameCheck = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "PreventMove")
                    this.PreventMove  = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "CheckuTorrent")
                    this.CheckuTorrent = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "MissingCheck")
                    this.MissingCheck = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "UpdateFileDates")
                    this.CorrectFileDates = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "SearchLocally")
                    this.SearchLocally = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "LeaveOriginals")
                    this.LeaveOriginals = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "AutoSearchForDownloadedFiles")
                    this.AutoSearchForDownloadedFiles = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "LookForDateInFilename")
                    this.LookForDateInFilename = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "AutoMergeEpisodes")
                    this.AutoMergeDownloadEpisodes = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "AutoMergeLibraryEpisodes")
                    this.AutoMergeLibraryEpisodes = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "RetainLanguageSpecificSubtitles")
                    this.RetainLanguageSpecificSubtitles = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "ForceBulkAddToUseSettingsOnly")
                    this.ForceBulkAddToUseSettingsOnly = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "MonitorFolders")
                    this.MonitorFolders = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "StartupScan")
                    this.runStartupCheck = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "PeriodicScan")
                    this.runPeriodicCheck = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "PeriodicScanHours")
                    this.periodCheckHours = reader.ReadElementContentAsInt();
                else if (reader.Name == "RemoveDownloadDirectoriesFiles")
                    this.RemoveDownloadDirectoriesFiles = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "EpJPGs")
                    this.EpJPGs = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "SeriesJpg")
                    this.SeriesJpg = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "Mede8erXML")
                    this.Mede8erXML = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "ShrinkLargeMede8erImages")
                    this.ShrinkLargeMede8erImages = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "FanArtJpg")
                    this.FanArtJpg = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "DeleteEmpty")
                    this.Tidyup.DeleteEmpty = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "DeleteEmptyIsRecycle")
                    this.Tidyup.DeleteEmptyIsRecycle = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "EmptyIgnoreWords")
                    this.Tidyup.EmptyIgnoreWords = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "EmptyIgnoreWordList")
                    this.Tidyup.EmptyIgnoreWordList = reader.ReadElementContentAsString();
                else if (reader.Name == "EmptyIgnoreExtensions")
                    this.Tidyup.EmptyIgnoreExtensions = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "EmptyIgnoreExtensionList")
                    this.Tidyup.EmptyIgnoreExtensionList = reader.ReadElementContentAsString();
                else if (reader.Name == "EmptyMaxSizeCheck")
                    this.Tidyup.EmptyMaxSizeCheck = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "EmptyMaxSizeMB")
                    this.Tidyup.EmptyMaxSizeMB = reader.ReadElementContentAsInt();
                else if (reader.Name == "BulkAddIgnoreRecycleBin")
                    this.BulkAddIgnoreRecycleBin = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "BulkAddCompareNoVideoFolders")
                    this.BulkAddCompareNoVideoFolders = reader.ReadElementContentAsBoolean();
                else if (reader.Name == "AutoAddMovieTerms")
                    this.AutoAddMovieTerms = reader.ReadElementContentAsString();
                else if (reader.Name == "AutoAddIgnoreSuffixes")
                    this.AutoAddIgnoreSuffixes = reader.ReadElementContentAsString();
                else if (reader.Name == "BetaMode")
                    this.mode = (BetaMode)reader.ReadElementContentAsInt();
                else if (reader.Name == "PercentDirtyUpgrade")
                    this.upgradeDirtyPercent = reader.ReadElementContentAsFloat();
                else if (reader.Name == "BaseSeasonName")
                    this.defaultSeasonWord = reader.ReadElementContentAsString( );
                else if (reader.Name == "SearchSeasonNames")
                    this.searchSeasonWordsString = reader.ReadElementContentAsString();
                else if (reader.Name == "PreferredRSSSearchTerms")
                    this.preferredRSSSearchTermsString = reader.ReadElementContentAsString();
                else if (reader.Name == "KeepTogetherType")
                    this.keepTogetherMode = (KeepTogetherModes) reader.ReadElementContentAsInt();
                else if (reader.Name == "KeepTogetherExtensions")
                    this.keepTogetherExtensionsString = reader.ReadElementContentAsString();
                else if (reader.Name == "FNPRegexs" && !reader.IsEmptyElement)
                {
                    this.FNPRegexs.Clear();
                    reader.Read();
                    while (!reader.EOF)
                    {
                        if ((reader.Name == "FNPRegexs") && (!reader.IsStartElement()))
                            break;
                        if (reader.Name == "Regex")
                        {
                            string s = reader.GetAttribute("Enabled");
                            bool en = s == null || bool.Parse(s);

                            this.FNPRegexs.Add(new FilenameProcessorRE(en, reader.GetAttribute("RE"),
                                                                       bool.Parse(reader.GetAttribute("UseFullPath")),
                                                                       reader.GetAttribute("Notes")));
                            reader.Read();
                        }
                        else
                            reader.ReadOuterXml();
                    }
                    reader.Read();
                }
                else if (reader.Name == "RSSURLs" && !reader.IsEmptyElement)
                {
                    this.RSSURLs.Clear();
                    reader.Read();
                    while (!reader.EOF)
                    {
                        if ((reader.Name == "RSSURLs") && (!reader.IsStartElement()))
                            break;
                        if (reader.Name == "URL")
                            this.RSSURLs.Add(reader.ReadElementContentAsString());
                        else
                            reader.ReadOuterXml();
                    }
                    reader.Read();
                }
                else if (reader.Name == "ShowStatusTVWColors" && !reader.IsEmptyElement)
                {
                    this.ShowStatusColors = new ShowStatusColoringTypeList();
                    reader.Read();
                    while (!reader.EOF)
                    {
                        if ((reader.Name == "ShowStatusTVWColors") && (!reader.IsStartElement()))
                            break;
                        if (reader.Name == "ShowStatusTVWColor")
                        {
                            ShowStatusColoringType type = null;
                            try
                            {
                                string showStatus = reader.GetAttribute("ShowStatus");
                                bool isMeta = bool.Parse(reader.GetAttribute("IsMeta"));
                                bool isShowLevel = bool.Parse(reader.GetAttribute("IsShowLevel"));

                                type = new ShowStatusColoringType(isMeta, isShowLevel, showStatus);
                            }
                            catch
                            {
                            }

                            string color = reader.GetAttribute("Color");
                            if (type != null && !string.IsNullOrEmpty(color))
                            {
                                try
                                {
                                    System.Drawing.Color c = System.Drawing.ColorTranslator.FromHtml(color);
                                    this.ShowStatusColors.Add(type, c);
                                }
                                catch
                                {
                                }
                            }
                            reader.Read();
                        }
                        else
                            reader.ReadOuterXml();
                    }
                    reader.Read();
                }
                else if (reader.Name == "ShowFilters" && !reader.IsEmptyElement)
                {
                    this.Filter = new ShowFilter();
                    reader.Read();
                    while (!reader.EOF)
                    {
                        if ((reader.Name == "ShowFilters") && (!reader.IsStartElement()))
                            break;
                        if (reader.Name == "ShowNameFilter")
                        {
                            this.Filter.ShowName = reader.GetAttribute("ShowName");
                            reader.Read();
                        }
                        else if (reader.Name == "ShowStatusFilter")
                        {
                            this.Filter.ShowStatus = reader.GetAttribute("ShowStatus");
                            reader.Read();
                        }
                        else if (reader.Name == "ShowRatingFilter")
                        {
                            this.Filter.ShowRating = reader.GetAttribute("ShowRating");
                            reader.Read();
                        }
                        else if (reader.Name == "ShowNetworkFilter")
                        {
                            this.Filter.ShowNetwork = reader.GetAttribute("ShowNetwork");
                            reader.Read();
                        }
                        else if (reader.Name == "GenreFilter")
                        {
                            this.Filter.Genres.Add(reader.GetAttribute("Genre"));
                            reader.Read();
                        }
                        else
                            reader.ReadOuterXml();
                    }
                    reader.Read();
                }
                else
                    reader.ReadOuterXml();
            }
        }

        public void SetToDefaults()
        {
            // defaults that aren't handled with default initialisers
            this.Ignore = new List<IgnoreItem>();

            this.DownloadFolders = new List<string>();
            this.IgnoreFolders = new List<string>();
            this.LibraryFolders = new List<string>();

            this.VideoExtensionsString =
                ".avi;.mpg;.mpeg;.mkv;.mp4;.wmv;.divx;.ogm;.qt;.rm;.m4v;.webm;.vob;.ovg;.ogg;.mov;.m4p;.3gp";
            this.OtherExtensionsString = ".srt;.nfo;.txt;.tbn";
            this.keepTogetherExtensionsString = ".srt;.nfo;.txt;.tbn";

            // have a guess at utorrent's path
            string[] guesses = new string[3];
            guesses[0] = System.Windows.Forms.Application.StartupPath + "\\..\\uTorrent\\uTorrent.exe";
            guesses[1] = "c:\\Program Files\\uTorrent\\uTorrent.exe";
            guesses[2] = "c:\\Program Files (x86)\\uTorrent\\uTorrent.exe";

            this.uTorrentPath = "";
            foreach (string g in guesses)
            {
                FileInfo f = new FileInfo(g);
                if (f.Exists)
                {
                    this.uTorrentPath = f.FullName;
                    break;
                }
            }

            // ResumeDatPath
            FileInfo f2 =
                new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"uTorrent\resume.dat"));
            this.ResumeDatPath = f2.Exists ? f2.FullName : "";
        }

        public void WriteXML(XmlWriter writer)
        {
            writer.WriteStartElement("Settings");
            this.TheSearchers.WriteXML(writer);
            XMLHelper.WriteElementToXML(writer,"BGDownload",this.BGDownload);
            XMLHelper.WriteElementToXML(writer,"OfflineMode",this.OfflineMode);
            writer.WriteStartElement("Replacements");
            foreach (Replacement R in this.Replacements)
            {
                writer.WriteStartElement("Replace");
                XMLHelper.WriteAttributeToXML(writer,"This",R.This);
                XMLHelper.WriteAttributeToXML(writer, "That", R.That);
                XMLHelper.WriteAttributeToXML(writer, "CaseInsensitive", R.CaseInsensitive ? "Y" : "N");
                writer.WriteEndElement(); //Replace
            }
            writer.WriteEndElement(); //Replacements
            
            XMLHelper.WriteElementToXML(writer,"ExportWTWRSS",this.ExportWTWRSS);
            XMLHelper.WriteElementToXML(writer,"ExportWTWRSSTo",this.ExportWTWRSSTo);
            XMLHelper.WriteElementToXML(writer,"ExportWTWXML",this.ExportWTWXML);
            XMLHelper.WriteElementToXML(writer,"ExportWTWXMLTo",this.ExportWTWXMLTo);
            XMLHelper.WriteElementToXML(writer,"WTWRecentDays",this.WTWRecentDays);
            XMLHelper.WriteElementToXML(writer,"ExportMissingXML",this.ExportMissingXML);
            XMLHelper.WriteElementToXML(writer,"ExportMissingXMLTo",this.ExportMissingXMLTo);
            XMLHelper.WriteElementToXML(writer,"ExportMissingCSV",this.ExportMissingCSV);
            XMLHelper.WriteElementToXML(writer,"ExportMissingCSVTo",this.ExportMissingCSVTo);
            XMLHelper.WriteElementToXML(writer,"ExportRenamingXML",this.ExportRenamingXML);
            XMLHelper.WriteElementToXML(writer,"ExportRenamingXMLTo",this.ExportRenamingXMLTo);
            XMLHelper.WriteElementToXML(writer,"ExportShowsTXT", this.ExportShowsTXT);
            XMLHelper.WriteElementToXML(writer, "ExportShowsTXTTo", this.ExportShowsTXTTo);
            XMLHelper.WriteElementToXML(writer, "ExportShowsHTML", this.ExportShowsHTML);
            XMLHelper.WriteElementToXML(writer, "ExportShowsHTMLTo", this.ExportShowsHTMLTo);
            XMLHelper.WriteElementToXML(writer,"ExportFOXML",this.ExportFOXML);
            XMLHelper.WriteElementToXML(writer,"ExportFOXMLTo",this.ExportFOXMLTo);
            XMLHelper.WriteElementToXML(writer,"StartupTab2",TabNameForNumber(this.StartupTab));
            XMLHelper.WriteElementToXML(writer,"NamingStyle",this.NamingStyle.StyleString);
            XMLHelper.WriteElementToXML(writer,"NotificationAreaIcon",this.NotificationAreaIcon);
            XMLHelper.WriteElementToXML(writer,"VideoExtensions",this.VideoExtensionsString);
            XMLHelper.WriteElementToXML(writer,"OtherExtensions",this.OtherExtensionsString);
            XMLHelper.WriteElementToXML(writer,"ExportRSSMaxDays",this.ExportRSSMaxDays);
            XMLHelper.WriteElementToXML(writer,"ExportRSSMaxShows",this.ExportRSSMaxShows);
            XMLHelper.WriteElementToXML(writer,"ExportRSSDaysPast",this.ExportRSSDaysPast);
            XMLHelper.WriteElementToXML(writer,"KeepTogether",this.KeepTogether);
            XMLHelper.WriteElementToXML(writer,"KeepTogetherType", (int)this.keepTogetherMode);
            XMLHelper.WriteElementToXML(writer,"KeepTogetherExtensions", this.keepTogetherExtensionsString);
            XMLHelper.WriteElementToXML(writer,"LeadingZeroOnSeason",this.LeadingZeroOnSeason);
            XMLHelper.WriteElementToXML(writer,"ShowInTaskbar",this.ShowInTaskbar);
            XMLHelper.WriteElementToXML(writer,"IgnoreSamples",this.IgnoreSamples);
            XMLHelper.WriteElementToXML(writer,"ForceLowercaseFilenames",this.ForceLowercaseFilenames);
            XMLHelper.WriteElementToXML(writer,"RenameTxtToSub",this.RenameTxtToSub);
            XMLHelper.WriteElementToXML(writer,"ParallelDownloads",this.ParallelDownloads);
            XMLHelper.WriteElementToXML(writer,"AutoSelectShowInMyShows",this.AutoSelectShowInMyShows);
            XMLHelper.WriteElementToXML(writer,"AutoCreateFolders", this.AutoCreateFolders );
            XMLHelper.WriteElementToXML(writer,"ShowEpisodePictures",this.ShowEpisodePictures);
            XMLHelper.WriteElementToXML(writer, "HideWtWSpoilers", this.HideWtWSpoilers);
            XMLHelper.WriteElementToXML(writer, "HideMyShowsSpoilers", this.HideMyShowsSpoilers);
            XMLHelper.WriteElementToXML(writer,"SpecialsFolderName",this.SpecialsFolderName);
            XMLHelper.WriteElementToXML(writer,"uTorrentPath",this.uTorrentPath);
            XMLHelper.WriteElementToXML(writer,"ResumeDatPath",this.ResumeDatPath);
            XMLHelper.WriteElementToXML(writer,"SearchRSS",this.SearchRSS);
            XMLHelper.WriteElementToXML(writer,"EpImgs",this.EpTBNs);
            XMLHelper.WriteElementToXML(writer,"NFOShows",this.NFOShows);
            XMLHelper.WriteElementToXML(writer,"NFOEpisodes", this.NFOEpisodes);
            XMLHelper.WriteElementToXML(writer,"KODIImages",this.KODIImages);
            XMLHelper.WriteElementToXML(writer,"pyTivoMeta",this.pyTivoMeta);
            XMLHelper.WriteElementToXML(writer,"pyTivoMetaSubFolder",this.pyTivoMetaSubFolder);
            XMLHelper.WriteElementToXML(writer,"FolderJpg",this.FolderJpg);
            XMLHelper.WriteElementToXML(writer,"FolderJpgIs",(int) this.FolderJpgIs);
            XMLHelper.WriteElementToXML(writer,"MonitoredFoldersScanType",(int)this.MonitoredFoldersScanType);
            XMLHelper.WriteElementToXML(writer,"SelectedKODIType",(int)this.SelectedKODIType);
            XMLHelper.WriteElementToXML(writer,"CheckuTorrent",this.CheckuTorrent);
            XMLHelper.WriteElementToXML(writer,"RenameCheck",this.RenameCheck);
            XMLHelper.WriteElementToXML(writer, "PreventMove", this.PreventMove);
            XMLHelper.WriteElementToXML(writer,"MissingCheck",this.MissingCheck);
            XMLHelper.WriteElementToXML(writer, "AutoSearchForDownloadedFiles", this.AutoSearchForDownloadedFiles);
            XMLHelper.WriteElementToXML(writer, "UpdateFileDates", this.CorrectFileDates);
            XMLHelper.WriteElementToXML(writer,"SearchLocally",this.SearchLocally);
            XMLHelper.WriteElementToXML(writer,"LeaveOriginals",this.LeaveOriginals);
            XMLHelper.WriteElementToXML(writer, "RetainLanguageSpecificSubtitles", this.RetainLanguageSpecificSubtitles);
            XMLHelper.WriteElementToXML(writer, "ForceBulkAddToUseSettingsOnly", this.ForceBulkAddToUseSettingsOnly);
            XMLHelper.WriteElementToXML(writer,"LookForDateInFilename",this.LookForDateInFilename);
            XMLHelper.WriteElementToXML(writer, "AutoMergeEpisodes", this.AutoMergeDownloadEpisodes);
            XMLHelper.WriteElementToXML(writer, "AutoMergeLibraryEpisodes", this.AutoMergeLibraryEpisodes);
            XMLHelper.WriteElementToXML(writer,"MonitorFolders",this.MonitorFolders);
            XMLHelper.WriteElementToXML(writer, "StartupScan", this.runStartupCheck);
            XMLHelper.WriteElementToXML(writer, "PeriodicScan", this.runPeriodicCheck);
            XMLHelper.WriteElementToXML(writer, "PeriodicScanHours", this.periodCheckHours);
            XMLHelper.WriteElementToXML(writer,"RemoveDownloadDirectoriesFiles", this.RemoveDownloadDirectoriesFiles);
            XMLHelper.WriteElementToXML(writer,"SABAPIKey",this.SABAPIKey);
            XMLHelper.WriteElementToXML(writer,"CheckSABnzbd",this.CheckSABnzbd);
            XMLHelper.WriteElementToXML(writer,"SABHostPort",this.SABHostPort);
            XMLHelper.WriteElementToXML(writer,"PreferredLanguage",this.PreferredLanguage);
            XMLHelper.WriteElementToXML(writer,"WTWDoubleClick",(int) this.WTWDoubleClick);
            XMLHelper.WriteElementToXML(writer,"EpJPGs",this.EpJPGs);
            XMLHelper.WriteElementToXML(writer,"SeriesJpg",this.SeriesJpg);
            XMLHelper.WriteElementToXML(writer,"Mede8erXML",this.Mede8erXML);
            XMLHelper.WriteElementToXML(writer,"ShrinkLargeMede8erImages",this.ShrinkLargeMede8erImages);
            XMLHelper.WriteElementToXML(writer,"FanArtJpg",this.FanArtJpg);
            XMLHelper.WriteElementToXML(writer,"DeleteEmpty",this.Tidyup.DeleteEmpty);
            XMLHelper.WriteElementToXML(writer,"DeleteEmptyIsRecycle",this.Tidyup.DeleteEmptyIsRecycle);
            XMLHelper.WriteElementToXML(writer,"EmptyIgnoreWords",this.Tidyup.EmptyIgnoreWords);
            XMLHelper.WriteElementToXML(writer,"EmptyIgnoreWordList",this.Tidyup.EmptyIgnoreWordList);
            XMLHelper.WriteElementToXML(writer,"EmptyIgnoreExtensions",this.Tidyup.EmptyIgnoreExtensions);
            XMLHelper.WriteElementToXML(writer,"EmptyIgnoreExtensionList",this.Tidyup.EmptyIgnoreExtensionList);
            XMLHelper.WriteElementToXML(writer,"EmptyMaxSizeCheck",this.Tidyup.EmptyMaxSizeCheck);
            XMLHelper.WriteElementToXML(writer,"EmptyMaxSizeMB",this.Tidyup.EmptyMaxSizeMB);
            XMLHelper.WriteElementToXML(writer, "BetaMode", (int)this.mode);
            XMLHelper.WriteElementToXML(writer, "PercentDirtyUpgrade", this.upgradeDirtyPercent);
            XMLHelper.WriteElementToXML(writer, "BaseSeasonName", this.defaultSeasonWord);
            XMLHelper.WriteElementToXML(writer, "SearchSeasonNames", this.searchSeasonWordsString);
            XMLHelper.WriteElementToXML(writer, "PreferredRSSSearchTerms", this.preferredRSSSearchTermsString);
            XMLHelper.WriteElementToXML(writer, "BulkAddIgnoreRecycleBin", this.BulkAddIgnoreRecycleBin);
            XMLHelper.WriteElementToXML(writer, "BulkAddCompareNoVideoFolders", this.BulkAddCompareNoVideoFolders);
            XMLHelper.WriteElementToXML(writer, "AutoAddMovieTerms", this.AutoAddMovieTerms);
            XMLHelper.WriteElementToXML(writer, "AutoAddIgnoreSuffixes", this.AutoAddIgnoreSuffixes);

            writer.WriteStartElement("FNPRegexs");
            foreach (FilenameProcessorRE re in this.FNPRegexs)
            {
                writer.WriteStartElement("Regex");
                XMLHelper.WriteAttributeToXML(writer,"Enabled",re.Enabled);
                XMLHelper.WriteAttributeToXML(writer,"RE",re.RE);
                XMLHelper.WriteAttributeToXML(writer,"UseFullPath",re.UseFullPath);
                XMLHelper.WriteAttributeToXML(writer,"Notes",re.Notes);
                writer.WriteEndElement(); // Regex
            }
            writer.WriteEndElement(); // FNPRegexs

            writer.WriteStartElement("RSSURLs");
            foreach (string s in this.RSSURLs) XMLHelper.WriteElementToXML(writer,"URL",s);
            writer.WriteEndElement(); // RSSURLs

            if (this.ShowStatusColors != null)
            {
                writer.WriteStartElement("ShowStatusTVWColors");
                foreach (KeyValuePair<ShowStatusColoringType, System.Drawing.Color> e in this.ShowStatusColors)
                {
                    writer.WriteStartElement("ShowStatusTVWColor");
                    // TODO ... Write Meta Flags
                    XMLHelper.WriteAttributeToXML(writer,"IsMeta",e.Key.IsMetaType);
                    XMLHelper.WriteAttributeToXML(writer,"IsShowLevel",e.Key.IsShowLevel);
                    XMLHelper.WriteAttributeToXML(writer,"ShowStatus",e.Key.Status);
                    XMLHelper.WriteAttributeToXML(writer,"Color",Helpers.TranslateColorToHtml(e.Value));
                    writer.WriteEndElement(); //ShowStatusTVWColor
                }
                writer.WriteEndElement(); // ShowStatusTVWColors
            }

            if (this.Filter != null)
            {
                writer.WriteStartElement("ShowFilters");

                XMLHelper.WriteInfo(writer, "NameFilter", "Name", this.Filter.ShowName);
                XMLHelper.WriteInfo(writer, "ShowStatusFilter", "ShowStatus", this.Filter.ShowStatus);
                XMLHelper.WriteInfo(writer, "ShowNetworkFilter", "ShowNetwork", this.Filter.ShowNetwork);
                XMLHelper.WriteInfo(writer, "ShowRatingFilter", "ShowRating", this.Filter.ShowRating);

                foreach (string genre in this.Filter.Genres) XMLHelper.WriteInfo(writer, "GenreFilter", "Genre", genre);
 
                writer.WriteEndElement(); //ShowFilters
            }

            writer.WriteEndElement(); // settings
        }

        internal float PercentDirtyUpgrade()
        {
            return this.upgradeDirtyPercent;
        }

        public FolderJpgIsType ItemForFolderJpg() => this.FolderJpgIs;

        public string GetVideoExtensionsString() =>this.VideoExtensionsString;
        public string GetOtherExtensionsString() => this.OtherExtensionsString;
        public string GetKeepTogetherString() => this.keepTogetherExtensionsString;
        
        public bool RunPeriodicCheck() => this.runPeriodicCheck;
        public int PeriodicCheckPeriod() =>  this.periodCheckHours * 60* 60 * 1000;
        public bool RunOnStartUp() => this.runStartupCheck;

        public string GetSeasonSearchTermsString() => this.searchSeasonWordsString;
        public string GetPreferredRSSSearchTermsString() => this.preferredRSSSearchTermsString;

        public static bool OKExtensionsString(string s)
        {
            if (string.IsNullOrEmpty(s))
                return true;

            string[] t = s.Split(';');
            foreach (string s2 in t)
            {
                if ((string.IsNullOrEmpty(s2)) || (!s2.StartsWith(".")))
                    return false;
            }
            return true;
        }

        public static string CompulsoryReplacements()
        {
            return "*?<>:/\\|\""; // invalid filename characters, must be in the list!
        }

        public static List<FilenameProcessorRE> DefaultFNPList()
        {
            // Default list of filename processors

            List<FilenameProcessorRE> l = new List<FilenameProcessorRE>
                                              {
                                                  new FilenameProcessorRE(true,
                                                      "(^|[^a-z])s?(?<s>[0-9]+).?[ex](?<e>[0-9]{2,})(-?e[0-9]{2,})*-?[ex](?<f>[0-9]{2,})[^a-z]",
                                                      false, "Multipart Rule : s04e01e02e03 S01E01-E02"),
                                                  new FilenameProcessorRE(true,
                                                                          "(^|[^a-z])s?(?<s>[0-9]+)[ex](?<e>[0-9]{2,})(e[0-9]{2,})*[^a-z]",
                                                                          false, "3x23 s3x23 3e23 s3e23 s04e01e02e03"),
                                                  new FilenameProcessorRE(false,
                                                                          "(^|[^a-z])s?(?<s>[0-9]+)(?<e>[0-9]{2,})[^a-z]",
                                                                          false,
                                                                          "323 or s323 for season 3, episode 23. 2004 for season 20, episode 4."),
                                                  new FilenameProcessorRE(false,
                                                                          "(^|[^a-z])s(?<s>[0-9]+)--e(?<e>[0-9]{2,})[^a-z]",
                                                                          false, "S02--E03"),
                                                  new FilenameProcessorRE(false,
                                                                          "(^|[^a-z])s(?<s>[0-9]+) e(?<e>[0-9]{2,})[^a-z]",
                                                                          false, "'S02.E04' and 'S02 E04'"),
                                                  new FilenameProcessorRE(false, "^(?<s>[0-9]+) (?<e>[0-9]{2,})", false,
                                                                          "filenames starting with '1.23' for season 1, episode 23"),
                                                  new FilenameProcessorRE(false,
                                                                          "(^|[^a-z])(?<s>[0-9])(?<e>[0-9]{2,})[^a-z]",
                                                                          false, "Show - 323 - Foo"),
                                                  new FilenameProcessorRE(true,
                                                                          "(^|[^a-z])se(?<s>[0-9]+)([ex]|ep|xep)?(?<e>[0-9]+)[^a-z]",
                                                                          false, "se3e23 se323 se1ep1 se01xep01..."),
                                                  new FilenameProcessorRE(true,
                                                                          "(^|[^a-z])(?<s>[0-9]+)-(?<e>[0-9]{2,})[^a-z]",
                                                                          false, "3-23 EpName"),
                                                  new FilenameProcessorRE(true,
                                                                          "(^|[^a-z])s(?<s>[0-9]+) +- +e(?<e>[0-9]{2,})[^a-z]",
                                                                          false, "ShowName - S01 - E01"),
                                                  new FilenameProcessorRE(true,
                                                                          "\\b(?<e>[0-9]{2,}) ?- ?.* ?- ?(?<s>[0-9]+)",
                                                                          false,
                                                                          "like '13 - Showname - 2 - Episode Title.avi'"),
                                                  new FilenameProcessorRE(true,
                                                                          "\\b(episode|ep|e) ?(?<e>[0-9]{2,}) ?- ?(series|season) ?(?<s>[0-9]+)",
                                                                          false, "episode 3 - season 4"),
                                                  new FilenameProcessorRE(true,
                                                                          "season (?<s>[0-9]+)\\\\e?(?<e>[0-9]{1,3}) ?-",
                                                                          true, "Show Season 3\\E23 - Epname"),
                                                  new FilenameProcessorRE(false,
                                                                          "season (?<s>[0-9]+)\\\\episode (?<e>[0-9]{1,3})",
                                                                          true, "Season 3\\Episode 23")
                                              };
            return l;
        }

        private static List<Replacement> DefaultListRE()
        {
            return new List<Replacement>
                       {
                           new Replacement("*", "#", false),
                           new Replacement("?", "", false),
                           new Replacement(">", "", false),
                           new Replacement("<", "", false),
                           new Replacement(":", "-", false),
                           new Replacement("/", "-", false),
                           new Replacement("\\", "-", false),
                           new Replacement("|", "-", false),
                           new Replacement("\"", "'", false)
                       };
        }

        private static List<string> DefaultRSSURLList()
        {
            List<string> sl = new List<string>();
            return sl;
        }

        public static string[] TabNames()
        {
            return new[] {"MyShows", "Scan", "WTW"};
        }

        public static string TabNameForNumber(int n)
        {
            if ((n >= 0) && (n < TabNames().Length))
                return TabNames()[n];
            return "";
        }

        public static int TabNumberFromName(string n)
        {
            int r = 0;
            if (!string.IsNullOrEmpty(n))
                r = Array.IndexOf(TabNames(), n);
            if (r < 0)
                r = 0;
            return r;
        }

        public bool UsefulExtension(string sn, bool otherExtensionsToo)
        {
            foreach (string s in this.VideoExtensionsArray)
            {
                if (sn.ToLower() == s)
                    return true;
            }
            if (otherExtensionsToo)
            {
                foreach (string s in this.OtherExtensionsArray)
                {
                    if (sn.ToLower() == s)
                        return true;
                }
            }

            return false;
        }

        public bool KeepExtensionTogether(string extension)
        {
            if (this.KeepTogether == false) return false;

            if (this.keepTogetherMode == KeepTogetherModes.All) return true;

            if (this.keepTogetherMode == KeepTogetherModes.Just) return this.keepTogetherExtensionsArray.Contains(extension);

            if (this.keepTogetherMode == KeepTogetherModes.AllBut ) return !this.keepTogetherExtensionsArray.Contains(extension);

            logger.Error("INVALID USE OF KEEP EXTENSION");
            return false;
        }

        public string BTSearchURL(ProcessedEpisode epi)
        {
            if (epi == null)
                return "";

            SeriesInfo s = epi.TheSeries;
            if (s == null)
                return "";

            string url = (epi.SI.UseCustomSearchURL && !string.IsNullOrWhiteSpace(epi.SI.CustomSearchURL))
                ? epi.SI.CustomSearchURL
                : this.TheSearchers.CurrentSearchURL();
            return CustomName.NameForNoExt(epi, url, true);
        }

        public string FilenameFriendly(string fn)
        {
            if (string.IsNullOrWhiteSpace(fn)) return "";

            foreach (Replacement rep in this.Replacements)
            {
                if (rep.CaseInsensitive)
                    fn = Regex.Replace(fn, Regex.Escape(rep.This), Regex.Escape(rep.That), RegexOptions.IgnoreCase);
                else
                    fn = fn.Replace(rep.This, rep.That);
            }
            if (this.ForceLowercaseFilenames)
                fn = fn.ToLower();
            return fn;
        }

        public bool NeedToDownloadBannerFile(){
            // Return true iff we need to download season specific images
            // There are 4 possible reasons
            return (SeasonSpecificFolderJPG() || this.KODIImages || this.SeriesJpg || this.FanArtJpg);
        }

        // ReSharper disable once InconsistentNaming
        public bool SeasonSpecificFolderJPG() {
            return (FolderJpgIsType.SeasonPoster == this.FolderJpgIs);
        }

        public bool DownloadFrodoImages()
        {
            return (this.KODIImages && (this.SelectedKODIType == KODIType.Both || this.SelectedKODIType == KODIType.Frodo));
        }

        public bool DownloadEdenImages()
        {
            return (this.KODIImages && (this.SelectedKODIType == KODIType.Both || this.SelectedKODIType == KODIType.Eden)); 
        }

        public bool KeepTogetherFilesWithType(string fileExtension)
        {
            if (this.KeepTogether == false) return false;

            switch (this.keepTogetherMode)
            {
                case KeepTogetherModes.All: return true;
                case KeepTogetherModes.Just: return this.keepTogetherExtensionsArray.Contains(fileExtension);
                case KeepTogetherModes.AllBut: return !this.keepTogetherExtensionsArray.Contains(fileExtension);

            }
            return true;
        }
    }
}
