using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BeautyCons.IconGlow
{
    public static class TileFinder
    {
        public static Image FindSelectedGameIcon(DependencyObject root)
        {
            if (root == null) return null;

            try
            {
                var gameView = FindChildByName<FrameworkElement>(root, "PART_ControlGameView");
                if (gameView != null)
                {
                    var icon = FindChildByName<Image>(gameView, "PART_ImageIcon");
                    if (icon != null && icon.ActualWidth > 0 && icon.ActualHeight > 0)
                        return icon;
                }

                var allIcons = new List<Image>();
                FindAllChildrenByName(root, "PART_ImageIcon", allIcons);

                foreach (var icon in allIcons)
                {
                    if (icon.IsVisible && icon.ActualWidth > 0 && icon.ActualHeight > 0)
                        return icon;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public static T FindChildByName<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T element && element.Name == name)
                    return element;

                var result = FindChildByName<T>(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }

        public static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T match)
                    return match;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private static void FindAllChildrenByName(DependencyObject parent, string name, List<Image> results)
        {
            if (parent == null) return;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is Image image && image.Name == name)
                    results.Add(image);

                FindAllChildrenByName(child, name, results);
            }
        }
    }
}
