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
