using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using GHIElectronics.Endpoint.Devices.Display;
using SkiaSharp;
using GHIElectronics.Endpoint.Core;
using GHIElectronics.Endpoint.Devices.Network;
using System.Net.NetworkInformation;
using GHIElectronics.Endpoint.Devices.Rtc;


var rtc = new RtcController();

rtc.DateTime = new DateTime(2024, 2, 28, 13, 17, 42);
//Initialize Display
var backlightPort = EPM815.Gpio.Pin.PD14 / 16;
var backlightPin = EPM815.Gpio.Pin.PD14 % 16;
var backlightDriver = new LibGpiodDriver((int)backlightPort);
var backlightController = new GpioController(PinNumberingScheme.Logical, backlightDriver);
backlightController.OpenPin(backlightPin);
backlightController.SetPinMode(backlightPin, PinMode.Output);
backlightController.Write(backlightPin, PinValue.High);
var screenWidth = 480;
var screenHeight = 272;

var configuration = new FBDisplay.Configuration()
{
    Clock = 10000,
    Width = 480,
    Hsync_start = 480 + 2,
    Hsync_end = 480 + 2 + 41,
    Htotal = 480 + 2 + 41 + 2,
    Height = 272,
    Vsync_start = 272 + 2,
    Vsync_end = 272 + 2 + 10,
    Vtotal = 272 + 2 + 10 + 2,
};
var fbDisplay = new FBDisplay(configuration);
var displayController = new DisplayController(fbDisplay);

//Initialize Network
bool NetworkReady = false;

var networkType = GHIElectronics.Endpoint.Devices.Network.NetworkInterfaceType.WiFi;

var networkSetting = new WiFiNetworkInterfaceSettings
{
    Ssid = "GHI",
    Password = "ghi555wifi.",
    DhcpEnable = true,
};
var network = new NetworkController(networkType, networkSetting);

network.NetworkLinkConnectedChanged += (a, b) =>
{
    if (b.Connected)
    {
        Console.WriteLine("Connected");
        NetworkReady = true;
    }
    else
    {
        Console.WriteLine("Disconnected");
    }
};

network.NetworkAddressChanged += (a, b) =>
{
    Console.WriteLine(string.Format("Address: {0}\n gateway: {1}\n DNS: {2}\n MAC: {3} ", b.Address, b.Gateway, b.Dns[0], b.MACAddress));
    NetworkReady = true;
};

network.Enable();

while (NetworkReady == false)
{
    Console.WriteLine("Waiting for connect");
    Thread.Sleep(250);
    
}


//SkiaSharp Initialization
SKBitmap bitmap = new SKBitmap(screenWidth, screenHeight, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
bitmap.Erase(SKColors.Transparent);
SKBitmap webBitmap;




//Initialize Screen
using (var screen = new SKCanvas(bitmap))
{
    
    HttpClient httpClient = new HttpClient();

    // Load web bitmap.
    string url = "https://www.ghielectronics.com/wp-content/uploads/2024/02/EndpointReleaseThumbnail.jpg";

    try
    {
        using (Stream stream = await httpClient.GetStreamAsync(url))
        using (MemoryStream memStream = new MemoryStream())
        {
            await stream.CopyToAsync(memStream);
            memStream.Seek(0, SeekOrigin.Begin);

            var info = new SKImageInfo(480, 272);
            webBitmap = SKBitmap.Decode(memStream,info);
            screen.DrawBitmap(webBitmap, 0, 0);

            var data = bitmap.Copy(SKColorType.Rgb565).Bytes;
            displayController.Flush(data);
            Thread.Sleep(1);
        };
    }
    catch
    {
    }
}
