using LC.Helpers;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PrintTest
{
    class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Print_Click(object sender, RoutedEventArgs e)
        {
            const int ITEMS_PER_PAGE = 25;

            var list = new List<Person>();

            // Populate dummy list.
            for (int i = 1; i < 51; i++)
            {
                list.Add(new Person { Name = "Person" + i, Age = i });
            }

            using (var printHelper = new PrintHelper())
            {
                await printHelper.PrintAsync(
                    list,
                    (pageNumber) => // New page incl. header.
                    {
                        // Init page panel.
                        var pagePanel = new StackPanel();
                        pagePanel.Orientation = Orientation.Vertical;

                        // Page header.
                        var textBlock = new TextBlock();
                        textBlock.Text = $"Page {pageNumber}";
                        textBlock.FontSize = 20;
                        textBlock.Margin = new Thickness(12);
                        pagePanel.Children.Add(textBlock);

                        return pagePanel;
                    },
                    (p) => // Page item.
                    {
                        // Item data.
                        var textBlock = new TextBlock();
                        textBlock.Text = $"{p.Name} - {p.Age.ToString().PadLeft(3)} year(s) old";
                        textBlock.FontFamily = new FontFamily("Consolas");
                        textBlock.Margin = new Thickness(12, 0, 0, 0);

                        return textBlock;
                    },
                    ITEMS_PER_PAGE,
                    "PrintTest document");
            }
        }
    }
}
