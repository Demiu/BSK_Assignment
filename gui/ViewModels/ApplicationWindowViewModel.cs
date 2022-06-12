﻿using System;
using System.Collections.ObjectModel;
using System.IO;

namespace gui.ViewModels;

public class ApplicationWindowViewModel
{
    public ObservableCollection<Node> Items { get; }
    public string strFolder { get; }

    public ApplicationWindowViewModel() {
        // TODO: to be removed, because it must be at the start /
        strFolder = @"D:\INFA\Rider\BSK_2\cli\bin\Debug\net6.0"; 

        Items = new ObservableCollection<Node>();

        Node rootNode = new Node(strFolder);
        rootNode.Subfolders = GetSubfolders(strFolder);

        Items.Add(rootNode);
    }
    
    public ObservableCollection<Node> GetSubfolders(string strPath) {
        ObservableCollection<Node> subfolders = new ObservableCollection<Node>();
        string[] subdirs = Directory.GetFileSystemEntries(strPath, "*", SearchOption.TopDirectoryOnly);

        foreach (string dir in subdirs) {
            Node thisnode = new Node(dir);
            
            try 
            {
                if (Directory.GetFileSystemEntries(dir, "*", SearchOption.TopDirectoryOnly).Length > 0) {
                    thisnode.Subfolders = new ObservableCollection<Node>();
                    thisnode.Subfolders = GetSubfolders(dir);
                }
            }
            catch (Exception error) {
                // ignored
            }

            subfolders.Add(thisnode);
        }

        return subfolders;
    }

    public class Node
    {
        public ObservableCollection<Node> Subfolders { get; set; }

        public string strNodeText { get; }
        public string strFullPath { get; }

        public Node(string _strFullPath) {
            strFullPath = _strFullPath;
            strNodeText = Path.GetFileName(_strFullPath);
        }
    }

}