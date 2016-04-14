using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Graphics.Printing;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Printing;

namespace LC.Helpers
{
    public class PrintHelper : IDisposable
    {
        private TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();
        private string _taskTitle = null;
        private PrintDocument _printDoc = null;
        private IPrintDocumentSource _printDocSource = null;
        private List<UIElement> _printPages = null;

        public PrintHelper()
        {
            RegisterForPrinting();
        }

        public async Task PrintAsync<T>(IEnumerable<T> list, Func<int, StackPanel> GetNewPage, Func<T, UIElement> GetPageItem, int itemsPerPage, string taskTitle)
        {
            List<UIElement> printPages = GetPrintPages(list,
                                                       GetNewPage,
                                                       GetPageItem,
                                                       itemsPerPage);

            if (printPages == null || printPages.Count == 0)
            {
                return;
            }

            _taskTitle = taskTitle;
            _printPages = printPages;

            await PrintManager.ShowPrintUIAsync();

            await _tcs.Task;
        }

        private List<UIElement> GetPrintPages<T>(IEnumerable<T> list, Func<int, StackPanel> GetNewPage, Func<T, UIElement> GetPageItem, int itemsPerPage)
        {
            var printPages = new List<UIElement>();

            // First page.
            int pageNumber = 1;
            StackPanel pagePanel = GetNewPage(pageNumber++);
            pagePanel.RequestedTheme = ElementTheme.Light; // Required.

            int pageItemsCounter = 0;

            foreach (var item in list)
            {
                // Add item to page and increase counter.
                UIElement pageItem = GetPageItem(item);
                pagePanel.Children.Add(pageItem);
                pageItemsCounter++;

                // Start new page if full.
                if (pageItemsCounter == itemsPerPage)
                {
                    // Add full page to list.
                    printPages.Add(pagePanel);

                    // Start new page.
                    pagePanel = GetNewPage(pageNumber++);
                    pagePanel.RequestedTheme = ElementTheme.Light; // Required.
                    pageItemsCounter = 0;
                }
            }

            // Add last page to list.
            if (pageItemsCounter > 0)
            {
                printPages.Add(pagePanel);
            }

            return printPages;
        }

        private void PrintDoc_Paginate(object sender, PaginateEventArgs e)
        {
            PrintTaskOptions printingOptions = e.PrintTaskOptions;
            PrintPageDescription pageDescription = printingOptions.GetPageDescription(0);

            ((PrintDocument)sender).SetPreviewPageCount(_printPages.Count, PreviewPageCountType.Intermediate);
        }

        private void PrintDoc_GetPreviewPage(object sender, GetPreviewPageEventArgs e)
        {
            ((PrintDocument)sender).SetPreviewPage(e.PageNumber, _printPages[e.PageNumber - 1]);
        }

        private void PrintDoc_AddPages(object sender, AddPagesEventArgs e)
        {
            foreach (UIElement item in _printPages)
            {
                _printDoc.AddPage(item);
            }

            _printDoc.AddPagesComplete();
        }

        private void PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs args)
        {
            PrintTask printTask = null;

            printTask = args.Request.CreatePrintTask(_taskTitle, (print) =>
            {
                // Print job completed.
                printTask.Completed += (s, _args) =>
                {
                    //if (_args.Completion == PrintTaskCompletion.Canceled)
                    //{
                    //    _tcs.SetCanceled();
                    //}

                    //if (_args.Completion == PrintTaskCompletion.Failed)
                    //{
                    //    _tcs.SetException(...);
                    //}

                    _tcs.SetResult(null);
                };

                print.SetSource(_printDocSource);
            });
        }

        private void RegisterForPrinting()
        {
            // Configure pagination, preview and printing using events,
            // because printing is asynchronous.
            _printDoc = new PrintDocument();
            _printDocSource = _printDoc.DocumentSource;
            _printDoc.Paginate += PrintDoc_Paginate;
            _printDoc.GetPreviewPage += PrintDoc_GetPreviewPage;
            _printDoc.AddPages += PrintDoc_AddPages;
            PrintManager.GetForCurrentView().PrintTaskRequested += PrintTaskRequested;
        }

        private void UnregisterForPrinting()
        {
            _printDoc.Paginate -= PrintDoc_Paginate;
            _printDoc.GetPreviewPage -= PrintDoc_GetPreviewPage;
            _printDoc.AddPages -= PrintDoc_AddPages;
            PrintManager.GetForCurrentView().PrintTaskRequested -= PrintTaskRequested;
        }

        public void Dispose()
        {
            UnregisterForPrinting();
        }
    }
}