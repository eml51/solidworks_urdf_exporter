﻿using log4net;
using SW2URDF.URDF;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace SW2URDF.UI
{
    /// <summary>
    /// Interaction logic for TreeMergeWPF.xaml
    /// </summary>
    public partial class TreeMergeWPF : Window
    {
        private static readonly ILog logger = Logger.GetLogger();

        private static readonly int MAX_LABEL_CHARACTER_WIDTH = 40;
        private static readonly int MAX_BUTTON_CHARACTER_WIDTH = 20;
        private static readonly string DEFAULT_COORDINATE_SYSTEM_TEXT = "Select Coordinate System";
        private static readonly string DEFAULT_AXIS_TEXT = "Select Reference Axis";
        private static readonly string DEFAULT_JOINT_TYPE_TEXT = "Select Joint Type";

        private readonly string CSVFileName;
        private readonly string AssemblyName;

        public TreeMergeWPF(List<string> coordinateSystems, List<string> referenceAxes, string csvFileName, string assemblyName)
        {
            CSVFileName = csvFileName;
            AssemblyName = assemblyName;

            InitializeComponent();
            ConfigureMenus(coordinateSystems, referenceAxes);
            ConfigureLabels();
        }

        public void SetTrees(LinkNode existingNode, LinkNode loadedNode)
        {
            ExistingTreeView.Items.Clear();
            LoadedTreeView.Items.Clear();

            TreeViewItem existing = BuildTreeViewItem(existingNode);
            TreeViewItem loaded = BuildTreeViewItem(loadedNode);

            ExistingTreeView.MouseMove += TreeViewMouseMove;
            ExistingTreeView.Drop += TreeViewDrop;

            ExistingTreeView.Items.Add(existing);
            LoadedTreeView.Items.Add(loaded);

            ExistingTreeView.AllowDrop = true;
            LoadedTreeView.AllowDrop = true;
        }

        private void FillExistingLinkProperties(Link link, bool isBaseLink)
        {
            ExistingLinkNameTextBox.Text = link.Name;

            if (isBaseLink)
            {
                ExistingJointNameTextBox.Text = "";
                ExistingCoordinatesMenu.Visibility = Visibility.Hidden;
                ExistingAxisMenu.Visibility = Visibility.Hidden;
                ExistingJointTypeMenu.Visibility = Visibility.Hidden;
            }
            else
            {
                ExistingJointNameTextBox.Text = link.Joint.Name;
                SetDropdownContextMenu(ExistingCoordinatesMenu, link.Joint.CoordinateSystemName, DEFAULT_COORDINATE_SYSTEM_TEXT);
                SetDropdownContextMenu(ExistingAxisMenu, link.Joint.AxisName, DEFAULT_AXIS_TEXT);
                SetDropdownContextMenu(ExistingJointTypeMenu, link.Joint.Type, DEFAULT_JOINT_TYPE_TEXT);
            }
        }

        private void FillLoadedLinkProperties(Link link, bool isBaseLink)
        {
            LoadedLinkNameTextBox.Text = link.Name;

            if (isBaseLink)
            {
                LoadedJointNameTextLabel.Content = null;
                LoadedCoordinateSystemTextLabel.Content = null;
                LoadedAxisTextLabel.Content = null;
                LoadedJointTypeTextLabel.Content = null;
            }
            else
            {
                LoadedJointNameTextLabel.Content = new TextBox { Text = link.Name };
                LoadedCoordinateSystemTextLabel.Content = new TextBox { Text = link.Joint.CoordinateSystemName };
                LoadedAxisTextLabel.Content = new TextBox { Text = link.Joint.AxisName };
                LoadedJointTypeTextLabel.Content = new TextBox { Text = link.Joint.Type };
            }
        }

        private void OnTreeItemClick(object sender, RoutedEventArgs e)
        {
            TreeView tree = (TreeView)sender;
            if (tree.SelectedItem == null)
            {
                return;
            }

            TreeViewItem selectedItem = (TreeViewItem)tree.SelectedItem;

            Link link = (Link)selectedItem.Tag;
            bool isBaseLink = selectedItem.Parent.GetType() == typeof(TreeView);
            if (tree == ExistingTreeView)
            {
                FillExistingLinkProperties(link, isBaseLink);
            }
            else if (tree == LoadedTreeView)
            {
                FillLoadedLinkProperties(link, isBaseLink);
            }
        }

        private void SetDropdownContextMenu(Button button, string name, string defaultText)
        {
            button.Visibility = Visibility.Visible;
            if (name == null)
            {
                return;
            }

            TextBox buttonText = (TextBox)button.Content;

            foreach (MenuItem item in button.ContextMenu.Items)
            {
                TextBlock header = (TextBlock)item.Header;
                if (header.Text == name)
                {
                    item.IsChecked = true;
                    buttonText.Text = name;
                    return;
                }
            }

            logger.Error("Item " + name + " was not found in the dropdown for " + button.Name);
            buttonText.Text = defaultText;
        }

        private string ShortenStringForLabel(string text, int numCharacters)
        {
            string result = text;
            if (text.Length > numCharacters)
            {
                string extension = Path.GetExtension(text);
                int numToKeep = numCharacters - "...".Length - extension.Length;
                result = text.Substring(0, numToKeep) + "..." + extension;
            }
            return result;
        }

        private TextBlock BuildTextBlock(string boldBit, string regularBit)
        {
            TextBlock block = new TextBlock();
            block.Inlines.Add(new Bold(new Run(boldBit)));
            block.Inlines.Add(regularBit);
            return block;
        }

        private void ConfigureLabels()
        {
            string longAssemblyName = ShortenStringForLabel(AssemblyName, MAX_LABEL_CHARACTER_WIDTH);
            string shortAssemblyName = ShortenStringForLabel(AssemblyName, MAX_BUTTON_CHARACTER_WIDTH);

            string longCSVFilename = ShortenStringForLabel(CSVFileName, MAX_LABEL_CHARACTER_WIDTH);
            string shortCSVFilename = ShortenStringForLabel(CSVFileName, MAX_BUTTON_CHARACTER_WIDTH);

            ExistingTreeLabel.Content = BuildTextBlock("Configuration from Assembly: ", longAssemblyName);
            LoadedTreeLabel.Content = BuildTextBlock("Configuration from CSV: ", longCSVFilename);

            MassInertiaExistingButton.Content = new TextBlock { Text = shortAssemblyName };
            VisualExistingButton.Content = new TextBlock { Text = shortAssemblyName };
            JointKinematicsExistingButton.Content = new TextBlock { Text = shortAssemblyName };
            OtherJointExistingButton.Content = new TextBlock { Text = shortAssemblyName };

            MassInertiaLoadedButton.Content = new TextBlock { Text = shortCSVFilename };
            VisualLoadedButton.Content = new TextBlock { Text = shortCSVFilename };
            JointKinematicsLoadedButton.Content = new TextBlock { Text = shortCSVFilename };
            OtherJointLoadedButton.Content = new TextBlock { Text = shortCSVFilename };
        }

        private void ProcessDragDrop(TreeView treeView, TreeViewItem target, TreeViewItem package)
        {
            if (package.Parent == treeView)
            {
            }
            else if (package.Parent.GetType() == typeof(TreeViewItem))
            {
                TreeViewItem packageParent = (TreeViewItem)package.Parent;

                packageParent.Items.Remove(package);

                target.Items.Add(package);
            }
            else
            {
                logger.Warn("Unhandled package parent " + package.Parent.GetType());
            }
        }

        private void TreeViewDrop(object sender, DragEventArgs e)
        {
            TreeViewItem package = e.Data.GetData(typeof(TreeViewItem)) as TreeViewItem;
            if (package != null & package != e.Source)
            {
                if (e.Source.GetType() == typeof(TreeViewItem))
                {
                    ProcessDragDrop((TreeView)sender, (TreeViewItem)e.Source, package);
                }
                else if (e.Source.GetType() == typeof(TreeView))
                {
                }
                else
                {
                    logger.Warn("Unhandled drop target " + e.Source.GetType());
                }
            }
        }

        private void TreeViewMouseMove(object sender, MouseEventArgs e)
        {
            TreeView treeView = sender as TreeView;
            if (e.MouseDevice.LeftButton == MouseButtonState.Pressed)
            {
                DependencyObject dependencyObject = treeView.InputHitTest(e.GetPosition(treeView)) as DependencyObject;
                //Point downPos = e.GetPosition(null);

                if (treeView.SelectedValue != null)
                {
                    //TreeViewItem treeviewItem = e.Source as TreeViewItem;
                    DragDrop.DoDragDrop(treeView, treeView.SelectedValue, DragDropEffects.Move);
                    e.Handled = true;
                }
            }
        }

        private void TreeViewClick(object sender, MouseButtonEventArgs e)
        {
            TreeView treeView = sender as TreeView;
        }

        private TreeViewItem BuildTreeViewItem(LinkNode node)
        {
            TreeViewItem item = new TreeViewItem
            {
                Tag = node.Link,
                IsExpanded = true,
                AllowDrop = true,
                Name = node.Name,
                Header = node.Name,
            };

            foreach (LinkNode child in node.Nodes)
            {
                item.Items.Add(BuildTreeViewItem(child));
            }

            return item;
        }

        private void ConfigureMenus(List<string> coordinateSystems, List<string> referenceAxes)
        {
            SetMenu(ExistingCoordinatesMenu, coordinateSystems);
            SetMenu(ExistingAxisMenu, referenceAxes);
        }

        private void SetMenu(Button button, List<string> menuContents)
        {
            bool isFirst = true;
            foreach (string menuItemLabel in menuContents)
            {
                MenuItem menuItem = new MenuItem
                {
                    Header = new TextBlock { Text = menuItemLabel },
                    IsCheckable = true,
                    IsChecked = isFirst,
                };
                isFirst = false;

                menuItem.Checked += MenuItemChecked;
                button.ContextMenu.Items.Add(menuItem);
            }
        }

        private void MenuClick(object sender, RoutedEventArgs e)
        {
            (sender as Button).ContextMenu.IsEnabled = true;
            (sender as Button).ContextMenu.PlacementTarget = (sender as Button);
            (sender as Button).ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            (sender as Button).ContextMenu.IsOpen = true;
        }

        private void MenuItemChecked(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            logger.Info("Parent type " + menuItem.Parent.GetType());
            ContextMenu contextMenuParent = menuItem.Parent as ContextMenu;
            foreach (MenuItem item in contextMenuParent.Items)
            {
                if (item != sender)
                {
                    logger.Info("Unchecking " + item.Header);
                    item.IsChecked = false;
                }
            }
            Button button = contextMenuParent.PlacementTarget as Button;
            TextBlock menuItemText = menuItem.Header as TextBlock;
            if (menuItemText == null)
            {
                logger.Info("MenuItemText is null here");
                return;
            }
            button.Content = new TextBlock
            {
                Text = menuItemText.Text,
            };
        }
    }
}