﻿using System;
using Telegram.Td.Api;
using Unigram.Common;
using Unigram.Controls;
using Unigram.ViewModels.Delegates;
using Unigram.ViewModels.Settings;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace Unigram.Views.Settings
{
    public sealed partial class SettingsBlockedUsersPage : HostedPage, IFileDelegate
    {
        public SettingsBlockedUsersViewModel ViewModel => DataContext as SettingsBlockedUsersViewModel;

        public SettingsBlockedUsersPage()
        {
            InitializeComponent();
            DataContext = TLContainer.Current.Resolve<SettingsBlockedUsersViewModel, IFileDelegate>(this);
        }

        private async void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is User user)
            {
                var response = await ViewModel.ProtoService.SendAsync(new CreatePrivateChat(user.Id, false));
                if (response is Chat chat)
                {
                    ViewModel.NavigationService.Navigate(typeof(ProfilePage), chat.Id);
                }
            }
        }

        #region Recycle

        private void OnChoosingItemContainer(ListViewBase sender, ChoosingItemContainerEventArgs args)
        {
            if (args.ItemContainer == null)
            {
                args.ItemContainer = new ListViewItem();
                args.ItemContainer.Style = sender.ItemContainerStyle;
                args.ItemContainer.ContentTemplate = sender.ItemTemplate;
                args.ItemContainer.ContextRequested += User_ContextRequested;
            }

            args.IsContainerPrepared = true;
        }

        private void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            var content = args.ItemContainer.ContentTemplateRoot as Grid;
            var user = args.Item as User;

            content.Tag = user;

            if (args.Phase == 0)
            {
                var title = content.Children[1] as TextBlock;
                title.Text = user.GetFullName();
            }
            else if (args.Phase == 1)
            {
                var subtitle = content.Children[2] as TextBlock;
                subtitle.Text = string.IsNullOrEmpty(user.PhoneNumber) ? Strings.Resources.NumberUnknown : PhoneNumber.Format(user.PhoneNumber);
            }
            else if (args.Phase == 2)
            {
                var photo = content.Children[0] as ProfilePicture;
                photo.Source = PlaceholderHelper.GetUser(ViewModel.ProtoService, user, 36);
            }

            if (args.Phase < 2)
            {
                args.RegisterUpdateCallback(OnContainerContentChanging);
            }

            args.Handled = true;
        }

        #endregion

        private void User_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            var flyout = new MenuFlyout();

            var element = sender as FrameworkElement;
            var user = element.Tag as User;

            flyout.Items.Add(new MenuFlyoutItem { Text = Strings.Resources.Unblock, Command = ViewModel.UnblockCommand, CommandParameter = user });

            if (args.TryGetPosition(sender, out Point point))
            {
                if (point.X < 0 || point.Y < 0)
                {
                    point = new Point(Math.Max(point.X, 0), Math.Max(point.Y, 0));
                }

                flyout.ShowAt(sender, point);
            }
        }

        public void UpdateFile(Telegram.Td.Api.File file)
        {
            foreach (User user in ScrollingHost.Items)
            {
                if (user.UpdateFile(file))
                {
                    var container = ScrollingHost.ContainerFromItem(user) as SelectorItem;
                    if (container == null)
                    {
                        return;
                    }

                    var content = container.ContentTemplateRoot as Grid;

                    var photo = content.Children[0] as ProfilePicture;
                    photo.Source = PlaceholderHelper.GetUser(null, user, 36);
                }
            }
        }
    }
}
