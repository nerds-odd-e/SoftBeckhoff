# Load the TwinCAT ADS assembly
#Add-Type -Path "Z:\opensource_workspace\SoftBeckhoff\SoftBeckhoff\bin\Debug\netcoreapp3.1\TwinCAT.Ads.dll"

# Create a new instance of the ADS client
$adsClient = New-Object TwinCAT.Ads.AdsClient

try {
    # Connect to the local ADS router
    $adsClient.Connect('127.0.0.1.1.1', 851)  # Adjust the AMS Net ID and port

    Write-Host "client timeout: $($adsClient.Timeout)"
    # Read device info
    $deviceInfo = $adsClient.ReadDeviceInfo()

    # Output the device information
    Write-Host "Device Name: $($deviceInfo.Name)"
    Write-Host "Version: $($deviceInfo.Version.Version)"
    Write-Host "Revision: $($deviceInfo.Version.Revision)"
    Write-Host "Build: $($deviceInfo.Version.Build)"
}
catch {
    Write-Error "Failed to connect or read from TwinCAT system: $_"
}
finally {
    $adsClient.Dispose()
}
