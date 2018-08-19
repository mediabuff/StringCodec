﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace StringCodec.UWP.Common
{
    class Utils
    {
        #region Clipboard Extentions
        static public void SetClipboard(Image image, int size)
        {
            SetClipboard(image, size, size);
        }

        static public async void SetClipboard(Image image, int width, int height)
        {
            if (image.Source == null) return;

            DataPackage dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;

            //Uri uri = new Uri("ms-appx:///Assets/ms.png");
            //StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            //dp.SetBitmap(RandomAccessStreamReference.CreateFromUri(uri));

            using (var fileStream = new InMemoryRandomAccessStream())
            {
                //把控件变成图像
                RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
                //传入参数Image控件
                await renderTargetBitmap.RenderAsync(image, width, height);
                var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();

                var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;
                var r_width = renderTargetBitmap.PixelWidth;
                var r_height = renderTargetBitmap.PixelHeight;
                if (width > 0 && height > 0)
                {
                    r_width = width;
                    r_height = height;
                }

                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);
                encoder.SetPixelData(
                    BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                    (uint)width, (uint)height,
                    dpi, dpi,
                    pixelBuffer.ToArray());
                await encoder.FlushAsync();

                dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromStream(fileStream));
                Clipboard.SetContent(dataPackage);
            }
        }

        static public async Task<string> GetClipboard(string text, Image image = null)
        {
            DataPackageView dataPackageView = Clipboard.GetContent();
            if (dataPackageView.Contains(StandardDataFormats.Text))
            {
                string content = await dataPackageView.GetTextAsync();
                // To output the text from this example, you need a TextBlock control
                return (content);
            }
            else if (dataPackageView.Contains(StandardDataFormats.Bitmap))
            {
                try
                {
                    var bmp = await dataPackageView.GetBitmapAsync();
                    WriteableBitmap bitmap = new WriteableBitmap(1, 1);
                    await bitmap.SetSourceAsync(await bmp.OpenReadAsync());

                    if (image != null)
                    {
                        image.Source = bitmap;
                        //text = await QRCodec.Decode(bitmap);
                    }
                }
                catch (Exception) { }
            }
            return (text);
        }


        static public void SetClipboard(string text)
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            dataPackage.SetText(text);
            Clipboard.SetContent(dataPackage);
        }

        static public async Task<string> GetClipboard(string text)
        {
            DataPackageView dataPackageView = Clipboard.GetContent();
            if (dataPackageView.Contains(StandardDataFormats.Text))
            {
                string content = await dataPackageView.GetTextAsync();
                // To output the text from this example, you need a TextBlock control
                return (content);
            }
            return (text);
        }
        #endregion

        #region ContentDialog Extentions
        static public async Task<Color> ShowColorDialog(Color color)
        {
            Color result = color;

            ColorDialog dlgColor = new ColorDialog() { Color = color, Alpha = false };
            ContentDialogResult ret = await dlgColor.ShowAsync();
            if (ret == ContentDialogResult.Primary)
            {
                result = dlgColor.Color;
            }

            return (result);
        }

        static public async Task<string> ShowSaveDialog(Image image, string prefix="")
        {
            var bmp = image.Source as WriteableBitmap;
            var width = bmp.PixelWidth;
            var height = bmp.PixelHeight;
            return (await ShowSaveDialog(image, width, height, prefix));
        }

        static public async Task<string> ShowSaveDialog(Image image, int size, string prefix="")
        {
            return (await ShowSaveDialog(image, size, size, prefix));
        }

        static public async Task<string> ShowSaveDialog(Image image, int width, int height, string prefix="")
        {
            if (image.Source == null) return (string.Empty);

            var now = DateTime.Now;
            FileSavePicker fp = new FileSavePicker();
            fp.SuggestedStartLocation = PickerLocationId.Desktop;
            fp.FileTypeChoices.Add("Image File", new List<string>() { ".png", ".jpg", ".jpeg", ".tif", ".tiff", ".gif", ".bmp" });
            if (!string.IsNullOrEmpty(prefix)) prefix = $"{prefix}_";
            fp.SuggestedFileName = $"{prefix}{now.ToString("yyyyMMddhhmmss")}.png";
            StorageFile TargetFile = await fp.PickSaveFileAsync();
            if (TargetFile != null)
            {
                StorageApplicationPermissions.MostRecentlyUsedList.Add(TargetFile, TargetFile.Name);
                if (StorageApplicationPermissions.FutureAccessList.Entries.Count >= 1000)
                    StorageApplicationPermissions.FutureAccessList.Remove(StorageApplicationPermissions.FutureAccessList.Entries.Last().Token);
                StorageApplicationPermissions.FutureAccessList.Add(TargetFile, TargetFile.Name);

                // 在用户完成更改并调用CompleteUpdatesAsync之前，阻止对文件的更新
                CachedFileManager.DeferUpdates(TargetFile);

                #region Save Image Control source data
                //using (var fileStream = await TargetFile.OpenAsync(FileAccessMode.ReadWrite))
                //{
                //    var bmp = imgQR.Source as WriteableBitmap;
                //    var w = bmp.PixelWidth;
                //    var h = bmp.PixelHeight;

                //    // set the source for WriteableBitmap  
                //    //await bmp.SetSourceAsync(fileStream);

                //    // Get pixels of the WriteableBitmap object 
                //    Stream pixelStream = bmp.PixelBuffer.AsStream();
                //    byte[] pixels = new byte[pixelStream.Length];
                //    await pixelStream.ReadAsync(pixels, 0, pixels.Length);

                //    var encId = BitmapEncoder.PngEncoderId;
                //    var fext = Path.GetExtension(TargetFile.Name).ToLower();
                //    switch (fext)
                //    {
                //        case ".bmp":
                //            encId = BitmapEncoder.BmpEncoderId;
                //            break;
                //        case ".gif":
                //            encId = BitmapEncoder.GifEncoderId;
                //            break;
                //        case ".png":
                //            encId = BitmapEncoder.PngEncoderId;
                //            break;
                //        case ".jpg":
                //            encId = BitmapEncoder.JpegEncoderId;
                //            break;
                //        case ".jpeg":
                //            encId = BitmapEncoder.JpegEncoderId;
                //            break;
                //        case ".tif":
                //            encId = BitmapEncoder.TiffEncoderId;
                //            break;
                //        case ".tiff":
                //            encId = BitmapEncoder.TiffEncoderId;
                //            break;
                //        default:
                //            encId = BitmapEncoder.PngEncoderId;
                //            break;
                //    }
                //    var encoder = await BitmapEncoder.CreateAsync(encId, fileStream);
                //    // Save the image file with jpg extension 
                //    encoder.SetPixelData(
                //        BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                //        //(uint)bmp.PixelWidth, (uint)bmp.PixelHeight, 
                //        (uint)size, (uint)size,
                //        96.0, 96.0, 
                //        pixels);
                //    await encoder.FlushAsync();
                //}
                //return (TargetFile.Name);
                #endregion

                #region Save Image Control Screen Display Data
                //把控件变成图像
                RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
                //传入参数Image控件
                await renderTargetBitmap.RenderAsync(image, width, height);
                var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();

                using (var fileStream = await TargetFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var r_width = renderTargetBitmap.PixelWidth;
                    var r_height = renderTargetBitmap.PixelHeight;
                    var dpi = DisplayInformation.GetForCurrentView().LogicalDpi;
                    if (width > 0 && height > 0)
                    {
                        r_width = width;
                        r_height = height;
                    }
                    var encId = BitmapEncoder.PngEncoderId;
                    var fext = Path.GetExtension(TargetFile.Name).ToLower();
                    switch (fext)
                    {
                        case ".bmp":
                            encId = BitmapEncoder.BmpEncoderId;
                            break;
                        case ".gif":
                            encId = BitmapEncoder.GifEncoderId;
                            break;
                        case ".png":
                            encId = BitmapEncoder.PngEncoderId;
                            break;
                        case ".jpg":
                            encId = BitmapEncoder.JpegEncoderId;
                            break;
                        case ".jpeg":
                            encId = BitmapEncoder.JpegEncoderId;
                            break;
                        case ".tif":
                            encId = BitmapEncoder.TiffEncoderId;
                            break;
                        case ".tiff":
                            encId = BitmapEncoder.TiffEncoderId;
                            break;
                        default:
                            encId = BitmapEncoder.PngEncoderId;
                            break;
                    }
                    var encoder = await BitmapEncoder.CreateAsync(encId, fileStream);
                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                        (uint)r_width, (uint)r_height, dpi, dpi,
                        pixelBuffer.ToArray()
                    );
                    //刷新图像
                    await encoder.FlushAsync();
                }
                return (TargetFile.Name);
                #endregion
            }
            return (string.Empty);
        }

        static public async Task<string> ShowSaveDialog(string content)
        {
            if (content.Length <= 0) return (string.Empty);

            var now = DateTime.Now;
            FileSavePicker fp = new FileSavePicker();
            fp.SuggestedStartLocation = PickerLocationId.Desktop;
            fp.FileTypeChoices.Add("Text File", new List<string>() { ".txt" });
            fp.SuggestedFileName = $"{now.ToString("yyyyMMddhhmmss")}.txt";
            StorageFile TargetFile = await fp.PickSaveFileAsync();
            if (TargetFile != null)
            {
                StorageApplicationPermissions.MostRecentlyUsedList.Add(TargetFile, TargetFile.Name);
                if (StorageApplicationPermissions.FutureAccessList.Entries.Count >= 1000)
                    StorageApplicationPermissions.FutureAccessList.Remove(StorageApplicationPermissions.FutureAccessList.Entries.Last().Token);
                StorageApplicationPermissions.FutureAccessList.Add(TargetFile, TargetFile.Name);

                // 在用户完成更改并调用CompleteUpdatesAsync之前，阻止对文件的更新
                CachedFileManager.DeferUpdates(TargetFile);
                await FileIO.WriteTextAsync(TargetFile, content);
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(TargetFile);

                return (TargetFile.Name);
            }
            return (string.Empty);
        }

        #endregion
    }
}
