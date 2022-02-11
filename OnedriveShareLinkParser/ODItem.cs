using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnedriveShareLinkParser
{
    class ODItem
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Size { get; set; }
        public string ModifiedTime { get; set; }
        public string Path { get; set; }
        public string DownloadLink { get; set; }

        public override bool Equals(object obj)
        {
            return obj is ODItem item &&
                   Id == item.Id;
        }

        public override int GetHashCode()
        {
            return 2108858624 + EqualityComparer<string>.Default.GetHashCode(Id);
        }

        public override string ToString()
        {
            return $"[Name: {Name}, ModifiedTime: {ModifiedTime}, Path: {Path}, DownloadLink: {DownloadLink}]";
        }
    }
}
