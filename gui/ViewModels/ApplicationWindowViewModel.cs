using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Gui.ViewModels;

public class ApplicationWindowViewModel : ViewModelBase
{
    Lib.Connection connection;

    public ObservableCollection<Node> Items { get; }
    //public string strFolder { get; }

    public ApplicationWindowViewModel(Lib.Connection connection) {
        this.connection = connection;
        this.Items = new(){
            new Node(this, Lib.Defines.Constants.BASE_NET_PATH, connection.RemoteEndPoint!.ToString()!, true)
        };

        connection.OnNewDirectoryEntry += NewDirectoryEntryCallback;
    }

    public void NewDirectoryEntryCallback(string path, Lib.Defines.FileSystemKind kind) {
        Items[0].AddEntry(path, path.PathGetSegments(), kind);
    }

    public void Explore(string path) {
        connection.GetFileDirectory(path);
    }

    public void Download(string path) {
        connection.RequestFile(path);
    }

    public class Node
    {
        ApplicationWindowViewModel vm;

        public ObservableCollection<Node> Subfolders { get; set; }
        public string strNodeText { get; }
        public string strFullPath { get; }
        public bool isFolder { get; }
        public IBrush textColor => 
            isFolder 
            ? new ImmutableSolidColorBrush(Color.Parse("#3b719f"), 1) 
            : new ImmutableSolidColorBrush(Color.Parse("#CB4C4E"), 1);

        public Node(ApplicationWindowViewModel vm, string fullPath, bool isFolder)
        : this(vm, fullPath, Path.GetFileName(fullPath), isFolder)
        { }

        public Node(ApplicationWindowViewModel vm, string fullPath, string nodeText, bool isFolder) {
            this.vm = vm;
            this.Subfolders = new();
            this.strFullPath = fullPath;
            this.strNodeText = nodeText;
            this.isFolder = isFolder;
        }

        public void AddEntry(string fullPath, Stack<string> segments, Lib.Defines.FileSystemKind kind) {
            var name = segments.Pop();
            Node? found = null;
            foreach (var item in Subfolders) {
                if (item.strNodeText == name) {
                    found = item;
                }
            }
            if (found == null) {
                found = new Node(vm, fullPath, kind == Lib.Defines.FileSystemKind.Directory);
                Subfolders.Add(found);
            }
            if (segments.Count != 0) {
                found.AddEntry(fullPath, segments, kind);
            }
        }

        public void ExploreClicked() {
            vm.Explore(strFullPath);
        }

        public void DownloadClicked() {
            vm.Download(strFullPath);
        }
    }
}