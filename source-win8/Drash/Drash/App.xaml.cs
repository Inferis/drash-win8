﻿using System;
using System.Linq;
using Callisto.Controls;
using Drash.Models;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.System;
using Windows.UI;
using Windows.UI.ApplicationSettings;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=234227

namespace Drash
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        public Model Model { get; private set; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            // if the view model is not loaded, create a new one
            if (Model == null) {
                Model = new Model();
            }

            var rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null) {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                if (args.PreviousExecutionState == ApplicationExecutionState.Terminated) {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null) {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                if (!rootFrame.Navigate(typeof(DrashPage), args.Arguments)) {
                    throw new Exception("Failed to create initial page");
                }
                ((DrashPage)rootFrame.Content).Model = new ViewModel(Model);
            }
            // Ensure the current window is active
            Window.Current.Activate();

            RegisterBackgroundTask();
            RegisterAppSettings();
        }

        private void RegisterBackgroundTask()
        {
            var registered = BackgroundTaskRegistration.AllTasks.Values.Any(x => x.Name == "Drash.Tasks.BackgroundTask");
            if (registered)
                return;

            var builder = new BackgroundTaskBuilder {
                Name = "Drash.Tasks.BackgroundTask",
                TaskEntryPoint = "Drash.Tasks.BackgroundTask"
            };
            builder.SetTrigger(new TimeTrigger(15, false));
            builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));

            builder.Register();
        }

        private void RegisterAppSettings()
        {
            var drashBlue = new SolidColorBrush(Color.FromArgb(0xcc, 0x1e, 0x4c, 0x67));
            SettingsPane.GetForCurrentView().CommandsRequested += (sender, args) => {
                args.Request.ApplicationCommands.Add(new SettingsCommand("about", "About Drash", command => {
                    new SettingsFlyout {
                        FlyoutWidth = SettingsFlyout.SettingsFlyoutWidth.Narrow,
                        Background = drashBlue,
                        HeaderBrush = drashBlue,
                        ContentForegroundBrush = new SolidColorBrush(Colors.White),
                        ContentBackgroundBrush = drashBlue,
                        HeaderText = "About Drash",
                        Content = new AboutFlyout(),
                        IsOpen = true
                    };
                }));

                // privacy policy
                args.Request.ApplicationCommands.Add(new SettingsCommand("privacyPolicy", "Privacy Policy", command => {
                    new SettingsFlyout {
                        FlyoutWidth = SettingsFlyout.SettingsFlyoutWidth.Narrow,
                        Background = drashBlue,
                        HeaderBrush = drashBlue,
                        ContentForegroundBrush = new SolidColorBrush(Colors.White),
                        ContentBackgroundBrush = drashBlue,
                        HeaderText = "Privacy Policy",
                        Content = new PrivacyPolicyFlyout(),
                        IsOpen = true
                    };
                }));
            };
        }
        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
