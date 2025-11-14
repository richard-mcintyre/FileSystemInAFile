using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FileSystemInAFile.Browser;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public static readonly RoutedCommand ExitApplicationCommand = new RoutedCommand();

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(FileSystemEntry), typeof(MainWindow),
            new PropertyMetadata(null, (s, a) =>
            {
                MainWindow wnd = (MainWindow)s;
                FileSystemEntry entry = (FileSystemEntry)a.NewValue;

                if (entry is not null && entry.IsFile)
                {
                    BitmapImage bm = new BitmapImage();
                    bm.BeginInit();
                    bm.CacheOption = BitmapCacheOption.OnLoad;
                    bm.StreamSource = wnd._fileSystem.Open(System.IO.Path.Combine(entry.Path, entry.Name), FileSystemFileMode.Read);
                    bm.EndInit();

                    wnd.img.Source = bm;
                }
                else
                {
                    wnd.img.Source = null;
                }
            }));

    public MainWindow()
    {
        InitializeComponent();

        _fileSystem = FileSystem.OpenExisting(@"C:\git\FileSystemInAFile\rcmtest.bin");

        //ICollectionView view = CollectionViewSource.GetDefaultView(_entries);
        //view.SortDescriptions.Add(new SortDescription(".", ListSortDirection.Ascending));

        ShowDirectoryContents(FileSystemPath.DirectorySeparatorChar.ToString());

        this.DataContext = this;
    }

    private readonly FileSystem _fileSystem;
    private readonly ObservableCollection<FileSystemEntry> _entries = new ObservableCollection<FileSystemEntry>();

    private string _currentPath;

    public ObservableCollection<FileSystemEntry> Entries => _entries;

    public FileSystemEntry SelectedItem
    {
        get => (FileSystemEntry)GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    protected override void OnClosed(EventArgs args)
    {
        _fileSystem?.Dispose();

        base.OnClosed(args);
    }
    private void ExitApplicationCommand_Executed(object sender, ExecutedRoutedEventArgs args) =>
        Application.Current.Shutdown();

    private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs args)
    {
        if (lstEntries.SelectedItem is null)
            return;

        FileSystemEntry entry = (FileSystemEntry)lstEntries.SelectedItem;
        if (entry.IsFile)
            return;

        if (entry.Name == "..")
        {
            string path = _currentPath;

            path = FileSystemPath.GetDirectoryName(path);
            ShowDirectoryContents(path);
        }
        else
        {
            ShowDirectoryContents(FileSystemPath.Combine(_currentPath, entry.Name));
        }
    }

    private void ShowDirectoryContents(string path)
    {
        _currentPath = path;

        _entries.Clear();

        if (path != FileSystemPath.DirectorySeparatorChar.ToString())
            _entries.Add(new FileSystemEntry(path, "..", false, 0));

        foreach (FileSystemEntry entry in _fileSystem.GetDirectoryContents(_currentPath).OrderBy(o => o.Name))
            _entries.Add(entry);
    }
}