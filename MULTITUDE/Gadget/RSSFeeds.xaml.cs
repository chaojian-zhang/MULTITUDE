using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MULTITUDE.Gadget
{
    /// <summary>
    /// Interaction logic for RSSFeeds.xaml
    /// </summary>
    public partial class RSSFeeds : Window
    {
        // Feeds locations provided by our application
        // Might provide an itnerface to customize it
        // http://www.cbc.ca/rss/
        // https://en.wikipedia.org/wiki/Wikipedia:Syndication#RSS_feeds
        // We can also use this for our Mobile

        public RSSFeeds()
        {
            InitializeComponent();
        }
    }
}
