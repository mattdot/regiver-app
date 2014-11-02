using Regiver.Common;
using Regiver.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Phone.UI.Input;
using Windows.Storage.Streams;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Ocr = WindowsPreview.Media.Ocr;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Regiver
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AddCardPage : Page, IDisposable
    {
        private MediaCapture mediaCapture;
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        private Ocr.OcrEngine engine;

        public AddCardPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            this.engine = new Ocr.OcrEngine(Ocr.OcrLanguage.English);
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            VisualStateManager.GoToState(this, "Scanning", false);

            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;

            this.navigationHelper.OnNavigatedTo(e);

            await StartPreviewAsync();
        }

        private async System.Threading.Tasks.Task StartPreviewAsync()
        {
            this.mediaCapture = new MediaCapture();

            this.mediaCapture.Failed += mediaCapture_Failed;

            System.Diagnostics.Debug.WriteLine("Getting video capture devices...");

            var rearCamera = (from item in await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(DeviceClass.VideoCapture)
                              where item.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Back
                              select item).FirstOrDefault();

            var settings = new MediaCaptureInitializationSettings
            {
                MediaCategory = MediaCategory.Other,
                PhotoCaptureSource = PhotoCaptureSource.VideoPreview,
                VideoDeviceId = rearCamera.Id
            };

            System.Diagnostics.Debug.WriteLine("Initializing media capture...");

            // this next line is consistently crashing my Nokia Lumia 920
            await this.mediaCapture.InitializeAsync(settings);

            var streamProperties = this.mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview);

            foreach (var item in streamProperties.Cast<VideoEncodingProperties>())
            {
                if (item.Width == PreviewBox.Width && item.Height == this.PreviewBox.Height)
                {
                    System.Diagnostics.Debug.WriteLine("Setting stream properties...");

                    await this.mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(
                        MediaStreamType.VideoPreview,
                        item);
                    break;
                }
            }

            //var photoStreamProperties = this.mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.Photo);

            //foreach (var item in streamProperties.OfType<VideoEncodingProperties>())
            //{
            //    System.Diagnostics.Debug.WriteLine(item.Subtype);

            //    if (item.Width == PreviewBox.Width && item.Height == this.PreviewBox.Height)
            //    {
            //        System.Diagnostics.Debug.WriteLine(item.Subtype);

            //        System.Diagnostics.Debug.WriteLine("Setting stream properties...");

            //        await this.mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(
            //            MediaStreamType.Photo,
            //            item);
            //        break;
            //    }
            //}

            
            var focusSettings = new FocusSettings();
            focusSettings.AutoFocusRange = AutoFocusRange.Macro;
            focusSettings.Mode = FocusMode.Auto;
            focusSettings.WaitForFocus = true;
            focusSettings.DisableDriverFallback = false;

            this.mediaCapture.VideoDeviceController.FocusControl.Configure(focusSettings);

            this.Preview.Source = this.mediaCapture;

            System.Diagnostics.Debug.WriteLine("Starting preview...");

            await this.mediaCapture.StartPreviewAsync();

            HardwareButtons.CameraPressed += HardwareButtons_CameraPressed;
            HardwareButtons.CameraHalfPressed += HardwareButtons_CameraHalfPressed;
        }

        void mediaCapture_Failed(MediaCapture sender, 
            MediaCaptureFailedEventArgs errorEventArgs)
        {
            System.Diagnostics.Debug.WriteLine("Error {0} capturing media: {1}", 
                errorEventArgs.Code,
                errorEventArgs.Message);
        }

        async void HardwareButtons_CameraHalfPressed(object sender, CameraEventArgs e)
        {
            if (this.mediaCapture.VideoDeviceController.FocusControl.Supported)
            {
                await this.mediaCapture.VideoDeviceController.FocusControl.FocusAsync();
            }
        }

        async void HardwareButtons_CameraPressed(object sender, CameraEventArgs e)
        {
            await TakePhotoAsync();
        }

        private async System.Threading.Tasks.Task TakePhotoAsync()
        {
            this.ProgressBar.Visibility = Visibility.Visible;

            this.ProgressBar.Value = 0;

            try
            {
                using (var stream = new InMemoryRandomAccessStream())
                {
                    var type = ImageEncodingProperties.CreateJpeg();

                    System.Diagnostics.Debug.WriteLine("Capturing photo...");

                    await this.mediaCapture.CapturePhotoToStreamAsync(type, stream);

                    this.ProgressBar.Value = 1;

                    System.Diagnostics.Debug.WriteLine("Creating decoder...");

                    var decoder = await BitmapDecoder.CreateAsync(stream);

                    this.ProgressBar.Value = 2;
                    var width = decoder.PixelWidth;
                    var height = decoder.PixelHeight;
                    
                    var scale = System.Convert.ToDouble(height) / this.PreviewBox.Height;

                    var x = (this.PreviewBox.Width - this.CaptureFrame.Width) / 2;
                    var y = (this.PreviewBox.Height - this.CaptureFrame.Height) / 2;

                    System.Diagnostics.Debug.WriteLine("Getting pixels...");

                    var transform = new BitmapTransform
                        {
                            Bounds = new BitmapBounds
                            {
                                X = System.Convert.ToUInt32(x),
                                Y = System.Convert.ToUInt32(y),
                                Width = System.Convert.ToUInt32(this.CaptureFrame.Width),
                                Height = System.Convert.ToUInt32(this.CaptureFrame.Height)
                            },
                        };
                    // Get pixels in BGRA format.
                    var pixels = await decoder.GetPixelDataAsync(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Straight,
                        transform,
                        ExifOrientationMode.RespectExifOrientation,
                        ColorManagementMode.ColorManageToSRgb);

                    var imageStream = new InMemoryRandomAccessStream();

                    var encoder = await BitmapEncoder.CreateForTranscodingAsync(imageStream, decoder);

                    encoder.BitmapTransform.Bounds = transform.Bounds;

                    await encoder.FlushAsync();

                    var image = new BitmapImage();

                    image.SetSource(imageStream);

                    this.CapturedImage.Source = image;

                    this.ProgressBar.Value = 3;

                    System.Diagnostics.Debug.WriteLine("Recognizing...");

                    var results = await this.engine.RecognizeAsync(
                        System.Convert.ToUInt32(this.CaptureFrame.Height), 
                        System.Convert.ToUInt32(this.CaptureFrame.Width), 
                        pixels.DetachPixelData());

                    this.ProgressBar.Value = 4;

                    if (results.Lines == null)
                    {
                        System.Diagnostics.Debug.WriteLine("No lines recognized");
                    }
                    else
                    {
                        foreach (var item in results.Lines)
                        {
                            try
                            {
                                var numberGroups = from word in item.Words
                                                   select int.Parse(word.Text);

                                if (numberGroups.Count() == 4)
                                {
                                    //this.CardNumber.Visibility = Windows.UI.Xaml.Visibility.Visible;

                                    this.CardNumber.Text = string.Format("{0} {1} {2} {3}",
                                        item.Words[0].Text,
                                        item.Words[1].Text,
                                        item.Words[2].Text,
                                        item.Words[3].Text);

                                    VisualStateManager.GoToState(this, "FoundCardNumber", false);

                                    //this.AcceptButton.Visibility = Windows.UI.Xaml.Visibility.Visible;

                                    var cardNumber = string.Format("{0}{1}{2}{3}",
                                        item.Words[0].Text,
                                        item.Words[1].Text,
                                        item.Words[2].Text,
                                        item.Words[3].Text);

                                    System.Diagnostics.Debug.WriteLine("Adding card...");

                                    var newCard = await DataModel.Current.AddCardAsync(cardNumber);

                                    this.ProgressBar.Value = 5;

                                    await Task.Delay(TimeSpan.FromSeconds(1.0));

                                    if (newCard != null)
                                    {
                                        System.Diagnostics.Debug.WriteLine("Navigating to card scanned page...");

                                        this.Frame.Navigate(typeof(CardScannedPage), newCard.Id);
                                    }
                                }
                            }
                            catch (System.Exception)
                            {
                                System.Diagnostics.Debug.WriteLine("words are not 4 number groups.");
                            }
                        }
                    }
                }
            }
            catch (System.Exception se)
            {
                System.Diagnostics.Debug.WriteLine("Error taking photo: {0}", se.Message);
            }
            finally
            {
                this.ProgressBar.Value = 0;

                this.ProgressBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        private void SetGroupPosition(Border rectangle, Ocr.OcrWord word)
        {
            rectangle.Visibility = Windows.UI.Xaml.Visibility.Visible;

            rectangle.Width = System.Convert.ToDouble(word.Width);
            rectangle.Height = System.Convert.ToDouble(word.Height);
            rectangle.RenderTransformOrigin = new Point(0, 0);
            rectangle.RenderTransform = new TranslateTransform
            {
                X = word.Left + 150,
                Y = word.Top + 195
            };
        }

        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;

            this.navigationHelper.OnNavigatedFrom(e);

            if (this.mediaCapture != null)
            {
                await this.mediaCapture.StopPreviewAsync();

                this.mediaCapture.Dispose();

                this.mediaCapture = null;
            }

            HardwareButtons.CameraPressed -= HardwareButtons_CameraPressed;
            HardwareButtons.CameraHalfPressed -= HardwareButtons_CameraHalfPressed;
        }

        #endregion

        public void Dispose()
        {
            if (this.mediaCapture != null)
            {
                this.mediaCapture.Dispose();
                this.mediaCapture = null;
            }
        }

        private async void ScanButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (this.mediaCapture.VideoDeviceController.FocusControl.Supported)
            {
                await this.mediaCapture.VideoDeviceController.FocusControl.FocusAsync();
            }

            await this.TakePhotoAsync();
        }

        private async void OnTappedViewbox(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (this.mediaCapture.VideoDeviceController.FocusControl.Supported)
            {
                await this.mediaCapture.VideoDeviceController.FocusControl.FocusAsync();
            }

            await TakePhotoAsync();
        }
    }
}
