using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ManufacturingERP.ViewModels;

namespace ManufacturingERP.Views;

public partial class MasterDataView : UserControl
{
    private Point _startPoint;

    public MasterDataView()
    {
        InitializeComponent();
    }

    private void RoutingList_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(null);
    }

    private void RoutingList_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            Point mousePos = e.GetPosition(null);
            Vector diff = _startPoint - mousePos;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                ListBox listBox = sender as ListBox;
                ListBoxItem listBoxItem = FindVisualParent<ListBoxItem>((DependencyObject)e.OriginalSource);

                if (listBoxItem != null)
                {
                    RoutingStepItem data = (RoutingStepItem)listBoxItem.DataContext;
                    DataObject dragData = new DataObject("RoutingStepFormat", data);
                    DragDrop.DoDragDrop(listBoxItem, dragData, DragDropEffects.Move);
                }
            }
        }
    }

    private void RoutingList_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("RoutingStepFormat"))
        {
            RoutingStepItem droppedData = e.Data.GetData("RoutingStepFormat") as RoutingStepItem;
            ListBoxItem listBoxItem = FindVisualParent<ListBoxItem>((DependencyObject)e.OriginalSource);

            if (listBoxItem != null && DataContext is MasterDataViewModel vm)
            {
                RoutingStepItem targetData = listBoxItem.DataContext as RoutingStepItem;

                int oldIndex = vm.RoutingSteps.IndexOf(droppedData);
                int newIndex = vm.RoutingSteps.IndexOf(targetData);

                if (oldIndex != -1 && newIndex != -1 && oldIndex != newIndex)
                {
                    vm.RoutingSteps.Move(oldIndex, newIndex);
                    vm.IsRoutingDirty = true;
                    
                    // Auto re-index StepNo by 10s
                    for (int i = 0; i < vm.RoutingSteps.Count; i++)
                    {
                        vm.RoutingSteps[i].StepNo = (i + 1) * 10;
                    }
                }
            }
        }
    }

    private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
        DependencyObject parentObject = VisualTreeHelper.GetParent(child);
        if (parentObject == null) return null;
        T parent = parentObject as T;
        if (parent != null) return parent;
        return FindVisualParent<T>(parentObject);
    }
}

