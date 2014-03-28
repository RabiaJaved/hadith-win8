using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Xml;

// For Live Tiles
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace HadithApp
{
    public class Hadith
    {
        public string Book { get; set; }
        public string Narrator { get; set; }
        public string Content { get; set; }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        bool _initialized = false;
        int index = 0;

        static List<Hadith> ahadith = new List<Hadith>();

        
        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private void Grid_Loaded_1(object sender, RoutedEventArgs e)
        {
            if (_initialized)
            {
                return;
            }

            List<Hadith> ahadith = GetAllHadithFromFile("Hadith.xml");

            List<FlipViewItem> flipViewItems = new List<FlipViewItem>();
            Random rand = new Random();

            foreach (Hadith currentHadith in ahadith)
            {
                TextBlock mainTextBlock = new TextBlock();
                mainTextBlock.Text = currentHadith.Narrator + "\r\n\r\n" + currentHadith.Content;
                mainTextBlock.TextWrapping = TextWrapping.Wrap;
                mainTextBlock.FontSize = 30;
                mainTextBlock.Foreground = new SolidColorBrush(Windows.UI.Colors.BlanchedAlmond);
                mainTextBlock.FontFamily = new FontFamily("Cambria");
                mainTextBlock.Opacity = 0.9;

                FlipViewItem item = new FlipViewItem();
                item.Content = mainTextBlock;

                // TODO: randomize order
                MainFlipView.Items.Add(item);
            }

            UpdateLiveTile();

             _initialized = true;
            
            // Data bind
            //MainFlipView.ItemsSource = Ahadith;
        }

        private static List<Hadith> GetAllHadithFromFile(string filename)
        {
            XmlReader reader = XmlReader.Create(filename);
            Hadith hadith = new Hadith();
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "book")
                {
                    reader.Read();
                    hadith.Book = "Sahih Bukhari: " + reader.Value;
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "narrator")
                {
                    reader.Read();
                    hadith.Narrator = reader.Value;
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "content")
                {
                    reader.Read();
                    hadith.Content = reader.Value;
                }

                // When we have all the info for this hadith, load it into ahadith list and reset current hadith
                if (hadith.Book != null && hadith.Content != null && hadith.Narrator != null)
                {
                    ahadith.Add(hadith);
                    hadith = new Hadith();
                }
            }
            return ahadith;
        }

        private void MainFlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Update the Heading TextBlock with Book/Chapter/etc information
            Hadith currentHadith = ahadith[MainFlipView.SelectedIndex];
            headingTextBlock.Text = currentHadith.Book;
        }

        private static void UpdateLiveTile()
        {
            // Get random hadith
            int hadithIndex = (new Random()).Next() % ahadith.Count;
            Hadith hadith = ahadith[hadithIndex];

            // Create wide tile
            XmlDocument tileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileWide310x150Text04);
            XmlNodeList tileXmlTextNodes = tileXml.GetElementsByTagName("text");
            tileXmlTextNodes[0].InnerText = hadith.Content;

            // Create square tile
            XmlDocument squareTileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare150x150Text02);
            XmlNodeList squareXmlTextNodes = tileXml.GetElementsByTagName("text");
            squareXmlTextNodes[0].InnerText = hadith.Content;

            // Combine wide and square into one XML
            IXmlNode node = tileXml.ImportNode(squareTileXml.GetElementsByTagName("binding").Item(0), true);
            tileXml.GetElementsByTagName("visual").Item(0).AppendChild(node);

            // Send notification
            TileNotification notification = new TileNotification(tileXml);
            notification.ExpirationTime = DateTimeOffset.UtcNow.AddDays(1);
            TileUpdateManager.CreateTileUpdaterForApplication().Update(notification);
        }
    }
}
