using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;

namespace TVRename
{
    public interface TVSource
    {
        void Setup(FileInfo loadFrom, FileInfo cacheFile, CommandLineArgs args);
        bool Connect();
        void SaveCache();

        bool EnsureUpdated(int code, bool bannersToo);
        bool GetUpdates();
        void UpdatesDoneOK();

        SeriesInfo GetSeries(string showName);
        SeriesInfo GetSeries(int id);
        bool HasSeries(int id);

        void Tidy(ICollection<ShowItem> libraryValues);

        void ForgetEverything();
        void ForgetShow(int id, bool makePlaceholder);

    }
}
