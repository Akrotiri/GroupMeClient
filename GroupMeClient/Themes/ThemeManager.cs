﻿using System;
using System.Windows;
using GroupMeClient.Core.Settings;
using MahApps.Metro;

namespace GroupMeClient.WpfUI.Themes
{
    /// <summary>
    /// <see cref="ThemeManager"/> provides support for changing the GroupMe Desktop Client theme at runtime.
    /// </summary>
    public class ThemeManager
    {
        private static readonly ResourceDictionary GroupMeLightTheme = new ResourceDictionary()
        {
            Source = new Uri("pack://application:,,,/Styles/GroupMeLight.xaml"),
        };

        private static readonly ResourceDictionary GroupMeDarkTheme = new ResourceDictionary()
        {
            Source = new Uri("pack://application:,,,/Styles/GroupMeDark.xaml"),
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
        /// Updates the current theme of the application.
        /// </summary>
        /// <param name="theme">The new theme mode to apply.</param>
        public static void UpdateTheme(ThemeOptions theme)
        {
            switch (theme)
            {
                case ThemeOptions.Dark:
                    SetDarkTheme();
                    break;

                case ThemeOptions.Light:
                    SetLightTheme();
                    break;

                case ThemeOptions.Default:
                default:
                    SetSystemTheme();
                    break;
            }
        }

        /// <summary>
        /// Applies the light mode theme.
        /// </summary>
        private static void SetLightTheme()
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
        private static void SetDarkTheme()
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
        private static void SetSystemTheme()
        {
            if (Native.WindowsThemeUtils.IsAppLightThemePreferred())
            {
                SetLightTheme();
            }
            else
            {
                SetDarkTheme();
            }

            Native.WindowsThemeUtils.ThemeUpdateHook.Instance.ThemeChangedEvent -= Windows_ThemeChangedEvent;
            Native.WindowsThemeUtils.ThemeUpdateHook.Instance.ThemeChangedEvent += Windows_ThemeChangedEvent;
        }

        private static void Windows_ThemeChangedEvent()
        {
            SetSystemTheme();
        }
    }
}
