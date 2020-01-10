﻿using System;
using System.Windows;
using MahApps.Metro;

namespace GroupMeClient.Themes
{
    /// <summary>
    /// <see cref="ThemeManager"/> provides support for changing the GroupMe Desktop Client theme at runtime.
    /// </summary>
    public class ThemeManager
    {
        private static readonly ResourceDictionary GroupMeLightTheme = new ResourceDictionary()
        {
            Source = new Uri("pack://application:,,,/GroupMeLight.xaml"),
        };

        private static readonly ResourceDictionary GroupMeDarkTheme = new ResourceDictionary()
        {
            Source = new Uri("pack://application:,,,/GroupMeDark.xaml"),
        };

        private static ResourceDictionary currentGroupMeTheme = null;

        private static ResourceDictionary CurrentGroupMeTheme
        {
            get
            {
                if (currentGroupMeTheme == null)
                {
                    foreach (var dictionary in Application.Current.Resources.MergedDictionaries)
                    {
                        if (dictionary.Source.ToString().Contains("GroupMe"))
                        {
                            currentGroupMeTheme = dictionary;
                        }
                    }
                }

                return currentGroupMeTheme;
            }

            set
            {
                Application.Current.Resources.MergedDictionaries.Remove(CurrentGroupMeTheme);
                Application.Current.Resources.MergedDictionaries.Add(value);
                currentGroupMeTheme = value;
            }
        }

        /// <summary>
        /// Applies the light mode theme.
        /// </summary>
        public static void SetLightTheme()
        {
            Tuple<AppTheme, Accent> appStyle = MahApps.Metro.ThemeManager.DetectAppStyle(Application.Current);
            MahApps.Metro.ThemeManager.ChangeAppStyle(
                Application.Current,
                appStyle.Item2,
                MahApps.Metro.ThemeManager.GetAppTheme("BaseLight"));

            CurrentGroupMeTheme = GroupMeLightTheme;
        }

        /// <summary>
        /// Applies the dark mode theme.
        /// </summary>
        public static void SetDarkTheme()
        {
            Tuple<AppTheme, Accent> appStyle = MahApps.Metro.ThemeManager.DetectAppStyle(Application.Current);
            MahApps.Metro.ThemeManager.ChangeAppStyle(
                Application.Current,
                appStyle.Item2,
                MahApps.Metro.ThemeManager.GetAppTheme("BaseDark"));

            CurrentGroupMeTheme = GroupMeDarkTheme;
        }

        /// <summary>
        /// Applies the system prefered theme.
        /// </summary>
        public static void SetSystemTheme()
        {
            if (Native.WindowsUtils.IsAppLightThemePreferred())
            {
                SetLightTheme();
            }
            else
            {
                SetDarkTheme();
            }
        }
    }
}
