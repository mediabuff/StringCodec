﻿using StringCodec.UWP.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace StringCodec.UWP.Pages
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class TextPage : Page
    {
        private TextCodecs.CODEC CURRENT_CODEC = TextCodecs.CODEC.URL;
        private Encoding CURRENT_ENC = Encoding.UTF8;
        private bool CURRENT_LINEBREAK = false;

        static private string text_src = string.Empty;
        static public string Text
        {
            get { return text_src; }
            set { text_src = value; }
        }

        public TextPage()
        {
            this.InitializeComponent();

            NavigationCacheMode = NavigationCacheMode.Enabled;

            optURL.IsChecked = true;
            optLangUTF8.IsChecked = true;
            optWrapText.IsChecked = false;

            optUUE.Visibility = Visibility.Collapsed;
            optUUE.IsEnabled = false;
            optXXE.Visibility = Visibility.Collapsed;
            optXXE.IsEnabled = false;
            optQuoted.Visibility = Visibility.Collapsed;
            optQuoted.IsEnabled = false;

            //if (!string.IsNullOrEmpty(text_src)) edSrc.Text = text_src;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //if (!string.IsNullOrEmpty(text_src)) edSrc.Text = text_src;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null)
            {
                if(e.Parameter is string)
                {
                    var data = e.Parameter;
                    edSrc.Text = data.ToString();
                    edDst.Text = TextCodecs.Encode(edSrc.Text, CURRENT_CODEC, CURRENT_LINEBREAK);
                }
            }
        }

        private void Codec_Click(object sender, RoutedEventArgs e)
        {
            if (sender == optLineBreak)
            {
                CURRENT_LINEBREAK = (sender as ToggleMenuFlyoutItem).IsChecked;
                return;
            }
            else if (sender == optWrapText)
            {
                if (optWrapText.IsChecked == true)
                {
                    edSrc.TextWrapping = TextWrapping.Wrap;
                    edDst.TextWrapping = TextWrapping.Wrap;
                }
                else
                {
                    edSrc.TextWrapping = TextWrapping.NoWrap;
                    edDst.TextWrapping = TextWrapping.NoWrap;
                }
                return;
            }

            ToggleMenuFlyoutItem[] opts = new ToggleMenuFlyoutItem[] { optBase64, optUUE, optXXE, optURL, optThunder, optRaw, optQuoted, optFlashGet };

            var btn = sender as ToggleMenuFlyoutItem;

            foreach (ToggleMenuFlyoutItem opt in opts)
            {
                if (string.Equals(opt.Name, btn.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    opt.IsChecked = true;
                }
                else opt.IsChecked = false;
            }
            CURRENT_CODEC = (TextCodecs.CODEC)Enum.Parse(typeof(TextCodecs.CODEC), btn.Name.ToUpper().Substring(3));
        }

        private void OptLang_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenuFlyoutItem[] opts = new ToggleMenuFlyoutItem[] {
                optLangAscii,
                optLang1250, optLang1251, optLang1253, optLang1254, optLang1255, optLang1256, optLang1257, optLang1258,
                optLangThai, optLangRussian,
                optLangGBK, optLangBIG5, optLangJIS, optLangKorean,
                optLangUnicode, optLangUTF8
            };

            var btn = sender as ToggleMenuFlyoutItem;
            foreach (ToggleMenuFlyoutItem opt in opts)
            {
                if (string.Equals(opt.Name, btn.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    opt.IsChecked = true;
                }
                else opt.IsChecked = false;
            }

            var ENC_NAME = btn.Name.Substring(7);
            CURRENT_ENC = TextCodecs.GetTextEncoder(ENC_NAME);
        }

        private void QRCode_Click(object sender, RoutedEventArgs e)
        {
            if(sender == btnSrcQRCode)
            {
                if (edSrc.Text.Length <= 0) return;
                Frame.Navigate(typeof(QRCodePage), edSrc.Text);
            }
            else if(sender == btnDstQRCode)
            {
                if (edDst.Text.Length <= 0) return;
                Frame.Navigate(typeof(QRCodePage), edDst.Text);
            }            
        }

        private void edSrc_TextChanged(object sender, TextChangedEventArgs e)
        {
            text_src = edSrc.Text;
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as AppBarButton;
            switch (btn.Name)
            {
                case "btnEncode":
                    edDst.Text = TextCodecs.Encode(edSrc.Text, CURRENT_CODEC, CURRENT_LINEBREAK);
                    text_src = edDst.Text;
                    break;
                case "btnDecode":
                    edDst.Text = TextCodecs.Decode(edSrc.Text, CURRENT_CODEC, CURRENT_ENC);
                    text_src = edDst.Text;
                    break;
                case "btnCopy":
                    Common.Utils.SetClipboard(edDst.Text);
                    break;
                case "btnPaste":
                    edSrc.Text = await Utils.GetClipboard(edSrc.Text);
                    break;
                case "btnSave":
                    await Utils.ShowSaveDialog(edDst.Text);
                    break;
                case "btnShare":
                    Utils.Share(edDst.Text);
                    break;
                default:
                    break;
            }
        }

        #region Drag/Drop routines
        private bool canDrop = true;
        private async void OnDragEnter(object sender, DragEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("drag enter.." + DateTime.Now);
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var deferral = e.GetDeferral(); // since the next line has 'await' we need to defer event processing while we wait
                try
                {
                    var items = await e.DataView.GetStorageItemsAsync();
                    if (items.Count > 0)
                    {
                        canDrop = false;
                        var item = items[0] as StorageFile;
                        string filename = item.Name;
                        string extension = item.FileType.ToLower();
#if DEBUG
                        System.Diagnostics.Debug.WriteLine(filename);
#endif
                        if (sender == edSrc)
                        {
                            if (Utils.text_ext.Contains(extension))
                            {
                                canDrop = true;
#if DEBUG
                                System.Diagnostics.Debug.WriteLine("Drag text to TextBox Control");
#endif
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
                deferral.Complete();
            }
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            if (sender == edSrc)
            {
                if (e.DataView.Contains(StandardDataFormats.StorageItems)){
                    if (canDrop) e.AcceptedOperation = DataPackageOperation.Copy;
                }
                else if (e.DataView.Contains(StandardDataFormats.Text) ||
                    e.DataView.Contains(StandardDataFormats.WebLink) ||
                    e.DataView.Contains(StandardDataFormats.Html) ||
                    e.DataView.Contains(StandardDataFormats.Rtf))
                {
                    e.AcceptedOperation = DataPackageOperation.Copy;
                }
            }
        }

        private async void OnDrop(object sender, DragEventArgs e)
        {
            // 记得获取Deferral对象
            //var def = e.GetDeferral();
            if (sender == edSrc)
            {
                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    var items = await e.DataView.GetStorageItemsAsync();
                    if (items.Count > 0)
                    {
                        var storageFile = items[0] as StorageFile;
                        if (Utils.url_ext.Contains(storageFile.FileType.ToLower()))
                        {
                            if (e.DataView.Contains(StandardDataFormats.WebLink))
                            {
                                var url = await e.DataView.GetWebLinkAsync();
                                if (url.IsUnc)
                                {
                                    edSrc.Text = url.ToString();
                                }
                                else if (url.IsFile)
                                {
                                    edSrc.Text = await FileIO.ReadTextAsync(storageFile);
                                }
                            }
                            else if (e.DataView.Contains(StandardDataFormats.Text))
                            {
                                //var content = await e.DataView.GetHtmlFormatAsync();
                                var content = await e.DataView.GetTextAsync();
                                if (content.Length > 0)
                                {
                                    edSrc.Text = content;
                                }
                            }
                        }
                        else if (Utils.text_ext.Contains(storageFile.FileType.ToLower()))
                            edSrc.Text = await FileIO.ReadTextAsync(storageFile);
                    }
                }
                else if (e.DataView.Contains(StandardDataFormats.Text) ||
                    e.DataView.Contains(StandardDataFormats.WebLink) ||
                    e.DataView.Contains(StandardDataFormats.Html) ||
                    e.DataView.Contains(StandardDataFormats.Rtf))
                {
                    var content = await e.DataView.GetTextAsync();
                    if (content.Length > 0)
                    {
                        edSrc.Text = content;
                    }
                }
            }
            //def.Complete();
        }
        #endregion
    }
}
